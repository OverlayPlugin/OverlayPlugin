﻿using System.Runtime.InteropServices;
using RainbowMage.OverlayPlugin.MemoryProcessors.AtkStage.FFXIVClientStructs.Global.FFXIV.Client.Graphics.Render;
namespace RainbowMage.OverlayPlugin.MemoryProcessors.AtkStage.FFXIVClientStructs.Global.FFXIV.Client.Graphics.Scene
{
    // Client::Graphics::Scene::DrawObject
    //   Client::Graphics::Scene::Object
    // base class for all drawn graphics objects

    // size = 0x90
    // ctor - E8 ? ? ? ? 48 8D 8F ? ? ? ? E8 ? ? ? ? 81 A7 ? ? ? ? ? ? ? ? 
    [StructLayout(LayoutKind.Explicit, Size = 0x90)]
    public unsafe struct DrawObject
    {
        [FieldOffset(0x0)] public Object Object;
        [FieldOffset(0xA0)] public Skeleton* Skeleton;
    }
}