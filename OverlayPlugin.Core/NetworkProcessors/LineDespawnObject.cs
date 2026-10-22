using System.Runtime.InteropServices;
using RainbowMage.OverlayPlugin.MemoryProcessors;
using RainbowMage.OverlayPlugin.NetworkProcessors.PacketHelper;

namespace RainbowMage.OverlayPlugin.NetworkProcessors
{
    class LineDespawnObject : LineBaseCustom<
            Server_MessageHeader_Global, LineDespawnObject.DespawnObject_v655,
            Server_MessageHeader_CN, LineDespawnObject.DespawnObject_v655,
            Server_MessageHeader_KR, LineDespawnObject.DespawnObject_v655>
    {
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public unsafe struct DespawnObject_v655 : IPacketStruct
        {
            public byte spawnIndex;
            public byte padding1;
            public ushort padding2;
            public uint padding3;

            public string ToString(long epoch, uint ActorID)
            {
                // The `ActorID` from the packet header is just the current player's ID
                // There's no data for the actual actor ID, instead the client is expected to
                // look up the correct actor via the index.
                return $"{spawnIndex:X2}";
            }
        }

        public const uint LogFileLineID = 276;
        public const string logLineName = "DespawnObject";
        public const string OpcodeName = "DespawnObject";

        public LineDespawnObject(TinyIoCContainer container)
            : base(container, LogFileLineID, logLineName, OpcodeName) { }
    }
}
