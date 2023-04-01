using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows.Forms;
using Advanced_Combat_Tracker;
using Newtonsoft.Json.Linq;

// TODO: print warnings on plugin ordering
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

        public ClipboardTechSupport(TinyIoCContainer container)
        {
            warnings = new SimpleTable { new List<string> { "Warnings" } };

            plugins = new SimpleTable { new List<string> { "Plugin Name", "Enabled", "Version", "Path" } };
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

                var tabPage = repository.GetPluginTabPage();
                if (tabPage != null)
                    GetCheckboxValues(settings, warnings, tabPage.Controls);
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

        private static void GetCheckboxValues(SimpleTable settings, SimpleTable warnings, Control.ControlCollection controls)
        {
            foreach (Control control in controls)
            {
                if (control.GetType() == typeof(CheckBox))
                {
                    // TODO: should we accumulate and sort these settings?
                    CheckBox cb = (CheckBox)control;
                    settings.Add(new List<string> { cb.Text, cb.Checked.ToString() });

                    // TODO: it's unfortunate to have to do a string comparison to find this,
                    // but Ravahn would have to expose this more programmatically otherwise.
                    if (cb.Text.Contains("Hide Chat Log") && cb.Checked)
                    {
                        warnings.Add(new List<string> { "Hide Chat Log for Privacy is enabled" });
                    }
                }
                if (control.Controls.Count > 0)
                {
                    GetCheckboxValues(settings, warnings, control.Controls);
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
                    text += String.Format($"{{0, {-lengths[column]}}}", elem);
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