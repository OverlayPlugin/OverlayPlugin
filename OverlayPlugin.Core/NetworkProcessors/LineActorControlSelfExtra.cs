﻿using System;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using RainbowMage.OverlayPlugin.NetworkProcessors.PacketHelper;

// To test `DisplayLogMessage`, you can:
// Open a treasure coffer that gives you item(s):
// 274|2024-01-10T19:28:37.5000000-05:00|10001234|020F|04D0|0|93E0|0|0|0|d274429622d0c27e
// 274|2024-01-10T19:28:37.5000000-05:00|10001234|020F|04D0|0|93F3|0|0|0|d274429622d0c27e
// 00|2024-01-10T19:28:36.0000000-05:00|0A3E||You obtain a windswept shamshir.|92337ce2a33e52f8
// 00|2024-01-10T19:28:36.0000000-05:00|0A3E||You obtain a windswept shield.|a48cbf20d0255d4e
// Sell TT cards for MGP:
// 274|2024-01-10T20:08:35.3520000-05:00|10001234|020F|129D|0|320|0|0|0|d274429622d0c27e
// 00|2024-01-10T20:08:35.0000000-05:00|0A3E||You obtain 800 MGP.|f768dc4f098c15a6
// Die in Eureka with a `Spirit of the Remembered` active:
// 274|2024-02-15T19:35:41.9950000-05:00|10001234|020F|236D|0|669|0|0|0|d274429622d0c27e
// 00|2024-02-15T19:35:41.0000000-05:00|0939||The memories of heroes past live on again!|bb3bfbfc487ad4e9

// To test `DisplayLogMessageParams`, you can play a Gold Saucer minigame:
// 274|2024-03-21T20:45:41.3680000-04:00|10001234|0210|129D|10001234|F|0|0|0|d274429622d0c27e
// 00|2024-03-21T20:45:40.0000000-04:00|08BE||You obtain 15 MGP.|97702e809544a633

namespace RainbowMage.OverlayPlugin.NetworkProcessors
{
    public class LineActorControlSelfExtra
    {
        public const uint LogFileLineID = 274;
        private readonly FFXIVRepository ffxiv;

        // Any category defined in this array will be allowed as an emitted line
        public static readonly Server_ActorControlCategory[] AllowedActorControlCategories = {
            // Some `LogMessage` messages can be triggered by both 0x020F and 0x0210 categories, not sure what the difference is
            // except that 0x0210 messages usually have another actor ID in the parameters
            Server_ActorControlCategory.DisplayLogMessage,
            Server_ActorControlCategory.DisplayLogMessageParams,
        };

        internal class ActorControlSelfExtraPacket : MachinaPacketWrapper
        {
            public override string ToString(long epoch, uint ActorID)
            {
                var category = Get<Server_ActorControlCategory>("category");

                if (!AllowedActorControlCategories.Contains(category)) return null;

                var param1 = Get<UInt32>("param1");
                var param2 = Get<UInt32>("param1");
                var param3 = Get<UInt32>("param1");
                var param4 = Get<UInt32>("param1");
                var param5 = Get<UInt32>("param1");
                var param6 = Get<UInt32>("param1");

                return string.Format(CultureInfo.InvariantCulture,
                    "{0:X8}|{1:X4}|{2:X}|{3:X}|{4:X}|{5:X}|{6:X}|{7:X}",
                    ActorID, category, param1, param2, param3, param4, param5, param6);
            }
        }

        private MachinaRegionalizedPacketHelper<ActorControlSelfExtraPacket> packetHelper;
        private GameRegion? currentRegion;

        private readonly Func<string, DateTime, bool> logWriter;

        public LineActorControlSelfExtra(TinyIoCContainer container)
        {
            ffxiv = container.Resolve<FFXIVRepository>();
            ffxiv.RegisterNetworkParser(MessageReceived);
            ffxiv.RegisterProcessChangedHandler(ProcessChanged);

            if (MachinaRegionalizedPacketHelper<ActorControlSelfExtraPacket>.Create("Server_NpcSpawn", out packetHelper))
            {
                var customLogLines = container.Resolve<FFXIVCustomLogLines>();
                logWriter = customLogLines.RegisterCustomLogLine(new LogLineRegistryEntry()
                {
                    Name = "ActorControlSelfExtra",
                    Source = "OverlayPlugin",
                    ID = LogFileLineID,
                    Version = 1,
                });
            }
            else
            {
                var logger = container.Resolve<ILogger>();
                logger.Log(LogLevel.Error, Resources.NetworkParserNoFfxiv);
            }
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