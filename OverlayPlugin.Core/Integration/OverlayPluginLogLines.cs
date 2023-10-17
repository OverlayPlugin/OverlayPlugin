using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RainbowMage.OverlayPlugin.MemoryProcessors.Combatant;
using RainbowMage.OverlayPlugin.MemoryProcessors.InCombat;
using RainbowMage.OverlayPlugin.NetworkProcessors;
using RainbowMage.OverlayPlugin.Updater;

namespace RainbowMage.OverlayPlugin
{
    using Opcodes = Dictionary<string, Dictionary<string, OpcodeConfigEntry>>;

    class OverlayPluginLogLines
    {
        public OverlayPluginLogLines(TinyIoCContainer container)
        {
            container.Register(new OverlayPluginLogLineConfig(container));
            container.Register(new LineMapEffect(container));
            container.Register(new LineFateControl(container));
            container.Register(new LineCEDirector(container));
            container.Register(new LineInCombat(container));
            container.Register(new LineCombatant(container));
            container.Register(new LineRSV(container));
            container.Register(new LineActorCastExtra(container));
            container.Register(new LineAbilityExtra(container));
        }
    }

    class OverlayPluginLogLineConfig
    {
        private Opcodes opcodesFile;
        private Opcodes opcodesConfig;
        private ILogger logger;
        private FFXIVRepository repository;
        private PluginConfig config;
        private TinyIoCContainer container;

        private int exceptionCount = 0;
        private const int maxExceptionsLogged = 3;

        private bool haveAttemptedOpcodeDownload = false;

        private const string remoteOpcodeUrl = "https://raw.githubusercontent.com/OverlayPlugin/OverlayPlugin/main/OverlayPlugin.Core/resources/opcodes.jsonc";

        public OverlayPluginLogLineConfig(TinyIoCContainer container)
        {
            this.container = container;
            logger = container.Resolve<ILogger>();
            repository = container.Resolve<FFXIVRepository>();
            config = container.Resolve<PluginConfig>();

            LoadCachedOpcodesFromConfig();
            LoadOpcodesFromFile();
        }

        private bool LoadCachedOpcodesFromConfig()
        {
            var versionStr = config.CachedOpcodeOverlayPluginVersion;
            Version version;

            if (versionStr == "")
            {
                return false;
            }

            try
            {
                version = new Version(versionStr);
            }
            catch (Exception ex)
            {
                LogException($"Invalid CachedOpcodeOverlayPluginVersion {versionStr}: {ex}");
                ClearCachedOpcodes();
                return false;
            }

            // Only load cached opcodes if we have loaded them for this version.
            // If they're old, clear them.
            if (repository.GetOverlayPluginVersion() > version)
            {
                ClearCachedOpcodes();
                return false;
            }

            try
            {
                // TODO: is there a better way to go JToken -> Dictionary here without a string intermediary?
                opcodesConfig = JsonConvert.DeserializeObject<Opcodes>(config.CachedOpcodeFile.ToString());
            }
            catch (Exception ex)
            {
                LogException($"Failed to parse cached opcodes: {ex}");
                ClearCachedOpcodes();
                return false;
            }

            logger.Log(LogLevel.Debug, "Loaded opcodes from config");
            return true;
        }

        private void SaveRemoteOpcodesToConfig()
        {
            if (!config.UpdateCheck)
            {
                logger.Log(LogLevel.Debug, "Skipping remote opcode fetch due to UpdateCheck=false");
                return;
            }

            try
            {
                var response = HttpClientWrapper.Get(remoteOpcodeUrl);
                var jsonData = JObject.Parse(response);
                // Validate that this can convert properly before storing it.
                JsonConvert.DeserializeObject<Opcodes>(response);

                config.CachedOpcodeFile = jsonData;
                config.CachedOpcodeOverlayPluginVersion = repository.GetOverlayPluginVersion().ToString();
                logger.Log(LogLevel.Debug, "Fetched remote opcodes");
                LoadCachedOpcodesFromConfig();
            }
            catch (Exception ex)
            {
                LogException($"Remote opcode error: {ex}");
                return;
            }
        }

        private void ClearCachedOpcodes()
        {
            logger.Log(LogLevel.Debug, "Clearing cached opcodes");
            config.CachedOpcodeOverlayPluginVersion = "";
            config.CachedOpcodeFile = new JObject();
        }

        private bool LoadOpcodesFromFile()
        {
            var main = container.Resolve<PluginMain>();
            var pluginDirectory = main.PluginDirectory;
            var opcodesPath = Path.Combine(pluginDirectory, "resources", "opcodes.jsonc");

            try
            {
                var jsonData = File.ReadAllText(opcodesPath);
                opcodesFile = JsonConvert.DeserializeObject<Opcodes>(jsonData);
                logger.Log(LogLevel.Debug, "Loaded opcodes from file");
                return true;
            }
            catch (Exception ex)
            {
                LogException(string.Format(Resources.ErrorCouldNotLoadReservedLogLines, ex));
                return false;
            }
        }

        private void LogException(string message)
        {
            if (exceptionCount >= maxExceptionsLogged)
                return;
            exceptionCount++;
            logger.Log(LogLevel.Error, message);
        }

        private IOpcodeConfigEntry GetOpcode(string name, Opcodes opcodes, string version, string opcodeType)
        {
            if (opcodes == null)
                return null;

            if (opcodes.ContainsKey(version))
            {
                var versionOpcodes = opcodes[version];
                if (versionOpcodes.ContainsKey(name))
                {
                    return versionOpcodes[name];
                }
                else
                {
                    LogException($"No {opcodeType} opcode for game version {version}, opcode name {name}");
                }
            }
            else
            {
                LogException($"No {opcodeType} opcodes for game version {version}");
            }

            return null;
        }

        public IOpcodeConfigEntry this[string name]
        {
            get
            {
                var version = repository.GetGameVersion();
                if (version == null)
                {
                    LogException("Could not detect game version from FFXIV_ACT_Plugin");
                    return null;
                }

                IOpcodeConfigEntry opcode = null;
                opcode = GetOpcode(name, opcodesConfig, version, "config");
                if (opcode == null)
                {
                    opcode = GetOpcode(name, opcodesFile, version, "file");

                    // Try once to get this remotely, but only if this opcode or version is missing.
                    // TODO: we could consider getting this once always too, but for now
                    // if we ever have an incorrect (but present) opcode, another release is required.
                    if (opcode == null && !haveAttemptedOpcodeDownload)
                    {
                        haveAttemptedOpcodeDownload = true;
                        SaveRemoteOpcodesToConfig();
                        return GetOpcode(name, opcodesConfig, version, "config");
                    }
                }

                return opcode;
            }
        }
    }
    interface IOpcodeConfigEntry
    {
        uint opcode { get; }
        uint size { get; }
    }

    [JsonObject(NamingStrategyType = typeof(Newtonsoft.Json.Serialization.DefaultNamingStrategy))]
    class OpcodeConfigEntry : IOpcodeConfigEntry
    {
        public uint opcode { get; set; }
        public uint size { get; set; }
    }
}
