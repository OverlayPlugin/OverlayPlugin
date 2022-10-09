using System;
using System.Threading.Tasks;
using Advanced_Combat_Tracker;
using RainbowMage.HtmlRenderer;
using System.IO;
using System.Reflection;
using RainbowMage.OverlayPlugin.EventSources;

namespace RainbowMage.OverlayPlugin
{
    class OverlayApi
    {
        public static event EventHandler<BroadcastMessageEventArgs> BroadcastMessage;
        public static event EventHandler<SendMessageEventArgs> SendMessage;
        public static event EventHandler<SendMessageEventArgs> OverlayMessage;

        private readonly EventDispatcher dispatcher;
        private readonly IApiBase receiver;
        private readonly ILogger logger;

        public OverlayApi(TinyIoCContainer container, IApiBase receiver)
        {
            this.dispatcher = container.Resolve<EventDispatcher>();
            this.receiver = receiver;
            this.logger = container.Resolve<ILogger>();
        }

        public void broadcastMessage(string msg)
        {
            logger.Log(LogLevel.Error, $"{receiver.Name}: OverlayPluginApi.broadcastMessage() is deprecated and will be removed in future OverlayPlugin versions!");
            BroadcastMessage(this, new BroadcastMessageEventArgs(msg));
        }

        public void sendMessage(string target, string msg)
        {
            logger.Log(LogLevel.Error, $"{receiver.Name}: OverlayPluginApi.sendMessage() is deprecated and will be removed in future OverlayPlugin versions!");
            SendMessage(this, new SendMessageEventArgs(target, msg));
        }

        public void overlayMessage(string target, string msg)
        {
            logger.Log(LogLevel.Error, $"{receiver.Name}: OverlayPluginApi.overlayMessage() is deprecated and will be removed in future OverlayPlugin versions!");
            if (target == receiver.Name)
            {
                receiver.OverlayMessage(msg);
            }
            else
            {
                OverlayMessage(this, new SendMessageEventArgs(target, msg));
            }
        }

        public void endEncounter()
        {
            ActGlobals.oFormActMain.Invoke((Action)(() =>
            {
                ActGlobals.oFormActMain.EndCombat(true);
            }));
        }

        public void makeScreenshot()
        {
            var actDir = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
            var screenshotDir = Path.Combine(actDir, "Screenshot");
            var i = 0;
            string filename;

            Directory.CreateDirectory(screenshotDir);

            do
            {
                filename = Path.Combine(screenshotDir, "ScreenShot_" + DateTime.Now.ToString("yyyyMMddHHmmss") + "_" + i + ".png");
                i++;
            } while (File.Exists(filename));

            var bmp = receiver.Screenshot();
            bmp.Save(filename);
        }

        public void setAcceptFocus(bool accept)
        {
            receiver.SetAcceptFocus(accept);
        }

        // Also handles (un)subscription to make switching between this and WS easier.
        public void callHandler(string data, object callback)
        {
            // Tell the overlay that the page is using the modern API.
            receiver.InitModernAPI();

            Task.Run(() =>
            {
                var result = dispatcher.ProcessHandlerMessage(receiver, data);
                if (callback != null)
                {
                    Renderer.ExecuteCallback(callback, result?.ToString(Newtonsoft.Json.Formatting.None));
                }
            });
        }
    }

    public class BroadcastMessageEventArgs : EventArgs
    {
        public string Message { get; private set; }

        public BroadcastMessageEventArgs(string message)
        {
            this.Message = message;
        }
    }

    public class SendMessageEventArgs : EventArgs
    {
        public string Target { get; private set; }
        public string Message { get; private set; }

        public SendMessageEventArgs(string target, string message)
        {
            this.Target = target;
            this.Message = message;
        }
    }

    public class EndEncounterEventArgs : EventArgs
    {
        public EndEncounterEventArgs()
        {

        }
    }
}
