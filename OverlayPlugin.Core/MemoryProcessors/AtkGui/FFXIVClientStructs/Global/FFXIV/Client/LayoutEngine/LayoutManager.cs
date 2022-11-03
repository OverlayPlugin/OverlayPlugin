﻿
using System.Runtime.InteropServices;
namespace RainbowMage.OverlayPlugin.MemoryProcessors.AtkStage.FFXIVClientStructs.Global.FFXIV.Client.LayoutEngine
{

    [StructLayout(LayoutKind.Explicit, Size = 0x98)]
    public unsafe struct LayoutManager
    {
        [FieldOffset(0x20)] public uint TerritoryTypeId;
        [FieldOffset(0x80)] public void* HousingController;
        [FieldOffset(0x90)] public IndoorAreaLayoutData* IndoorAreaData;


    }

    [StructLayout(LayoutKind.Explicit)]
    public unsafe struct IndoorAreaLayoutData
    {
        [FieldOffset(0x28)] public IndoorFloorLayoutData Floor0;
        [FieldOffset(0x3C)] public IndoorFloorLayoutData Floor1;
        [FieldOffset(0x50)] public IndoorFloorLayoutData Floor2;
        [FieldOffset(0x80)] public float LightLevel;
    }

    [StructLayout(LayoutKind.Explicit)]
    public unsafe struct IndoorFloorLayoutData
    {
        [FieldOffset(0x00)] public int Part0;
        [FieldOffset(0x04)] public int Part1;
        [FieldOffset(0x08)] public int Part2;
        [FieldOffset(0x0C)] public int Part3;
        [FieldOffset(0x10)] public int Part4;
    }
}