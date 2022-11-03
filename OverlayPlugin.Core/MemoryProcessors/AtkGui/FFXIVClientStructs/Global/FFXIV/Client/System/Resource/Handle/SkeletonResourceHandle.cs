﻿using System.Runtime.InteropServices;
using RainbowMage.OverlayPlugin.MemoryProcessors.AtkStage.FFXIVClientStructs.Global.FFXIV.Client.Graphics;
using RainbowMage.OverlayPlugin.MemoryProcessors.AtkStage.FFXIVClientStructs.Global.STD;
namespace RainbowMage.OverlayPlugin.MemoryProcessors.AtkStage.FFXIVClientStructs.Global.FFXIV.Client.System.Resource.Handle
{

    [StructLayout(LayoutKind.Explicit)]
    public unsafe struct SkeletonResourceHandle
    {
        [StructLayout(LayoutKind.Explicit, Size = 0x30)]
        public struct SkeletonHeader
        {
            [FieldOffset(0x0)] public uint SklbMagic;
            [FieldOffset(0x4)] public uint SklbVersion;
            [FieldOffset(0x8)] public uint LayerOffset;
            [FieldOffset(0xC)] public uint SklbOffset;
            [FieldOffset(0x10)] public ushort ConnectBoneIndex;
            [FieldOffset(0x12)] private ushort pad;
            [FieldOffset(0x14)] public uint CharacterId;
            [FieldOffset(0x18)] public fixed uint SkeletonMappers[4];
            [FieldOffset(0x28)] public fixed ushort ConnectBoneIds[4];
        }

        [FieldOffset(0x0)] public ResourceHandle ResourceHandle;
        [FieldOffset(0xC8)] public uint BoneCount;
        [FieldOffset(0xD0)] public void* HavokSkeleton;
        [FieldOffset(0xD8)] public StdMap SkeletonMapperDict1;
        [FieldOffset(0xE8)] public StdMap SkeletonMapperDict2;
        [FieldOffset(0xF8)] public Matrix44* InverseBoneMatrix;
        [FieldOffset(0x100)] public void* HavokLoader;
        [FieldOffset(0x108)] public SkeletonHeader SkeletonData;
    }
}