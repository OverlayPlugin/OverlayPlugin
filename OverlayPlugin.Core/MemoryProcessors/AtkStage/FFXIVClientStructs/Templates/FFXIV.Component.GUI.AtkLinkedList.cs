using System.Runtime.InteropServices;

namespace FFXIVClientStructs.__NAMESPACE__.FFXIV.Component.GUI {
    [StructLayout(LayoutKind.Sequential, Size=0x18)]
    public unsafe struct AtkLinkedList
    {
        public void* End;
        public void* Start;
        public uint Count;
    }
}
