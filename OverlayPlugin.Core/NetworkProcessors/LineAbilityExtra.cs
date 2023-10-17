using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Runtime.InteropServices;

namespace RainbowMage.OverlayPlugin.NetworkProcessors
{
    /**
     * Class for position/angle info from ActionEffect (i.e. line 21/22).
     */
    public class LineAbilityExtra
    {
        public const uint LogFileLineID = 264;
        private ILogger logger;
        private int minSize = Int32.MaxValue;
        private readonly int offsetMessageType;
        private readonly FFXIVRepository ffxiv;

        private readonly Type actionEffectHeaderType;
        private readonly Func<string, DateTime, bool> logWriter;
        private readonly Type headerType;

        private readonly Dictionary<int, ActionEffectTypeInfo> opcodeToType =
            new Dictionary<int, ActionEffectTypeInfo>();

        private readonly FieldInfo fieldCastSourceId;
        private readonly FieldInfo fieldAbilityId;
        private readonly FieldInfo fieldGlobalEffectCounter;
        private readonly FieldInfo fieldR;

        private class ActionEffectTypeInfo
        {
            public int actionEffectSize;
            public int packetSize;
            public Type extraType;
            public bool hasError;
        }

        interface IActionEffectExtra
        {
            ushort x { get; }
            ushort y { get; }
            ushort z { get; }
        }

        [StructLayout(LayoutKind.Explicit, Pack = 1, Size = 0x29C)]
        private struct Server_ActionEffect8_Extra : IActionEffectExtra
        {
            [FieldOffset(0x290)]
            private ushort _x;

            [FieldOffset(0x292)]
            private ushort _z;

            [FieldOffset(0x294)]
            private ushort _y;

            public ushort x => _x;
            public ushort y => _y;
            public ushort z => _z;
        }

        [StructLayout(LayoutKind.Explicit, Pack = 1, Size = 0x4DC)]
        private struct Server_ActionEffect16_Extra : IActionEffectExtra
        {
            [FieldOffset(0x4D0)]
            public ushort _x;

            [FieldOffset(0x4D2)]
            public ushort _z;

            [FieldOffset(0x4D4)]
            public ushort _y;

            public ushort x => _x;
            public ushort y => _y;
            public ushort z => _z;
        }

        [StructLayout(LayoutKind.Explicit, Pack = 1, Size = 0x71C)]
        private struct Server_ActionEffect24_Extra : IActionEffectExtra
        {
            [FieldOffset(0x710)]
            public ushort _x;

            [FieldOffset(0x712)]
            public ushort _z;

            [FieldOffset(0x714)]
            public ushort _y;

            public ushort x => _x;
            public ushort y => _y;
            public ushort z => _z;
        }

        [StructLayout(LayoutKind.Explicit, Pack = 1, Size = 0x95C)]
        private struct Server_ActionEffect32_Extra : IActionEffectExtra
        {
            [FieldOffset(0x950)]
            public ushort _x;

            [FieldOffset(0x952)]
            public ushort _z;

