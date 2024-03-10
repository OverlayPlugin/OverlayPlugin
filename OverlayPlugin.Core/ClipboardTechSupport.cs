using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using Advanced_Combat_Tracker;
using Newtonsoft.Json.Linq;

// TODO: print warning on cactbot plugin / url / user dir mismatch
// TODO: include first N lines of OverlayPlugin log

namespace RainbowMage.OverlayPlugin
{
    using SimpleTable = List<List<string>>;

    // Facilitates copying useful info to the clipboard about OP plugins and overlays
    // so that folks don't have to paste screenshots in discord.
    class ClipboardTechSupport
    {
        private SimpleTable plugins;
        private SimpleTable overlays;
        private SimpleTable settings;
        private SimpleTable warnings;

        private const ulong WS_POPUP = 0x80000000L;
        private const ulong WS_CAPTION = 0x00C00000L;

        private static string screenMode = null;

        [DllImport("user32.dll")]
        static extern ulong GetWindowLongPtr(IntPtr hWnd, int nIndex);

        static string hideChatLogForPrivacyName = "chkDisableCombatLog";

        // A map of CheckBox names to text.  Right now this text matches what the FFXIV Plugin usues in English.
        static List<(string, string)> pluginCheckboxMap = new List<(string, string)> {
            ( "chkUseDeucalion", "Inject and use Deucalion for network data" ),
            ( hideChatLogForPrivacyName, "Hide Chat Log (for privacy)" ),
            ( "chkUsePcap", "Use WinPCap-compatible library for network data" ),
            ( "chkDisableSocketFilter", "Disable high-performance network filter" ),
            ( "chkDisableCombinePets", "Disable Combine Pets with Owner" ),
            ( "chkDisableDamageShield", "Disable Damage Shield estimates" ),
            ( "chkShowDebug", "(DEBUG) Enable Debug Options" ),
            ( "chkLogAllNetwork", "(DEBUG) Log all Network Packets" ),
            ( "chkShowRealDoTs", "(DEBUG) Also Show 'Real' DoT Ticks" ),
            ( "chkSimulateDoTCrits", "(DEBUG) Simulate Individual DoT Crits" ),
            ( "chkGraphPotency", "(DEBUG) Graph Potency, not Damage" ),
            ( "chkEnableBenchmark", "(DEBUG) Enable Benchmark Tab" ),
        };

        public ClipboardTechSupport(TinyIoCContainer container)
        {
            warnings = new SimpleTable { new List<string> { "Warnings" } };

            plugins = new SimpleTable { new List<string> { "Plugin Name", "Enabled", "Version", "Path" } };

            bool foundFFIXVActPlugin = false;
            bool foundOverlayPlugin = false;

            foreach (var plugin in ActGlobals.oFormActMain.ActPlugins)
            {
                // TODO: plugin.pluginVersion has FileVersion, ProductVersion, etc, but all as a string.
                // For now, ask FileVersionInfo to get this for us.
                var fullPath = plugin.pluginFile.FullName;
                var version = FileVersionInfo.GetVersionInfo(fullPath);
                plugins.Add(new List<string>{
                    plugin.pluginFile.Name,
                    // TODO: could check plugin.pluginObj to see if it loaded successfully.
                    plugin.cbEnabled.Checked.ToString(),
                    version?.FileVersion?.ToString() ?? "",
                    fullPath,
                });

                if (plugin.pluginFile.Name == "FFXIV_ACT_Plugin.dll")
                {
                    foundFFIXVActPlugin = true;
                }
                else if (plugin.pluginFile.Name == "OverlayPlugin.dll")
                {
                    foundOverlayPlugin = true;
                    if (!foundFFIXVActPlugin)
                    {
                        warnings.Add(new List<string> { "OverlayPlugin.dll loaded before FFXIV_ACT_Plugin.dll" });
                    }
                }
                else if (!foundFFIXVActPlugin || !foundOverlayPlugin)
                {
                    warnings.Add(new List<string> { $"{plugin.pluginFile.Name} loaded before FFXIV_ACT_Plugin.dll or OverlayPlugion.dll" });
                }
            }

            var pluginConfig = container.Resolve<IPluginConfig>();
            overlays = new SimpleTable { new List<string> { "Overlay Name", "URL" } };
            foreach (var overlay in pluginConfig.Overlays)
            {
                overlays.Add(new List<string>{
                    overlay.Name,
                    overlay.Url,
                });
            }

            settings = new SimpleTable { new List<string> { "Various Settings", "Value" } };
            var repository = container.Resolve<FFXIVRepository>();
            if (repository.IsFFXIVPluginPresent())
            {
                settings.Add(new List<string> { "Plugin Language", repository.GetLanguage().ToString() });
                settings.Add(new List<string> { "Machina Region", repository.GetMachinaRegion().ToString() });
                string gameVersion = repository.GetGameVersion();
                settings.Add(new List<string> { "Game Version", gameVersion != "" ? gameVersion : "(not running)" });

                if (screenMode == null)
                {
                    screenMode = "(unknown)";
                    repository.RegisterProcessChangedHandler(GetFFXIVScreenMode);
                }
                settings.Add(new List<string> { "Screen Mode", screenMode });

                var tabPage = repository.GetPluginTabPage();
                if (tabPage != null)
                {
                    Dictionary<string, CheckBox> checkboxes = new Dictionary<string, CheckBox>();
                    GetCheckboxes(tabPage.Controls, checkboxes);

                    // Include all known checkboxes first in order, with English text.
                    foreach (var (cbName, settingText) in pluginCheckboxMap)
                    {
                        CheckBox cb;
                        if (!checkboxes.TryGetValue(cbName, out cb))
                        {
                            continue;
                        }

                        settings.Add(new List<string> { settingText, cb.Checked.ToString() });

                        if (cb.Name == hideChatLogForPrivacyName && cb.Checked)
                        {
                            warnings.Add(new List<string> { "Hide Chat Log for Privacy is enabled" });
                        }

                        checkboxes.Remove(cbName);
                    }

                    // Include any unknown checkboxes last with text as written.
                    foreach (var cb in checkboxes.Values)
                    {
                        settings.Add(new List<string> { cb.Text, cb.Checked.ToString() });
                    }
                }
            }
            else
            {
                warnings.Add(new List<string> { "FFXIV plugin not present" });
            }

            // Note: this is a little bit of an abstraction violation to have OverlayPlugin
            // throw up information about cactbot.  For now, this is a single one-off
            // with no expectation that we will add more settings to this list.
            // If other plugins(?) need this or cactbot needs more information, we should
            // consider making this more generic and having some API here for other
            // plugins to inject information more abstractly.
            var cactbotConfig = GetCactbotConfig(pluginConfig);
            if (cactbotConfig != null)
            {
                try
                {
                    var userDir = cactbotConfig["options"]["general"]["CactbotUserDirectory"];
                    settings.Add(new List<string> { "Cactbot User Dir", userDir.ToString() });
                }
                catch { }
            }
        }

