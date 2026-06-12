using System;
using System.Runtime.InteropServices;
using Newtonsoft.Json;

namespace RainbowMage.OverlayPlugin.MemoryProcessors.JobGauge
{
    partial class JobGaugeMemory655 : JobGaugeMemory, IJobGaugeMemory655
    {
        // Due to lack of multi-version support in FFXIVClientStructs, we need to duplicate these structures here per-version
        // We use FFXIVClientStructs versions of the structs because they have more required details than FFXIV_ACT_Plugin's struct definitions
        #region FFXIVClientStructs structs
        [StructLayout(LayoutKind.Explicit, Size = 0x60)]
        public unsafe partial struct JobGaugeManager
        {
            [JsonIgnore]
            [FieldOffset(0x00)] public JobGauge* CurrentGauge;

            [FieldOffset(0x08)] public JobGauge EmptyGauge;

            [FieldOffset(0x08)] public WhiteMageGauge WhiteMage;
            [FieldOffset(0x08)] public ScholarGauge Scholar;
            [FieldOffset(0x08)] public AstrologianGauge Astrologian;
            [FieldOffset(0x08)] public SageGauge Sage;

            [FieldOffset(0x08)] public BardGauge Bard;
            [FieldOffset(0x08)] public MachinistGauge Machinist;
            [FieldOffset(0x08)] public DancerGauge Dancer;

            [FieldOffset(0x08)] public BlackMageGauge BlackMage;
            [FieldOffset(0x08)] public SummonerGauge Summoner;
            [FieldOffset(0x08)] public RedMageGauge RedMage;

            [FieldOffset(0x08)] public MonkGauge Monk;
            [FieldOffset(0x08)] public DragoonGauge Dragoon;
            [FieldOffset(0x08)] public NinjaGauge Ninja;
            [FieldOffset(0x08)] public SamuraiGauge Samurai;
            [FieldOffset(0x08)] public ReaperGauge Reaper;

            [FieldOffset(0x08)] public DarkKnightGauge DarkKnight;
            [FieldOffset(0x08)] public PaladinGauge Paladin;
            [FieldOffset(0x08)] public WarriorGauge Warrior;
            [FieldOffset(0x08)] public GunbreakerGauge Gunbreaker;

            [JsonIgnore]
            [FieldOffset(0x10)] public fixed byte RawGaugeData[8];

            [FieldOffset(0x58)] public byte ClassJobID;

            public byte[] GetRawGaugeData => new byte[] {
                RawGaugeData[0], RawGaugeData[1], RawGaugeData[2], RawGaugeData[3],
                RawGaugeData[4], RawGaugeData[5], RawGaugeData[6], RawGaugeData[7]
            };
        }

        [StructLayout(LayoutKind.Explicit, Size = 0x08)]
        public struct JobGauge : IBaseJobGauge
        {
            // empty base class for other gauges, this only has the vtable
        }

        #region Healer

        [StructLayout(LayoutKind.Explicit, Size = 0x10)]
        public struct WhiteMageGauge : IBaseJobGauge
        {
            [FieldOffset(0x0A)] public short LilyTimer;
            [FieldOffset(0x0C)] public byte Lily;
            [FieldOffset(0x0D)] public byte BloodLily;
        }

        [StructLayout(LayoutKind.Explicit, Size = 0x10)]
        public struct ScholarGauge : IBaseJobGauge
        {
            [FieldOffset(0x08)] public byte Aetherflow;
            [FieldOffset(0x09)] public byte FairyGauge;
            [FieldOffset(0x0A)] public short SeraphTimer;
            [FieldOffset(0x0C)] public byte DismissedFairy;
        }

        [StructLayout(LayoutKind.Explicit, Size = 0x10)]
        public unsafe struct AstrologianGauge : IBaseJobGauge
        {
            [FieldOffset(0x08)] public short Timer;
            [FieldOffset(0x0D)] public byte Card;
            [FieldOffset(0x0E)] public byte Seals; // 6 bits, 0,1-3,1-3,1-3 depending on astrosign

            public AstrologianCard CurrentCard => (AstrologianCard)Card;

            public AstrologianSeal[] CurrentSeals => new[]
            {
                (AstrologianSeal)(3 & (this.Seals >> 0)),
                (AstrologianSeal)(3 & (this.Seals >> 2)),
                (AstrologianSeal)(3 & (this.Seals >> 4)),
            };
        }

        [StructLayout(LayoutKind.Explicit, Size = 0x10)]
        public struct SageGauge : IBaseJobGauge
        {
            [FieldOffset(0x08)] public short AddersgallTimer;
            [FieldOffset(0x0A)] public byte Addersgall;
            [FieldOffset(0x0B)] public byte Addersting;
            [FieldOffset(0x0C)] public byte Eukrasia;

            public bool EukrasiaActive => Eukrasia > 0;
        }

        #endregion

        #region MagicDPS

