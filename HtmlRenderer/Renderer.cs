using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;
using System.Reflection;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using CefSharp.OffScreen;
using CefSharp.Structs;
using CefSharp.Enums;
using CefSharp;
using CefSharp.Internals;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Threading.Tasks;

namespace RainbowMage.HtmlRenderer
{
    public class Renderer : IDisposable
    {
        const string CRASH_SERVER = "https://sentry.gruenprint.de/api/12/minidump/?sentry_key=5742b37c4b71484e9ba590c6a6a0e137";

        public event EventHandler<BrowserErrorEventArgs> BrowserError;
        public event EventHandler<BrowserLoadEventArgs> BrowserStartLoading;
        public event EventHandler<BrowserLoadEventArgs> BrowserLoad;
        public event EventHandler<BrowserConsoleLogEventArgs> BrowserConsoleLog;

        private BrowserWrapper _browser;
        private bool _isWindowless;
        protected IRenderTarget _target;
        private object _api;
        private Func<int, int, bool> _ctxMenuCallback = null;
        private List<String> scriptQueue = new List<string>();
        private string urlToLoad = null;
        private string lastUrl = null;

        private int clickCount;
        private MouseButtonType lastClickButton;
        private DateTime lastClickTime;
        private int lastClickPosX;
        private int lastClickPosY;
        private string overlayName;
        private string overlayUuid;
        public Region DraggableRegion;

        public Renderer(string overlayName, string overlayUuid, string url, IRenderTarget target, object api)
        {
            this.overlayName = overlayName;
            this.overlayUuid = overlayUuid;
            this._target = target;
            this.lastUrl = url;
            this._api = api;

            InitBrowser();
        }

        public void InitBrowser()
        {
            this._browser = new BrowserWrapper(lastUrl ?? "about:blank", automaticallyCreateBrowser: false, target: _target);
            _browser.RequestHandler = new CustomRequestHandler(this);
            _browser.MenuHandler = new ContextMenuHandler(_ctxMenuCallback);
            _browser.BrowserInitialized += _browser_BrowserInitialized;
            _browser.FrameLoadStart += Browser_FrameLoadStart;
            _browser.FrameLoadEnd += Browser_FrameLoadEnd;
            _browser.LoadError += Browser_LoadError;
            _browser.ConsoleMessage += Browser_ConsoleMessage;
            _browser.DragHandler = new DragHandler(this);

            if (_api != null)
            {
                SetApi(_api);
            }
        }

        public void SetApi(object api)
        {
            _api = api;
            if (api != null)
                _browser.JavascriptObjectRepository.Register("OverlayPluginApi", api, isAsync: true);
        }

        public void SetContextMenuCallback(Func<int, int, bool> ctxMenuCallback)
        {
            _ctxMenuCallback = ctxMenuCallback;
            _browser.MenuHandler = new ContextMenuHandler(ctxMenuCallback);
        }

        private void _browser_BrowserInitialized(object sender, EventArgs e)
        {
            // Make sure we use the correct size for rendering. CEF sometimes ignores the size passed in WindowInfo (see BeginRender()).
            Resize(_target.Width, _target.Height);
        }

        private void Browser_FrameLoadStart(object sender, FrameLoadStartEventArgs e)
        {
            if (!e.Frame.IsMain)
                return;

            lastUrl = e.Url;

            if (_api != null)
            {
                var initScript = @"(async () => {
                    await CefSharp.BindObjectAsync('OverlayPluginApi');
                    OverlayPluginApi.overlayName = " + JsonConvert.SerializeObject(this.overlayName) + @";
                    OverlayPluginApi.overlayUuid = " + JsonConvert.SerializeObject(this.overlayUuid) + @";
                    OverlayPluginApi.ready = true;
                })();";
                e.Frame.ExecuteJavaScriptAsync(initScript, "init");
            }

