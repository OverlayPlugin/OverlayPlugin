using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;

namespace RainbowMage.OverlayPlugin.NetworkProcessors
{
    public class LineFateControl
    {
        public const uint LogFileLineID = 258;
        private ILogger logger;
        private IOpcodeConfigEntry opcode = null;
        private readonly int offsetMessageType;
        private readonly int offsetPacketData;
        private readonly FFXIVRepository ffxiv;
        // fates<fateID, progress>
        private static Dictionary<uint, uint> fates = new Dictionary<uint, uint>();

        private const ushort FateAddCategory = 0x935;
        private const ushort FateRemoveCategory = 0x936;
        private const ushort FateUpdateCategory = 0x93E;
        private static readonly Dictionary<uint, string> FateCategories = new Dictionary<uint, string>
        {
            [FateAddCategory] = "Add",
            [FateRemoveCategory] = "Remove",
            [FateUpdateCategory] = "Update",
        };

        private Func<string, DateTime, bool> logWriter;

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct ActorControlSelf_v62
        {
            public ushort category;
            public ushort padding;
            public uint fateID;
            public uint progress;
            public uint param3;
            public uint param4;
            public uint param5;
            public uint param6;
            public uint padding1;

            public override string ToString()
            {
                return $"{category:X4}|{padding:X4}|{fateID:X8}|{progress:X8}|{param3:X8}|{param4:X8}|{param5:X8}|{param6:X8}|{padding1:X8}";
            }
        }

        public LineFateControl(TinyIoCContainer container)
        {
            logger = container.Resolve<ILogger>();
            ffxiv = container.Resolve<FFXIVRepository>();
            var netHelper = container.Resolve<NetworkParser>();
            if (!ffxiv.IsFFXIVPluginPresent())
                return;
            var customLogLines = container.Resolve<FFXIVCustomLogLines>();
            this.logWriter = customLogLines.RegisterCustomLogLine(new LogLineRegistryEntry()
            {
                Name = "FateDirector",
                Source = "OverlayPlugin",
                ID = LogFileLineID,
                Version = 1,
            });
            try
            {
                var mach = Assembly.Load("Machina.FFXIV");
                var msgHeaderType = mach.GetType("Machina.FFXIV.Headers.Server_MessageHeader");
                offsetMessageType = netHelper.GetOffset(msgHeaderType, "MessageType");
                offsetPacketData = Marshal.SizeOf(msgHeaderType);
                var packetType = mach.GetType("Machina.FFXIV.Headers.Server_ActorControlSelf");
                opcode = new OpcodeConfigEntry()
                {
                    opcode = netHelper.GetOpcode("ActorControlSelf"),
                    size = (uint)Marshal.SizeOf(typeof(ActorControlSelf_v62)),
                };
                ffxiv.RegisterNetworkParser(MessageReceived);
            }
            catch (System.IO.FileNotFoundException)
            {
                logger.Log(LogLevel.Error, Resources.NetworkParserNoFfxiv);
            }
            catch (Exception e)
            {
                logger.Log(LogLevel.Error, Resources.NetworkParserInitException, e);
            }

            ffxiv.RegisterZoneChangeDelegate((zoneID, zoneName) => fates.Clear());
        }

        private unsafe void MessageReceived(string id, long epoch, byte[] message)
        {
            if (opcode == null)
            {
                return;
            }

            if (message.Length < opcode.size + offsetPacketData)
            {
                return;
            }

            fixed (byte* buffer = message)
            {
                if (*(ushort*)&buffer[offsetMessageType] != opcode.opcode)
                {
                    return;
                }

                ActorControlSelf_v62 mapEffectPacket = *(ActorControlSelf_v62*)&buffer[offsetPacketData];
                string categoryStr;
                if (!FateCategories.TryGetValue(mapEffectPacket.category, out categoryStr))
                {
                    return;
                }

                var category = mapEffectPacket.category;
                var fateID = mapEffectPacket.fateID;
                var progress = mapEffectPacket.progress;

                // Do some basic filtering on fate data to avoid spamming the log needlessly.
                if (category == FateAddCategory)
                {
                    if (fates.ContainsKey(fateID))
                    {
                        return;
                    }
                    fates.Add(fateID, 0);
                }
                else if (category == FateRemoveCategory)
                {
                    if (!fates.Remove(fateID))
                    {
                        return;
                    }
                }
                else if (category == FateUpdateCategory)
                {
                    uint oldProgress;
                    if (fates.TryGetValue(fateID, out oldProgress))
                    {
                        if (progress == oldProgress)
                        {
                            return;
                        }
                    }
                    fates[fateID] = progress;
                }

                DateTime serverTime = ffxiv.EpochToDateTime(epoch);
                logWriter(
                    $"{categoryStr}|" +
                    $"{mapEffectPacket.padding:X4}|" +
                    $"{mapEffectPacket.fateID:X8}|" +
                    $"{mapEffectPacket.progress:X8}|" +
                    $"{mapEffectPacket.param3:X8}|" +
                    $"{mapEffectPacket.param4:X8}|" +
                    $"{mapEffectPacket.param5:X8}|" +
                    $"{mapEffectPacket.param6:X8}|" +
                    $"{mapEffectPacket.padding1:X8}",
                    serverTime
                );
            }
        }

    }
}