            [FieldOffset(0x954)]
            public ushort _y;

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
            logger = container.Resolve<ILogger>();
            ffxiv = container.Resolve<FFXIVRepository>();
            var netHelper = container.Resolve<NetworkParser>();
            if (!ffxiv.IsFFXIVPluginPresent())
                return;
            try
            {
                var mach = Assembly.Load("Machina.FFXIV");
                var msgHeaderType = mach.GetType("Machina.FFXIV.Headers.Server_MessageHeader");
                headerType = mach.GetType("Machina.FFXIV.Headers.Server_MessageHeader");
                fieldCastSourceId = headerType.GetField("ActorID");

                actionEffectHeaderType = mach.GetType("Machina.FFXIV.Headers.Server_ActionEffectHeader");
                fieldAbilityId = actionEffectHeaderType.GetField("actionId");
                fieldR = actionEffectHeaderType.GetField("rotation");
                fieldGlobalEffectCounter = actionEffectHeaderType.GetField("globalEffectCounter");

                int[] sizes = { 1, 8, 16, 24, 32 };
                foreach (int size in sizes)
                {
                    Type aeType = mach.GetType("Machina.FFXIV.Headers.Server_ActionEffect" + size);
                    int machinaTypeSize = Marshal.SizeOf(aeType);
                    var actionEffectTypeInfo = new ActionEffectTypeInfo()
                    {
                        actionEffectSize = size,
                        packetSize = machinaTypeSize,
                    };
                    minSize = Math.Min(machinaTypeSize, minSize);
                    logger.Log(LogLevel.Debug, "ActionEffect Size {0} -> {1:X4}", size,
                        actionEffectTypeInfo.packetSize);
                    switch (size)
                    {
                        case 8:
                            {
                                actionEffectTypeInfo.extraType = typeof(Server_ActionEffect8_Extra);
                                break;
                            }
                        case 16:
                            {
                                actionEffectTypeInfo.extraType = typeof(Server_ActionEffect16_Extra);
                                break;
                            }
                        case 24:
                            {
                                actionEffectTypeInfo.extraType = typeof(Server_ActionEffect24_Extra);
                                break;
                            }
                        case 32:
                            {
                                actionEffectTypeInfo.extraType = typeof(Server_ActionEffect32_Extra);
                                break;
                            }
                    }

                    if (size != 1)
                    {
                        int extraSize = Marshal.SizeOf(actionEffectTypeInfo.extraType);
                        if (machinaTypeSize != extraSize)
                        {
                            logger.Log(LogLevel.Error, "ActionEffect size mismatch! {} -> {}", machinaTypeSize,
                                extraSize);
                            actionEffectTypeInfo.hasError = true;
                        }
                    }
                    opcodeToType.Add(netHelper.GetOpcode("Ability" + size), actionEffectTypeInfo);
                }

                offsetMessageType = netHelper.GetOffset(msgHeaderType, "MessageType");
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

            var customLogLines = container.Resolve<FFXIVCustomLogLines>();
            this.logWriter = customLogLines.RegisterCustomLogLine(new LogLineRegistryEntry()
            {
                Name = "ActorCastExtra",
                Source = "OverlayPlugin",
                ID = LogFileLineID,
                Version = 1,
            });
        }

        private unsafe void MessageReceived(string id, long epoch, byte[] message)
        {
            if (message.Length < minSize)
                return;

            fixed (byte* buffer = message)
            {
                int opCode = *(ushort*)&buffer[offsetMessageType];
                ActionEffectTypeInfo info;
                bool result = opcodeToType.TryGetValue(opCode, out info);
                if (result)
                {
                    if (message.Length < info.packetSize)
                    {
                        return;
                    }

                    DateTime serverTime = ffxiv.EpochToDateTime(epoch);

                    object header = Marshal.PtrToStructure(new IntPtr(buffer), headerType);
                    UInt32 sourceId = (UInt32)fieldCastSourceId.GetValue(header);

                    object aeHeader = Marshal.PtrToStructure(new IntPtr(buffer), actionEffectHeaderType);
                    // Ability ID is really 16-bit, so it is formatted as such, but we will get an
                    // exception if we try to prematurely cast it to UInt16
                    UInt32 abilityId = (UInt32)fieldAbilityId.GetValue(aeHeader);
                    UInt32 globalEffectCounter = (UInt32)fieldGlobalEffectCounter.GetValue(aeHeader);

                    if (info.actionEffectSize == 1)
                    {
                        // AE1 is not useful. It does not contain this data. But we still need to write something
                        // to indicate that a proper line will not be happening.
                        logWriter(string.Format(CultureInfo.InvariantCulture,
                            "{0:X8}|{1:X4}|{2:X8}|{3}||||",
                            sourceId, abilityId, globalEffectCounter, (int)LineSubType.NO_DATA), serverTime);
                        return;
                    }

                    if (info.hasError)
                    {
                        logWriter(string.Format(CultureInfo.InvariantCulture,
                            "{0:X8}|{1:X4}|{2:X8}|{3}||||",
                            sourceId, globalEffectCounter, (int)LineSubType.ERROR), serverTime);
                        return;
                    }

                    IActionEffectExtra aeExtra =
                        (IActionEffectExtra)Marshal.PtrToStructure(new IntPtr(buffer), info.extraType);

                    float x = ffxiv.ConvertUInt16Coordinate(aeExtra.x);
                    float y = ffxiv.ConvertUInt16Coordinate(aeExtra.y);
                    float z = ffxiv.ConvertUInt16Coordinate(aeExtra.z);

                    double h = ffxiv.ConvertHeading((ushort)fieldR.GetValue(aeHeader));
                    string line = string.Format(CultureInfo.InvariantCulture,
                        "{0:X8}|{1:X4}|{2:X8}|{3}|{4:F3}|{5:F3}|{6:F3}|{7:F3}",
                        sourceId, abilityId, globalEffectCounter, (int)LineSubType.DATA_PRESENT, x, y, z, h);

                    logWriter(line, serverTime);
                }
            }
        }
    }
}