            foreach (var item in this.scriptQueue)
            {
                e.Frame.ExecuteJavaScriptAsync(item, "injectOnLoad");
            }
            this.scriptQueue.Clear();

            try
            {
                BrowserStartLoading?.Invoke(this, new BrowserLoadEventArgs(0, e.Url));
            }
            catch (Exception ex)
            {
                BrowserConsoleLog?.Invoke(this, new BrowserConsoleLogEventArgs(ex.ToString(), "", 1));
            }
        }

        private void Browser_LoadError(object sender, LoadErrorEventArgs e)
        {
            try
            {
                BrowserError?.Invoke(sender, new BrowserErrorEventArgs(e.ErrorCode, e.ErrorText, e.FailedUrl));
            }
            catch (Exception ex)
            {
                BrowserConsoleLog?.Invoke(this, new BrowserConsoleLogEventArgs(ex.ToString(), "", 1));
            }
        }

        public void Browser_ConsoleMessage(object sender, ConsoleMessageEventArgs e)
        {
            // Ignore FLoC exception errors. CEF doesn't include FLoC code which means that it doesn't understand
            // the FLoC exception rule. However, since it can't use FLoC to begin with, that's not an issue either way.
            if (!e.Message.StartsWith("Error with Permissions-Policy header:"))
                BrowserConsoleLog?.Invoke(sender, new BrowserConsoleLogEventArgs(e.Message, e.Source, e.Line));
        }

        private void Browser_FrameLoadEnd(object sender, FrameLoadEndEventArgs e)
        {
            if (!e.Frame.IsMain)
                return;

            if (urlToLoad != null)
            {
                _browser.Load(urlToLoad);
                urlToLoad = null;
            }

            try
            {
                BrowserLoad?.Invoke(sender, new BrowserLoadEventArgs(e.HttpStatusCode, e.Url));
            }
            catch (Exception ex)
            {
                BrowserConsoleLog?.Invoke(this, new BrowserConsoleLogEventArgs(ex.ToString(), "", 1));
            }
        }

        public void Load(string url)
        {
            if (this._browser != null && _browser.IsBrowserInitialized)
            {
                this._browser.Load(url);
            }
            else
            {
                urlToLoad = url;
            }
        }

        protected virtual WindowInfo CreateWindowInfo()
        {
            var cefWindowInfo = new WindowInfo();
            cefWindowInfo.SetAsWindowless(IntPtr.Zero);
            cefWindowInfo.Width = _target.Width;
            cefWindowInfo.Height = _target.Height;

            return cefWindowInfo;
        }

        public void BeginRender()
        {
            EndRender();

            var cefWindowInfo = CreateWindowInfo();
            _isWindowless = cefWindowInfo.WindowlessRenderingEnabled;

            var cefBrowserSettings = new BrowserSettings();
            cefBrowserSettings.WindowlessFrameRate = _target.MaxFrameRate;
            _browser.CreateBrowser(cefWindowInfo, cefBrowserSettings);

            cefBrowserSettings.Dispose();
            cefWindowInfo.Dispose();
        }

        public void EndRender()
        {
            if (_browser != null && _browser.IsBrowserInitialized)
            {
                _browser.GetBrowser().CloseBrowser(true);
                _browser.GetBrowserHost().CloseBrowser(true);
                _browser.Dispose();

                InitBrowser();
            }
        }

        public void SetMaxFramerate(int fps)
        {
            if (_browser != null && _browser.IsBrowserInitialized)
            {
                _browser.GetBrowserHost().WindowlessFrameRate = fps;
            }
        }

        public void Reload()
        {
            if (this._browser != null && _browser.IsBrowserInitialized)
            {
                this._browser.Reload();
            }
        }

        public void Resize(int width, int height)
        {
            if (this._browser != null)
            {
                this._browser.Size = new System.Drawing.Size(width, height);

                if (!this._isWindowless && this._browser.IsBrowserInitialized)
                {
                    this._browser.Resize(width, height);
                }
            }
        }

        public void SetZoomLevel(double zoom)
        {
            if (this._browser != null && this._browser.IsBrowserInitialized)
            {
                this._browser.SetZoomLevel(zoom);
            }
        }

        public void SetMuted(bool muted)
        {
            if (this._browser != null && this._browser.IsBrowserInitialized)
            {
                this._browser.GetBrowserHost().SetAudioMuted(muted);
            }
        }

        public void NotifyMoveStarted()
        {
            if (this._browser != null && this._browser.IsBrowserInitialized)
            {
                this._browser.GetBrowserHost().NotifyMoveOrResizeStarted();
            }
        }

        public void SetVisible(bool visible)
        {
            if (this._browser != null && this._browser.IsBrowserInitialized)
            {
                // Sometimes, CEF stops emitting OnPaint events after this call.
                // Not sure why, probably a timing issue.
                // TODO: Debug this further. Will probably require debugging libcef.dll directly.
                //       Modifying _browser.Size immidiately after the WasHidden() call leads to a crash in libcef.dll,
                //       that might be a good place to start investigating.
                //Task.Run(() => this._browser.GetBrowserHost().WasHidden(!visible));
            }
        }

        public void SendMouseMove(int x, int y, MouseButtonType button)
        {
            if (this._browser != null && this._browser.IsBrowserInitialized)
            {
                var host = this._browser.GetBrowserHost();
                var modifiers = CefEventFlags.None;
                if (button == MouseButtonType.Left)
                {
                    modifiers = CefEventFlags.LeftMouseButton;
                }
                else if (button == MouseButtonType.Middle)
                {
                    modifiers = CefEventFlags.MiddleMouseButton;
                }
                else if (button == MouseButtonType.Right)
                {
                    modifiers = CefEventFlags.RightMouseButton;
                }

                var mouseEvent = new MouseEvent(x, y, modifiers);
                host.SendMouseMoveEvent(mouseEvent, false);
            }
        }

        public void SendMouseLeave()
        {
            if (this._browser != null && this._browser.IsBrowserInitialized)
            {
                this._browser.GetBrowserHost().SendMouseMoveEvent(new MouseEvent(0, 0, 0), true);
            }
        }

        public void SendMouseUpDown(int x, int y, MouseButtonType button, bool isMouseUp)
        {
            if (this._browser != null && this._browser.IsBrowserInitialized)
            {
                var host = this._browser.GetBrowserHost();

                if (!isMouseUp)
                {
                    if (IsContinuousClick(x, y, button))
                    {
                        clickCount++;
                    }
                    else
                    {
                        clickCount = 1;
                    }
                }

                var mouseEvent = new MouseEvent(x, y, CefEventFlags.None);
                host.SendMouseClickEvent(mouseEvent, button, isMouseUp, clickCount);

                lastClickPosX = x;
                lastClickPosY = y;
                lastClickButton = button;
                lastClickTime = DateTime.Now;
            }
        }

        public void SendMouseWheel(int x, int y, int delta, bool isVertical)
        {
            if (this._browser != null && this._browser.IsBrowserInitialized)
            {
                var host = this._browser.GetBrowserHost();
                var mouseEvent = new MouseEvent(x, y, CefEventFlags.None);
                host.SendMouseWheelEvent(mouseEvent, isVertical ? delta : 0, !isVertical ? delta : 0);
            }
        }

        private bool IsContinuousClick(int x, int y, MouseButtonType button)
        {
            // ダブルクリックとして認識するクリックの間隔よりも大きかった場合は継続的なクリックとみなさない
            var delta = (DateTime.Now - lastClickTime).TotalMilliseconds;
            if (delta > System.Windows.Forms.SystemInformation.DoubleClickTime)
            {
                return false;
            }

            // クリック位置が違う、もしくはボタンが違う場合にも継続的なクリックとはみなさない
            if (lastClickPosX != x || lastClickPosY != y || lastClickButton != button)
            {
                return false;
            }

            return true;
        }

        public void SendKeyEvent(KeyEvent keyEvent)
        {
            if (this._browser != null && this._browser.IsBrowserInitialized)
            {
                var host = this._browser.GetBrowserHost();

                host.SendKeyEvent(keyEvent);
            }
        }

        public void showDevTools(bool firstwindow = true)
        {
            if (_browser != null && _browser.IsBrowserInitialized)
            {
                WindowInfo wi = new WindowInfo();
                wi.SetAsPopup(_browser.GetBrowserHost().GetWindowHandle(), "DevTools");
                _browser.GetBrowserHost().ShowDevTools(wi);
            }
        }

        public Bitmap Screenshot()
        {
            if (_browser != null && _browser.IsBrowserInitialized)
            {
                return _browser.Screenshot();
            }

            return null;
        }

        public async void ClearCache()
        {
            try
            {
                if (_browser == null) return;

                // Find the first / after https://
                var slashAfterHost = lastUrl.IndexOf("/", 9);

                // If we can't build an origin, there's nothing we can do.
                if (slashAfterHost < 0) return;

                var origin = lastUrl.Substring(0, slashAfterHost);
                var dtc = DevToolsExtensions.GetDevToolsClient(_browser);
                var result = await dtc.Storage.ClearDataForOriginAsync(origin, "appcache,cookies,file_systems,cache_storage");

                BrowserConsoleLog?.Invoke(null, new BrowserConsoleLogEventArgs(result.Success ? result.ResponseAsJsonString : "fail", "", 0));

                // Reload the overlay to refill the cache and replace that potentially corrupted resources.
                Reload();
            }
            catch (Exception ex)
            {
                BrowserConsoleLog?.Invoke(null, new BrowserConsoleLogEventArgs($"Failed to clear cache: {ex}", "", 0));
            }
        }

        public void Dispose()
        {
            if (this._browser != null)
            {
                this._browser.Dispose();
                this._browser = null;
            }
        }

        static bool initialized = false;

        public static void Initialize(string pluginDirectory, string appDataDirectory, bool reportErrors)
        {
            if (!initialized)
            {
                if (reportErrors)
                {
                    try
                    {
                        // Enable the Crashpad reporter. This *HAS* to happen before libcef.dll is loaded.
                        EnableErrorReports(appDataDirectory);
                    }
                    catch (Exception)
                    {
                        // TODO: Log this exception.
                    }
                }

#if DEBUG
                var cefPath = Path.Combine(pluginDirectory, "libs", Environment.Is64BitProcess ? "x64" : "x86");
#else
                var cefPath = Path.Combine(appDataDirectory, "OverlayPluginCef", Environment.Is64BitProcess ? "x64" : "x86");
#endif

                var lang = System.Globalization.CultureInfo.CurrentCulture.Name;
                var langPak = Path.Combine(cefPath, "locales", lang + ".pak");

                // Fall back to en-US if we can't find the current locale.
                if (!File.Exists(langPak))
                {
                    lang = "en-US";
                }

                var cefSettings = new CefSettings
                {
                    WindowlessRenderingEnabled = true,
                    Locale = lang,
                    CachePath = Path.Combine(appDataDirectory, "OverlayPluginCache"),
                    MultiThreadedMessageLoop = true,
                    LogFile = Path.Combine(appDataDirectory, "OverlayPluginCEF.log"),
#if DEBUG
                    LogSeverity = LogSeverity.Info,
#else
                    LogSeverity = LogSeverity.Error,
#endif
                    BrowserSubprocessPath = Path.Combine(cefPath, "CefSharp.BrowserSubprocess.exe"),
                };

                try
                {
                    File.WriteAllText(cefSettings.LogFile, "");
                }
                catch (Exception)
                {
                    // Ignore; if we can't open the log, CEF can't do it either which means that we don't have to worry about log size.
                }

                // Necessary to avoid input lag with a framerate limit below 60.
                cefSettings.CefCommandLineArgs["enable-begin-frame-scheduling"] = "1";

                // Allow websites to play sound even if the user never interacted with that site (pretty common for our overlays)
                cefSettings.CefCommandLineArgs["autoplay-policy"] = "no-user-gesture-required";

                // Disable Flash. We don't need it and it can cause issues.
                cefSettings.CefCommandLineArgs.Remove("enable-system-flash");

                cefSettings.EnableAudio();

                // Enables software compositing instead of GPU compositing -> less CPU load but no WebGL
                cefSettings.SetOffScreenRenderingBestPerformanceArgs();

                Cef.Initialize(cefSettings, performDependencyCheck: true, browserProcessHandler: null);

                initialized = true;
            }
        }

        public static void Shutdown()
        {
            if (initialized)
            {
                Cef.Shutdown();
            }
        }

        public static void EnableErrorReports(string appDataDirectory)
        {
            // Unfortunately, we can't change the path to this configuration file without using our own .exe for
            // all overlays (which would add IPC overhead) or recompiling CEF.
            // For details, see this https://bitbucket.org/chromiumembedded/cef/wiki/CrashReporting.md

            var subProcess = Path.Combine(appDataDirectory,
                                          "OverlayPluginCef",
                                          Environment.Is64BitProcess ? "x64" : "x86");
            var subCfgPath = Path.Combine(subProcess, "crash_reporter.cfg");

            var cfgPath = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
            cfgPath = Path.Combine(cfgPath, "crash_reporter.cfg");

            var version = Assembly.GetExecutingAssembly().GetName().Version.ToString();

            var cfgText = $@"
# This file was generated by OverlayPlugin to configure CEF's Crahspad reporter.
# For details on the purpose of this file see https://bitbucket.org/chromiumembedded/cef/wiki/CrashReporting.md.
# Please don't modify this file manually. Instead, disable it in OverlayPlugin's settings.

[Config]
ProductName=OverlayPlugin
ProductVersion={version}
AppName=ACT_OverlayPlugin
ExternalHandler={subProcess}\CefSharp.BrowserSubprocess.exe
ServerURL={CRASH_SERVER}

# Disable rate limiting so that all crashes are uploaded.
RateLimitEnabled=false
MaxUploadsPerDay=0
";
            File.WriteAllText(cfgPath, cfgText);
            File.WriteAllText(subCfgPath, cfgText);
        }

        public static void DisableErrorReports(string appDataDirectory)
        {
            var subProcess = Path.Combine(appDataDirectory,
                                          "OverlayPluginCef",
                                          Environment.Is64BitProcess ? "x64" : "x86");
            var subCfgPath = Path.Combine(subProcess, "crash_reporter.cfg");

            var cfgPath = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
            cfgPath = Path.Combine(cfgPath, "crash_reporter.cfg");

            if (File.Exists(cfgPath))
                File.Delete(cfgPath);

            if (File.Exists(subCfgPath))
                File.Delete(subCfgPath);
        }

        public void ExecuteScript(string script)
        {
            if (_browser != null && _browser.IsBrowserInitialized)
            {
                _browser.GetMainFrame().ExecuteJavaScriptAsync(script, "injected");
            }
            else
            {
                this.scriptQueue.Add(script);
            }
        }

        // IJavascriptCallback can't be used outside HtmlRenderer. This helper allows other code
        // to invoke callbacks regardless.

        public static void ExecuteCallback(object callback, object param)
        {
            if (callback.GetType().GetInterface("IJavascriptCallback") == null)
            {
                throw new Exception("Invalid parameter passed for callback!");
            }

            var cb = (IJavascriptCallback)callback;
            using (cb)
            {
                if (cb.CanExecute)
                {
                    cb.ExecuteAsync(param);
                }
            }
        }
    }

    public class BrowserErrorEventArgs : EventArgs
    {
        public string ErrorCode { get; private set; }
        public string ErrorText { get; private set; }
        public string Url { get; private set; }
        public BrowserErrorEventArgs(CefErrorCode errorCode, string errorText, string url)
        {
            this.ErrorCode = errorCode.ToString();
            this.ErrorText = errorText;
            this.Url = url;
        }
    }

    public class BrowserLoadEventArgs : EventArgs
    {
        public int HttpStatusCode { get; private set; }
        public string Url { get; private set; }
        public BrowserLoadEventArgs(int httpStatusCode, string url)
        {
            this.HttpStatusCode = httpStatusCode;
            this.Url = url;
        }
    }

    public class BrowserConsoleLogEventArgs : EventArgs
    {
        public string Message { get; private set; }
        public string Source { get; private set; }
        public int Line { get; private set; }
        public BrowserConsoleLogEventArgs(string message, string source, int line)
        {
            this.Message = message;
            this.Source = source;
            this.Line = line;
        }
    }

    public class RendererFeatureRequestEventArgs : EventArgs
    {
        public string Request { get; private set; }
        public RendererFeatureRequestEventArgs(string request)
        {
            this.Request = request;
        }
    }

    public class HandlerCallEventArgs : EventArgs
    {
        public JObject Payload { get; private set; }
        public Action<JObject> Callback { get; private set; }
        public HandlerCallEventArgs(JObject payload, Action<JObject> callback)
        {
            this.Payload = payload;
            this.Callback = callback;
        }
    }

    internal class BrowserWrapper : ChromiumWebBrowser, IRenderWebBrowser
    {
        IRenderTarget target;
        TaskCompletionSource<Bitmap> screenshotSource = null;
        object screenshotLock = new object();

        public BrowserWrapper(string address = "", BrowserSettings browserSettings = null,
            RequestContext requestContext = null, bool automaticallyCreateBrowser = true, IRenderTarget target = null) :
            base(address, browserSettings, requestContext, automaticallyCreateBrowser)
        {
            this.target = target;
        }

        public void Resize(int width, int height)
        {
            ((IWebBrowserInternal)this).BrowserAdapter.Resize(width, height);
        }

        public Bitmap Screenshot()
        {
            lock (screenshotLock)
            {
                var source = new TaskCompletionSource<Bitmap>();
                screenshotSource = source;
                source.Task.Wait();

                if (source.Task.Exception != null) throw source.Task.Exception;
                return source.Task.Result;
            }
        }

        void IRenderWebBrowser.OnPaint(PaintElementType type, Rect dirtyRect, IntPtr buffer, int width, int height)
        {
            if (screenshotSource != null)
            {
                try
                {
                    var bmp = new Bitmap(width, height);
                    var data = bmp.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
                    NativeMethods.CopyMemory(data.Scan0, buffer, (uint)(width * height * 4));
                    bmp.UnlockBits(data);

                    screenshotSource.SetResult(bmp);
                    screenshotSource = null;
                }
                catch (Exception ex)
                {
                    screenshotSource.SetException(ex);
                    screenshotSource = null;
                }
            }
            else
            {
                target.RenderFrame(type, dirtyRect, buffer, width, height);
            }
        }

        void IRenderWebBrowser.OnPopupSize(Rect rect)
        {
            target.MovePopup(rect);
        }

        void IRenderWebBrowser.OnPopupShow(bool show)
        {
            target.SetPopupVisible(show);
        }

        void IRenderWebBrowser.OnCursorChange(IntPtr cursor, CursorType type, CursorInfo customCursorInfo)
        {
            target.Cursor = new Cursor(cursor);
        }

        bool IRenderWebBrowser.GetScreenPoint(int contentX, int contentY, out int screenX, out int screenY)
        {
            screenX = (contentX + target.Location.X);
            screenY = (contentY + target.Location.Y);

            return true;
        }
    }
}
