using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Windows.Forms;
using Advanced_Combat_Tracker;
using FFXIV_ACT_Plugin.Common;

namespace RainbowMage.OverlayPlugin
{
    /* Taken from FFIXV_ACT_Plugin.Logfile. Copy&pasted to avoid issues if the FFXIV plugin ever changes this enum. */
    public enum LogMessageType
    {
        LogLine,
        ChangeZone,
        ChangePrimaryPlayer,
        AddCombatant,
        RemoveCombatant,
        AddBuff,
        RemoveBuff,
        FlyingText,
        OutgoingAbility,
        IncomingAbility = 10,
        PartyList,
        PlayerStats,
        CombatantHP,
        ParsedPartyMember,
        NetworkStartsCasting = 20,
        NetworkAbility,
        NetworkAOEAbility,
        NetworkCancelAbility,
        NetworkDoT,
        NetworkDeath,
        NetworkBuff,
        NetworkTargetIcon,
        NetworkTargetMarker = 29,
        NetworkBuffRemove,
        NetworkGauge,
        NetworkWorld,
        Network6D,
        NetworkNameToggle,
        NetworkTether,
        NetworkLimitBreak,
        NetworkEffectResult,
        NetworkStatusList,
        NetworkUpdateHp,
        ChangeMap,
        Settings = 249,
        Process,
        Debug,
        PacketDump,
        Version,
        Error,
        Timer,
        // OverlayPlugin lines
        RegisterLogLine = 256,
        MapEffect,
        FateDirector,
        CEDirector,
        InCombat,
    }

    public enum GameRegion
    {
        Global = 1,
        Chinese = 2,
        Korean = 3,
        TraditionalChinese = 4
    }

    public class FFXIVRepository
    {
        private readonly ILogger logger;
        private IDataRepository repository;
        private IDataSubscription subscription;
        private MethodInfo logOutputWriteLineFunc;
        private object logOutput;
        private Func<long, DateTime> machinaEpochToDateTimeWrapper;

        public FFXIVRepository(TinyIoCContainer container)
        {
            logger = container.Resolve<ILogger>();
        }

        private ActPluginData GetPluginData()
        {
            return ActGlobals.oFormActMain.ActPlugins.FirstOrDefault(plugin =>
            {
                if (!plugin.cbEnabled.Checked || plugin.pluginObj == null)
                    return false;
                return plugin.lblPluginTitle.Text.StartsWith("FFXIV_ACT_Plugin");
            });
        }

        private IDataRepository GetRepository()
        {
            if (repository != null)
                return repository;

            var FFXIV = GetPluginData();
            if (FFXIV != null)
            {
                try
                {
                    repository = (IDataRepository)FFXIV.pluginObj.GetType().GetProperty("DataRepository").GetValue(FFXIV.pluginObj);
                }
                catch (Exception ex)
                {
                    logger.Log(LogLevel.Error, Resources.FFXIVDataRepositoryException, ex);
                }
            }

            return repository;
        }

