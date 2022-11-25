using System.Runtime.InteropServices;

namespace FFXIVClientStructs.__NAMESPACE__.STD {
    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct StdVector
    {
        public void* First;
        public void* Last;
        public void* End;
    }
}
