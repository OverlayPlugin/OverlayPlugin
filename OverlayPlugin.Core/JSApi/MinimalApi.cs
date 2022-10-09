using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RainbowMage.HtmlRenderer;
using Newtonsoft.Json;

namespace RainbowMage.OverlayPlugin
{
    public class MinimalApi : IEventReceiver
    {
        public string Name => "";
        private readonly Renderer renderer;
        private readonly ILogger logger;
        private readonly EventDispatcher dispatcher;

        public MinimalApi(Renderer r, TinyIoCContainer container)
        {
            renderer = r;
            logger = container.Resolve<ILogger>();
            dispatcher = container.Resolve<EventDispatcher>();

            r.BrowserStartLoading += (o, e) =>
            {
                dispatcher.UnsubscribeAll(this);
            };

            r.BrowserConsoleLog += (o, e) =>
            {
                logger.Log(LogLevel.Info, $"OverlayControl: {e.Message} ({e.Source})");
            };
        }

        public static void AttachTo(Renderer r, TinyIoCContainer container)
        {
            r.SetApi(new MinimalApi(r, container));
        }

        [Obsolete("Please pass your IoC container to AttachTo().")]
        public static void AttachTo(Renderer r)
        {
            r.SetApi(new MinimalApi(r, Registry.GetContainer()));
        }

        public void HandleEvent(JObject e)
        {
            renderer.ExecuteScript("if(window.__OverlayCallback) __OverlayCallback(" + e.ToString(Formatting.None) + ");");
        }

        // Also handles (un)subscription to make switching between this and WS easier.
        public void callHandler(string data, object callback)
        {
            Task.Run(() =>
            {
                var result = dispatcher.ProcessHandlerMessage(this, data);
                if (callback != null)
                {
                    Renderer.ExecuteCallback(callback, result?.ToString(Formatting.None));
                }
            });
        }
    }
}
