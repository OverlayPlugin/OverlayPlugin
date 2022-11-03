﻿using System.Runtime.InteropServices;
using RainbowMage.OverlayPlugin.MemoryProcessors.AtkStage.FFXIVClientStructs.Global.FFXIV.Client.Game;
using RainbowMage.OverlayPlugin.MemoryProcessors.AtkStage.FFXIVClientStructs.Global.FFXIV.Client.Game.Character;
using RainbowMage.OverlayPlugin.MemoryProcessors.AtkStage.FFXIVClientStructs.Global.FFXIV.Client.Game.Object;
using RainbowMage.OverlayPlugin.MemoryProcessors.AtkStage.FFXIVClientStructs.Global.FFXIV.Client.System.String;
using RainbowMage.OverlayPlugin.MemoryProcessors.AtkStage.FFXIVClientStructs.Global.FFXIV.Component.GUI;
using RainbowMage.OverlayPlugin.MemoryProcessors.AtkStage.FFXIVClientStructs.Global.STD;
namespace RainbowMage.OverlayPlugin.MemoryProcessors.AtkStage.FFXIVClientStructs.Global.FFXIV.Client.UI.Agent
{

    [StructLayout(LayoutKind.Explicit, Size = 0xF0)]
    public unsafe struct BalloonInfo
    {
        [FieldOffset(0x0)] public Utf8String FormattedText; // Contains breaks for newlines
        [FieldOffset(0x68)] public Utf8String OriginalText;
        [FieldOffset(0xD0)] public GameObjectID ObjectId;

        [FieldOffset(0xD8)]
        public Character*
            Character; // this keeps getting changed between null and the character pointer so isn't entirely reliable

        [FieldOffset(0xE0)] public float CameraDistance;
        [FieldOffset(0xE4)] public int BalloonId; // matches BalloonCounter when the balloon is made
        [FieldOffset(0xE8)] public BalloonType Type;
        [FieldOffset(0xE9)] public byte Slot;
        [FieldOffset(0xEA)] public byte UnknownByteEA;
    }

    // not sure how this maps to the addon yet, might just be in order though
    [StructLayout(LayoutKind.Explicit, Size = 0x8)]
    public struct BalloonSlot
    {
        [FieldOffset(0x0)] public int Id;
        [FieldOffset(0x4)] public byte Available; // bool
    }

    [StructLayout(LayoutKind.Explicit, Size = 0x3F0)]
    public unsafe struct AgentScreenLog
    {
        [FieldOffset(0x0)] public AgentInterface AgentInterface;

        [FieldOffset(0x350)] public StdDeque BalloonQueue;

        [FieldOffset(0x378)]
        public byte BalloonsHaveUpdate; // bool used to know if any balloons have been added/changed since last frame update

        [FieldOffset(0x37C)]
        public int BalloonCounter; // count of all balloons since game launch, used as unique balloon ID

        [FieldOffset(0x390)] public fixed byte BalloonSlots[10 * 0x8]; // type BalloonSlot array
    }
}