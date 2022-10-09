using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RainbowMage.HtmlRenderer;

namespace RainbowMage.OverlayPlugin
{
    public abstract class OverlayBase<TConfig> : IOverlay, IEventReceiver, IApiBase
        where TConfig : OverlayConfigBase
    {
        private bool disableLog = false;
        private List<Action> hotKeyCallbacks = new List<Action>();
        protected readonly TinyIoCContainer container;
        protected readonly ILogger logger;
        private readonly EventDispatcher dispatcher;

        protected System.Timers.Timer timer;
        /// <summary>
        /// オーバーレイがログを出力したときに発生します。
        /// </summary>
        public event EventHandler<LogEventArgs> OnLog;

        /// <summary>
        /// ユーザーが設定したオーバーレイの名前を取得します。
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// オーバーレイフォームを取得します。
        /// </summary>
        public OverlayForm Overlay { get; private set; }

        /// <summary>
        /// オーバーレイの設定を取得します。
        /// </summary>
        public TConfig Config { get; private set; }

        /// <summary>
        /// プラグインの設定を取得します。
        /// </summary>
        public IPluginConfig PluginConfig { get; private set; }
        IOverlayConfig IOverlay.Config { get => Config; set => Config = (TConfig)value; }
        IntPtr IOverlay.Handle { get => Overlay == null ? IntPtr.Zero : Overlay.Handle; }

        public bool Visible
        {
            get
            {
                return Overlay == null ? false : Overlay.Visible;
            }
            set
            {
                if (Overlay != null) Overlay.Visible = value;
            }
        }

        protected OverlayBase(TConfig config, string name, TinyIoCContainer container)
        {
            this.container = container;
            this.logger = container.Resolve<ILogger>();
            this.dispatcher = container.Resolve<EventDispatcher>();
            this.PluginConfig = container.Resolve<IPluginConfig>();
            this.Config = config;
            this.Name = name;

            if (this.Config == null)
            {
                var construct = typeof(TConfig).GetConstructor(new Type[] { typeof(TinyIoCContainer), typeof(string) });
                if (construct == null)
                {
                    construct = typeof(TConfig).GetConstructor(new Type[] { typeof(string) });
                    if (construct == null)
                    {
                        throw new Exception("No usable constructor for config type found (" + typeof(TConfig).ToString() + ")!");
                    }

                    this.Config = (TConfig)construct.Invoke(new object[] { name });
                }
                else
                {
                    this.Config = (TConfig)construct.Invoke(new object[] { container, name });
                }
            }

            InitializeOverlay();
            InitializeTimer();
            InitializeConfigHandlers();
            UpdateHotKey();
        }

        public abstract Control CreateConfigControl();

        /// <summary>
        /// オーバーレイの更新を開始します。
        /// </summary>
        public virtual void Start()
        {
            if (Config == null) throw new InvalidOperationException("Configuration is missing!");

            timer.Start();
        }

        /// <summary>
        /// オーバーレイの更新を停止します。
        /// </summary>
        public virtual void Stop()
        {
            timer.Stop();
        }

        /// <summary>
        /// オーバーレイを初期化します。
        /// </summary>
        protected virtual void InitializeOverlay()
        {
            try
            {
                this.Overlay = new OverlayForm(this.Name, Config.Uuid.ToString(), Config.Url, this.Config.MaxFrameRate, new OverlayApi(container, this));

                // 画面外にウィンドウがある場合は、初期表示位置をシステムに設定させる
                if (!Util.IsOnScreen(this.Overlay))
                {
                    this.Overlay.StartPosition = FormStartPosition.WindowsDefaultLocation;
                }
                else
                {
                    this.Overlay.Location = this.Config.Position;
                }

                this.Overlay.Text = this.Name;
                this.Overlay.Size = this.Config.Size;
                this.Overlay.IsClickThru = this.Config.IsClickThru;

                // イベントハンドラを設定
                this.Overlay.Renderer.BrowserError += (o, e) =>
                {
                    Log(LogLevel.Error, "BrowserError: {0}, {1}, {2}", e.ErrorCode, e.ErrorText, e.Url);
                };
                this.Overlay.Renderer.BrowserStartLoading += (o, e) =>
                {
                    // Drop the event subscriptions from the previous page.
                    UnsubscribeAll();
                };
                this.Overlay.Renderer.BrowserLoad += (o, e) =>
                {
                    Log(LogLevel.Debug, "BrowserLoad: {0}: {1}", e.HttpStatusCode, e.Url);
                    NotifyOverlayState();
                };
                this.Overlay.Renderer.BrowserConsoleLog += (o, e) =>
                {
                    if (Config.LogConsoleMessages)
                        Log(LogLevel.Info, "BrowserConsole: {0} (Source: {1}, Line: {2})", e.Message, e.Source, e.Line);
                };

                if (!this.Config.Disabled)
                {
                    this.Overlay.Renderer.BeginRender();
                }

                this.Overlay.Show();

                this.Overlay.Visible = this.Config.IsVisible;
                this.Overlay.Locked = this.Config.IsLocked;
                this.Overlay.MaxFrameRate = this.Config.MaxFrameRate;
            }
            catch (Exception ex)
            {
                Log(LogLevel.Error, "InitializeOverlay: {0} {1}", this.Name, ex);
            }
        }

        private ModifierKeys GetModifierKey(Keys modifier)
        {
            ModifierKeys modifiers = new ModifierKeys();
            if ((modifier & Keys.Shift) == Keys.Shift)
            {
                modifiers |= ModifierKeys.Shift;
            }
            if ((modifier & Keys.Control) == Keys.Control)
            {
                modifiers |= ModifierKeys.Control;
            }
            if ((modifier & Keys.Alt) == Keys.Alt)
            {
                modifiers |= ModifierKeys.Alt;
            }
            if ((modifier & Keys.LWin) == Keys.LWin || (modifier & Keys.RWin) == Keys.RWin)
            {
                modifiers |= ModifierKeys.Win;
            }
            return modifiers;
        }

        private void UpdateHotKey()
        {
            var hook = container.Resolve<KeyboardHook>();

            // Clear the old hotkeys
            foreach (var cb in hotKeyCallbacks)
            {
                hook.UnregisterHotKey(cb);
            }
            hotKeyCallbacks.Clear();

            foreach (var entry in Config.GlobalHotkeys)
            {
                if (entry.Enabled && entry.Key != Keys.None)
                {
                    var modifierKeys = GetModifierKey(entry.Modifiers);
                    Action cb = null;

                    switch (entry.Type)
                    {
                        case GlobalHotkeyType.ToggleVisible:
                            cb = () => this.Config.IsVisible = !this.Config.IsVisible;
                            break;
                        case GlobalHotkeyType.ToggleClickthru:
                            cb = () => this.Config.IsClickThru = !this.Config.IsClickThru;
                            break;
                        case GlobalHotkeyType.ToggleLock:
                            cb = () => this.Config.IsLocked = !this.Config.IsLocked;
                            break;
                        case GlobalHotkeyType.ToogleEnabled:
                            cb = () => this.Config.Disabled = !this.Config.Disabled;
                            break;
                        default:
                            cb = () => this.Config.IsVisible = !this.Config.IsVisible;
                            break;
                    }

                    hotKeyCallbacks.Add(cb);
                    try
                    {
                        hook.RegisterHotKey(modifierKeys, entry.Key, cb);
                    }
                    catch (Exception e)
                    {
                        Log(LogLevel.Error, Resources.OverlayBaseRegisterHotkeyError, e.Message);
                        hotKeyCallbacks.Remove(cb);
                    }
                }
            }
        }

        /// <summary>
        /// タイマーを初期化します。
        /// </summary>
        protected virtual void InitializeTimer()
        {
            timer = new System.Timers.Timer();
            timer.Interval = 1000;
            timer.Elapsed += (o, e) =>
            {
                try
                {
                    Update();
                }
                catch (Exception ex)
                {
                    Log(LogLevel.Error, "Update: {0}", ex.ToString());
                }
            };
        }

        /// <summary>
        /// 設定クラスのイベントハンドラを設定します。
        /// </summary>
        protected virtual void InitializeConfigHandlers()
        {
            this.Config.VisibleChanged += (o, e) =>
            {
                if (this.Overlay != null) this.Overlay.Visible = e.IsVisible;
            };

            this.Config.ClickThruChanged += (o, e) =>
            {
                if (this.Overlay != null) this.Overlay.IsClickThru = e.IsClickThru;
            };
            this.Config.LockChanged += (o, e) =>
            {
                if (this.Overlay != null) this.Overlay.Locked = e.IsLocked;
                NotifyOverlayState();
            };
            this.Config.MaxFrameRateChanged += (o, e) =>
            {
                if (this.Overlay != null) this.Overlay.MaxFrameRate = this.Config.MaxFrameRate;
            };
            this.Config.GlobalHotkeyChanged += (o, e) => UpdateHotKey();
            this.Config.UrlChanged += (o, e) =>
            {
                Navigate(e.NewUrl);
            };
        }

        /// <summary>
        /// オーバーレイを更新します。
        /// </summary>
        protected abstract void Update();

        /// <summary>
        /// オーバーレイのインスタンスを破棄します。
        /// </summary>
        public virtual void Dispose()
        {
            try
            {
                dispatcher.UnsubscribeAll(this);

                if (this.timer != null)
                {
                    this.timer.Stop();
                }
                if (this.Overlay != null)
                {
                    this.Overlay.Close();
                    this.Overlay.Dispose();
                }

                var hook = container.Resolve<KeyboardHook>();
                foreach (var cb in hotKeyCallbacks)
                {
                    hook.UnregisterHotKey(cb);
                }
            }
            catch (Exception ex)
            {
                Log(LogLevel.Error, "Dispose: {0}", ex);
            }
        }

        public virtual void Navigate(string url)
        {
            if (this.Overlay != null)
            {
                this.Overlay.Url = url;
            }
        }

        public virtual void Reload()
        {
            if (this.Overlay != null)
            {
                this.Overlay.Reload();
            }
        }

        protected void Log(LogLevel level, string message)
        {
            if (logger != null && !disableLog)
            {
                if (message.Contains("Xilium.CefGlue"))
                {
                    Log(LogLevel.Error, string.Format(Resources.IncompatibleAddon, this));
                    Stop();
                    disableLog = true;
                }

                logger.Log(level, "{0}: {1}", this.Name, message);
            }
        }

        protected void Log(LogLevel level, string format, params object[] args)
        {
            Log(level, string.Format(format, args));
        }


        public void SavePositionAndSize()
        {
            this.Config.Position = this.Overlay.Location;
            this.Config.Size = this.Overlay.Size;
        }

        public void ExecuteScript(string script)
        {
            if (this.Overlay != null &&
                this.Overlay.Renderer != null)
            {
                this.Overlay.Renderer.ExecuteScript(script);
            }
        }

        private void NotifyOverlayState()
        {
            ExecuteScript(string.Format(
                "document.dispatchEvent(new CustomEvent('onOverlayStateUpdate', {{ detail: {{ isLocked: {0} }} }}));",
                this.Config.IsLocked ? "true" : "false"));
        }

        public void SendMessage(string message)
        {
            ExecuteScript(string.Format(
                "document.dispatchEvent(new CustomEvent('onBroadcastMessageReceive', {{ detail: {{ message: \"{0}\" }} }}));",
                Util.CreateJsonSafeString(message)));
        }

        public virtual void OverlayMessage(string message)
        {
        }

        public virtual void SetAcceptFocus(bool accept)
        {
            Overlay.SetAcceptFocus(accept);
        }

        // Event Source stuff

        public virtual void HandleEvent(JObject e)
        {
            ExecuteScript("if(window.__OverlayCallback) __OverlayCallback(" + e.ToString(Formatting.None) + ");");
        }

        public void Subscribe(string eventType)
        {
            dispatcher.Subscribe(eventType, this);
        }

        public void Unsubscribe(string eventType)
        {
            dispatcher.Unsubscribe(eventType, this);
        }

        public void UnsubscribeAll()
        {
            dispatcher.UnsubscribeAll(this);
        }

        public virtual void InitModernAPI()
        {

        }

        public virtual Bitmap Screenshot()
        {
            return Overlay.Renderer.Screenshot();
        }
    }
}
