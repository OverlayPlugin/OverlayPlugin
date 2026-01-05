using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using RainbowMage.OverlayPlugin.NetworkProcessors.PacketHelper;
using static RainbowMage.OverlayPlugin.NetworkProcessors.NetworkParser;

namespace RainbowMage.OverlayPlugin.NetworkProcessors
{
    public class FateWatcher
    {
        private ILogger logger;

        private MachinaRegionalizedPacketHelper<ActorControlPacket> actorControlSelfPacketHelper;
        private RegionalizedPacketHelper<
            Server_MessageHeader_Global, LineCEDirector.CEDirector_v62,
            Server_MessageHeader_CN, LineCEDirector.CEDirector_v62,
            Server_MessageHeader_KR, LineCEDirector.CEDirector_v62,
            Server_MessageHeader_TC, LineCEDirector.CEDirector_v62> cePacketHelper;

        private static FFXIVRepository ffxiv;
        private GameRegion? currentRegion;

        private const string actorControlSelfMachinaPacketName = "ActorControlSelf";

        // Fate start
        // param1: fateID
        // param2: unknown
        //
        // Fate end
        // param1: fateID
        //
        // Fate update
        // param1: fateID
        // param2: progress (0-100)
        private struct ActorControlSelfFateUpdateOpcodes
        {
            public ActorControlSelfFateUpdateOpcodes(int add_, int remove_, int update_) { this.add = add_; this.remove = remove_; this.update = update_; }
            public readonly int add;
            public readonly int remove;
            public readonly int update;
        };
        private static readonly ActorControlSelfFateUpdateOpcodes acFateUpdate_v5_2 = new ActorControlSelfFateUpdateOpcodes(
          0x935,
          0x936,
          0x93E
        );

        [Serializable]
        [StructLayout(LayoutKind.Explicit)]
        public struct CEDirectorData
        {

            [FieldOffset(0x20)]
            public uint popTime;
            [FieldOffset(0x24)]
            public ushort timeRemaining;
            [FieldOffset(0x28)]
            public byte ceKey;
            [FieldOffset(0x29)]
            public byte numPlayers;
            [FieldOffset(0x2A)]
            public byte status;
            [FieldOffset(0x2C)]
            public byte progress;
        };

        private static SemaphoreSlim fateSemaphore;
        private static SemaphoreSlim ceSemaphore;
        private Dictionary<GameRegion, ActorControlSelfFateUpdateOpcodes> fateControlOpcodes
            = new Dictionary<GameRegion, ActorControlSelfFateUpdateOpcodes>() {
                { GameRegion.Korean, acFateUpdate_v5_2 },
                { GameRegion.Chinese, acFateUpdate_v5_2 },
                { GameRegion.Tc, acFateUpdate_v5_2 },
                { GameRegion.Global, acFateUpdate_v5_2 },
        };

        // fates<fateID, progress>
        private static Dictionary<int, int> fates;
        private static Dictionary<int, CEDirectorData> ces;

        public event EventHandler<FateChangedArgs> OnFateChanged;

        public FateWatcher(TinyIoCContainer container)
        {
            logger = container.Resolve<ILogger>();
            ffxiv = ffxiv ?? container.Resolve<FFXIVRepository>();
            if (!ffxiv.IsFFXIVPluginPresent())
                return;

            var opcodeConfig = container.Resolve<OverlayPluginLogLineConfig>();

            cePacketHelper = RegionalizedPacketHelper<
                Server_MessageHeader_Global, LineCEDirector.CEDirector_v62,
                Server_MessageHeader_CN, LineCEDirector.CEDirector_v62,
                Server_MessageHeader_KR, LineCEDirector.CEDirector_v62,
                Server_MessageHeader_TC, LineCEDirector.CEDirector_v62>.CreateFromOpcodeConfig(opcodeConfig, LineCEDirector.MachinaPacketName);

            ffxiv.RegisterNetworkParser(Parse);
            ffxiv.RegisterProcessChangedHandler(ProcessChanged);

            if (!MachinaRegionalizedPacketHelper<ActorControlPacket>.Create(actorControlSelfMachinaPacketName, out actorControlSelfPacketHelper))
            {
                logger.Log(LogLevel.Error, $"Failed to initialize NetworkParser: Failed to create {actorControlSelfMachinaPacketName} packet helper from Machina structs");
                return;
            }

            fateSemaphore = new SemaphoreSlim(0, 1);
            ceSemaphore = new SemaphoreSlim(0, 1);

            fates = new Dictionary<int, int>();
            ces = new Dictionary<int, CEDirectorData>();
        }

        private void ProcessChanged(Process process)
        {
            if (!ffxiv.IsFFXIVPluginPresent())
                return;

            currentRegion = null;
        }