        [StructLayout(LayoutKind.Explicit, Size = 0x30)]
        public struct BlackMageGauge : IBaseJobGauge
        {
            [FieldOffset(0x08)] public short EnochianTimer;
            [FieldOffset(0x0A)] public short ElementTimeRemaining;
            [FieldOffset(0x0C)] public sbyte ElementStance;
            [FieldOffset(0x0D)] public byte UmbralHearts;
            [FieldOffset(0x0E)] public byte PolyglotStacks;
            [FieldOffset(0x0F)] public EnochianFlags EnochianFlags;

            public int UmbralStacks => ElementStance >= 0 ? 0 : ElementStance * -1;
            public int AstralStacks => ElementStance <= 0 ? 0 : ElementStance;
            public bool EnochianActive => EnochianFlags.HasFlag(EnochianFlags.Enochian);
            public bool ParadoxActive => EnochianFlags.HasFlag(EnochianFlags.Paradox);
        }

        [StructLayout(LayoutKind.Explicit, Size = 0x10)]
        public struct SummonerGauge : IBaseJobGauge
        {
            [FieldOffset(0x8)] public ushort SummonTimer; // millis counting down
            [FieldOffset(0xA)] public ushort AttunementTimer; // millis counting down
            [FieldOffset(0xC)] public byte ReturnSummon; // Pet sheet (23=Carbuncle, the only option now)
            [FieldOffset(0xD)] public byte ReturnSummonGlam; // PetMirage sheet
            [FieldOffset(0xE)] public byte Attunement; // Count of "Attunement cost" resource
            [FieldOffset(0xF)] public AetherFlags AetherFlags; // bitfield
        }

        [StructLayout(LayoutKind.Explicit, Size = 0x50)]
        public struct RedMageGauge : IBaseJobGauge
        {
            [FieldOffset(0x08)] public byte WhiteMana;
            [FieldOffset(0x09)] public byte BlackMana;
            [FieldOffset(0x0A)] public byte ManaStacks;
        }

        #endregion

        #region RangeDPS

        [StructLayout(LayoutKind.Explicit, Size = 0x10)]
        public struct BardGauge : IBaseJobGauge
        {
            [FieldOffset(0x08)] public ushort SongTimer;
            [FieldOffset(0x0C)] public byte Repertoire;
            [FieldOffset(0x0D)] public byte SoulVoice;
            [FieldOffset(0x0E)] public SongFlags SongFlags; // bitfield
        }

        [StructLayout(LayoutKind.Explicit, Size = 0x10)]
        public struct MachinistGauge : IBaseJobGauge
        {
            [FieldOffset(0x08)] public short OverheatTimeRemaining;
            [FieldOffset(0x0A)] public short SummonTimeRemaining;
            [FieldOffset(0x0C)] public byte Heat;
            [FieldOffset(0x0D)] public byte Battery;
            [FieldOffset(0x0E)] public byte LastSummonBatteryPower;
            [FieldOffset(0x0F)] public byte TimerActive;
        }

        [StructLayout(LayoutKind.Explicit, Size = 0x10)]
        public unsafe struct DancerGauge : IBaseJobGauge
        {
            [FieldOffset(0x08)] public byte Feathers;
            [FieldOffset(0x09)] public byte Esprit;
            [FieldOffset(0x0A)] public fixed byte DanceSteps[4];
            [FieldOffset(0x0E)] public byte StepIndex;

            public DanceStep CurrentStep => (DanceStep)(StepIndex >= 4 ? 0 : DanceSteps[StepIndex]);
        }

        #endregion

        #region MeleeDPS

        [StructLayout(LayoutKind.Explicit, Size = 0x10)]
        public struct MonkGauge : IBaseJobGauge
        {
            [FieldOffset(0x08)] public byte Chakra; // Chakra count

            [FieldOffset(0x09)]
            public BeastChakraType BeastChakra1; // CoeurlChakra = 1, RaptorChakra = 2, OpoopoChakra = 3 (only one value)

            [FieldOffset(0x0A)]
            public BeastChakraType BeastChakra2; // CoeurlChakra = 1, RaptorChakra = 2, OpoopoChakra = 3 (only one value)

            [FieldOffset(0x0B)]
            public BeastChakraType BeastChakra3; // CoeurlChakra = 1, RaptorChakra = 2, OpoopoChakra = 3 (only one value)

            [FieldOffset(0x0C)] public NadiFlags Nadi; // LunarNadi = 2, SolarNadi = 4 (If both then 2+4=6)
            [FieldOffset(0x0E)] public ushort BlitzTimeRemaining; // 20 seconds

            public BeastChakraType[] BeastChakra => new[] { BeastChakra1, BeastChakra2, BeastChakra3 };
        }

        [StructLayout(LayoutKind.Explicit, Size = 0x10)]
        public struct DragoonGauge : IBaseJobGauge
        {
            [FieldOffset(0x08)] public short LotdTimer;
            [FieldOffset(0x0A)] public byte LotdState; // This seems to only ever be 0 or 2 now
            [FieldOffset(0x0B)] public byte EyeCount;
            [FieldOffset(0x0C)] public byte FirstmindsFocusCount;
        }

