using System;
using System.Runtime.InteropServices;
using RainbowMage.OverlayPlugin.NetworkProcessors.PacketHelper;

/*
Example lines from fishing:
cast line was done before capturing.
exclamation point animation:
275|2024-05-12T00:43:34.1040000-04:00|1|10001234|0|150001|5|40001|0|1|125|4927e2b6b7d6d118
reeling animation:
275|2024-05-12T00:43:34.8510000-04:00|4|10001234|0|150001|6|40001|0|4|11C|120|10F|0|b0042cd36facf644
finish reeling animation:
275|2024-05-12T00:43:44.2490000-04:00|1|10001234|0|150001|2|40001|0|2|0|359f9119b64af7fd
capture animation:
275|2024-05-12T00:43:44.5690000-04:00|8|10001234|0|150001|4|40001|0|5|10F|0|1|1|7|0|0|0|157dc732c1530126
 */
namespace RainbowMage.OverlayPlugin.NetworkProcessors
{
    class LineEventStartPlayFinish : LineBaseCustom<
            Server_MessageHeader_Global, LineEventStartPlayFinish.EventPlay1,
            Server_MessageHeader_CN, LineEventStartPlayFinish.EventPlay1,
            Server_MessageHeader_KR, LineEventStartPlayFinish.EventPlay1>
    {
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        internal unsafe struct EventPlayBase
        {
            uint actorID;
            uint unknown1;
            uint eventID;
            ushort scene;
            ushort padding;
            uint flags;
            uint param3;
            byte param4;
            byte padding1;
            ushort padding2;

            public unsafe string ToString(uint* eventParams, int eventParamsCount)
            {
                var retString = $"{eventParamsCount}|{actorID:X8}|{unknown1:X}|{eventID:X}|{scene:X}|{flags:X}|{param3:X}|{param4:X}";

                for (var i = 0; i < eventParamsCount; ++i)
                {
                    retString += $"|{eventParams[i]:X}";
                }

                return retString;
            }
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        internal unsafe struct EventPlay1 : IPacketStruct
        {
            EventPlayBase eventPlayBase;

            const int eventParamsCount = 1;

            fixed uint eventParams[eventParamsCount];

            public string ToString(long epoch, uint ActorID)
            {
                fixed (uint* paramsPtr = eventParams)
                {
                    return eventPlayBase.ToString(paramsPtr, eventParamsCount);
                }
            }
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        internal unsafe struct EventPlay4 : IPacketStruct
        {
            EventPlayBase eventPlayBase;

            const int eventParamsCount = 4;

            fixed uint eventParams[eventParamsCount];

            public string ToString(long epoch, uint ActorID)
            {
                fixed (uint* paramsPtr = eventParams)
                {
                    return eventPlayBase.ToString(paramsPtr, eventParamsCount);
                }
            }
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        internal unsafe struct EventPlay8 : IPacketStruct
        {
            EventPlayBase eventPlayBase;

            const int eventParamsCount = 8;

            fixed uint eventParams[eventParamsCount];

            public string ToString(long epoch, uint ActorID)
            {
                fixed (uint* paramsPtr = eventParams)
                {
                    return eventPlayBase.ToString(paramsPtr, eventParamsCount);
                }
            }
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        internal unsafe struct EventPlay16 : IPacketStruct
        {
            EventPlayBase eventPlayBase;

            const int eventParamsCount = 16;

            fixed uint eventParams[eventParamsCount];

            public string ToString(long epoch, uint ActorID)
            {
                fixed (uint* paramsPtr = eventParams)
                {
                    return eventPlayBase.ToString(paramsPtr, eventParamsCount);
                }
            }
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        internal unsafe struct EventPlay32 : IPacketStruct
        {
            EventPlayBase eventPlayBase;

            const int eventParamsCount = 32;

            fixed uint eventParams[eventParamsCount];

            public string ToString(long epoch, uint ActorID)
            {
                fixed (uint* paramsPtr = eventParams)
                {
                    return eventPlayBase.ToString(paramsPtr, eventParamsCount);
                }
            }
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        internal unsafe struct EventPlay64 : IPacketStruct
        {
            EventPlayBase eventPlayBase;

            const int eventParamsCount = 64;

            fixed uint eventParams[eventParamsCount];

            public string ToString(long epoch, uint ActorID)
            {
                fixed (uint* paramsPtr = eventParams)
                {
                    return eventPlayBase.ToString(paramsPtr, eventParamsCount);
                }
            }
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        internal unsafe struct EventPlay128 : IPacketStruct
        {
            EventPlayBase eventPlayBase;

            const int eventParamsCount = 128;

            fixed uint eventParams[eventParamsCount];

            public string ToString(long epoch, uint ActorID)
            {
                fixed (uint* paramsPtr = eventParams)
                {
                    return eventPlayBase.ToString(paramsPtr, eventParamsCount);
                }
            }
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        internal unsafe struct EventPlay255 : IPacketStruct
        {
            EventPlayBase eventPlayBase;

            const int eventParamsCount = 255;

            fixed uint eventParams[eventParamsCount];

            public string ToString(long epoch, uint ActorID)
            {
                fixed (uint* paramsPtr = eventParams)
                {
                    return eventPlayBase.ToString(paramsPtr, eventParamsCount);
                }
            }
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public unsafe struct EventStart : IPacketStruct
        {
            uint actorID;
            uint unknown1;
            uint eventID;
            byte param1;
            byte param2;
            ushort padding;
            uint param3;
            uint padding1;

            public string ToString(long epoch, uint ActorID)
            {
                return $"S|{actorID:X8}|{unknown1:X8}|{eventID:X8}|{param1:X2}|{param2:X2}|{param3:X8}";
            }
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public unsafe struct EventFinish : IPacketStruct
        {
            uint eventID;
            byte param1;
            byte param2;
            ushort padding;
            uint param3;
            uint padding1;

            public string ToString(long epoch, uint ActorID)
            {
                return $"F|{eventID:X8}|{param1:X2}|{param2:X2}|{param3:X8}";
            }
        }

        protected readonly RegionalizedPacketHelper<
            Server_MessageHeader_Global, EventPlay4,
            Server_MessageHeader_CN, EventPlay4,
            Server_MessageHeader_KR, EventPlay4> eventPlay4Helper;

        protected readonly RegionalizedPacketHelper<
            Server_MessageHeader_Global, EventPlay8,
            Server_MessageHeader_CN, EventPlay8,
            Server_MessageHeader_KR, EventPlay8> eventPlay8Helper;

        protected readonly RegionalizedPacketHelper<
            Server_MessageHeader_Global, EventPlay16,
            Server_MessageHeader_CN, EventPlay16,
            Server_MessageHeader_KR, EventPlay16> eventPlay16Helper;

        protected readonly RegionalizedPacketHelper<
            Server_MessageHeader_Global, EventPlay32,
            Server_MessageHeader_CN, EventPlay32,
            Server_MessageHeader_KR, EventPlay32> eventPlay32Helper;

        protected readonly RegionalizedPacketHelper<
            Server_MessageHeader_Global, EventPlay64,
            Server_MessageHeader_CN, EventPlay64,
            Server_MessageHeader_KR, EventPlay64> eventPlay64Helper;

        protected readonly RegionalizedPacketHelper<
            Server_MessageHeader_Global, EventPlay128,
            Server_MessageHeader_CN, EventPlay128,
            Server_MessageHeader_KR, EventPlay128> eventPlay128Helper;

        protected readonly RegionalizedPacketHelper<
            Server_MessageHeader_Global, EventPlay255,
            Server_MessageHeader_CN, EventPlay255,
            Server_MessageHeader_KR, EventPlay255> eventPlay255Helper;

        protected readonly RegionalizedPacketHelper<
            Server_MessageHeader_Global, EventStart,
            Server_MessageHeader_CN, EventStart,
            Server_MessageHeader_KR, EventStart> eventStartHelper;

        protected readonly RegionalizedPacketHelper<
            Server_MessageHeader_Global, EventFinish,
            Server_MessageHeader_CN, EventFinish,
            Server_MessageHeader_KR, EventFinish> eventFinishHelper;

        public const uint LogFileLineID = 275;
        public const string LogLineName = "LineEventStartPlayFinish";
        public const string OpcodeName = "EventPlay1";

        public LineEventStartPlayFinish(TinyIoCContainer container) : base(container, LogFileLineID, LogLineName, OpcodeName)
        {
            var opcodeConfig = container.Resolve<OverlayPluginLogLineConfig>();
            var logger = container.Resolve<ILogger>();

            eventPlay4Helper = RegionalizedPacketHelper<
                            Server_MessageHeader_Global, EventPlay4,
                            Server_MessageHeader_CN, EventPlay4,
                            Server_MessageHeader_KR, EventPlay4>.CreateFromOpcodeConfig(opcodeConfig, "EventPlay4");
            if (eventPlay4Helper == null)
            {
                logger.Log(LogLevel.Error, $"Failed to initialize LineEventStartPlayFinish: Creating eventPlay4Helper failed");
                return;
            }

            eventPlay8Helper = RegionalizedPacketHelper<
                            Server_MessageHeader_Global, EventPlay8,
                            Server_MessageHeader_CN, EventPlay8,
                            Server_MessageHeader_KR, EventPlay8>.CreateFromOpcodeConfig(opcodeConfig, "EventPlay8");
            if (eventPlay8Helper == null)
            {
                logger.Log(LogLevel.Error, $"Failed to initialize LineEventStartPlayFinish: Creating eventPlay8Helper failed");
                return;
            }

            eventPlay16Helper = RegionalizedPacketHelper<
                            Server_MessageHeader_Global, EventPlay16,
                            Server_MessageHeader_CN, EventPlay16,
                            Server_MessageHeader_KR, EventPlay16>.CreateFromOpcodeConfig(opcodeConfig, "EventPlay16");
            if (eventPlay16Helper == null)
            {
                logger.Log(LogLevel.Error, $"Failed to initialize LineEventStartPlayFinish: Creating eventPlay16Helper failed");
                return;
            }

            eventPlay32Helper = RegionalizedPacketHelper<
                            Server_MessageHeader_Global, EventPlay32,
                            Server_MessageHeader_CN, EventPlay32,
                            Server_MessageHeader_KR, EventPlay32>.CreateFromOpcodeConfig(opcodeConfig, "EventPlay32");
            if (eventPlay32Helper == null)
            {
                logger.Log(LogLevel.Error, $"Failed to initialize LineEventStartPlayFinish: Creating eventPlay32Helper failed");
                return;
            }

            eventPlay64Helper = RegionalizedPacketHelper<
                            Server_MessageHeader_Global, EventPlay64,
                            Server_MessageHeader_CN, EventPlay64,
                            Server_MessageHeader_KR, EventPlay64>.CreateFromOpcodeConfig(opcodeConfig, "EventPlay64");
            if (eventPlay64Helper == null)
            {
                logger.Log(LogLevel.Error, $"Failed to initialize LineEventStartPlayFinish: Creating eventPlay64Helper failed");
                return;
            }

            eventPlay128Helper = RegionalizedPacketHelper<
                            Server_MessageHeader_Global, EventPlay128,
                            Server_MessageHeader_CN, EventPlay128,
                            Server_MessageHeader_KR, EventPlay128>.CreateFromOpcodeConfig(opcodeConfig, "EventPlay128");
            if (eventPlay128Helper == null)
            {
                logger.Log(LogLevel.Error, $"Failed to initialize LineEventStartPlayFinish: Creating eventPlay128Helper failed");
                return;
            }

            eventPlay255Helper = RegionalizedPacketHelper<
                            Server_MessageHeader_Global, EventPlay255,
                            Server_MessageHeader_CN, EventPlay255,
                            Server_MessageHeader_KR, EventPlay255>.CreateFromOpcodeConfig(opcodeConfig, "EventPlay255");
            if (eventPlay255Helper == null)
            {
                logger.Log(LogLevel.Error, $"Failed to initialize LineEventStartPlayFinish: Creating eventPlay255Helper failed");
                return;
            }

            eventStartHelper = RegionalizedPacketHelper<
                            Server_MessageHeader_Global, EventStart,
                            Server_MessageHeader_CN, EventStart,
                            Server_MessageHeader_KR, EventStart>.CreateFromOpcodeConfig(opcodeConfig, "EventStart");
            if (eventStartHelper == null)
            {
                logger.Log(LogLevel.Error, $"Failed to initialize LineEventStartPlayFinish: Creating eventStartHelper failed");
                return;
            }

            eventFinishHelper = RegionalizedPacketHelper<
                            Server_MessageHeader_Global, EventFinish,
                            Server_MessageHeader_CN, EventFinish,
                            Server_MessageHeader_KR, EventFinish>.CreateFromOpcodeConfig(opcodeConfig, "EventFinish");
            if (eventFinishHelper == null)
            {
                logger.Log(LogLevel.Error, $"Failed to initialize LineEventStartPlayFinish: Creating eventFinishHelper failed");
                return;
            }
        }

        protected override unsafe void MessageReceived(string id, long epoch, byte[] message)
        {
            // Check the last packet helper we initialize
            if (eventFinishHelper == null || !currentRegion.HasValue)
                return;

            var line = packetHelper[currentRegion.Value].ToString(epoch, message);

            if (line == null)
            {
                line = eventPlay4Helper[currentRegion.Value].ToString(epoch, message);
            }

            if (line == null)
            {
                line = eventPlay8Helper[currentRegion.Value].ToString(epoch, message);
            }

            if (line == null)
            {
                line = eventPlay16Helper[currentRegion.Value].ToString(epoch, message);
            }

            if (line == null)
            {
                line = eventPlay32Helper[currentRegion.Value].ToString(epoch, message);
            }

            if (line == null)
            {
                line = eventPlay64Helper[currentRegion.Value].ToString(epoch, message);
            }

            if (line == null)
            {
                line = eventPlay128Helper[currentRegion.Value].ToString(epoch, message);
            }

            if (line == null)
            {
                line = eventPlay255Helper[currentRegion.Value].ToString(epoch, message);
            }

            if (line == null)
            {
                line = eventStartHelper[currentRegion.Value].ToString(epoch, message);
            }

            if (line == null)
            {
                line = eventFinishHelper[currentRegion.Value].ToString(epoch, message);
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
