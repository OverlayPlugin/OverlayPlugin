﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Advanced_Combat_Tracker;
using Newtonsoft.Json.Linq;

namespace RainbowMage.OverlayPlugin
{
    using SimpleTable = List<List<string>>;

    // Helper class to send a warning when cactbot paths don't match.
    // Yes, it's not great to have this in OverlayPlugin, but cactbot doesn't
    // know about overlay urls and so this is the best place to figure out
    // this information.
    class CactbotPathWarning
    {
        private static string cactbotDllName = "CactbotOverlay.dll";

        private static string GetCactbotOverlayPath()
        {
            foreach (var plugin in ActGlobals.oFormActMain.ActPlugins)
            {
                if (!plugin.cbEnabled.Checked || plugin.pluginObj == null)
                    continue;
                if (plugin.pluginFile.Name != cactbotDllName)
                    continue;
                return plugin.pluginFile.FullName;
            }
            return null;
        }

        private static List<IOverlayConfig> GetProbableBrokenCactbotOverlays(TinyIoCContainer container)
        {
            List<IOverlayConfig> overlays = new List<IOverlayConfig>();
            var pluginConfig = container.Resolve<IPluginConfig>();
            foreach (var overlay in pluginConfig.Overlays)
            {
                if (!(overlay is Overlays.MiniParseOverlayConfig))
                    continue;
                var miniParseOverlay = (Overlays.MiniParseOverlayConfig)overlay;
                if (miniParseOverlay.Disabled)
                    continue;
                if (overlay.Url.Contains("cactbot-"))
                    overlays.Add(overlay);
            }
            return overlays;
        }

        private static string GetStartThroughSubstring(string fullPath, string substring)
        {
            var idx = fullPath.LastIndexOf(substring);
            if (idx == -1)
                return null;

            var substringLen = substring.Length;
            return fullPath.Substring(0, idx + substringLen);
        }

        private static string GetGoodCactbotDirWithForwardSlashes(string fullPath)
        {
            var fullPathForwardSlash = fullPath.Replace('\\', '/');
            var correctPath = "/cactbot/cactbot/";

            var idx = fullPathForwardSlash.LastIndexOf(correctPath);
            if (idx == -1)
                return null;

            var substringLen = correctPath.Length;
            return fullPathForwardSlash.Substring(0, idx + substringLen);
        }

        public CactbotPathWarning(TinyIoCContainer container)
        {
            // Find the first enabled cactbot plugin.
            var pluginPath = GetCactbotOverlayPath();
            if (pluginPath == null)
                return;

            // Find any cactbot overlays that might contain a version string.
            var overlays = GetProbableBrokenCactbotOverlays(container);
            if (overlays.Count == 0)
                return;

            // Find the likely correct cactbot install path.
            // If this is an old install, e.g. cactbot/cactbot-version and the
            // plugin is in that path, just leave it for now as it will continue
            // to work until the user removes the plugin and re-adds it.
            // This will also ignore any other places that people have put
            // cactbot manually or are building out of local builds.
            var cactbotPath = GetGoodCactbotDirWithForwardSlashes(pluginPath);
            if (cactbotPath == null)
                return;

            List<IOverlayConfig> broken = overlays.FindAll((overlay) => !overlay.Url.Contains(cactbotPath));
            string brokenNames = String.Join(", ", broken.Select((overlay) => $"\"{overlay.Name}\""));

            Advanced_Combat_Tracker.ActGlobals.oFormActMain.NotificationAdd(
                Resources.CactbotPathWarningTitle,
                string.Format(Resources.CactbotPathWarning, brokenNames)
            );
        }
    }
}