        [StructLayout(LayoutKind.Explicit, Size = 0x10)]
        public struct NinjaGauge : IBaseJobGauge
        {
            [FieldOffset(0x08)] public ushort HutonTimer;
            [FieldOffset(0x0A)] public byte Ninki;
            [FieldOffset(0x0B)] public byte HutonManualCasts;
        }

        [StructLayout(LayoutKind.Explicit, Size = 0x10)]
        public struct SamuraiGauge : IBaseJobGauge
        {
            [FieldOffset(0x0A)] public KaeshiAction Kaeshi;
            [FieldOffset(0x0B)] public byte Kenki;
            [FieldOffset(0x0C)] public byte MeditationStacks;
            [FieldOffset(0x0D)] public SenFlags SenFlags;
        }

        [StructLayout(LayoutKind.Explicit, Size = 0x10)]
        public struct ReaperGauge : IBaseJobGauge
        {
            [FieldOffset(0x08)] public byte Soul;
            [FieldOffset(0x09)] public byte Shroud;
            [FieldOffset(0x0A)] public ushort EnshroudedTimeRemaining;
            [FieldOffset(0x0C)] public byte LemureShroud;
            [FieldOffset(0x0D)] public byte VoidShroud;
        }

        #endregion

        #region Tanks

        [StructLayout(LayoutKind.Explicit, Size = 0x10)]
        public struct DarkKnightGauge : IBaseJobGauge
        {
            [FieldOffset(0x08)] public byte Blood;
            [FieldOffset(0x0A)] public ushort DarksideTimer;
            [FieldOffset(0x0C)] public byte DarkArtsState;
            [FieldOffset(0x0E)] public ushort ShadowTimer;
        }

        [StructLayout(LayoutKind.Explicit, Size = 0x10)]
        public struct PaladinGauge : IBaseJobGauge
        {
            [FieldOffset(0x08)] public byte OathGauge;
        }

        [StructLayout(LayoutKind.Explicit, Size = 0x10)]
        public struct WarriorGauge : IBaseJobGauge
        {
            [FieldOffset(0x08)] public byte BeastGauge;
        }

        [StructLayout(LayoutKind.Explicit, Size = 0x10)]
        public struct GunbreakerGauge : IBaseJobGauge
        {
            [FieldOffset(0x08)] public byte Ammo;
            [FieldOffset(0x0A)] public short MaxTimerDuration;
            [FieldOffset(0x0C)] public byte AmmoComboStep;
        }

        #endregion

        #region FFXIVClientStructs Flags

        public enum AstrologianCard
        {
            None = 0,
            Balance = 1,
            Bole = 2,
            Arrow = 3,
            Spear = 4,
            Ewer = 5,
            Spire = 6,
            Lord = 0x70,
            Lady = 0x80
        }

        public enum AstrologianSeal
        {
            Solar = 1,
            Lunar = 2,
            Celestial = 3
        }

        public enum DanceStep : byte
        {
            Finish = 0,
            Emboite = 1,
            Entrechat = 2,
            Jete = 3,
            Pirouette = 4
        }

        [Flags]
        public enum EnochianFlags : byte
        {
            None = 0,
            Enochian = 1,
            Paradox = 2
        }

        public enum KaeshiAction : byte
        {
            Higanbana = 1,
            Goken = 2,
            Setsugekka = 3,
            Namikiri = 4
        }

        [Flags]
        public enum SenFlags : byte
        {
            None = 0,
            Setsu = 1 << 0,
            Getsu = 1 << 1,
            Ka = 1 << 2
        }

        [Flags]
        public enum SongFlags : byte
        {
            None = 0,
            MagesBallad = 1 << 0,
            ArmysPaeon = 1 << 1,
            WanderersMinuet = MagesBallad | ArmysPaeon,
            MagesBalladLastPlayed = 1 << 2,
            ArmysPaeonLastPlayed = 1 << 3,
            WanderersMinuetLastPlayed = MagesBalladLastPlayed | ArmysPaeonLastPlayed,
            MagesBalladCoda = 1 << 4,
            ArmysPaeonCoda = 1 << 5,
            WanderersMinuetCoda = 1 << 6
        }

        [Flags]
        public enum AetherFlags : byte
        {
            None = 0,
            Aetherflow1 = 1 << 0,
            Aetherflow2 = 1 << 1,
            Aetherflow = Aetherflow1 | Aetherflow2,
            IfritAttuned = 1 << 2,
            TitanAttuned = 1 << 3,
            GarudaAttuned = TitanAttuned | IfritAttuned,
            PhoenixReady = 1 << 4,
            IfritReady = 1 << 5,
            TitanReady = 1 << 6,
            GarudaReady = 1 << 7
        }

        public enum BeastChakraType : byte
        {
            None = 0,
            Coeurl = 1,
            OpoOpo = 2,
            Raptor = 3
        }

        [Flags]
        public enum NadiFlags : byte
        {
            Lunar = 2,
            Solar = 4
        }
        #endregion

        #endregion
    }
}
