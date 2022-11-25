using System;
using System.Collections.Generic;
using System.IO;
using YamlDotNet.RepresentationModel;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace RainbowMage.OverlayPlugin.MemoryProcessors.AtkStage.FFXIVClientStructs
{
    public enum DataNamespace
    {
        Global
    }

    public class Data
    {
        private readonly ILogger logger;
        private readonly string yamlFilePath;
        private readonly Dictionary<DataNamespace, ClientStructsData> data = new Dictionary<DataNamespace, ClientStructsData>();

        public Data(TinyIoCContainer container)
        {
            logger = container.Resolve<ILogger>();

            var main = container.Resolve<PluginMain>();
            var pluginDirectory = main.PluginDirectory;

            yamlFilePath = Path.Combine(pluginDirectory, "resources", "FFXIVClientStructs.{0}.data.yml");
        }

        public long? GetClassInstanceAddress(DataNamespace ns, string targetClass)
        {
            var curObj = GetBaseObject(ns);
            if (curObj == null)
            {
                return null;
            }

            GameClass classObj;

            if (!curObj.classes.TryGetValue(targetClass, out classObj))
            {
                return null;
            }

            var instances = classObj.instances;
            if (instances == null || instances.Length < 1)
            {
                return null;
            }

            return instances[0].ea;
        }

        public ClientStructsData GetBaseObject(DataNamespace ns)
        {
            ClientStructsData baseObj;
            if (!data.TryGetValue(ns, out baseObj))
            {
                using (var reader = File.OpenText(string.Format(yamlFilePath, ns.ToString())))
                {
                    YamlDocument doc = new YamlDocument(yamlFilePath);
                    var deserializer = new DeserializerBuilder()
                        .WithNamingConvention(NullNamingConvention.Instance)
                        .Build();
                    baseObj = deserializer.Deserialize<ClientStructsData>(reader);
                    data[ns] = baseObj;
                }
            }
            return baseObj;
        }

        public class ClientStructsData
        {
            public string version;
            public Dictionary<long, string> globals = new Dictionary<long, string>();
            public Dictionary<long, string> functions = new Dictionary<long, string>();
            public Dictionary<string, GameClass> classes = new Dictionary<string, GameClass>();
        }

        public class GameClass
        {
            public ClassVtbl[] vtbls;
            public Dictionary<long, string> funcs = new Dictionary<long, string>();
            public Dictionary<long, string> vfuncs = new Dictionary<long, string>();
            public ClassInstance[] instances;
        }

        public class ClassVtbl
        {
            public long ea;
            public string @base;
        }

        public class ClassInstance
        {
            public long ea;
            public bool? pointer;
            public string name;
        }
    }
}
