using System;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using RainbowMage.OverlayPlugin.NetworkProcessors.PacketHelper;

namespace RainbowMage.OverlayPlugin.NetworkProcessors
{
    class NetworkParser
    {
        public event EventHandler<OnlineStatusChangedArgs> OnOnlineStatusChanged;

        public class ActorControlPacket : MachinaPacketWrapper
        {
            public override string ToString(long epoch, uint ActorID)
            {
                return "";
            }
        }

        private MachinaRegionalizedPacketHelper<ActorControlPacket> actorControlPacketHelper;

        private static FFXIVRepository ffxiv;
        private GameRegion? currentRegion;

        private const string machinaPacketName = "ActorControl";

        public NetworkParser(TinyIoCContainer container)
        {
            var logger = container.Resolve<ILogger>();

            ffxiv = ffxiv ?? container.Resolve<FFXIVRepository>();
            ffxiv.RegisterNetworkParser(Parse);
            ffxiv.RegisterProcessChangedHandler(ProcessChanged);

            if (!MachinaRegionalizedPacketHelper<ActorControlPacket>.Create(machinaPacketName, out actorControlPacketHelper))
            {
                logger.Log(LogLevel.Error, $"Failed to initialize NetworkParser: Failed to create {machinaPacketName} packet helper from Machina structs");
            }
        }

        private void ProcessChanged(Process process)
        {
            if (!ffxiv.IsFFXIVPluginPresent())
                return;

            currentRegion = null;
        }

        private unsafe void Parse(string id, long epoch, byte[] message)
        {
            if (actorControlPacketHelper == null)
                return;

            if (currentRegion == null)
                currentRegion = ffxiv.GetMachinaRegion();

            if (currentRegion == null)
                return;

            MachinaPacketHelper<ActorControlPacket> helper = (MachinaPacketHelper<ActorControlPacket>)actorControlPacketHelper[currentRegion.Value];

            if (helper.ToStructs(message, out var header, out var packet))
            {
                var category = packet.Get<Server_ActorControlCategory>("category");
                if (category != Server_ActorControlCategory.StatusUpdate) return;

                var actorID = header.ActorID;
                var param1 = packet.Get<UInt32>("param1");

                OnOnlineStatusChanged?.Invoke(null, new OnlineStatusChangedArgs(actorID, param1));
            }
        }
    }

    public class OnlineStatusChangedArgs : EventArgs
    {
        public uint Target { get; private set; }
        public uint Status { get; private set; }

        public OnlineStatusChangedArgs(uint target, uint status)
        {
            this.Target = target;
            this.Status = status;
        }
    }
}
