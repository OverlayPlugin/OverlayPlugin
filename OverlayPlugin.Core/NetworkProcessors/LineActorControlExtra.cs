using System;
using System.Globalization;
using System.Linq;
using RainbowMage.OverlayPlugin.NetworkProcessors.PacketHelper;

// The easiest place to test SetAnimationState is Lunar Subteranne.
// On the first Ruinous Confluence, each staff has this line:
// 273|2023-12-05T10:57:43.4770000-08:00|4000A145|003E|00000001|00000000|00000000|00000000|06e7eff4a949812c
// On the second Ruinous Confluence, each staff has this line:
// 273|2023-12-05T10:58:00.3460000-08:00|4000A144|003E|00000001|00000001|00000000|00000000|a4af9f90928636a3

namespace RainbowMage.OverlayPlugin.NetworkProcessors
{
    class LineActorControlExtra : LineBaseSubMachina<LineActorControlExtra.ActorControlExtraPacket>
    {
        public const uint LogFileLineID = 273;
        public const string LogLineName = "ActorControlExtra";
        public const string MachinaPacketName = "ActorControl";

        // Any category defined in this array will be allowed as an emitted line
        public static readonly Server_ActorControlCategory[] AllowedActorControlCategories = {
            Server_ActorControlCategory.SetAnimationState,
            Server_ActorControlCategory.DisplayPublicContentTextMessage
        };

        internal class ActorControlExtraPacket : MachinaPacketWrapper
        {
            public override string ToString(long epoch, uint ActorID)
            {
                var category = Get<Server_ActorControlCategory>("category");

                if (!AllowedActorControlCategories.Contains(category)) return null;

                var param1 = Get<UInt32>("param1");
                var param2 = Get<UInt32>("param2");
                var param3 = Get<UInt32>("param3");
                var param4 = Get<UInt32>("param4");

                return $"{ActorID:X8}|{(ushort)category:X4}|{param1:X}|{param2:X}|{param3:X}|{param4:X}";
            }
        }
        public LineActorControlExtra(TinyIoCContainer container)
            : base(container, LogFileLineID, LogLineName, MachinaPacketName) { }
    }
}