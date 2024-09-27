using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;
using Advanced_Combat_Tracker;
using Newtonsoft.Json.Linq;
using RainbowMage.OverlayPlugin.MemoryProcessors.ClientFramework;

// TODO: print warning on cactbot plugin / url / user dir mismatch
// TODO: include first N lines of OverlayPlugin log

namespace RainbowMage.OverlayPlugin
{
    // Facilitates copying useful info to the clipboard about OP plugins and overlays
    // so that folks don't have to paste screenshots in discord.
    class ClipboardTechSupport
    {
        string ACTPathPattern;
        string ACTPathPatternAlt;
        readonly string ACTPathReplace = "<ACT Folder>";
        private List<string> plugins;
        private List<string> overlays;
        private List<string> settings;
        private List<string> warnings;

        private const ulong WS_POPUP = 0x80000000L;
        private const ulong WS_CAPTION = 0x00C00000L;
        private const uint TOKEN_QUERY = 0x0008;

        private static IntPtr ph = IntPtr.Zero;
        private static WindowsIdentity actWi = null;
        private static WindowsIdentity ffxivWi = null;
        private static Process[] actProcesses = null;
        private static string gameLanguage = null;
        private static string actIsAdmin = null;
        private static string ffxivIsAdmin = null;
        private static string screenMode = null;

        [DllImport("user32.dll")]
        static extern ulong GetWindowLongPtr(IntPtr hWnd, int nIndex);

        [DllImport("advapi32.dll", SetLastError = true)]
        private static extern bool OpenProcessToken(IntPtr processHandle, uint desiredAccess, out IntPtr tokenHandle);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool CloseHandle(IntPtr hObject);

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
            string ActAppData = ActGlobals.oFormActMain.AppDataFolder.FullName;
            string ActAppDataAlt = ActGlobals.oFormActMain.AppDataFolder.FullName.Replace('\\', '/').Replace(" ", "%20");
            ACTPathPattern = $@"{Regex.Escape(ActAppData)}";
            ACTPathPatternAlt = $@"{ActAppDataAlt}";

            warnings = new List<string> { "Warnings" };
            plugins = new List<string> { "Plugin Name" + " - Status" + " - Version" + " - Path" };

            actProcesses = Process.GetProcessesByName("Advanced Combat Tracker");
            if (actProcesses.Length > 1)
            {
                warnings.Add("Multiple instances of ACT running");
            }

            bool foundFFIXVActPlugin = false;
            bool foundOverlayPlugin = false;

            foreach (var plugin in ActGlobals.oFormActMain.ActPlugins)
            {
                // TODO: plugin.pluginVersion has FileVersion, ProductVersion, etc, but all as a string.
                // For now, ask FileVersionInfo to get this for us.
                var fullPath = plugin.pluginFile.FullName;
                var censoredFullPath = Regex.Replace(fullPath, ACTPathPattern, ACTPathReplace);
                censoredFullPath = censoredFullPath.Replace(Environment.UserName, "<USER>");
                var version = FileVersionInfo.GetVersionInfo(fullPath);
                string versionString = version.FileVersion?.ToString() ?? "";
                var state = plugin.cbEnabled.Checked.ToString() == "True" ? "Enabled" : "Disabled";
                plugins.Add(
                    plugin.pluginFile.Name + " - " +
                    // TODO: could check plugin.pluginObj to see if it loaded successfully.
                    state + " - " +
                    versionString + " - " +
                    censoredFullPath
                );

                if (plugin.pluginFile.Name == "FFXIV_ACT_Plugin.dll")
                {
                    foundFFIXVActPlugin = true;
                }
                else if (plugin.pluginFile.Name == "OverlayPlugin.dll")
                {
                    foundOverlayPlugin = true;
                    if (!foundFFIXVActPlugin)
                    {
                        warnings.Add("OverlayPlugin.dll loaded before FFXIV_ACT_Plugin.dll");
                    }
                }
                else if (!foundFFIXVActPlugin || !foundOverlayPlugin)
                {
                    warnings.Add($"{plugin.pluginFile.Name} loaded before FFXIV_ACT_Plugin.dll or OverlayPlugin.dll");
                }
            }

            var pluginConfig = container.Resolve<IPluginConfig>();
            overlays = new List<string> { "Overlay Name" + " - URL" };
            foreach (var overlay in pluginConfig.Overlays)
            {

                var censoredUrl = Regex.Replace(overlay.Url, ACTPathPatternAlt, ACTPathReplace);
                censoredUrl = censoredUrl.Replace(Environment.UserName, "<USER>");
                overlays.Add(
                    overlay.Name + " - " +
                    censoredUrl
                );
            }

