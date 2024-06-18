using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Advanced_Combat_Tracker;
using Newtonsoft.Json;
using RainbowMage.HtmlRenderer;
using RainbowMage.OverlayPlugin.Controls;
using RainbowMage.OverlayPlugin.EventSources;
using RainbowMage.OverlayPlugin.Overlays;

namespace RainbowMage.OverlayPlugin
{
    public class PluginMain : IDisposable
    {
        private TinyIoCContainer _container;
        private ILogger _logger;
        TabPage tabPage;
        Label label;
        ControlPanel controlPanel;

        TabPage wsTabPage;
        WSConfigPanel wsConfigPanel;

        Timer configSaveTimer;
        private bool clearCache = false;
        private bool _disposed;

        internal PluginConfig Config { get; private set; }
        internal List<IOverlay> Overlays { get; private set; }
        internal event EventHandler OverlaysChanged;

        internal string PluginDirectory { get; private set; }

        public PluginMain(string pluginDirectory)
        {
            _container = TinyIoCContainer.Current;
            PluginDirectory = pluginDirectory;
            _logger = _container.Resolve<ILogger>();

            configSaveTimer = new Timer();
            configSaveTimer.Interval = 300000; // 5 minutes
            configSaveTimer.Tick += (o, e) => SaveConfig();

            _container.Register(this);
        }

        /// <summary>
        /// プラグインが有効化されたときに呼び出されます。
        /// </summary>
        /// <param name="pluginScreenSpace"></param>
        /// <param name="pluginStatusText"></param>
        public async Task InitPlugin(TabPage pluginScreenSpace, Label pluginStatusText)
        {
            try
            {
                this.tabPage = pluginScreenSpace;
                this.label = pluginStatusText;
                await SetPluginStatusText("Init Phase 1: Infrastructure");

#if DEBUG
                _logger.Log(LogLevel.Warning, "##################################");
                _logger.Log(LogLevel.Warning, "    THIS IS THE DEBUG BUILD");
                _logger.Log(LogLevel.Warning, "##################################");
#endif

                _logger.Log(LogLevel.Info, "InitPlugin: PluginDirectory = {0}", PluginDirectory);

#if TRACEPERF
                Stopwatch watch = new Stopwatch();
                watch.Start();
#endif

                // ** Init phase 1
                // Only init stuff here that works without the FFXIV plugin or addons (event sources, overlays).
                // Everything else should be initialized in the second phase.
                // 1.a Stuff without state
                FFXIVExportVariables.Init();

                await SetPluginStatusText("Init Phase 1: Config");
                if (!LoadConfig())
                {
                    _logger.Log(LogLevel.Error, "Failed to load the plugin config. Please report this error on the GitHub repo or on the ACT Discord.");
                    _logger.Log(LogLevel.Error, "");
                    _logger.Log(LogLevel.Error, "  ACT Discord: https://discord.gg/ahFKcmx");
                    _logger.Log(LogLevel.Error, "  GitHub repo: https://github.com/OverlayPlugin/OverlayPlugin");

                    FailWithLog();
                    return;
                }

                TinyIoCAutoHelper.AutoRegisterBeforeInit();
                TinyIoCAutoHelper.AutoConstructBeforeInit();

#if TRACEPERF
                _logger.Log(LogLevel.Debug, "Component init and config load took {0}s.", watch.Elapsed.TotalSeconds);
                watch.Reset();
#endif

                await SetPluginStatusText("Init Phase 1: CEF");
                try
                {
                    Renderer.Initialize(PluginDirectory, ActGlobals.oFormActMain.AppDataFolder.FullName, Config.ErrorReports);
                }
                catch (Exception e)
                {
                    _logger.Log(LogLevel.Error, "InitPlugin: {0}", e);
                }

#if TRACEPERF
                _logger.Log(LogLevel.Debug, "CEF init took {0}s.", watch.Elapsed.TotalSeconds);
                watch.Reset();
#endif

                await SetPluginStatusText("Init Phase 1: Legacy message bus");
                // プラグイン間のメッセージ関連
                OverlayApi.BroadcastMessage += (o, e) =>
                {
                    Task.Run(() =>
                    {
                        foreach (var overlay in this.Overlays)
                        {
                            overlay.SendMessage(e.Message);
                        }
                    });
                };
                OverlayApi.SendMessage += (o, e) =>
                {
                    Task.Run(() =>
                    {
                        var targetOverlay = this.Overlays.FirstOrDefault(x => x.Name == e.Target);
                        if (targetOverlay != null)
                        {
                            targetOverlay.SendMessage(e.Message);
                        }
                    });
                };
                OverlayApi.OverlayMessage += (o, e) =>
                {
                    Task.Run(() =>
                    {
                        var targetOverlay = this.Overlays.FirstOrDefault(x => x.Name == e.Target);
                        if (targetOverlay != null)
                        {
                            targetOverlay.OverlayMessage(e.Message);
                        }
                    });
                };

#if TRACEPERF
                watch.Reset();
#endif

                await SetPluginStatusText("Init Phase 1: UI");

                // Setup the UI
                this.controlPanel = new ControlPanel();
                this.controlPanel.Dock = DockStyle.Fill;
                this.tabPage.Controls.Add(this.controlPanel);
                this.tabPage.Name = "OverlayPlugin";

                this.wsConfigPanel = new WSConfigPanel();
                this.wsConfigPanel.Dock = DockStyle.Fill;

                this.wsTabPage = new TabPage("OverlayPlugin WSServer");
                this.wsTabPage.Controls.Add(wsConfigPanel);
                ((TabControl)this.tabPage.Parent).TabPages.Add(this.wsTabPage);

                _logger.Log(LogLevel.Info, "InitPlugin: Initialised.");

                // Fire off the update check (which runs in the background)
                if (Config.UpdateCheck)
                {
                    Updater.Updater.PerformUpdateIfNecessary(PluginDirectory);
                }

                await SetPluginStatusText("Init Phase 1: Presets");
                // Load our presets
                try
                {
#if DEBUG
                    var presetFile = Path.Combine(PluginDirectory, "libs", "resources", "presets.json");
#else
                    var presetFile = Path.Combine(PluginDirectory, "resources", "presets.json");
#endif
                    var presetData = "{}";

                    try
                    {
                        presetData = File.ReadAllText(presetFile);
                    }
                    catch (Exception ex)
                    {
                        _logger.Log(LogLevel.Error, string.Format(Resources.ErrorCouldNotLoadPresets, ex));
                    }

                    var presets = JsonConvert.DeserializeObject<Dictionary<string, OverlayPreset>>(presetData);
                    var registry = _container.Resolve<Registry>();
                    foreach (var pair in presets)
                    {
                        pair.Value.Name = pair.Key;
                        registry.RegisterOverlayPreset2(pair.Value);
                    }

                    wsConfigPanel.RebuildOverlayOptions();
                }
                catch (Exception ex)
                {
                    _logger.Log(LogLevel.Error, string.Format("Failed to load presets: {0}", ex));
                }

                await SetPluginStatusText("Init Phase 1: Waiting for plugins to load");

                if (ActGlobals.oFormActMain.InitActDone)
                {
                    InitPostPlugins();
                }
                else
                {
                    ActGlobals.oFormActMain.VisibleChanged += Handle_FormActMain_VisibleChanged;
                }
            }
            catch (Exception e)
            {
                _logger.Log(LogLevel.Error, "InitPlugin: {0}", e.ToString());
                MessageBox.Show(e.ToString());
                FailWithLog();
                throw;
            }
        }

