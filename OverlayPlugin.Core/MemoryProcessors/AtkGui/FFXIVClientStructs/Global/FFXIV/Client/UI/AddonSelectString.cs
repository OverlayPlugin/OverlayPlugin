﻿using System.Runtime.InteropServices;
using RainbowMage.OverlayPlugin.MemoryProcessors.AtkStage.FFXIVClientStructs.Global.FFXIV.Component.GUI;
namespace RainbowMage.OverlayPlugin.MemoryProcessors.AtkStage.FFXIVClientStructs.Global.FFXIV.Client.UI
{

    // Client::UI::AddonSelectString
    //   Component::GUI::AtkUnitBase
    //     Component::GUI::AtkEventListener
    [StructLayout(LayoutKind.Explicit, Size = 0x2A0)]
    public struct AddonSelectString
    {
        [FieldOffset(0x0)] public AtkUnitBase AtkUnitBase;
        [FieldOffset(0x220)] public PopupMenuDerive PopupMenu;

        [StructLayout(LayoutKind.Explicit, Size = 0x68)]
        public struct PopupMenuDerive
        {
            [FieldOffset(0x0)] public PopupMenu PopupMenu;
        }
    }
}