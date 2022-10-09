using RainbowMage.OverlayPlugin.Overlays;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Newtonsoft.Json.Linq;
using System.Windows.Forms;

namespace RainbowMage.OverlayPlugin
{
    [Serializable]
    public class PluginConfig : IPluginConfig
    {
        const string BACKUP_SUFFIX = ".backup";
        private TinyIoCContainer _container;

        [JsonIgnore]
        private bool isDirty = false;

        [JsonIgnore]
        private string filePath;

        private bool _followLatestLog;
        public bool FollowLatestLog
        {
            get
            {
                return _followLatestLog;
            }
            set
            {
                _followLatestLog = value;
                isDirty = true;
            }
        }

        private bool _hideOverlaysWhenNotActive;
        public bool HideOverlaysWhenNotActive
        {
            get
            {
                return _hideOverlaysWhenNotActive;
            }
            set
            {
                _hideOverlaysWhenNotActive = value;
                isDirty = true;
            }
        }

        private bool _hideOverlayDuringCutscene;
        public bool HideOverlayDuringCutscene
        {
            get
            {
                return _hideOverlayDuringCutscene;
            }
            set
            {
                _hideOverlayDuringCutscene = value;
                isDirty = true;
            }
        }

        private bool _errorReports;
        public bool ErrorReports
        {
            get
            {
                return _errorReports;
            }
            set
            {
                _errorReports = value;
                isDirty = true;
            }
        }

        private bool _updateCheck;
        public bool UpdateCheck
        {
            get
            {
                return _updateCheck;
            }
            set
            {
                _updateCheck = value;
                isDirty = true;
            }
        }

        private string _WSServerIP;
        public string WSServerIP
        {
            get
            {
                return _WSServerIP;
            }
            set
            {
                _WSServerIP = value;
                isDirty = true;
            }
        }

        private int _WSServerPort;
        public int WSServerPort
        {
            get
            {
                return _WSServerPort;
            }
            set
            {
                _WSServerPort = value;
                isDirty = true;
            }
        }

        private bool _WSServerSSL;
        public bool WSServerSSL
        {
            get
            {
                return _WSServerSSL;
            }
            set
            {
                _WSServerSSL = value;
                isDirty = true;
            }
        }

        private bool _WSServerRunning;
        public bool WSServerRunning
        {
            get
            {
                return _WSServerRunning;
            }
            set
            {
                _WSServerRunning = value;
                isDirty = true;
            }
        }

        private string _tunnelRegion;
        public string TunnelRegion
        {
            get
            {
                return _tunnelRegion;
            }
            set
            {
                _tunnelRegion = value;
                isDirty = true;
            }
        }

        [JsonIgnore]
        public Version Version
        {
            get
            {
                if (string.IsNullOrEmpty(this.VersionString))
                {
                    return null;
                }
                else
                {
                    return new Version(this.VersionString);
                }
            }
            set
            {
                if (value != null)
                {
                    this.VersionString = value.ToString();
                }
                else
                {
                    this.VersionString = null;
                }
            }
        }

        private string _versionString;
        [JsonProperty("Version")]
        public string VersionString
        {
            get
            {
                return _versionString;
            }
            set
            {
                _versionString = value;
                isDirty = true;
            }
        }

        private DateTime _lastUpdateCheck;
        public DateTime LastUpdateCheck
        {
            get
            {
                return _lastUpdateCheck;
            }
            set
            {
                _lastUpdateCheck = value;
                isDirty = true;
            }
        }

        private bool _isFirstLaunch;
        [JsonIgnore]
        public bool IsFirstLaunch
        {
            get
            {
                return _isFirstLaunch;
            }
            set
            {
                _isFirstLaunch = value;
                isDirty = true;
            }
        }

        [JsonIgnore]
        public OverlayConfigList<IOverlayConfig> Overlays { get; set; }

        [JsonProperty("Overlays")]
        public List<JObject> OverlayObjects;

        public Dictionary<string, JObject> EventSourceConfigs { get; set; }

        [JsonIgnore]
        private ILogger logger;

