using System.Runtime.InteropServices;

namespace RainbowMage.OverlayPlugin.MemoryProcessors.ClientFramework
{
    partial class ClientFrameworkMemory655 : ClientFrameworkMemory, IClientFrameworkMemory655
    {
        // Many offsets are commented because they are typed and we don't need to bother with them.
        #region FFXIVClientStructs structs
        [StructLayout(LayoutKind.Explicit, Size = 0x35C8)]
        public unsafe partial struct Framework
        {
            // [FieldOffset(0x0010)] public SystemConfig SystemConfig;
            // [FieldOffset(0x0460)] public DevConfig DevConfig;
            // [FieldOffset(0x0570)] public SavedAppearanceManager* SavedAppearanceData;
            [FieldOffset(0x0580)] public byte ClientLanguage;
            [FieldOffset(0x0581)] public byte Region;
            // [FieldOffset(0x0588)] public Cursor* Cursor;
            // [FieldOffset(0x0590)] public nint CallerWindow;
            // [FieldOffset(0x0598)] public FileAccessPath ConfigPath;
            // [FieldOffset(0x07A8)] public GameWindow* GameWindow;

            [FieldOffset(0x09F8)] public int CursorPosX;
            [FieldOffset(0x09FC)] public int CursorPosY;

            [FieldOffset(0x1104)] public int CursorPosX2;
            [FieldOffset(0x1108)] public int CursorPosY2;

            // [FieldOffset(0x1670)] public NetworkModuleProxy* NetworkModuleProxy;
            [FieldOffset(0x1678)] public bool IsNetworkModuleInitialized;
            [FieldOffset(0x1679)] public bool EnableNetworking;
            // [FieldOffset(0x1680)] public TimePoint UtcTime;
            [FieldOffset(0x1698)] public uint TimerResolutionMillis;
            [FieldOffset(0x16A0)] public long PerformanceCounterFrequency;
            [FieldOffset(0x16A8)] public long PerformanceCounterValue;
            [FieldOffset(0x16B8)] public float FrameDeltaTime;
            [FieldOffset(0x16BC)] public float RealFrameDeltaTime;
            [FieldOffset(0x16C0)] public float FrameDeltaTimeOverride;
            [FieldOffset(0x16C4)] public float FrameDeltaFactor;
            [FieldOffset(0x16C8)] public uint FrameCounter;
            [FieldOffset(0x16D0)] public long FrameDeltaTimeMSInt;
            [FieldOffset(0x16D8)] public float FrameDeltaTimeMSRem;
            [FieldOffset(0x16E0)] public long FrameDeltaTimeUSInt;
            [FieldOffset(0x16E8)] public float FrameDeltaTimeUSRem;
            // [FieldOffset(0x16F8)] public TaskManager TaskManager;
            // [FieldOffset(0x1768)] public ClientTime ClientTime;
            [FieldOffset(0x17B0)] public float GameSpeedMultiplier;
            [FieldOffset(0x17C4)] public float FrameRate;
            [FieldOffset(0x17C8)] public bool DiscardFrame;
            [FieldOffset(0x17CC)] public float NextFrameDeltaTimeOverride;
            [FieldOffset(0x17D0)] public bool WindowInactive;

            [FieldOffset(0x17E0)] public int DataPathType;

            // [FieldOffset(0x19EC), FixedSizeArray(isString: true)] internal FixedSizeArray260<char> _gamePath;
            // [FieldOffset(0x1DFC), FixedSizeArray(isString: true)] internal FixedSizeArray260<char> _sqPackPath;
            // [FieldOffset(0x220C), FixedSizeArray(isString: true)] internal FixedSizeArray260<char> _userPath;

            // [FieldOffset(0x2B30)] public ExcelModuleInterface* ExcelModuleInterface;
            // [FieldOffset(0x2B38)] public ExdModule* ExdModule;
            // [FieldOffset(0x2B50)] public BGCollisionModule* BGCollisionModule;
            // [FieldOffset(0x2B60)] public UIModule* UIModule;
            // [FieldOffset(0x2B68)] public UIClipboard* UIClipboard;
            // [FieldOffset(0x2B78)] public EnvironmentManager* EnvironmentManager;
            // [FieldOffset(0x2B80)] public SoundManager* SoundManager;
            // [FieldOffset(0x2BC8)] public LuaState LuaState;

            // [FieldOffset(0x2BF0), FixedSizeArray(isString: true)] internal FixedSizeArray256<byte> _gameVersion;
            // [FieldOffset(0x2CF0 + 0 * 0x20), FixedSizeArray(isString: true)] internal FixedSizeArray32<byte> _ex1Version; // Heavensward
            // [FieldOffset(0x2CF0 + 1 * 0x20), FixedSizeArray(isString: true)] internal FixedSizeArray32<byte> _ex2Version; // Stormblood
            // [FieldOffset(0x2CF0 + 2 * 0x20), FixedSizeArray(isString: true)] internal FixedSizeArray32<byte> _ex3Version; // Shadowbringers
            // [FieldOffset(0x2CF0 + 3 * 0x20), FixedSizeArray(isString: true)] internal FixedSizeArray32<byte> _ex4Version; // Endwalker
            // [FieldOffset(0x2CF0 + 4 * 0x20), FixedSizeArray(isString: true)] internal FixedSizeArray32<byte> _ex5Version; // Dawntrail

            [FieldOffset(0x3500)] public bool UseWatchDogThread;

            [FieldOffset(0x3510)] public int FramesUntilDebugCheck;
            [FieldOffset(0x35B4)] public bool IsSteamGame;

            // [FieldOffset(0x35B8)] public SteamApi* SteamApi;

            // [FieldOffset(0x35C0)] public nint SteamApiLibraryHandle;
        }
        #endregion
    }
}
