﻿using Newtonsoft.Json;
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
        }
    }

    class OverlayPluginLogLineConfig
    {
        private Dictionary<string, Dictionary<string, OpcodeConfigEntry>> config = new Dictionary<string, Dictionary<string, OpcodeConfigEntry>>();
        private ILogger logger;
        private FFXIVRepository repository;
        public OverlayPluginLogLineConfig(TinyIoCContainer container)
        {
            logger = container.Resolve<ILogger>();
            repository = container.Resolve<FFXIVRepository>();
            var main = container.Resolve<PluginMain>();

            var pluginDirectory = main.PluginDirectory;

            var opcodesPath = Path.Combine(pluginDirectory, "resources", "opcodes.json");

            try
            {
                var jsonData = File.ReadAllText(opcodesPath);
                config = JsonConvert.DeserializeAnonymousType(jsonData, config);
            }
            catch (Exception ex)
            {
                logger.Log(LogLevel.Error, string.Format(Resources.ErrorCouldNotLoadReservedLogLines, ex));
            }
        }

        public IOpcodeConfigEntry this[string name]
        {
            get
            {
                var version = repository.GetGameVersion();
                if (version == null)
                {
                    logger.Log(LogLevel.Error, "Could not detect game version from FFXIV_ACT_Plugin");
                    return null;
                }
                if (!config.ContainsKey(version))
                {
                    logger.Log(LogLevel.Error, $"No opcodes for game version {version}");
                    return null;
                }
                var versionOpcodes = config[version];
                if (!versionOpcodes.ContainsKey(name))
                {
                    logger.Log(LogLevel.Error, $"No opcode for game version {version}, opcode name {name}");
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
