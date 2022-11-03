﻿using System.Runtime.InteropServices;
using RainbowMage.OverlayPlugin.MemoryProcessors.AtkStage.FFXIVClientStructs.Global.FFXIV.Client.Game.Object;
using RainbowMage.OverlayPlugin.MemoryProcessors.AtkStage.FFXIVClientStructs.Global.FFXIV.Client.System.Framework;
using RainbowMage.OverlayPlugin.MemoryProcessors.AtkStage.FFXIVClientStructs.Global.FFXIV.Client.System.String;
using RainbowMage.OverlayPlugin.MemoryProcessors.AtkStage.FFXIVClientStructs.Global.FFXIV.Component.GUI;
namespace RainbowMage.OverlayPlugin.MemoryProcessors.AtkStage.FFXIVClientStructs.Global.FFXIV.Client.UI.Agent
{

    // Client::UI::Agent::AgentCharaCard
    //   Client::UI::Agent::AgentInterface
    //     Component::GUI::AtkModuleInterface::AtkEventInterface
    [StructLayout(LayoutKind.Explicit, Size = 0x38)]
    public unsafe struct AgentCharaCard
    {
        [FieldOffset(0x0)] public AgentInterface AgentInterface;

        [FieldOffset(0x28)] public Storage* Data;

        // Client::UI::Agent::AgentCharaCard::Storage
        // ctor E8 ?? ?? ?? ?? 48 8B F0 48 89 73 ?? C6 06
        // dtor E8 ?? ?? ?? ?? BA ?? ?? ?? ?? 48 8B CF E8 ?? ?? ?? ?? 48 89 73 ?? E8 
        [StructLayout(LayoutKind.Explicit, Size = 0x920)]
        public struct Storage
        {
            [FieldOffset(0x58)] public Utf8String Name;
            [FieldOffset(0xD8)] public Utf8String FreeCompany;
            [FieldOffset(0x140)] public Utf8String UnkString1;
            [FieldOffset(0x1A8)] public Utf8String UnkString2;

            [FieldOffset(0x250)] public uint Activity1IconId;
            [FieldOffset(0x258)] public Utf8String Activity1Name;
            [FieldOffset(0x2C0)] public uint Activity2IconId;
            [FieldOffset(0x2C8)] public Utf8String Activity2Name;
            [FieldOffset(0x330)] public uint Activity3IconId;
            [FieldOffset(0x338)] public Utf8String Activity3Name;
            [FieldOffset(0x3A0)] public uint Activity4IconId;
            [FieldOffset(0x3A8)] public Utf8String Activity4Name;
            [FieldOffset(0x410)] public uint Activity5IconId;
            [FieldOffset(0x418)] public Utf8String Activity5Name;
            [FieldOffset(0x480)] public uint Activity6IconId;
            [FieldOffset(0x488)] public Utf8String Activity6Name;

            [FieldOffset(0x540)] public void* CharaView; // size >= 0x390
        }
    }
}