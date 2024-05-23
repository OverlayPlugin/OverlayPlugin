﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Runtime.InteropServices;
using RainbowMage.OverlayPlugin.NetworkProcessors.PacketHelper;

namespace RainbowMage.OverlayPlugin.NetworkProcessors
{
    /**
     * Class for position/angle info from ActionEffect (i.e. line 21/22).
     */
    public class LineAbilityExtra
    {
        public const uint LogFileLineID = 264;
        private readonly FFXIVRepository ffxiv;

        private class AbilityExtraPacket<T> : MachinaPacketWrapper
            where T : unmanaged, IActionEffectExtra
        {
            public unsafe override string ToString(long epoch, uint ActorID)
            {
                var packetPtr = Marshal.AllocHGlobal(Marshal.SizeOf(packetValue));
                T rawPacket = *(T*)packetPtr.ToPointer();

                MachinaPacketHelper<DummyActionEffectHeaderPacket> packetHelper = (MachinaPacketHelper<DummyActionEffectHeaderPacket>)aeHelper[currentRegion.Value];

                packetHelper.ToStructs(packetPtr, out var _, out var aeHeader);

                // Ability ID is really 16-bit, so it is formatted as such, but we will get an
                // exception if we try to prematurely cast it to UInt16
                var abilityId = aeHeader.Get<uint>("actionId");
                var globalEffectCounter = aeHeader.Get<uint>("globalEffectCounter");

                if (rawPacket.actionEffectCount == 1)
                {
                    // AE1 is not useful. It does not contain this data. But we still need to write something
                    // to indicate that a proper line will not be happening.
                    return string.Format(CultureInfo.InvariantCulture,
                        "{0:X8}|{1:X4}|{2:X8}|{3}||||",
                        ActorID, abilityId, globalEffectCounter, (int)LineSubType.NO_DATA);
                }

                float x = FFXIVRepository.ConvertUInt16Coordinate(rawPacket.x);
                float y = FFXIVRepository.ConvertUInt16Coordinate(rawPacket.y);
                float z = FFXIVRepository.ConvertUInt16Coordinate(rawPacket.z);

                var h = FFXIVRepository.ConvertHeading(aeHeader.Get<ushort>("rotation"));
                return string.Format(CultureInfo.InvariantCulture,
                    "{0:X8}|{1:X4}|{2:X8}|{3}|{4:F3}|{5:F3}|{6:F3}|{7:F3}",
                    ActorID, abilityId, globalEffectCounter, (int)LineSubType.DATA_PRESENT, x, y, z, h);
            }
        }

        // This just exists so that we can leverage the regionalization for ActorControlSelfExtraPacket
        private class DummyActionEffectHeaderPacket : MachinaPacketWrapper
        {
            public override string ToString(long epoch, uint ActorID)
            {
                return "";
            }
        }

        private MachinaRegionalizedPacketHelper<AbilityExtraPacket<Server_ActionEffect1_Extra>> packetHelper_1;
        private MachinaRegionalizedPacketHelper<AbilityExtraPacket<Server_ActionEffect8_Extra>> packetHelper_8;
        private MachinaRegionalizedPacketHelper<AbilityExtraPacket<Server_ActionEffect16_Extra>> packetHelper_16;
        private MachinaRegionalizedPacketHelper<AbilityExtraPacket<Server_ActionEffect24_Extra>> packetHelper_24;
        private MachinaRegionalizedPacketHelper<AbilityExtraPacket<Server_ActionEffect32_Extra>> packetHelper_32;
        private static MachinaRegionalizedPacketHelper<DummyActionEffectHeaderPacket> aeHelper;
        private static GameRegion? currentRegion;

        private readonly Func<string, DateTime, bool> logWriter;
        private readonly NetworkParser netHelper;

        interface IActionEffectExtra
        {
            uint actionEffectCount { get; }
            ushort x { get; }
            ushort y { get; }
            ushort z { get; }
        }

        [StructLayout(LayoutKind.Explicit, Pack = 1)]
        private struct Server_ActionEffect1_Extra : IActionEffectExtra
        {
            public uint actionEffectCount => 1;

