using System.Runtime.InteropServices;
using RainbowMage.OverlayPlugin.MemoryProcessors;
using RainbowMage.OverlayPlugin.NetworkProcessors.PacketHelper;

/*
Example lines from T5 for Neurolink spawns, alongside 261 CombatantMemory line for comparison.
Ignore the grossly desynced timestamps, I had VS debugger attached and a breakpoint was hit without me realizing it
`baseId` == "1E88FF" is the Neurolink actor

275|2024-07-28T14:51:25.4850000-04:00|40003085|04|07|05|001E88FF|40003085|00000000|80037536|E0000000|00000000|1.0000|0000|-0.0001|0000|00|00|00000000|00000000|-3.6630|-14.8297|50.0047|0e02c84215561dd0
261|2024-07-28T14:51:40.6490000-04:00|Add|40003085|BNpcID|1E88FF|BNpcNameID|C320FC32|CastBuffID|240253696|CastDurationCurrent|1.0000|CastDurationMax|1.0000|CastGroundTargetX|-159.9851|CastGroundTargetY|-117.9983|CastGroundTargetZ|21.5000|CastTargetID|A000000|CurrentMP|32759|Heading|0.0000|Job|200|Level|29|MaxMP|-788549488|PosX|-3.6630|PosY|-14.8297|PosZ|50.0047|Radius|0.5000|Type|7|0e64f879962f8565

275|2024-07-28T14:51:45.5340000-04:00|40003091|05|07|05|001E88FF|40003091|00000000|80037536|E0000000|00000000|1.0000|0000|-0.0001|0000|00|00|00000000|00000000|5.6163|-22.8206|51.4939|d972744632b7529e
261|2024-07-28T14:51:45.0270000-04:00|Add|40003091|BNpcID|1E88FF|BNpcNameID|C320FC32|CastBuffID|240253696|CastDurationCurrent|1.0000|CastDurationMax|1.0000|CastGroundTargetX|-159.9851|CastGroundTargetY|-117.9983|CastGroundTargetZ|21.5000|CastTargetID|A000000|CurrentMP|32759|Heading|0.0000|Job|200|Level|29|MaxMP|-788543648|PosX|5.6163|PosY|-22.8206|PosZ|51.4939|Radius|0.5000|Type|7|7a31913d48a069f8

These are despawned at the end of the fight:
276|2024-07-28T14:54:54.0140000-04:00|04|315e1c7c6cc49ec6
276|2024-07-28T14:54:54.0140000-04:00|05|988744f812b56584
 */

namespace RainbowMage.OverlayPlugin.NetworkProcessors
{
    class LineSpawnObject : LineBaseCustom<
            Server_MessageHeader_Global, LineSpawnObject.SpawnObject_v700,
            Server_MessageHeader_CN, LineSpawnObject.SpawnObject_v655,
            Server_MessageHeader_KR, LineSpawnObject.SpawnObject_v655>
    {
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public unsafe struct SpawnObject_v655 : IPacketStruct
        {
            public byte index;
            public byte kind;
            public byte flags;
            public byte padding1;
            public uint baseId;
            public uint entityId;
            public uint layoutId;
            public uint contentId;
            public uint ownerId;
            public uint bindLayoutId; // Other sources call this `gimmickId` as well
            public float scale;
            public ushort sharedGroupTimelineState;
            public ushort rotation;
            public ushort fate;
            public byte permissionInvisibility; // According to Sapphire. No clue what this actually means
            public byte args1;
            public uint args2;
            public uint args3;
            public float x;
            public float y;
            public float z;
            public uint padding2;

            public string ToString(long epoch, uint ActorID)
            {
                return $"{ActorID:X8}|{index:X2}|{kind:X2}|{flags:X2}|" +
                    $"{baseId:X8}|{entityId:X8}|{layoutId:X8}|{contentId:X8}|{ownerId:X8}|{bindLayoutId:X8}|" +
                    $"{scale:F4}|{sharedGroupTimelineState:X4}|{FFXIVRepository.ConvertHeading(rotation):F4}|{fate:X4}|" +
                    $"{permissionInvisibility:X2}|{args1:X2}|{args2:X8}|{args3:X8}|" +
                    // Y and Z coordinates inverted to match other lines
                    $"{x:F4}|{z:F4}|{y:F4}";
            }
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public unsafe struct SpawnObject_v700 : IPacketStruct
        {
            public byte index;
            public byte kind;
            public byte flags;
            public byte padding1;
            public uint baseId;
            public uint entityId;
            public uint layoutId;
            public uint contentId;
            public uint ownerId;
            public uint bindLayoutId; // Other sources call this `gimmickId` as well
            public float scale;
            public ushort sharedGroupTimelineState;
            public ushort rotation;
            public ushort fate;
            public byte permissionInvisibility; // According to Sapphire. No clue what this actually means
            public byte args1;
            public uint args2;
            public uint args3;
            // This padding was moved in 7.0, probably due to compiler changes?
            public uint padding2;
            public float x;
            public float y;
            public float z;

            public string ToString(long epoch, uint ActorID)
            {
                return $"{ActorID:X8}|{index:X2}|{kind:X2}|{flags:X2}|" +
                    $"{baseId:X8}|{entityId:X8}|{layoutId:X8}|{contentId:X8}|{ownerId:X8}|{bindLayoutId:X8}|" +
                    $"{scale:F4}|{sharedGroupTimelineState:X4}|{FFXIVRepository.ConvertHeading(rotation):F4}|{fate:X4}|" +
                    $"{permissionInvisibility:X2}|{args1:X2}|{args2:X8}|{args3:X8}|" +
                    // Y and Z coordinates inverted to match other lines
                    $"{x:F4}|{z:F4}|{y:F4}";
            }
        }

        public const uint LogFileLineID = 275;
        public const string logLineName = "SpawnObject";
        public const string OpcodeName = "SpawnObject";

        public LineSpawnObject(TinyIoCContainer container)
            : base(container, LogFileLineID, logLineName, OpcodeName) { }
    }
}
