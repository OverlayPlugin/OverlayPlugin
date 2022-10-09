using Advanced_Combat_Tracker;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RainbowMage.HtmlRenderer;

namespace RainbowMage.OverlayPlugin.Overlays
{
    public partial class MiniParseOverlay : OverlayBase<MiniParseOverlayConfig>
    {
        protected DateTime lastUrlChange;
        protected string lastLoadedUrl;
        protected System.Threading.Timer previewTimer;
        private readonly FFXIVRepository repository;

        public bool Preview = false;

        public bool ModernApi { get; protected set; }

        public MiniParseOverlay(MiniParseOverlayConfig config, string name, TinyIoCContainer container)
            : base(config, name, container)
        {
            if (Overlay == null) return;
            repository = container.Resolve<FFXIVRepository>();

            if (Config.Zoom == 1)
            {
                // Set zoom to 0% if it's set to exactly 1% since that was mistakenly the default for too long.
                Config.Zoom = 0;
            }

            Config.ActwsCompatibilityChanged += (o, e) =>
            {
                if (lastLoadedUrl != null && lastLoadedUrl != "about:blank") Navigate(lastLoadedUrl);
            };
            Config.NoFocusChanged += (o, e) =>
            {
                Overlay.SetAcceptFocus(!Config.NoFocus);
            };
            Config.ZoomChanged += (o, e) =>
            {
                Overlay.Renderer.SetZoomLevel(Config.Zoom / 100.0);
            };
            Config.ForceWhiteBackgroundChanged += (o, e) =>
            {
                var color = Config.ForceWhiteBackground ? "white" : "transparent";
                ExecuteScript($"document.body.style.backgroundColor = \"{color}\";");
            };
            Config.DisabledChanged += (o, e) =>
            {
                if (Config.Disabled)
                {
                    Overlay.Renderer.EndRender();
                    Overlay.ClearFrame();
                }
                else
                {
                    Overlay.Renderer.BeginRender();
                }
            };
            Config.VisibleChanged += (o, e) =>
            {
                if (Config.MuteWhenHidden)
                {
                    Overlay.Renderer.SetMuted(!Config.IsVisible);
                }
            };
            Config.MuteWhenHiddenChanged += (o, e) =>
            {
                Overlay.Renderer.SetMuted(Config.MuteWhenHidden ? !Config.IsVisible : false);
            };
            Config.HideOutOfCombatChanged += (o, e) =>
            {
                container.Resolve<OverlayHider>().UpdateOverlays();
            };

            if (Config.MuteWhenHidden && !Config.IsVisible)
            {
                Overlay.Renderer.SetMuted(true);
            }

            if (Config.HideOutOfCombat)
            {
                // Assume that we're not in combat when ACT starts.
                Overlay.Visible = false;
            }

            Overlay.Renderer.BrowserStartLoading += PrepareWebsite;
            Overlay.Renderer.BrowserLoad += FinishLoading;
            Overlay.Renderer.BrowserConsoleLog += Renderer_BrowserConsoleLog;
        }

        public override void Dispose()
        {
            base.Dispose();
            previewTimer?.Dispose();
        }

        public override Control CreateConfigControl()
        {
            return new MiniParseConfigPanel(container, this);
        }

        private void Renderer_BrowserConsoleLog(object sender, BrowserConsoleLogEventArgs e)
        {
            if (Config.ActwsCompatibility && e.Message.Contains("ws://127.0.0.1/") && (e.Message.Contains("SecurityError:") || e.Message.Contains("ERR_CONNECTION_")))
            {
                Overlay.Reload();
            }
        }

