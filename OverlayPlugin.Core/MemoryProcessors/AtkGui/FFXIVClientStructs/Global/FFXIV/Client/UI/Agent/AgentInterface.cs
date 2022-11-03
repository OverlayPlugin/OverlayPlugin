﻿using System.Runtime.InteropServices;
using RainbowMage.OverlayPlugin.MemoryProcessors.AtkStage.FFXIVClientStructs.Global.FFXIV.Client.UI;
namespace RainbowMage.OverlayPlugin.MemoryProcessors.AtkStage.FFXIVClientStructs.Global.FFXIV.Component.GUI
{
    // Client::UI::Agent::AgentInterface
    //   Component::GUI::AtkModuleInterface::AtkEventInterface

    // size = 0x8
    // ctor E8 ? ? ? ? F6 C3 01 74 0D BA ? ? ? ? 48 8B CF E8 ? ? ? ? 48 8B C7 48 8B 5C 24 ? 48 83 C4 20 5F C3 CC 48 89 5C 24 ? 48 89 6C 24 ? 
    [StructLayout(LayoutKind.Explicit, Size = 0x28)]
    public unsafe struct AgentInterface
    {
        [FieldOffset(0x0)] public AtkEventInterface AtkEventInterface;
        [FieldOffset(0x10)] public UIModule* UiModule;
        [FieldOffset(0x20)] public uint AddonId;






    }
}