﻿using System;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using RainbowMage.OverlayPlugin.MemoryProcessors;
using RainbowMage.OverlayPlugin.NetworkProcessors.PacketHelper;

namespace RainbowMage.OverlayPlugin.NetworkProcessors
{
    using RPH = RegionalizedPacketHelper<
            Server_MessageHeader_Global, LineActorMove.ActorMove_v655,
            Server_MessageHeader_CN, LineActorMove.ActorMove_v655,
            Server_MessageHeader_KR, LineActorMove.ActorMove_v655>;

    public class LineActorMove
    {
        [StructLayout(LayoutKind.Explicit, Size = structSize, Pack = 1)]
        internal unsafe struct ActorMove_v655 : IPacketStruct
        {
            // 6.5.5 packet data (minus header):
            // 1897 3004 3C00 B681 AA80 9F83 00000000
            // AAAA BBBB CCCC DDDD EEEE FFFF GGGGGGGG
            // 0x0  0x2  0x4  0x6  0x8  0xA  0xC
            // Rot  Unk  Unk  X    Y    Z    Unk

            // Have never seen data in 0xC, probably padding?

            public const int structSize = 16;

            [FieldOffset(0x0)]
            public ushort rotation;

            [FieldOffset(0x2)]
            public ushort unknown1;
            [FieldOffset(0x4)]
            public ushort unknown2;

            [FieldOffset(0x6)]
            public ushort x;
            [FieldOffset(0x8)]
            public ushort y;
            [FieldOffset(0xA)]
            public ushort z;

            public string ToString(long epoch, uint ActorID)
            {
                // Only emit for non-player actors
                if (ActorID < 0x40000000)
                {
                    return null;
                }

                return $"{ActorID:X8}|" +
                    $"{FFXIVRepository.ConvertHeading(rotation):F4}|" +
                    $"{unknown1:X4}|" +
                    $"{unknown2:X4}|" +
                    $"{FFXIVRepository.ConvertUInt16Coordinate(x):F4}|" +
                    // y and z are intentionally flipped to match other log lines
                    $"{FFXIVRepository.ConvertUInt16Coordinate(z):F4}|" +
                    $"{FFXIVRepository.ConvertUInt16Coordinate(y):F4}";
            }
        }

        public const uint LogFileLineID = 270;

        private readonly FFXIVRepository ffxiv;

        private Func<string, DateTime, bool> logWriter;
        private RPH packetHelper;
        private GameRegion? currentRegion;

        public LineActorMove(TinyIoCContainer container)
        {
            ffxiv = container.Resolve<FFXIVRepository>();
            ffxiv.RegisterNetworkParser(MessageReceived);
            ffxiv.RegisterProcessChangedHandler(ProcessChanged);

            var opcodeConfig = container.Resolve<OverlayPluginLogLineConfig>();

            packetHelper = RPH.CreateFromOpcodeConfig(opcodeConfig, "ActorMove");

            if (packetHelper == null)
            {
                var logger = container.Resolve<ILogger>();
                logger.Log(LogLevel.Error, "Failed to initialize LineActorMove: Failed to create ActorMove packet helper from opcode configs and native structs");
                return;
            }

            var customLogLines = container.Resolve<FFXIVCustomLogLines>();
            this.logWriter = customLogLines.RegisterCustomLogLine(new LogLineRegistryEntry()
            {
                Name = "ActorMove",
                Source = "OverlayPlugin",
                ID = LogFileLineID,
                Version = 1,
            });
        }

        private void ProcessChanged(Process process)
        {
            if (!ffxiv.IsFFXIVPluginPresent())
                return;

            currentRegion = null;
        }

        private unsafe void MessageReceived(string id, long epoch, byte[] message)
        {
            if (packetHelper == null)
                return;

            if (currentRegion == null)
                currentRegion = ffxiv.GetMachinaRegion();

            if (currentRegion == null)
                return;

            var line = packetHelper[currentRegion.Value].ToString(epoch, message);

            if (line != null)
            {
                DateTime serverTime = ffxiv.EpochToDateTime(epoch);
                logWriter(line, serverTime);
            }
        }
    }
}