        private void Handle_FormActMain_VisibleChanged(object sender, EventArgs e)
        {
            ActGlobals.oFormActMain.VisibleChanged -= Handle_FormActMain_VisibleChanged;
            InitPostPlugins();
        }

        private async void InitPostPlugins()
        {
            try
            {
                // ** Init phase 2
                await SetPluginStatusText("Init Phase 2: Integrations");

                // Wrap FFXIV plugin related initialization in try/catch to allow OP to work when FFXIV plugin isn't present
                try
                {
                    TinyIoCAutoHelper.AutoRegisterDuringInit();
                    TinyIoCAutoHelper.AutoConstructDuringInit();
                }
                catch (Exception ex)
                {
                    _logger.Log(LogLevel.Warning, "InitPlugin: Could not init FFXIV integration: {0}", ex);
                }

                // This timer runs on the UI thread (it has to since we create UI controls) but LoadAddons()
                // can block for some time. We run it on the background thread to avoid blocking the UI.
                // We can't run LoadAddons() in the first init phase since it checks other ACT plugins for
                // addons. Plugins below OverlayPlugin wouldn't have been loaded in the first init phase.
                // However, in the second phase all plugins have been loaded which means we can look for addons
                // in that list.
                await SetPluginStatusText("Init Phase 2: Addons");
                await Task.Run(LoadAddons);
                wsConfigPanel.RebuildOverlayOptions();

                await SetPluginStatusText("Init Phase 2: UI");
                await InvokeOnUIThread(() =>
                {
                    try
                    {
                        // Now that addons have been loaded, we can finish the overlay setup.
                        this.label.Text = "Init Phase 2: Overlays";

                        InitializeOverlays();
                        controlPanel.InitializeOverlayConfigTabs();

                        this.label.Text = "Init Phase 2: Overlay tasks";
                        TinyIoCAutoHelper.AutoRegisterAfterInit();
                        TinyIoCAutoHelper.AutoConstructAfterInit();

                        // WSServer has to start after the LoadAddons() call because clients can connect immediately
                        // after it's initialized and that requires the event sources to be initialized.
                        if (Config.WSServerRunning)
                        {
                            this.label.Text = "Init Phase 2: WSServer";
                            _container.Resolve<WSServer>().Start();
                        }

                        this.label.Text = "Init Phase 2: Save timer";
                        configSaveTimer.Start();

                        this.label.Text = "Initialised";
                        // Make the log small; startup was successful and there shouldn't be any error message to show.
                        controlPanel.ResizeLog();
                    }
                    catch (Exception ex)
                    {
                        _logger.Log(LogLevel.Error, "InitPlugin: {0}", ex);
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Error, "InitPlugin: {0}", ex);
            }
        }

        internal void ClearCacheOnRestart()
        {
            this.clearCache = true;
        }

        private void FailWithLog()
        {
            // If the tab hasn't been initialized, yet, make sure we show at least the log.
            if (controlPanel == null)
            {
                var logPanel = new LogPanel(_container);
                logPanel.Dock = DockStyle.Fill;
                tabPage.Controls.Add(logPanel);
            }
        }

        /// <summary>
        /// コンフィグのオーバーレイ設定を基に、オーバーレイを初期化・登録します。
        /// </summary>
        private void InitializeOverlays()
        {
            // オーバーレイ初期化
            this.Overlays = new List<IOverlay>();
            foreach (var overlayConfig in this.Config.Overlays)
            {
                var parameters = new NamedParameterOverloads();
                parameters["config"] = overlayConfig;
                parameters["name"] = overlayConfig.Name;

                var overlay = (IOverlay)_container.Resolve(overlayConfig.OverlayType, parameters);
                if (overlay != null)
                {
                    RegisterOverlay(overlay);
                }
                else
                {
                    _logger.Log(LogLevel.Error, "InitPlugin: Could not find addon for {0}.", overlayConfig.Name);
                }
            }
        }

        /// <summary>
        /// オーバーレイを登録します。
        /// </summary>
        /// <param name="overlay"></param>
        internal void RegisterOverlay(IOverlay overlay)
        {
            overlay.Start();
            this.Overlays.Add(overlay);

            OverlaysChanged?.Invoke(this, null);
        }

        /// <summary>
        /// 登録されているオーバーレイを削除します。
        /// </summary>
        /// <param name="overlay">削除するオーバーレイ。</param>
        internal void RemoveOverlay(IOverlay overlay)
        {
            this.Overlays.Remove(overlay);
            overlay.Dispose();

            OverlaysChanged?.Invoke(this, null);
        }

        /// <summary>
        /// プラグインが無効化されたときに呼び出されます。
        /// </summary>
        public void DeInitPlugin()
        {
            SaveConfig(true);

            if (_container.TryResolve(out OverlayZCorrector corrector))
            {
                corrector.DeInit();
            }

            if (controlPanel != null) controlPanel.Dispose();

            if (Overlays != null)
            {
                foreach (var overlay in this.Overlays)
                {
                    overlay.Dispose();
                }

                this.Overlays.Clear();
            }

            try { _container.Resolve<WSServer>().Stop(); }
            catch { }

            if (this.wsConfigPanel != null)
            {
                this.wsConfigPanel.Stop();
            }

            if (this.wsTabPage != null && this.wsTabPage.Parent != null)
                ((TabControl)this.wsTabPage.Parent).TabPages.Remove(this.wsTabPage);

            // Can only shut down CEF if ACT is closing
            if (ActGlobals.oFormActMain.IsActClosing)
            {
                var shutdown = Renderer.Shutdown();

                // Can only clear cache if CEF is shut down, otherwise some cache files are still in use.
                if (shutdown && this.clearCache == true)
                    Renderer.ClearCache();
            }

            _logger.Log(LogLevel.Info, "DeInitPlugin: Finalized.");
            _ = SetPluginStatusText("Finalized.");
        }

        private void LoadAddons()
        {
            try
            {
                var registry = _container.Resolve<Registry>();
                _container.Register(BuiltinEventConfig.LoadConfig(Config));

                // Make sure the event sources are ready before we load any overlays.
                registry.StartEventSource(new MiniParseEventSource(_container));
                registry.StartEventSource(new FFXIVOptionalEventSource(_container));
                registry.StartEventSource(new FFXIVRequiredEventSource(_container));
                registry.StartEventSource(new EnmityEventSource(_container));
                registry.StartEventSource(new FFXIVClientStructsEventSource(_container));

                registry.RegisterOverlay<MiniParseOverlay>();
                registry.RegisterOverlay<SpellTimerOverlay>();
                registry.RegisterOverlay<LabelOverlay>();

                var version = typeof(PluginMain).Assembly.GetName().Version;
                var Addons = new List<IOverlayAddonV2>();

                foreach (var plugin in ActGlobals.oFormActMain.ActPlugins)
                {
                    if (plugin.pluginObj == null) continue;

                    try
                    {
                        if (plugin.pluginObj.GetType().GetInterface(typeof(IOverlayAddonV2).FullName) != null)
                        {
                            try
                            {
                                var addon = (IOverlayAddonV2)plugin.pluginObj;
                                addon.Init();

                                _logger.Log(LogLevel.Info, "LoadAddons: {0}: Initialized {1}", plugin.lblPluginTitle.Text, addon.ToString());
                            }
                            catch (Exception e)
                            {
                                _logger.Log(LogLevel.Error, "LoadAddons: {0}: {1}", plugin.lblPluginTitle.Text, e);
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        _logger.Log(LogLevel.Error, "LoadAddons: {0}: {1}", plugin.lblPluginTitle.Text, e);
                    }
                }

                registry.StartEventSources();
            }
            catch (Exception e)
            {
                _logger.Log(LogLevel.Error, "LoadAddons: {0}", e);
                Trace.WriteLine("LoadAddons: " + e.ToString());
            }
        }

        private bool LoadConfig()
        {
            if (Config != null)
                return true;

            try
            {
                Config = new PluginConfig(GetConfigPath(), _container);
            }
            catch (Exception e)
            {
                Config = null;
                _logger.Log(LogLevel.Error, "LoadConfig: {0}", e);
                return false;
            }

            _container.Register(Config);
            _container.Register<IPluginConfig>(Config);
            return true;
        }

        /// <summary>
        /// 設定を保存します。
        /// </summary>
        private void SaveConfig(bool force = false)
        {
            Registry registry;
            if (!_container.TryResolve(out registry)) return;
            if (Config == null || Overlays == null || registry.EventSources == null) return;

            try
            {
                foreach (var overlay in this.Overlays)
                {
                    overlay.SavePositionAndSize();
                }

                foreach (var es in registry.EventSources)
                {
                    if (es != null)
                        es.SaveConfig(Config);
                }

                _container.Resolve<BuiltinEventConfig>().SaveConfig(Config);
                Config.SaveJson(force);
            }
            catch (Exception e)
            {
                _logger.Log(LogLevel.Error, "SaveConfig: {0}", e);
                MessageBox.Show(e.ToString());
            }
        }

        /// <summary>
        /// 設定ファイルのパスを取得します。
        /// </summary>
        /// <returns></returns>
        private static string GetConfigPath(bool xml = false)
        {
            var path = Path.Combine(
                ActGlobals.oFormActMain.AppDataFolder.FullName,
                "Config",
                "RainbowMage.OverlayPlugin.config." + (xml ? "xml" : "json"));

            return path;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    controlPanel?.Dispose();
                    wsTabPage?.Dispose();
                    wsConfigPanel?.Dispose();
                    configSaveTimer?.Stop();
                    configSaveTimer?.Dispose();
                }
                _disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        public async Task SetPluginStatusText(string text)
        {
            await InvokeOnUIThread(() =>
            {
                if (label != null) label.Text = text;
            });
        }

        public static Task<T> InvokeOnUIThread<T>(Func<T> p)
        {
            return Task.Run(new Func<T>(() =>
            {
                if (ActGlobals.oFormActMain.InvokeRequired)
                {
                    return (T)ActGlobals.oFormActMain.EndInvoke(ActGlobals.oFormActMain.BeginInvoke(p));
                }
                else
                {
                    return p();
                }
            }));
        }

        public static Task InvokeOnUIThread(Action p)
        {
            return Task.Run(() =>
            {
                if (ActGlobals.oFormActMain.InvokeRequired)
                {
                    ActGlobals.oFormActMain.EndInvoke(ActGlobals.oFormActMain.BeginInvoke(p));
                }
                else
                {
                    p();
                }
            });
        }
    }
}