        public PluginConfig(string configPath, TinyIoCContainer container)
        {
            if (configPath == null) throw new Exception("Invalid config path passed to PluginConfig!");

            this._container = container;
            this.filePath = configPath;
            this.logger = container.Resolve<ILogger>();
            this.Overlays = new OverlayConfigList<IOverlayConfig>(logger);
            this.EventSourceConfigs = new Dictionary<string, JObject>();

            this.FollowLatestLog = false;
            this.HideOverlaysWhenNotActive = true;
            this.HideOverlayDuringCutscene = false;
            this.ErrorReports = false;
            this.UpdateCheck = true;
            this.IsFirstLaunch = true;

            var useBackup = true;
            var initEmpty = false;

            if (File.Exists(configPath))
            {
                try
                {
                    LoadJson(configPath);
                    useBackup = false;
                }
                catch (Exception ex)
                {
                    logger.Log(LogLevel.Error, "LoadConfig: Failed to load configuration: {0}", ex);
                }
            }
            else
            {
                useBackup = true;
            }

            if (useBackup)
            {
                if (File.Exists(configPath + BACKUP_SUFFIX))
                {
                    logger.Log(LogLevel.Info, "LoadConfig: Loading backup config...");

                    try
                    {
                        LoadJson(configPath + BACKUP_SUFFIX);
                    }
                    catch (Exception ex)
                    {
                        logger.Log(LogLevel.Error, "LoadConfig: Failed to load backup: {0}", ex);

                        var dialog = new Controls.ConfigErrorPrompt();
                        if (dialog.ShowDialog() == DialogResult.Yes)
                        {
                            initEmpty = true;
                        }
                        else
                        {
                            throw ex;
                        }
                    }
                }
                else
                {
                    initEmpty = true;
                }
            }

            if (initEmpty)
            {
                this.Overlays = new OverlayConfigList<IOverlayConfig>(logger);

                this.WSServerIP = "127.0.0.1";
                this.WSServerPort = 10501;
                this.WSServerRunning = false;
                this.WSServerSSL = false;
            }

            this.isDirty = false;
        }

        public void MarkDirty()
        {
            isDirty = true;
        }

        public void SaveJson(bool force = false)
        {
            if (!force && !isDirty) return;

            // Create a backup of the old config
            if (File.Exists(filePath))
            {
                // First, make sure it's actually valid.
                var oldConfigValid = false;
                try
                {
                    using (var stream = new StreamReader(filePath))
                    {
                        var reader = new JsonTextReader(stream);
                        JToken.ReadFrom(reader);
                    }
                    oldConfigValid = true;
                }
                catch (Exception ex)
                {
                    logger.Log(LogLevel.Error, "Failed to read old config. Skipping backup... {0}", ex);
                }

                if (oldConfigValid)
                {
                    File.Copy(filePath, filePath + BACKUP_SUFFIX, true);
                }
            }

            // Convert Overlays
            OverlayObjects = new List<JObject>();

            foreach (var item in this.Overlays)
            {
                var obj = JObject.FromObject(item);
                obj["$type"] = item.GetType().FullName + ", " + item.GetType().Assembly.GetName();
                OverlayObjects.Add(obj);
            }

            using (var stream = new StreamWriter(filePath))
            {
                var serializer = new JsonSerializer();
                serializer.Formatting = Formatting.Indented;
                serializer.TypeNameHandling = TypeNameHandling.Auto;
                serializer.Serialize(stream, this);
            }

            isDirty = false;
        }

        private void LoadJson(string configPath)
        {
            using (var stream = new StreamReader(configPath))
            {
                var reader = new JsonTextReader(stream);
                var serializer = new JsonSerializer();
                serializer.TypeNameHandling = TypeNameHandling.Auto;
                serializer.Populate(reader, this);
            }

            this.IsFirstLaunch = false;

            // Convert Overlays
            var overlayLeftOvers = new List<JObject>();
            this.Overlays = new OverlayConfigList<IOverlayConfig>(this.logger);

            foreach (var item in this.OverlayObjects)
            {
                try
                {
                    var typeName = item["$type"].ToString();
                    var type = GetType(typeName.Split(',')[0]);
                    if (type == null)
                    {
                        throw new Exception($"Type {typeName} not found!");
                    }

                    this.Overlays.Add((IOverlayConfig)JsonConvert.DeserializeObject(
                        item.ToString(Formatting.None),
                        type,
                        new ConfigCreationConverter(_container)
                    ));
                }
                catch (Exception e)
                {
                    this.logger.Log(LogLevel.Error, $"Failed to load an overlay config: ${e}");
                    overlayLeftOvers.Add(item);
                }
            }

            this.OverlayObjects = overlayLeftOvers;
        }

        public static PluginConfig LoadJson(string path, TinyIoCContainer container)
        {
            if (!File.Exists(path))
            {
                return null;
            }

            return new PluginConfig(path, container);
        }

        private static Type GetType(string fullName)
        {
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                var type = asm.GetType(fullName, false);
                if (type != null)
                {
                    return type;
                }
            }
            return null;
        }
    }
}