        private unsafe void Parse(string id, long epoch, byte[] message)
        {
            if (actorControlSelfPacketHelper == null)
                return;

            if (currentRegion == null)
                currentRegion = ffxiv.GetMachinaRegion();

            if (currentRegion == null)
                return;

            MachinaPacketHelper<ActorControlPacket> acHelper = (MachinaPacketHelper<ActorControlPacket>)actorControlSelfPacketHelper[currentRegion.Value];

            if (acHelper.ToStructs(message, out var acHeader, out var acPacket))
            {
                var category = acPacket.Get<ushort>("category");
                var param1 = acPacket.Get<UInt32>("param1");
                var param2 = acPacket.Get<UInt32>("param2");

                ProcessFateUpdatePacket(currentRegion.Value, category, param1, param2);
            }
            else
            {
                dynamic ceHelper = cePacketHelper[currentRegion.Value];
                if (ceHelper.ToStructs(message, out dynamic header, out dynamic packet))
                {
                    CEDirectorData data = new CEDirectorData();
                    data.popTime = packet.popTime;
                    data.timeRemaining = packet.timeRemaining;
                    data.ceKey = packet.ceKey;
                    data.numPlayers = packet.numPlayers;
                    data.status = packet.status;
                    data.progress = packet.progress;

                    ProcessCEDirector(data);
                }
            }
        }

        public unsafe void ProcessFateUpdatePacket(GameRegion region, ushort opcode, uint param1, uint param2)
        {
            fateSemaphore.WaitAsync().ContinueWith(_ =>
            {
                try
                {
                    if (opcode == fateControlOpcodes[region].add)
                    {
                        AddFate((int)param1);
                    }
                    else if (opcode == fateControlOpcodes[region].remove)
                    {
                        RemoveFate((int)param1);
                    }
                    else if (opcode == fateControlOpcodes[region].update)
                    {
                        if (!fates.ContainsKey((int)param1))
                        {
                            AddFate((int)param1);
                        }
                        if (fates[(int)param1] != (int)param2)
                        {
                            UpdateFate((int)param1, (int)param2);
                        }
                    }
                }
                finally
                {
                    fateSemaphore.Release();
                }
            });
        }

        public unsafe void ProcessCEDirector(CEDirectorData data)
        {
            ceSemaphore.WaitAsync().ContinueWith(_ =>
            {
                try
                {
                    if (data.status != 0 && !ces.ContainsKey(data.ceKey))
                    {
                        AddCE(data);
                        return;
                    }
                    else
                    {

                        // Don't update if key is about to be removed
                        if (!ces[data.ceKey].Equals(data) &&
                          data.status != 0)
                        {
                            UpdateCE(data.ceKey, data);
                            return;
                        }

                        // Needs removing
                        if (data.status == 0)
                        {
                            RemoveCE(data);
                            return;
                        }
                    }
                }
                finally
                {
                    ceSemaphore.Release();
                }
            });
        }

        private void AddCE(CEDirectorData data)
        {
            ces.Add(data.ceKey, data);
            // TODO
            // client_.DoCEEvent(new JSEvents.CEEvent("add", JObject.FromObject(data)));
        }

        private void RemoveCE(CEDirectorData data)
        {
            if (ces.ContainsKey(data.ceKey))
            {
                // TODO
                // client_.DoCEEvent(new JSEvents.CEEvent("remove", JObject.FromObject(data)));
                ces.Remove(data.ceKey);
            }
        }
        private void UpdateCE(byte ceKey, CEDirectorData data)
        {
            ces[data.ceKey] = data;
            // TODO
            // client_.DoCEEvent(new JSEvents.CEEvent("update", JObject.FromObject(data)));
        }

        public void RemoveAndClearCEs()
        {
            foreach (int ceKey in ces.Keys)
            {
                // TODO
                // client_.DoCEEvent(new JSEvents.CEEvent("remove", JObject.FromObject(ces[ceKey])));
            }
            ces.Clear();
        }

        private void AddFate(int fateID)
        {
            if (!fates.ContainsKey(fateID))
            {
                fates[fateID] = 0;
                OnFateChanged(null, new FateChangedArgs("add", fateID, 0));
            }
        }

        private void RemoveFate(int fateID)
        {
            if (fates.ContainsKey(fateID))
            {
                OnFateChanged(null, new FateChangedArgs("remove", fateID, fates[fateID]));
                fates.Remove(fateID);
            }
        }

        private void UpdateFate(int fateID, int progress)
        {
            fates[fateID] = progress;
            OnFateChanged(null, new FateChangedArgs("update", fateID, progress));
        }

        public void RemoveAndClearFates()
        {
            foreach (int fateID in fates.Keys)
            {
                OnFateChanged(null, new FateChangedArgs("remove", fateID, fates[fateID]));
            }
            fates.Clear();
        }

        public class FateChangedArgs : EventArgs
        {
            public string eventType { get; private set; }
            public int fateID { get; private set; }
            public int progress { get; private set; }

            public FateChangedArgs(string eventType, int fateID, int progress) : base()
            {
                this.eventType = eventType;
                this.fateID = fateID;
                this.progress = progress;
            }
        }
    }
}
