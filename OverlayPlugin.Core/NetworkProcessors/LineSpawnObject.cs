using System.Runtime.InteropServices;
using RainbowMage.OverlayPlugin.MemoryProcessors;
using RainbowMage.OverlayPlugin.NetworkProcessors.PacketHelper;

namespace RainbowMage.OverlayPlugin.NetworkProcessors
{
    class LineSpawnObject : LineBaseCustom<
            Server_MessageHeader_Global, LineSpawnObject.SpawnObject_v655,
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

        public const uint LogFileLineID = 275;
        public const string logLineName = "SpawnObject";
        public const string OpcodeName = "SpawnObject";

        public LineSpawnObject(TinyIoCContainer container)
            : base(container, LogFileLineID, logLineName, OpcodeName) { }
    }
}
