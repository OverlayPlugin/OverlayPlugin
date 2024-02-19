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
        public struct JobGauge : BaseJobGauge
        {
            // empty base class for other gauges, this only has the vtable
        }

        #region Healer

        [StructLayout(LayoutKind.Explicit, Size = 0x10)]
        public struct WhiteMageGauge : BaseWhiteMageGauge
        {
            [FieldOffset(0x0A)] public short LilyTimer;
            [FieldOffset(0x0C)] public byte Lily;
            [FieldOffset(0x0D)] public byte BloodLily;

            short BaseWhiteMageGauge.LilyTimer => LilyTimer;

            byte BaseWhiteMageGauge.Lily => Lily;

            byte BaseWhiteMageGauge.BloodLily => BloodLily;
        }

        [StructLayout(LayoutKind.Explicit, Size = 0x10)]
        public struct ScholarGauge : BaseScholarGauge
        {
            [FieldOffset(0x08)] public byte Aetherflow;
            [FieldOffset(0x09)] public byte FairyGauge;
            [FieldOffset(0x0A)] public short SeraphTimer;
            [FieldOffset(0x0C)] public byte DismissedFairy;

            byte BaseScholarGauge.Aetherflow => Aetherflow;

            byte BaseScholarGauge.FairyGauge => FairyGauge;

            short BaseScholarGauge.SeraphTimer => SeraphTimer;

            byte BaseScholarGauge.DismissedFairy => DismissedFairy;
        }

        [StructLayout(LayoutKind.Explicit, Size = 0x10)]
        public unsafe struct AstrologianGauge : BaseAstrologianGauge
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

            short BaseAstrologianGauge.Timer => Timer;

            byte BaseAstrologianGauge.Card => Card;

            byte BaseAstrologianGauge.Seals => Seals;

            AstrologianCard BaseAstrologianGauge.CurrentCard => CurrentCard;

            AstrologianSeal[] BaseAstrologianGauge.CurrentSeals => CurrentSeals;
        }

        [StructLayout(LayoutKind.Explicit, Size = 0x10)]
        public struct SageGauge : BaseSageGauge
        {
            [FieldOffset(0x08)] public short AddersgallTimer;
            [FieldOffset(0x0A)] public byte Addersgall;
            [FieldOffset(0x0B)] public byte Addersting;
            [FieldOffset(0x0C)] public byte Eukrasia;

            public bool EukrasiaActive => Eukrasia > 0;

            short BaseSageGauge.AddersgallTimer => AddersgallTimer;

            byte BaseSageGauge.Addersgall => Addersgall;

            byte BaseSageGauge.Addersting => Addersting;

            byte BaseSageGauge.Eukrasia => Eukrasia;
        }

        #endregion

        #region MagicDPS

        [StructLayout(LayoutKind.Explicit, Size = 0x30)]
        public struct BlackMageGauge : BaseBlackMageGauge
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

            short BaseBlackMageGauge.EnochianTimer => EnochianTimer;

            short BaseBlackMageGauge.ElementTimeRemaining => ElementTimeRemaining;

            sbyte BaseBlackMageGauge.ElementStance => ElementStance;

            byte BaseBlackMageGauge.UmbralHearts => UmbralHearts;

            byte BaseBlackMageGauge.PolyglotStacks => PolyglotStacks;

            EnochianFlags BaseBlackMageGauge.EnochianFlags => EnochianFlags;
        }

        [StructLayout(LayoutKind.Explicit, Size = 0x10)]
        public struct SummonerGauge : BaseSummonerGauge
        {
            [FieldOffset(0x8)] public ushort SummonTimer; // millis counting down
            [FieldOffset(0xA)] public ushort AttunementTimer; // millis counting down
            [FieldOffset(0xC)] public byte ReturnSummon; // Pet sheet (23=Carbuncle, the only option now)
            [FieldOffset(0xD)] public byte ReturnSummonGlam; // PetMirage sheet
            [FieldOffset(0xE)] public byte Attunement; // Count of "Attunement cost" resource
            [FieldOffset(0xF)] public AetherFlags AetherFlags; // bitfield

            ushort BaseSummonerGauge.SummonTimer => SummonTimer;

            ushort BaseSummonerGauge.AttunementTimer => AttunementTimer;

            byte BaseSummonerGauge.ReturnSummon => ReturnSummon;

            byte BaseSummonerGauge.ReturnSummonGlam => ReturnSummonGlam;

            byte BaseSummonerGauge.Attunement => Attunement;

            AetherFlags BaseSummonerGauge.AetherFlags => AetherFlags;
        }

        [StructLayout(LayoutKind.Explicit, Size = 0x50)]
        public struct RedMageGauge : BaseRedMageGauge
        {
            [FieldOffset(0x08)] public byte WhiteMana;
            [FieldOffset(0x09)] public byte BlackMana;
            [FieldOffset(0x0A)] public byte ManaStacks;

            byte BaseRedMageGauge.WhiteMana => WhiteMana;

            byte BaseRedMageGauge.BlackMana => BlackMana;

            byte BaseRedMageGauge.ManaStacks => ManaStacks;
        }

        #endregion

        #region RangeDPS

        [StructLayout(LayoutKind.Explicit, Size = 0x10)]
        public struct BardGauge : BaseBardGauge
        {
            [FieldOffset(0x08)] public ushort SongTimer;
            [FieldOffset(0x0C)] public byte Repertoire;
            [FieldOffset(0x0D)] public byte SoulVoice;
            [FieldOffset(0x0E)] public SongFlags SongFlags; // bitfield

            ushort BaseBardGauge.SongTimer => SongTimer;

            byte BaseBardGauge.Repertoire => Repertoire;

            byte BaseBardGauge.SoulVoice => SoulVoice;

            SongFlags BaseBardGauge.SongFlags => SongFlags;
        }

        [StructLayout(LayoutKind.Explicit, Size = 0x10)]
        public struct MachinistGauge : BaseMachinistGauge
        {
            [FieldOffset(0x08)] public short OverheatTimeRemaining;
            [FieldOffset(0x0A)] public short SummonTimeRemaining;
            [FieldOffset(0x0C)] public byte Heat;
            [FieldOffset(0x0D)] public byte Battery;
            [FieldOffset(0x0E)] public byte LastSummonBatteryPower;
            [FieldOffset(0x0F)] public byte TimerActive;

            short BaseMachinistGauge.OverheatTimeRemaining => OverheatTimeRemaining;

            short BaseMachinistGauge.SummonTimeRemaining => SummonTimeRemaining;

            byte BaseMachinistGauge.Heat => Heat;

            byte BaseMachinistGauge.Battery => Battery;

            byte BaseMachinistGauge.LastSummonBatteryPower => LastSummonBatteryPower;

            byte BaseMachinistGauge.TimerActive => TimerActive;
        }

        [StructLayout(LayoutKind.Explicit, Size = 0x10)]
        public unsafe struct DancerGauge : BaseDancerGauge
        {
            [FieldOffset(0x08)] public byte Feathers;
            [FieldOffset(0x09)] public byte Esprit;
            [FieldOffset(0x0A)] public fixed byte DanceSteps[4];
            [FieldOffset(0x0E)] public byte StepIndex;

            public DanceStep CurrentStep => (DanceStep)(StepIndex >= 4 ? 0 : DanceSteps[StepIndex]);

            byte BaseDancerGauge.Feathers => Feathers;

            byte BaseDancerGauge.Esprit => Esprit;

            byte[] BaseDancerGauge.DanceSteps => new byte[] { DanceSteps[0], DanceSteps[1], DanceSteps[2], DanceSteps[3] };

            byte BaseDancerGauge.StepIndex => StepIndex;
        }

        #endregion

        #region MeleeDPS

        [StructLayout(LayoutKind.Explicit, Size = 0x10)]
        public struct MonkGauge : BaseMonkGauge
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

            byte BaseMonkGauge.Chakra => Chakra;

            BeastChakraType BaseMonkGauge.BeastChakra1 => BeastChakra1;

            BeastChakraType BaseMonkGauge.BeastChakra2 => BeastChakra2;

            BeastChakraType BaseMonkGauge.BeastChakra3 => BeastChakra3;

            NadiFlags BaseMonkGauge.Nadi => Nadi;

            ushort BaseMonkGauge.BlitzTimeRemaining => BlitzTimeRemaining;
        }

        [StructLayout(LayoutKind.Explicit, Size = 0x10)]
        public struct DragoonGauge : BaseDragoonGauge
        {
            [FieldOffset(0x08)] public short LotdTimer;
            [FieldOffset(0x0A)] public byte LotdState; // This seems to only ever be 0 or 2 now
            [FieldOffset(0x0B)] public byte EyeCount;
            [FieldOffset(0x0C)] public byte FirstmindsFocusCount;

            short BaseDragoonGauge.LotdTimer => LotdTimer;

            byte BaseDragoonGauge.LotdState => LotdState;

            byte BaseDragoonGauge.EyeCount => EyeCount;

            byte BaseDragoonGauge.FirstmindsFocusCount => FirstmindsFocusCount;
        }

        [StructLayout(LayoutKind.Explicit, Size = 0x10)]
        public struct NinjaGauge : BaseNinjaGauge
        {
            [FieldOffset(0x08)] public ushort HutonTimer;
            [FieldOffset(0x0A)] public byte Ninki;
            [FieldOffset(0x0B)] public byte HutonManualCasts;

            ushort BaseNinjaGauge.HutonTimer => HutonTimer;

            byte BaseNinjaGauge.Ninki => Ninki;

            byte BaseNinjaGauge.HutonManualCasts => HutonManualCasts;
        }

        [StructLayout(LayoutKind.Explicit, Size = 0x10)]
        public struct SamuraiGauge : BaseSamuraiGauge
        {
            [FieldOffset(0x0A)] public KaeshiAction Kaeshi;
            [FieldOffset(0x0B)] public byte Kenki;
            [FieldOffset(0x0C)] public byte MeditationStacks;
            [FieldOffset(0x0D)] public SenFlags SenFlags;

            KaeshiAction BaseSamuraiGauge.Kaeshi => Kaeshi;

            byte BaseSamuraiGauge.Kenki => Kenki;

            byte BaseSamuraiGauge.MeditationStacks => MeditationStacks;

            SenFlags BaseSamuraiGauge.SenFlags => SenFlags;
        }

        [StructLayout(LayoutKind.Explicit, Size = 0x10)]
        public struct ReaperGauge : BaseReaperGauge
        {
            [FieldOffset(0x08)] public byte Soul;
            [FieldOffset(0x09)] public byte Shroud;
            [FieldOffset(0x0A)] public ushort EnshroudedTimeRemaining;
            [FieldOffset(0x0C)] public byte LemureShroud;
            [FieldOffset(0x0D)] public byte VoidShroud;

            byte BaseReaperGauge.Soul => Soul;

            byte BaseReaperGauge.Shroud => Shroud;

            ushort BaseReaperGauge.EnshroudedTimeRemaining => EnshroudedTimeRemaining;

            byte BaseReaperGauge.LemureShroud => LemureShroud;

            byte BaseReaperGauge.VoidShroud => VoidShroud;
        }

        #endregion

        #region Tanks

        [StructLayout(LayoutKind.Explicit, Size = 0x10)]
        public struct DarkKnightGauge : BaseDarkKnightGauge
        {
            [FieldOffset(0x08)] public byte Blood;
            [FieldOffset(0x0A)] public ushort DarksideTimer;
            [FieldOffset(0x0C)] public byte DarkArtsState;
            [FieldOffset(0x0E)] public ushort ShadowTimer;

            byte BaseDarkKnightGauge.Blood => Blood;

            ushort BaseDarkKnightGauge.DarksideTimer => DarksideTimer;

            byte BaseDarkKnightGauge.DarkArtsState => DarkArtsState;

            ushort BaseDarkKnightGauge.ShadowTimer => ShadowTimer;
        }

        [StructLayout(LayoutKind.Explicit, Size = 0x10)]
        public struct PaladinGauge : BasePaladinGauge
        {
            [FieldOffset(0x08)] public byte OathGauge;

            byte BasePaladinGauge.OathGauge => OathGauge;
        }

        [StructLayout(LayoutKind.Explicit, Size = 0x10)]
        public struct WarriorGauge : BaseWarriorGauge
        {
            [FieldOffset(0x08)] public byte BeastGauge;

            byte BaseWarriorGauge.BeastGauge => BeastGauge;
        }

        [StructLayout(LayoutKind.Explicit, Size = 0x10)]
        public struct GunbreakerGauge : BaseGunbreakerGauge
        {
            [FieldOffset(0x08)] public byte Ammo;
            [FieldOffset(0x0A)] public short MaxTimerDuration;
            [FieldOffset(0x0C)] public byte AmmoComboStep;

            byte BaseGunbreakerGauge.Ammo => Ammo;

            short BaseGunbreakerGauge.MaxTimerDuration => MaxTimerDuration;

            byte BaseGunbreakerGauge.AmmoComboStep => AmmoComboStep;
        }

        #endregion

        #endregion
    }
}
