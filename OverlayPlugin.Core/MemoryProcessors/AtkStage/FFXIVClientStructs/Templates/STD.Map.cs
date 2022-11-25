using System.Runtime.InteropServices;

namespace FFXIVClientStructs.__NAMESPACE__.STD {
    [StructLayout(LayoutKind.Sequential, Size = 0x10)]
    public unsafe struct StdMap
    {
        public void* Head;
        public ulong Count;
    }
}