        private void FinishLoading(object sender, BrowserLoadEventArgs e)
        {
            if (Config.ActwsCompatibility)
            {
                var charName = JsonConvert.SerializeObject(repository.GetPlayerName() ?? "YOU");
                var charID = JsonConvert.SerializeObject(repository.GetPlayerID());

                ExecuteScript(@"var msg = { charName: " + charName + ", charID: " + charID + @" };
                if (window.__OverlayPlugin_ws_faker) {
                    __OverlayPlugin_ws_faker({ type: 'broadcast', msgtype: 'SendCharName', msg });
                } else {
                    window.__OverlayPlugin_char_msg = msg;
                }");
            }

            if (Preview)
            {
                try
                {
                    var pluginPath = container.Resolve<PluginMain>().PluginDirectory;
#if DEBUG
                    var previewPath = Path.Combine(pluginPath, "libs", "resources", "preview.json");
#else
                    var previewPath = Path.Combine(pluginPath, "resources", "preview.json");
#endif
                    var eventData = JObject.Parse(File.ReadAllText(previewPath));

                    // Since we can't be sure when the overlay is ready to receive events, we'll just send one
                    // per second (which is the same rate the real events are sent at).
                    previewTimer = new System.Threading.Timer((state) =>
                    {
                        HandleEvent(eventData);

                        ExecuteScript("document.dispatchEvent(new CustomEvent('onExampleShowcase', null));");
                    }, null, TimeSpan.Zero, TimeSpan.FromSeconds(1));
                }
                catch (Exception ex)
                {
                    logger.Log(LogLevel.Error, $"{Name}: Failed to load preview data: {ex}");
                }
            }
        }

        private void PrepareWebsite(object sender, BrowserLoadEventArgs e)
        {
            if (e.Url.StartsWith("about:blank"))
                return;

            lastLoadedUrl = e.Url;
            Config.Url = e.Url;

            // Reset page-specific state
            Overlay.Renderer.SetZoomLevel(Config.Zoom / 100.0);
            ModernApi = false;

            if (Config.ForceWhiteBackground)
            {
                ExecuteScript("document.body.style.backgroundColor = 'white';");
            }

            if (Config.ActwsCompatibility)
            {
                Overlay.SetAcceptFocus(!Config.NoFocus);

                var shimMsg = Resources.ActwsShimEnabled;

                // Install a fake WebSocket so we can directly call the event handler.
                ExecuteScript(@"(function() {
                    let realWS = window.WebSocket;
                    let queue = [];
                    window.__OverlayPlugin_ws_faker = (msg) => queue.push(msg);
                    window.overlayWindowId = 'ACTWS_shim';

                    window.WebSocket = function(url) {
                        if (url.indexOf('ws://127.0.0.1/') > -1)
                        {
                            window.__OverlayPlugin_ws_faker = (msg) => {
                                if (this.onmessage) this.onmessage({ data: JSON.stringify(msg) });
                            };
                            this.close = () => null;
                            this.send = (msg) => {
                                if (msg === '.') return;

                                msg = JSON.parse(msg);
                                switch (msg.msgtype) {
                                    case 'Capture':
                                        OverlayPluginApi.makeScreenshot();
                                        break;
                                    case 'RequestEnd':
                                        OverlayPluginApi.endEncounter();
                                        break;
                                }
                            };

                            console.log(" + JsonConvert.SerializeObject(shimMsg) + @");

                            if (queue !== null) {
                                setTimeout(() => {
                                    queue.forEach(__OverlayPlugin_ws_faker);
                                    queue = null;

                                    if (window.__OverlayPlugin_char_msg) this.__OverlayPlugin_ws_faker(window.__OverlayPlugin_char_msg);
                                }, 100);
                            }
                        }
                        else
                        {
                            return new realWS(url);
                        }
                    };
                })();");

                Subscribe("CombatData");
                Subscribe("LogLine");
                Subscribe("ChangeZone");
                Subscribe("ChangePrimaryPlayer");
            }
            else
            {
                if (Preview)
                {
                    ExecuteScript("if (window.OverlayPluginApi) window.OverlayPluginApi.preview = true;");
                }

                // Reset page-specific state
                Overlay.SetAcceptFocus(false);

                // Subscriptions are cleared on page navigation so we have to restore this after every load.
                Subscribe("CombatData");
                Subscribe("LogLine");
            }
        }

        public override void Navigate(string url)
        {
            if (Config.ActwsCompatibility)
            {
                if (!url.Contains("HOST_PORT=") && url != "about:blank")
                {
                    if (!url.EndsWith("?"))
                    {
                        if (url.Contains("?"))
                        {
                            url += "&";
                        }
                        else
                        {
                            url += "?";
                        }
                    }
                    url += "HOST_PORT=ws://127.0.0.1/fake/";
                }
            }
            else
            {
                int pos = url.IndexOf("HOST_PORT=");
                if (pos > -1 && url.Contains("/fake/"))
                {
                    url = url.Substring(0, pos).Trim(new char[] { '?', '&' });
                }
            }

            // If this URL was just loaded (see PrepareWebsite), ignore this request since we're loading that URL already.
            if (url == lastLoadedUrl) return;

            lastUrlChange = DateTime.Now;
            if (url != Overlay.Url)
            {
                base.Navigate(url);
            }
        }

        public override void Reload()
        {
            // If the user changed the URL less than a second ago, ignore the reload since it would interrupt
            // the currently loading overlay and end up with an empty page.
            // The user probably just wanted to load the page so doing nothing here (in that case) is fine.

            if (DateTime.Now - lastUrlChange > new TimeSpan(0, 0, 1))
            {
                base.Reload();
            }
        }

        public override void Start()
        {
        }

        public override void Stop()
        {
        }

        public override void HandleEvent(JObject e)
        {
            if (Config.ActwsCompatibility)
            {
                // NOTE: Keep this in sync with WSServer's LegacyHandler.
                switch (e["type"].ToString())
                {
                    case "CombatData":
                        ExecuteScript("__OverlayPlugin_ws_faker({'type': 'broadcast', 'msgtype': 'CombatData', 'msg': " + e.ToString(Formatting.None) + " });");
                        break;
                    case "LogLine":
                        ExecuteScript("__OverlayPlugin_ws_faker({'type': 'broadcast', 'msgtype': 'Chat', 'msg': " + JsonConvert.SerializeObject(e["rawLine"].ToString()) + " });");
                        break;
                    case "ChangeZone":
                        ExecuteScript("__OverlayPlugin_ws_faker({'type': 'broadcast', 'msgtype': 'ChangeZone', 'msg': " + e.ToString(Formatting.None) + " });");
                        break;
                    case "ChangePrimaryPlayer":
                        ExecuteScript("__OverlayPlugin_ws_faker({'type': 'broadcast', 'msgtype': 'SendCharName', 'msg': " + e.ToString(Formatting.None) + " });");
                        break;
                }
            }
            else if (ModernApi)
            {
                base.HandleEvent(e);
            }
            else
            {
                // Old OverlayPlugin API
                switch (e["type"].ToString())
                {
                    case "CombatData":
                        ExecuteScript("document.dispatchEvent(new CustomEvent('onOverlayDataUpdate', { detail: " + e.ToString(Formatting.None) + " }));");
                        break;
                    case "LogLine":
                        ExecuteScript("document.dispatchEvent(new CustomEvent('onLogLine', { detail: " + JsonConvert.SerializeObject(e["line"].ToString(Formatting.None)) + " }));");
                        break;
                }
            }
        }

        public override void InitModernAPI()
        {
            if (!ModernApi)
            {
                // Clear the subscription set in PrepareWebsite().
                Unsubscribe("CombatData");
                Unsubscribe("LogLine");
                ModernApi = true;
            }
        }

        protected override void Update()
        {

        }
    }
}
