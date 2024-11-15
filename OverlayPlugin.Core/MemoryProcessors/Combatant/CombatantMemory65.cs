using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace RainbowMage.OverlayPlugin.MemoryProcessors.Combatant
{
    interface ICombatantMemory65 : ICombatantMemory { }

    class CombatantMemory65 : CombatantMemory, ICombatantMemory65
    {
        private const string charmapSignature = "488B5720B8000000E0483BD00F84????????488D0D";

        public CombatantMemory65(TinyIoCContainer container)
            : base(container, charmapSignature, CombatantMemory.Size, EffectMemory.Size)
        {

        }

        public override Version GetVersion()
        {
            return new Version(6, 5);
        }

        // Returns a combatant if the combatant is a mob or a PC.
        protected override unsafe Combatant GetMobFromByteArray(byte[] source, uint mycharID)
        {
            fixed (byte* p = source)
            {
                CombatantMemory mem = *(CombatantMemory*)&p[0];
                ObjectType type = (ObjectType)mem.Type;
                if (mem.ID == 0 || mem.ID == emptyID)
                    return null;
            }
            return GetCombatantFromByteArray(source, mycharID, false);
        }

        // Will return any kind of combatant, even if not a mob.
        // This function always returns a combatant object, even if empty.
        protected override unsafe Combatant GetCombatantFromByteArray(byte[] source, uint mycharID, bool isPlayer, bool exceptEffects = false)
        {
            fixed (byte* p = source)
            {
                CombatantMemory mem = *(CombatantMemory*)&p[0];

                if (isPlayer)
                {
                    mycharID = mem.ID;
                }

                Combatant combatant = new Combatant()
                {
                    Name = FFXIVMemory.GetStringFromBytes(mem.Name, CombatantMemory.NameBytes),
                    ID = mem.ID,
                    OwnerID = mem.OwnerID == emptyID ? 0 : mem.OwnerID,
                    Type = (ObjectType)mem.Type,
                    Status = (ObjectStatus)mem.Status,
                    RawEffectiveDistance = mem.EffectiveDistance,
                    PosX = mem.PosX,
                    // Y and Z are deliberately swapped to match FFXIV_ACT_Plugin's data model
                    PosY = mem.PosZ,
                    PosZ = mem.PosY,
                    Heading = mem.Heading,
                    Radius = mem.Radius,
                    BNpcID = mem.BNpcID,
                    Address = (IntPtr)p,
                };

                if (combatant.Type == ObjectType.PC || combatant.Type == ObjectType.Monster ||
                    combatant.Type == ObjectType.NPC || combatant.Type == ObjectType.Retainer)
                {
                    // The following fields are only valid for these types above,
                    // while other types have garbage memory.
                    combatant.Job = mem.Job;
                    combatant.Level = mem.Level;
                    combatant.CurrentHP = mem.CurrentHP;
                    combatant.MaxHP = mem.MaxHP;
                    combatant.CurrentMP = mem.CurrentMP;
                    combatant.MaxMP = mem.MaxMP;
                    combatant.CurrentCP = mem.CurrentCP;
                    combatant.MaxCP = mem.MaxCP;
                    combatant.CurrentGP = mem.CurrentGP;
                    combatant.MaxGP = mem.MaxGP;
                    combatant.MonsterType = (MonsterType)mem.MonsterType;
                    combatant.IsEnemy = mem.MonsterType != 0;
                    // Convert to the previous definition of AggressionStatus
                    combatant.AggressionStatus = (AggressionStatus)(mem.AggressionStatus / 16 % 4);
                    combatant.PartyType = mem.PartyType;
                    combatant.InParty = (mem.PartyType & 0x8) != 0;
                    combatant.InAlliance = (mem.PartyType & 0x10) != 0;
                    combatant.IsFriend = (mem.PartyType & 0x20) != 0;
                    combatant.BNpcNameID = mem.BNpcNameID;
                    combatant.TransformationId = mem.TransformationId;
                    combatant.WeaponId = mem.WeaponId;
                    combatant.ModelStatus = (ModelStatus)mem.ModelStatus;
                    // In-memory there are separate values for PC's current target and NPC's current target
                    combatant.PCTargetID = mem.PCTargetID;
                    combatant.NPCTargetID = mem.NPCTargetID;
                    combatant.TargetID = ((ObjectType)mem.Type == ObjectType.PC || (ObjectType)mem.Type == ObjectType.Retainer) ? mem.PCTargetID : mem.NPCTargetID;
                    combatant.IsTargetable = (combatant.ModelStatus == ModelStatus.Visible)
                        && ((combatant.Status == ObjectStatus.NormalActorStatus) || (combatant.Status == ObjectStatus.NormalSubActorStatus));
                    combatant.Effects = exceptEffects ? new List<EffectEntry>() : GetEffectEntries(mem.Effects, (ObjectType)mem.Type, mycharID);
                    combatant.IsCasting1 = mem.IsCasting1;
                    combatant.IsCasting2 = mem.IsCasting2;
                    combatant.CastBuffID = mem.CastBuffID;
                    combatant.CastTargetID = mem.CastTargetID;
                    // Y and Z are deliberately swapped to match FFXIV_ACT_Plugin's data model
                    combatant.CastGroundTargetX = mem.CastGroundTargetX;
                    combatant.CastGroundTargetY = mem.CastGroundTargetZ;
                    combatant.CastGroundTargetZ = mem.CastGroundTargetY;
                    combatant.CastDurationCurrent = mem.CastDurationCurrent;
                    combatant.CastDurationMax = mem.CastDurationMax;
                }
                return combatant;
            }
        }

        [StructLayout(LayoutKind.Explicit)]
        private unsafe struct CombatantMemory
        {
            public static int Size => Marshal.SizeOf(typeof(CombatantMemory));

            // 64 bytes per both FFXIV_ACT_Plugin and aers/FFXIVClientStructs
            public const int NameBytes = 64;

            public const int EffectCount = 60;
            public const int EffectBytes = EffectMemory.Size * EffectCount;

            [FieldOffset(0x30)]
            public fixed byte Name[NameBytes];

            [FieldOffset(0x74)]
            public uint ID;

            [FieldOffset(0x80)]
            public uint BNpcID;

            [FieldOffset(0x84)]
            public uint OwnerID;

            [FieldOffset(0x8C)]
            public byte Type;

            [FieldOffset(0x92)]
            public byte EffectiveDistance;

            [FieldOffset(0x94)]
            public byte Status;

            [FieldOffset(0xB0)]
            public Single PosX;

            [FieldOffset(0xB4)]
            public Single PosY;

            [FieldOffset(0xB8)]
            public Single PosZ;

            [FieldOffset(0xC0)]
            public Single Heading;

            [FieldOffset(0xD0)]
            public Single Radius;

            // All combatants have the first 0x1A0 bytes (`GameObject` per FFXIVClientStructs)
            // Fields below this point are only valid for PCs, Monsters, NPCs (mainly empty), and Retainers.

            [FieldOffset(0x114)]
            public int ModelStatus;

            [FieldOffset(0x1BC)]
            public int CurrentHP;

            [FieldOffset(0x1C0)]
            public int MaxHP;

            [FieldOffset(0x1C4)]
            public int CurrentMP;

            [FieldOffset(0x1C8)]
            public int MaxMP;

            [FieldOffset(0x1CC)]
            public ushort CurrentGP;

            [FieldOffset(0x1CE)]
            public ushort MaxGP;

            [FieldOffset(0x1D0)]
            public ushort CurrentCP;

            [FieldOffset(0x1D2)]
            public ushort MaxCP;

            [FieldOffset(0x1D4)]
            public short TransformationId;

            [FieldOffset(0x1DA)]
            public byte Job;

            [FieldOffset(0x1DB)]
            public byte Level;

            [FieldOffset(0x1E9)]
            public byte MonsterType;

            [FieldOffset(0x1EB)]
            public byte AggressionStatus;

            [FieldOffset(0x1EC)]
            public byte PartyType;

            [FieldOffset(0xC30)]
            public byte WeaponId;

            [FieldOffset(0xD00)]
            public uint PCTargetID;

            [FieldOffset(0x1B58)]
            public uint NPCTargetID;

            [FieldOffset(0x1B98)]
            public uint BNpcNameID;

            [FieldOffset(0x1BB0)]
            public ushort CurrentWorldID;

            [FieldOffset(0x1BB2)]
            public ushort WorldID;

            [FieldOffset(0x1C18)]
            public fixed byte Effects[EffectBytes];

            [FieldOffset(0x1F00)]
            public byte IsCasting1;

            [FieldOffset(0x1F02)]
            public byte IsCasting2;

            [FieldOffset(0x1F04)]
            public uint CastBuffID;

            [FieldOffset(0x1F10)]
            public uint CastTargetID;

            [FieldOffset(0x1F20)]
            public float CastGroundTargetX;

            [FieldOffset(0x1F24)]
            public float CastGroundTargetY;

            [FieldOffset(0x1F28)]
            public float CastGroundTargetZ;

            [FieldOffset(0x1F34)]
            public float CastDurationCurrent;

            [FieldOffset(0x1F38)]
            public float CastDurationMax;
        }
    }
}
