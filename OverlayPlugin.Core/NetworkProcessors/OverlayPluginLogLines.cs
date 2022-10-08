using Newtonsoft.Json;
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
        private Dictionary<string, Dictionary<string, OpcodeConfigEntry>> config = new Dictionary<string, Dictionary<string, OpcodeConfigEntry>>();
        private ILogger logger;
        private FFXIVRepository repository;

        private int exceptionCount = 0;
        private const int maxExceptionsLogged = 3;

        public OverlayPluginLogLineConfig(TinyIoCContainer container)
        {
            logger = container.Resolve<ILogger>();
            repository = container.Resolve<FFXIVRepository>();
            var main = container.Resolve<PluginMain>();

            var pluginDirectory = main.PluginDirectory;

            var opcodesPath = Path.Combine(pluginDirectory, "resources", "opcodes.jsonc");

            try
            {
                var jsonData = File.ReadAllText(opcodesPath);
                config = JsonConvert.DeserializeAnonymousType(jsonData, config);
            }
            catch (Exception ex)
            {
                LogException(string.Format(Resources.ErrorCouldNotLoadReservedLogLines, ex));
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
                if (!config.ContainsKey(version))
                {
                    LogException($"No opcodes for game version {version}");
                    return null;
                }
                var versionOpcodes = config[version];
                if (!versionOpcodes.ContainsKey(name))
                {
                    LogException($"No opcode for game version {version}, opcode name {name}");
                    return null;
                }
                return versionOpcodes[name];
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
