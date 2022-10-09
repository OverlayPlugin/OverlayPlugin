using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Advanced_Combat_Tracker;
using Newtonsoft.Json.Linq;

namespace RainbowMage.OverlayPlugin
{
    class TriggIntegration
    {
        private PluginMain _plugin;
        public delegate void CustomCallbackDelegate(object o, string param);

        private ActPluginData GetPluginData()
        {
            return ActGlobals.oFormActMain.ActPlugins.FirstOrDefault(plugin =>
            {
                if (!plugin.cbEnabled.Checked || plugin.pluginObj == null)
                    return false;
                return plugin.lblPluginTitle.Text == "Triggernometry.dll";
            });
        }

        public TriggIntegration(TinyIoCContainer container)
        {
            var logger = container.Resolve<ILogger>();
            _plugin = container.Resolve<PluginMain>();

            try
            {
                var trigg = GetPluginData();
                if (trigg == null || trigg.pluginObj == null)
                    return;

                var triggType = trigg.pluginObj.GetType();
                var deleType = triggType.GetNestedType("CustomCallbackDelegate");
                if (deleType == null)
                    return;

                var registerType = triggType.GetMethod("RegisterNamedCallback");

                var sendDele = Delegate.CreateDelegate(deleType, this, typeof(TriggIntegration).GetMethod("SendOverlayMessage"));
                registerType?.Invoke(trigg.pluginObj, new object[] { "OverlayPluginMessage", sendDele, null });

                var hideDele = Delegate.CreateDelegate(deleType, this, typeof(TriggIntegration).GetMethod("HideOverlay"));
                registerType?.Invoke(trigg.pluginObj, new object[] { "HideOverlay", hideDele, null });

                var showDele = Delegate.CreateDelegate(deleType, this, typeof(TriggIntegration).GetMethod("ShowOverlay"));
                registerType?.Invoke(trigg.pluginObj, new object[] { "ShowOverlay", showDele, null });
            }
            catch (Exception ex)
            {
                logger.Log(LogLevel.Error, $"Failed to register Triggernometry callback: {ex}");
            }
        }

        public void SendOverlayMessage(object _, string msg)
        {
            var pos = msg.IndexOf('|');
            if (pos < 1) return;

            var overlayName = msg.Substring(0, pos);
            msg = msg.Substring(pos + 1);

            foreach (var overlay in _plugin.Overlays)
            {
                if (overlay.Name == overlayName)
                {
                    ((IEventReceiver)overlay).HandleEvent(JObject.FromObject(new
                    {
                        type = "Triggernometry",
                        message = msg
                    }));
                    break;
                }
            }
        }

        public void HideOverlay(object _, string msg)
        {
            foreach (var overlay in _plugin.Overlays)
            {
                if (overlay.Name == msg)
                {
                    overlay.Config.IsVisible = false;
                    break;
                }
            }
        }

        public void ShowOverlay(object _, string msg)
        {
            foreach (var overlay in _plugin.Overlays)
            {
                if (overlay.Name == msg)
                {
                    overlay.Config.IsVisible = true;
                    break;
                }
            }
        }
    }
}