            public ushort x => 0;
            public ushort y => 0;
            public ushort z => 0;
        }

        [StructLayout(LayoutKind.Explicit, Pack = 1)]
        private struct Server_ActionEffect8_Extra : IActionEffectExtra
        {
            [FieldOffset(0x290)]
            private ushort _x;

            [FieldOffset(0x292)]
            private ushort _z;

            [FieldOffset(0x294)]
            private ushort _y;

            public uint actionEffectCount => 8;

            public ushort x => _x;
            public ushort y => _y;
            public ushort z => _z;
        }

        [StructLayout(LayoutKind.Explicit, Pack = 1)]
        private struct Server_ActionEffect16_Extra : IActionEffectExtra
        {
            [FieldOffset(0x4D0)]
            public ushort _x;

            [FieldOffset(0x4D2)]
            public ushort _z;

            [FieldOffset(0x4D4)]
            public ushort _y;

            public uint actionEffectCount => 16;

            public ushort x => _x;
            public ushort y => _y;
            public ushort z => _z;
        }

        [StructLayout(LayoutKind.Explicit, Pack = 1)]
        private struct Server_ActionEffect24_Extra : IActionEffectExtra
        {
            [FieldOffset(0x710)]
            public ushort _x;

            [FieldOffset(0x712)]
            public ushort _z;

            [FieldOffset(0x714)]
            public ushort _y;

            public uint actionEffectCount => 24;

            public ushort x => _x;
            public ushort y => _y;
            public ushort z => _z;
        }

        [StructLayout(LayoutKind.Explicit, Pack = 1)]
        private struct Server_ActionEffect32_Extra : IActionEffectExtra
        {
            [FieldOffset(0x950)]
            public ushort _x;

            [FieldOffset(0x952)]
            public ushort _z;

            [FieldOffset(0x954)]
            public ushort _y;

            public uint actionEffectCount => 32;

            public ushort x => _x;
            public ushort y => _y;
            public ushort z => _z;
        }

        // For possible future expansion.
        private enum LineSubType
        {
            NO_DATA = 0,
            DATA_PRESENT = 1,
            ERROR = 256
        }

        public LineAbilityExtra(TinyIoCContainer container)
        {
            ffxiv = container.Resolve<FFXIVRepository>();
            if (!ffxiv.IsFFXIVPluginPresent())
                return;
            netHelper = container.Resolve<NetworkParser>();
            ffxiv.RegisterNetworkParser(MessageReceived);
            ffxiv.RegisterProcessChangedHandler(ProcessChanged);

            var customLogLines = container.Resolve<FFXIVCustomLogLines>();
            logWriter = customLogLines.RegisterCustomLogLine(new LogLineRegistryEntry()
            {
                Name = "AbilityExtra",
                Source = "OverlayPlugin",
                ID = LogFileLineID,
                Version = 1,
            });
        }

        private void ProcessChanged(Process process)
        {
            if (!ffxiv.IsFFXIVPluginPresent())
                return;

            try
            {
                currentRegion = null;
            }
            catch
            {
            }
        }

        private unsafe void MessageReceived(string id, long epoch, byte[] message)
        {
            if (packetHelper_32 == null)
                return;

            if (currentRegion == null)
                currentRegion = ffxiv.GetMachinaRegion();

            if (currentRegion == null)
                return;

            var line = packetHelper_1[currentRegion.Value].ToString(epoch, message);

            if (line == null)
            {
                line = packetHelper_8[currentRegion.Value].ToString(epoch, message);
            }

            if (line == null)
            {
                line = packetHelper_16[currentRegion.Value].ToString(epoch, message);
            }

            if (line == null)
            {
                line = packetHelper_24[currentRegion.Value].ToString(epoch, message);
            }

            if (line == null)
            {
                line = packetHelper_32[currentRegion.Value].ToString(epoch, message);
            }

            if (line != null)
            {
                DateTime serverTime = ffxiv.EpochToDateTime(epoch);
                logWriter(line, serverTime);

                return;
            }
        }
    }
}