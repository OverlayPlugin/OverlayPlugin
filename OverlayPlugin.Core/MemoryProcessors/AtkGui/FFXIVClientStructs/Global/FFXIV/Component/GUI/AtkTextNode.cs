﻿using System;
using System.Runtime.InteropServices;
using System.Text;
using RainbowMage.OverlayPlugin.MemoryProcessors.AtkStage.FFXIVClientStructs.Global.FFXIV.Client.Graphics;
using RainbowMage.OverlayPlugin.MemoryProcessors.AtkStage.FFXIVClientStructs.Global.FFXIV.Client.System.String;
namespace RainbowMage.OverlayPlugin.MemoryProcessors.AtkStage.FFXIVClientStructs.Global.FFXIV.Component.GUI
{

    [Flags]
    public enum TextFlags
    {
        AutoAdjustNodeSize = 0x01,
        Bold = 0x02,
        Italic = 0x04,
        Edge = 0x08,
        Glare = 0x10,
        Emboss = 0x20,
        WordWrap = 0x40,
        MultiLine = 0x80
    }

    [Flags]
    public enum TextFlags2
    {
        Ellipsis = 0x04
    }

    public enum FontType : byte
    {
        Axis = 0x0,
        MiedingerMed = 0x1,
        Miedinger = 0x2,
        TrumpGothic = 0x3,
        Jupiter = 0x4,
        JupiterLarge = 0x5,
    }

    // Component::GUI::AtkTextNode
    //   Component::GUI::AtkResNode
    //     Component::GUI::AtkEventTarget

    // simple text node

    // size = 0x158
    // common CreateAtkNode function E8 ? ? ? ? 48 8B 4E 08 49 8B D5 
    // type 3
    [StructLayout(LayoutKind.Explicit, Size = 0x158)]
    public unsafe struct AtkTextNode
    {
        [FieldOffset(0x0)] public AtkResNode AtkResNode;
        [FieldOffset(0xA8)] public uint TextId;
        [FieldOffset(0xAC)] public ByteColor TextColor;
        [FieldOffset(0xB0)] public ByteColor EdgeColor;
        [FieldOffset(0xB4)] public ByteColor BackgroundColor;

        [FieldOffset(0xB8)] public Utf8String NodeText;

        [FieldOffset(0x128)] public void* UnkPtr_1;

        // if text is "asdf" and you selected "sd" this is 2, 3
        [FieldOffset(0x138)] public uint SelectStart;
        [FieldOffset(0x13C)] public uint SelectEnd;
        [FieldOffset(0x14A)] public byte LineSpacing;

        [FieldOffset(0x14B)] public byte CharSpacing;

        // alignment bits 0-3 font type bits 4-7
        [FieldOffset(0x14C)] public byte AlignmentFontType;
        [FieldOffset(0x14D)] public byte FontSize;
        [FieldOffset(0x14E)] public byte SheetType;
        [FieldOffset(0x150)] public ushort FontCacheHandle;
        [FieldOffset(0x152)] public byte TextFlags;
        [FieldOffset(0x153)] public byte TextFlags2;











    }
}