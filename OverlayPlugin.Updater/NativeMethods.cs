using System;
using System.Runtime.InteropServices;

namespace RainbowMage.OverlayPlugin.Updater
{
    static class NativeMethods
    {
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern IntPtr LoadLibrary(string name);

        [DllImport("kernel32.dll")]
        public static extern void FreeLibrary(IntPtr handle);

        [DllImport("user32.dll")]
        public static extern short GetKeyState(int nVirtKey);

        public const int VK_SHIFT = 0x10;

    }
}