            settings = new List<string> { "Various Settings" + " - Value" };
            gameLanguage = GetGameLanguage(container);
            settings.Add("Game Language" + " - " + gameLanguage);
            var repository = container.Resolve<FFXIVRepository>();
            if (repository.IsFFXIVPluginPresent())
            {
                settings.Add("Plugin Language" + " - " + repository.GetLanguage().ToString());
                settings.Add("Machina Region" + " - " + repository.GetMachinaRegion().ToString());
                string gameVersion = repository.GetGameVersion();
                gameVersion = gameVersion != "" ? gameVersion : "(not running)";
                settings.Add("Game Version" + " - " + gameVersion);

                if (screenMode == null)
                {
                    screenMode = "(unknown)";
                    repository.RegisterProcessChangedHandler(GetFFXIVScreenMode);
                }
                settings.Add("Screen Mode" + " - " + screenMode);

                try
                {
                    actWi = WindowsIdentity.GetCurrent();
                    if (actWi.Owner.IsWellKnown(WellKnownSidType.BuiltinAdministratorsSid))
                    {
                        actIsAdmin = "Elevated (Admin)";
                    }
                    else
                    {
                        actIsAdmin = "Not Elevated";
                    }
                }
                catch (Exception e)
                {
                    // The most common exception is an access denied error.
                    // This *shouldn't* happen when checking the WindowsIdentity of the
                    // current process, but just in case.
                    actIsAdmin = "(unknown - check warnings)";
                    warnings.Add("Could not check for ACT process elevation: " + e.Message);
                }
                settings.Add("ACT Process Elevation" + " - " + actIsAdmin);

                if (ffxivIsAdmin == null)
                {
                    ffxivIsAdmin = "(unknown)";
                    repository.RegisterProcessChangedHandler(GetFFXIVIsRunningAsAdmin);
                }
                settings.Add("FFXIV Process Elevation" + " - " + ffxivIsAdmin);

                var tabPage = repository.GetPluginTabPage();
                if (tabPage != null)
                {
                    Dictionary<string, CheckBox> checkboxes = new Dictionary<string, CheckBox>();
                    GetCheckboxes(tabPage.Controls, checkboxes);
                    var debug = true;
                    // Include all known checkboxes first in order, with English text.
                    foreach (var (cbName, settingText) in pluginCheckboxMap)
                    {
                        CheckBox cb;
                        if (!checkboxes.TryGetValue(cbName, out cb))
                        {
                            continue;
                        }

                        settings.Add(settingText + " - " + cb.Checked.ToString());

                        if (cb.Name == "chkShowDebug" && !cb.Checked)
                        {
                            debug = false;
                            // If Show Debug Option Not Enabled, Not printing the DEBUG options
                            break;
                        }

                        if (cb.Name == hideChatLogForPrivacyName && cb.Checked)
                        {
                            warnings.Add("Hide Chat Log for Privacy is enabled");
                        }

                        checkboxes.Remove(cbName);
                    }

                    // Include any unknown checkboxes last with text as written.
                    foreach (var cb in checkboxes.Values)
                    {
                        if (!debug && cb.Text.Contains("DEBUG"))
                        {
                            // This foreach loop goes through the CB left, 
                            //and if debug is not enable those would be left in the list, so we skip those
                            continue;
                        }
                        settings.Add(cb.Text + " - " + cb.Checked.ToString());
                    }
                }
            }
            else
            {
                warnings.Add("FFXIV plugin not present");
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
                    var userdirregex = Regex.Replace(userDir.ToString(), ACTPathPattern, ACTPathReplace);
                    userdirregex = userdirregex.Replace(Environment.UserName, "<USER>");
                    settings.Add("Cactbot User Dir" + " - " + userdirregex);
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

        private static string GetGameLanguage(TinyIoCContainer container)
        {
            var clientFrameworkMemory = container.Resolve<IClientFrameworkMemory>();
            if (!clientFrameworkMemory.IsValid())
            {
                return "(unknown)";
            }

            var clientFramework = clientFrameworkMemory.GetClientFramework();
            if (!clientFramework.foundLanguage)
            {
                return "(unknown)";
            }
            return clientFramework.clientLanguage.ToString();
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
                warnings.Add("Game running in Full Screen mode.");
                screenMode = "Full Screen";
            }
            return;
        }

        private void GetFFXIVIsRunningAsAdmin(Process process)
        {
            if (process == null)
            {
                ffxivIsAdmin = "(not running)";
                return;
            }

            try
            {
                OpenProcessToken(process.Handle, TOKEN_QUERY, out ph);
                ffxivWi = new WindowsIdentity(ph);
                if (ffxivWi.Owner.IsWellKnown(WellKnownSidType.BuiltinAdministratorsSid))
                {
                    ffxivIsAdmin = "Elevated (Admin)";
                }
                else
                {
                    ffxivIsAdmin = "Not Elevated";
                }
            }
            catch (Exception e)
            {
                // Will get an access-denied exception if ACT is not elevated, but FFXIV is,
                // since ACT won't have sufficient permissions to check the FFXIV process.
                // Could theoretically be triggered if FFXIV is running under a different
                // (non-admin) user, so give a somewhat non-comittal output.
                if (e.Message.Contains("Access is denied"))
                {
                    ffxivIsAdmin = "Likely Elevated (access violation)";
                }
                else
                {
                    ffxivIsAdmin = "(unknown - check warnings)";
                    warnings.Add("Could not check for FFXIV process elevation: " + e.Message);
                }
            }
            finally
            {
                if (ph != IntPtr.Zero)
                {
                    CloseHandle(ph);
                }
            }
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

        private string ListToString(List<string> input)
        {
            var text = string.Join("\n", input);
            return text;
        }

        public void CopyToClipboard()
        {
            string text = "```\n";
            if (warnings.Count > 1)
            {
                text += ListToString(warnings);
                text += "\n\n";
            }

            text += ListToString(plugins);
            text += "\n\n";
            text += ListToString(overlays);
            text += "\n\n";
            text += ListToString(settings);
            text += "\n```\n";


            Clipboard.SetText(text);
        }
    }
}
