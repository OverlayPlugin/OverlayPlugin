using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RainbowMage.OverlayPlugin.Updater;
using System;
using System.Collections.Generic;
using System.IO;

namespace RainbowMage.OverlayPlugin.NetworkProcessors
{
    class OverlayPluginLogLines
    {
        public OverlayPluginLogLines(TinyIoCContainer container)
        {
            container.Register(new OverlayPluginLogLineConfig(container));
            container.Register(new LineMapEffect(container));
            container.Register(new LineFateControl(container));
            container.Register(new LineCEDirector(container));
        }
    }

    class OverlayPluginLogLineConfig
    {
        private Dictionary<string, Dictionary<string, OpcodeConfigEntry>> opcodes = new Dictionary<string, Dictionary<string, OpcodeConfigEntry>>();
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

            // TODO: should we fall back to the file if the remote cached config is somehow broken?
            if (!LoadCachedOpcodesFromConfig())
            {
                LoadOpcodesFromFile();
            }
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
                opcodes = JsonConvert.DeserializeAnonymousType(config.CachedOpcodeFile.ToString(), opcodes);
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
                var response = CurlWrapper.Get(remoteOpcodeUrl);
                var jsonData = JObject.Parse(response);
                // Validate that this can convert properly before storing it.
                JsonConvert.DeserializeAnonymousType(response, opcodes);

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
                opcodes = JsonConvert.DeserializeAnonymousType(jsonData, opcodes);
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

                if (opcodes.ContainsKey(version))
                {
                    var versionOpcodes = opcodes[version];
                    if (versionOpcodes.ContainsKey(name))
                    {
                        return versionOpcodes[name];
                    }
                    else
                    {
                        LogException($"No opcode for game version {version}, opcode name {name}");
                    }
                }
                else
                {
                    LogException($"No opcodes for game version {version}");
                }

                // Try once to get this remotely if this opcode or version is missing.
                if (!haveAttemptedOpcodeDownload)
                {
                    haveAttemptedOpcodeDownload = true;
                    SaveRemoteOpcodesToConfig();
                    return this[name];
                }

                return null;
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