        private IDataSubscription GetSubscription()
        {
            if (subscription != null)
                return subscription;

            var FFXIV = GetPluginData();
            if (FFXIV != null)
            {
                try
                {
                    subscription = (IDataSubscription)FFXIV.pluginObj.GetType().GetProperty("DataSubscription").GetValue(FFXIV.pluginObj);
                }
                catch (Exception ex)
                {
                    logger.Log(LogLevel.Error, Resources.FFXIVDataSubscriptionException, ex);
                }
            }

            return subscription;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private Process GetCurrentFFXIVProcessImpl()
        {
            var repo = GetRepository();
            if (repo == null) return null;

            return repo.GetCurrentFFXIVProcess();
        }

        [Obsolete("Subscribe to the ProcessChanged event instead (See RegisterProcessChangedHandler())")]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public Process GetCurrentFFXIVProcess()
        {
            try
            {
                return GetCurrentFFXIVProcessImpl();
            }
            catch (FileNotFoundException)
            {
                // The FFXIV plugin isn't loaded
                return null;
            }
        }

        private bool IsFFXIVPluginPresentImpl()
        {
            return GetRepository() != null;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public bool IsFFXIVPluginPresent()
        {
            try
            {
                return IsFFXIVPluginPresentImpl();
            }
            catch (FileNotFoundException)
            {
                return false;
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private uint? GetCurrentTerritoryIDImpl()
        {
            return GetRepository()?.GetCurrentTerritoryID();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public uint? GetCurrentTerritoryID()
        {
            try
            {
                return GetCurrentTerritoryIDImpl();
            }
            catch (FileNotFoundException)
            {
                // The FFXIV plugin isn't loaded
                return null;
            }
        }

        public Version GetOverlayPluginVersion()
        {
            return Assembly.GetExecutingAssembly().GetName().Version;
        }

        public Version GetPluginVersion()
        {
            return typeof(IDataRepository).Assembly.GetName().Version;
        }

        public string GetPluginPath()
        {
            return typeof(IDataRepository).Assembly.Location;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private string GetGameVersionImpl()
        {
            return GetRepository()?.GetGameVersion();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public string GetGameVersion()
        {
            try
            {
                return GetGameVersionImpl();
            }
            catch (FileNotFoundException)
            {
                // The FFXIV plugin isn't loaded
                return null;
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public uint GetPlayerIDImpl()
        {
            var repo = GetRepository();
            if (repo == null) return 0;

            return repo.GetCurrentPlayerID();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public uint GetPlayerID()
        {
            try
            {
                return GetPlayerIDImpl();
            }
            catch (FileNotFoundException)
            {
                // The FFXIV plugin isn't loaded
                return 0;
            }
        }

        public string GetPlayerNameImpl()
        {
            var repo = GetRepository();
            if (repo == null) return null;

            var playerId = repo.GetCurrentPlayerID();

            var playerInfo = repo.GetCombatantList().FirstOrDefault(x => x.ID == playerId);
            if (playerInfo == null) return null;

            return playerInfo.Name;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public IDictionary<uint, string> GetResourceDictionary(ResourceType resourceType)
        {
            try
            {
                return GetResourceDictionaryImpl(resourceType);
            }
            catch (FileNotFoundException)
            {
                // The FFXIV plugin isn't loaded
                return null;
            }
        }

        public IDictionary<uint, string> GetResourceDictionaryImpl(ResourceType resourceType)
        {
            var repo = GetRepository();
            if (repo == null) return null;

            return repo.GetResourceDictionary(resourceType);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public string GetPlayerName()
        {
            try
            {
                return GetPlayerNameImpl();
            }
            catch (FileNotFoundException)
            {
                // The FFXIV plugin isn't loaded
                return null;
            }
        }

        public ReadOnlyCollection<FFXIV_ACT_Plugin.Common.Models.Combatant> GetCombatants()
        {
            var repo = GetRepository();
            if (repo == null) return null;

            return repo.GetCombatantList();
        }

        public TabPage GetPluginTabPage()
        {
            var plugin = GetPluginData();
            if (plugin == null) return null;
            return plugin.tpPluginSpace;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public Language GetLanguage()
        {
            var repo = GetRepository();
            if (repo == null)
            {
                // Defaults to English
                return Language.English;
            }
            return repo.GetSelectedLanguageID();
        }

        public string GetLocaleString()
        {
            switch (GetLanguage())
            {
                case Language.English:
                    return "en";
                case Language.French:
                    return "fr";
                case Language.German:
                    return "de";
                case Language.Japanese:
                    return "ja";
                case Language.Chinese:
                    return "cn";
                case Language.Korean:
                    return "ko";
                // @TODO: Replace with `Language.Tc` once we can reference a newer version of FFXIV_ACT_Plugin SDK for all releases
                case (Language)7:
                    return "tc";
                default:
                    return null;
            }
        }

        public static Dictionary<GameRegion, Dictionary<string, ushort>> GetMachinaOpcodes()
        {
            try
            {
                var mach = Assembly.Load("Machina.FFXIV");
                var opcode_manager_type = mach.GetType("Machina.FFXIV.Headers.Opcodes.OpcodeManager");
                var opcode_manager = opcode_manager_type.GetProperty("Instance").GetValue(null);

                // This is ugly, but C# doesn't like typecasting directly because our local `GameRegion` isn't the exact same as Machina's.
                var machinaOpcodes = (System.Collections.IDictionary)opcode_manager_type.GetField("_opcodes", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(opcode_manager);

                var opcodes = new Dictionary<GameRegion, Dictionary<string, ushort>>();
                foreach (var key in machinaOpcodes.Keys)
                {
                    opcodes.Add((GameRegion)(int)key, (Dictionary<string, ushort>)machinaOpcodes[key]);
                }

                return opcodes;
            }
            catch (Exception) { }
            return null;
        }

        public GameRegion GetMachinaRegion()
        {
            try
            {
                var mach = Assembly.Load("Machina.FFXIV");
                var opcode_manager_type = mach.GetType("Machina.FFXIV.Headers.Opcodes.OpcodeManager");
                var opcode_manager = opcode_manager_type.GetProperty("Instance").GetValue(null);
                var machina_region = opcode_manager_type.GetProperty("GameRegion").GetValue(opcode_manager).ToString();

                if (Enum.TryParse<GameRegion>(machina_region, out var region))
                    return region;

                logger.Log(LogLevel.Error, "Unknown Machina region: {0}", machina_region);
            }
            catch (Exception ex)
            {
                logger.Log(LogLevel.Error, "Exception parsing Machina region: {0}", ex);
            }
            return GameRegion.Global;
        }

        public DateTime EpochToDateTime(long epoch)
        {
            if (machinaEpochToDateTimeWrapper == null)
            {
                try
                {
                    var mach = Assembly.Load("Machina");
                    var conversionUtility = mach.GetType("Machina.Infrastructure.ConversionUtility");
                    var epochToDateTime = conversionUtility.GetMethod("EpochToDateTime");
                    machinaEpochToDateTimeWrapper = (e) =>
                    {
                        return (DateTime)epochToDateTime.Invoke(null, new object[] { e });
                    };
                }
                catch (Exception e)
                {
                    logger.Log(LogLevel.Error, e.ToString());
                }
            }
            return machinaEpochToDateTimeWrapper(epoch).ToLocalTime();
        }

        /**
         * Convert a coordinate expressed as a uint16 to a float.
         *
         * See https://github.com/ravahn/FFXIV_ACT_Plugin/issues/298, though this has been
         * updated to be more accurate.
         */
        public static float ConvertUInt16Coordinate(ushort value)
        {
            // This is the exact same formula the game client uses
            return (float)(value * 3.0518043 * 0.0099999998 - 1000.0);
        }

        /**
         * Convert a packet heading to an in-game heading.
         * 
         * When a heading is sent in certain packets, the heading is expressed as a uint16, where
         * 0=north and each increment is 1/65536 of a turn in the CCW direction.
         * 
         * See https://github.com/ravahn/FFXIV_ACT_Plugin/issues/298, though this has been
         * updated to be more accurate.
         */
        public static double ConvertHeading(ushort heading)
        {
            // This is the exact same formula the game client uses
            return heading * 0.009587526 * 0.0099999998 - Math.PI;
        }

        internal object GetFFXIVACTPluginIOCService(string parentAssemblyName, string type)
        {
            var plugin = GetPluginData();
            if (plugin == null) return false;
            var field = plugin.pluginObj.GetType().GetField("_iocContainer", BindingFlags.NonPublic | BindingFlags.Instance);
            if (field == null)
            {
                logger.Log(LogLevel.Error, "Unable to retrieve _iocContainer field information from FFXIV_ACT_Plugin");
                return null;
            }
            var iocContainer = field.GetValue(plugin.pluginObj);
            if (iocContainer == null)
            {
                logger.Log(LogLevel.Error, "Unable to retrieve _iocContainer field value from FFXIV_ACT_Plugin");
                return null;
            }
            var getServiceMethod = iocContainer.GetType().GetMethod("GetService");
            if (getServiceMethod == null)
            {
                logger.Log(LogLevel.Error, "Unable to retrieve _iocContainer field value from FFXIV_ACT_Plugin");
                return null;
            }
            var parentAssembly = AppDomain.CurrentDomain.GetAssemblies().
                SingleOrDefault(assembly => assembly.GetName().Name == parentAssemblyName);
            if (parentAssembly == null)
            {
                logger.Log(LogLevel.Error, $"Unable to retrieve {parentAssemblyName} assembly");
                return null;
            }
            var returnObject = getServiceMethod.Invoke(iocContainer, new object[] { parentAssembly.GetType(type) });
            if (returnObject == null)
            {
                logger.Log(LogLevel.Error, $"Unable to retrieve {type} singleton from FFXIV_ACT_Plugin IOC");
                return null;
            }

            return returnObject;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal bool WriteLogLineImpl(uint ID, DateTime timestamp, string line)
        {
            if (logOutputWriteLineFunc == null)
            {
                logOutput = GetFFXIVACTPluginIOCService("FFXIV_ACT_Plugin.Logfile", "FFXIV_ACT_Plugin.Logfile.ILogOutput");
                if (logOutput == null)
                {
                    logger.Log(LogLevel.Error, "Unable to retrieve LogOutput singleton from FFXIV_ACT_Plugin IOC");
                    return false;
                }
                logOutputWriteLineFunc = logOutput.GetType().GetMethod("WriteLine");
                if (logOutputWriteLineFunc == null)
                {
                    logger.Log(LogLevel.Error, "Unable to retrieve LogOutput singleton from FFXIV_ACT_Plugin IOC");
                    return false;
                }
            }

            logOutputWriteLineFunc.Invoke(logOutput, new object[] { (int)ID, timestamp, line });

            return true;
        }

        // LogLineDelegate(uint EventType, uint Seconds, string logline);
        public void RegisterLogLineHandler(Action<uint, uint, string> handler)
        {
            var sub = GetSubscription();
            if (sub != null)
                sub.LogLine += new LogLineDelegate(handler);
        }

        // NetworkReceivedDelegate(string connection, long epoch, byte[] message)
        public void RegisterNetworkParser(Action<string, long, byte[]> handler)
        {
            var sub = GetSubscription();
            if (sub != null)
                sub.NetworkReceived += new NetworkReceivedDelegate(handler);
        }

        // PartyListChangedDelegate(ReadOnlyCollection<uint> partyList, int partySize)
        //
        // Details: partySize may differ from partyList.Count.
        // In non-cross world parties, players who are not in the same
        // zone count in the partySize but do not appear in the partyList.
        // In cross world parties, nobody will appear in the partyList.
        // Alliance data members show up in partyList but not in partySize.
        public void RegisterPartyChangeDelegate(Action<ReadOnlyCollection<uint>, int> handler)
        {
            var sub = GetSubscription();
            if (sub != null)
                sub.PartyListChanged += new PartyListChangedDelegate(handler);
        }

        public void RegisterZoneChangeDelegate(Action<uint, string> handler)
        {
            var sub = GetSubscription();
            if (sub != null)
                sub.ZoneChanged += new ZoneChangedDelegate(handler);
        }

        // ProcessChangedDelegate(Process process)
        public void RegisterProcessChangedHandler(Action<Process> handler)
        {
            var sub = GetSubscription();
            if (sub != null)
            {
                sub.ProcessChanged += new ProcessChangedDelegate(handler);

                var repo = GetRepository();
                if (repo != null)
                {
                    var process = repo.GetCurrentFFXIVProcess();
                    if (process != null) handler(process);
                }
            }
        }

        public DateTime GetServerTimestamp()
        {
            return GetRepository()?.GetServerTimestamp() ?? DateTime.Now;
        }
    }
}
