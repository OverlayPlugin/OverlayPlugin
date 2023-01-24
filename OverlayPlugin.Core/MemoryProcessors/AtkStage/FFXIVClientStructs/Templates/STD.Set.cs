using System.Diagnostics;

using System;
using System.Runtime.InteropServices;
using FFXIVClientStructs.Global.STD;
using FFXIVClientStructs.Global.FFXIV.Client.Graphics;
using FFXIVClientStructs.Global.FFXIV.Common.Math;
namespace FFXIVClientStructs.Global.STD { 

[StructLayout(LayoutKind.Sequential, Size = 0x10)]
public unsafe struct StdSet{
	public void* Head;
	public ulong Count;
}}


