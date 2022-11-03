﻿using System.Runtime.InteropServices;
namespace RainbowMage.OverlayPlugin.MemoryProcessors.AtkStage.FFXIVClientStructs.Global.FFXIV.Component.GUI
{

    [StructLayout(LayoutKind.Explicit, Size = 0x24)]
    public unsafe struct AtkUldComponentDataRadioButton
    {
        [FieldOffset(0x00)] public AtkUldComponentDataBase Base;
        [FieldOffset(0x0C)] public fixed uint Nodes[4];
        [FieldOffset(0x1C)] public uint TextId;
        [FieldOffset(0x20)] public uint GroupId;
    }
}