        private Dictionary<string, JToken> GetCactbotConfig(IPluginConfig pluginConfig)
        {
            if (!pluginConfig.EventSourceConfigs.ContainsKey("CactbotESConfig"))
                return null;

            var obj = pluginConfig.EventSourceConfigs["CactbotESConfig"];
            if (!obj.TryGetValue("OverlayData", out JToken value))
                return null;

            try
            {
                return value.ToObject<Dictionary<string, JToken>>();
            }
            catch
            {
                return null;
            }
        }

        private void GetFFXIVScreenMode(Process process)
        {
            if (process == null)
            {
                screenMode = "(not running)";
                return;
            }

            // If a handler exists when the game is closed and later re-opened, GetFFXIVScreenMode()
            // will be called with the new process before a main window handle is available.
            // In this case, just sleep for 15 seconds and try again.
            IntPtr mainWindowHandle = process.MainWindowHandle;
            if (mainWindowHandle == IntPtr.Zero)
            {
                Thread.Sleep(15000);
                mainWindowHandle = process.MainWindowHandle;
                if (mainWindowHandle == IntPtr.Zero)
                {
                    screenMode = "(not running)";
                    return;
                }
            }

            ulong style = GetWindowLongPtr(mainWindowHandle, -16);

            if ((style & WS_POPUP) != 0)
            {
                screenMode = "Borderless Windowed";
            }
            else if ((style & WS_CAPTION) != 0)
            {
                screenMode = "Windowed";
            }
            else
            {
                warnings.Add(new List<string> { "Game running in Full Screen mode." });
                screenMode = "Full Screen";
            }
            return;
        }

        private static void GetCheckboxes(Control.ControlCollection controls, Dictionary<string, CheckBox> checkboxes)
        {
            foreach (Control control in controls)
            {
                if (control.GetType() == typeof(CheckBox))
                {
                    CheckBox cb = (CheckBox)control;
                    checkboxes.Add(cb.Name, cb);
                }
                if (control.Controls.Count > 0)
                {
                    GetCheckboxes(control.Controls, checkboxes);
                }
            }
        }

        private string TableToString(SimpleTable input)
        {
            // Find the maximum length of each column.
            List<int> lengths = new List<int>();
            int numColumns = input.Select(x => x.Count).Max();
            for (int column = 0; column < numColumns; ++column)
            {
                lengths.Add(input.Select(x => x.ElementAtOrDefault(column)?.Length ?? 0).Max());
            }

            // Construct a line of hyphens to place after the first row.
            int numHyphens = lengths.Sum() + numColumns - 1;
            string hyphens = new String('-', numHyphens) + "\n";

            string text = "";
            foreach (var row in input)
            {
                for (int column = 0; column < numColumns; ++column)
                {
                    if (column != 0)
                        text += " ";
                    var elem = row.ElementAtOrDefault(column);
                    var padLength = -lengths[column];
                    if (column == numColumns - 1)
                        padLength = 0;
                    text += String.Format($"{{0, {padLength}}}", elem);
                }
                text += "\n";

                text += hyphens;
                hyphens = "";
            }

            return text;
        }

        public void CopyToClipboard()
        {
            string text = "```\n";
            if (warnings.Count > 1)
            {
                text += TableToString(warnings);
                text += "\n\n";
            }

            text += TableToString(plugins);
            text += "\n\n";
            text += TableToString(overlays);
            text += "\n\n";
            text += TableToString(settings);
            text += "```\n";

            Clipboard.SetText(text);
        }
    }
}