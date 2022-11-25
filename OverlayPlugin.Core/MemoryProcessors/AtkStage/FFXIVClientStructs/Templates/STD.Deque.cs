using System.Runtime.InteropServices;

namespace FFXIVClientStructs.__NAMESPACE__.STD {
    [StructLayout(LayoutKind.Sequential, Size = 0x28)]
    public unsafe struct StdDeque
    {
        public void* ContainerBase; // iterator base nonsense
        public void** Map; // pointer to array of pointers (size MapSize) to arrays of T (size BlockSize)
        public ulong MapSize; // size of map
        public ulong MyOff; // offset of current first element
        public ulong MySize; // current length 
    }
}
