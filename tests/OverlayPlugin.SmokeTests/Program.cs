using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Forms;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RainbowMage.HtmlRenderer;
using RainbowMage.OverlayPlugin;
using RainbowMage.OverlayPlugin.Updater;

namespace RainbowMage.OverlayPlugin.SmokeTests
{
    internal static class Program
    {
        private static string root;
        private static string state;
        private static readonly Color BackdropColor = Color.Magenta;

        [STAThread]
        private static int Main(string[] args)
        {
            if (args.Length != 2 || (args[0] != "install" && args[0] != "reuse" && args[0] != "render" && args[0] != "websocket"))
            {
                Console.Error.WriteLine("Usage: OverlayPlugin.SmokeTests.exe install|reuse|render|websocket STATE_DIRECTORY");
                return 2;
            }

            root = AppDomain.CurrentDomain.BaseDirectory;
            state = Path.GetFullPath(args[1]);
            Directory.CreateDirectory(state);
            Console.WriteLine("CLR: " + Environment.Version + "; 64-bit: " + Environment.Is64BitProcess);
            var cef = Path.Combine(state, "OverlayPluginCef", "x64");
            Environment.SetEnvironmentVariable("PATH", cef + ";" + Path.Combine(root, "libs") + ";" + Environment.GetEnvironmentVariable("PATH"));
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.ThrowException);
            using (var resolver = new AssemblyResolver(new[] { Path.Combine(root, "libs"), cef }))
            using (var window = new Form
            {
                Text = "OverlayPlugin CI",
                ClientSize = new Size(480, 320),
                StartPosition = FormStartPosition.Manual,
                Location = new Point(20, 20),
                FormBorderStyle = FormBorderStyle.None,
                BackColor = BackdropColor,
                TopMost = true,
            })
            {
                int exitCode = 1;
                window.Shown += async (sender, e) =>
                {
                    try
                    {
                        await Run(args[0], window, cef);
                        Console.WriteLine("PASS " + args[0]);
                        exitCode = 0;
                    }
                    catch (Exception ex)
                    {
                        Console.Error.WriteLine(ex);
                    }
                    finally
                    {
                        window.Close();
                    }
                };
                Application.Run(window);
                return exitCode;
            }
        }

        // Resolve product dependencies only after the production assembly resolver is registered.
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static async Task Run(string mode, Form window, string cef)
        {
            if (mode == "install")
            {
                Console.WriteLine("CEF source: " + CefInstaller.GetUrl());
                var archive = Path.Combine(state, "cef.7z");
                // shortcut: ACT's HTTP dispatcher is bypassed, replace this when testing a fully initialized ACT host.
                using (var client = new HttpClient())
                using (var response = await client.GetAsync(CefInstaller.GetUrl(), HttpCompletionOption.ResponseHeadersRead))
                {
                    response.EnsureSuccessStatusCode();
                    using (var output = File.Create(archive))
                    {
                        await response.Content.CopyToAsync(output);
                    }
                }
                Check(await CefInstaller.InstallCef(cef, archive), "CEF installation failed");
                Check(await CefInstaller.EnsureCef(cef), "Installed CEF is incomplete");
                return;
            }
            if (mode == "reuse")
            {
                Check(await CefInstaller.EnsureCef(cef), "Installed CEF could not be reused");
                return;
            }
            if (mode == "websocket")
            {
                await TestWebSocket();
                return;
            }
            await TestRendering(window, cef);
        }

        // Keep CefSharp out of the install mode's JIT dependency graph.
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static async Task TestRendering(Form window, string cef)
        {
            TestDesktopAssertions();
            Renderer.Initialize(root, state, reportErrors: false);
            try
            {
                foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                {
                    if (assembly.GetName().Name.StartsWith("CefSharp"))
                        Console.WriteLine(assembly.FullName + " " + assembly.Location);
                }
#if DEBUG
                string native = Path.Combine(root, "libs", "libcef.dll");
#else
                string native = Path.Combine(cef, "libcef.dll");
#endif
                Console.WriteLine("Chromium: " + FileVersionInfo.GetVersionInfo(native).FileVersion + " " + native);
                await TestOverlay(window);
                await TestControl(window);
            }
            finally
            {
                Renderer.Shutdown();
            }
        }

        private static async Task TestOverlay(Form backdrop)
        {
            var api = new TestApi();
            using (var overlay = new OverlayForm("CI overlay", "ci-uuid", Fixture("initial"), api: api))
            {
                overlay.ClientSize = new Size(320, 200);
                overlay.Location = backdrop.PointToScreen(new Point(20, 20));
                overlay.TopMost = true;
                Observe(overlay.Renderer);
                overlay.Show();
                overlay.Renderer.BeginRender();
                await Page(api, 320, 200, "initial");
                await Capture(overlay.Renderer, overlay, "overlay-initial");

                api.Reset();
                overlay.ClientSize = new Size(400, 240);
                overlay.Url = Fixture("resize");
                await Page(api, 400, 240, "resize");
                await Capture(overlay.Renderer, overlay, "overlay-resize");

                api.Reset();
                overlay.Reload();
                await Page(api, 400, 240, "resize");
                await Capture(overlay.Renderer, overlay, "overlay-reload");

                api.Reset();
                overlay.Renderer.BeginRender();
                await Page(api, 400, 240, "resize");
                await Capture(overlay.Renderer, overlay, "overlay-recreated");
                overlay.Close();
            }
        }

        private static async Task TestControl(Form window)
        {
            var api = new TestApi();
            using (var control = new OverlayControl
            {
                Size = new Size(320, 200),
                Location = new Point(20, 20),
            })
            {
                window.Controls.Add(control);
                window.Activate();
                control.Init(Fixture("control"), api: api);
                Observe(control.Renderer);
                await Page(api, 320, 200, "control");
                await CaptureDesktop(control, "control");
            }
        }

        private static void Observe(Renderer renderer)
        {
            renderer.BrowserConsoleLog += (sender, e) => Console.WriteLine("Browser: " + e.Message);
            renderer.BrowserError += (sender, e) => Console.Error.WriteLine("Browser error: " + e.ErrorCode + " " + e.ErrorText);
        }

        private static string Fixture(string stage)
        {
            return new Uri(Path.Combine(root, "fixtures", "render.html")).AbsoluteUri + "?stage=" + stage;
        }

        private static async Task Page(TestApi api, int width, int height, string stage)
        {
            var page = JObject.Parse(await Within(api.Reported.Task));
            Check((string)page["stage"] == stage, "Wrong document loaded: " + page);
            Check((int)page["width"] == width && (int)page["height"] == height, "Wrong viewport: " + page);
            Check((string)page["echo"] == "ci-echo", "JavaScript binding round trip failed");
            Check((string)page["name"] == (stage == "control" ? "" : "CI overlay"), "Wrong overlay name");
            Check((string)page["uuid"] == (stage == "control" ? "" : "ci-uuid"), "Wrong overlay UUID");
            Console.WriteLine("Page: " + page.ToString(Formatting.None));
        }

        private static async Task Capture(Renderer renderer, OverlayForm overlay, string name)
        {
            // Screenshot waits for the next paint; the fixture animates so a paint is guaranteed.
            using (var bitmap = await Within(Task.Run(() => renderer.Screenshot())))
            {
                Check(bitmap != null, "Missing offscreen frame");
                bitmap.Save(Path.Combine(state, name + ".png"), ImageFormat.Png);
                Check(bitmap.Width == overlay.Width && bitmap.Height == overlay.Height, "Wrong frame size");
                CheckGreen(bitmap.GetPixel(40, 40));
                Check(bitmap.GetPixel(160, 100).A == 0, "Transparent region became opaque");
            }
            await CaptureDesktop(overlay, name + "-desktop");
        }

        private static async Task CaptureDesktop(Control control, string name)
        {
            var transparent = control is OverlayForm;
            using (var bitmap = new Bitmap(control.Width, control.Height, PixelFormat.Format32bppRgb))
            using (var graphics = Graphics.FromImage(bitmap))
            {
                // The offscreen screenshot can arrive before the window manager presents it.
                for (int attempt = 0; attempt < 50; attempt++)
                {
                    CopyDesktop(graphics, control.PointToScreen(Point.Empty), control.Size);
                    if (IsExpectedDesktopFrame(bitmap, transparent)) break;
                    await Task.Delay(100);
                }
                bitmap.Save(Path.Combine(state, name + ".png"), ImageFormat.Png);
                Check(IsExpectedDesktopFrame(bitmap, transparent),
                    "Unexpected desktop frame: sample=" + bitmap.GetPixel(40, 40) +
                    ", transparent region=" + bitmap.GetPixel(160, 100));
            }
        }

        private static void CopyDesktop(Graphics graphics, Point location, Size size)
        {
            var screen = NativeMethods.GetDC(IntPtr.Zero);
            Check(screen != IntPtr.Zero, "Could not acquire the desktop device context");
            try
            {
                var target = graphics.GetHdc();
                try
                {
                    // CopyFromScreen rejects combined raster operations; include layered windows explicitly.
                    if (!NativeMethods.BitBlt(target, 0, 0, size.Width, size.Height, screen,
                        location.X, location.Y, NativeMethods.SRCCOPY | NativeMethods.CAPTUREBLT))
                    {
                        throw new Win32Exception();
                    }
                }
                finally
                {
                    graphics.ReleaseHdc(target);
                }
            }
            finally
            {
                NativeMethods.ReleaseDC(IntPtr.Zero, screen);
            }
        }

        private static bool IsExpectedDesktopFrame(Bitmap bitmap, bool transparent)
        {
            return bitmap.GetPixel(40, 40).ToArgb() == Color.Lime.ToArgb() &&
                (!transparent || bitmap.GetPixel(160, 100).ToArgb() == BackdropColor.ToArgb());
        }

        private static void TestDesktopAssertions()
        {
            using (var bitmap = new Bitmap(320, 200, PixelFormat.Format32bppRgb))
            using (var graphics = Graphics.FromImage(bitmap))
            {
                graphics.Clear(BackdropColor);
                Check(!IsExpectedDesktopFrame(bitmap, true), "A missing overlay passed the desktop check");
                graphics.FillRectangle(Brushes.Lime, 0, 0, 80, 80);
                Check(IsExpectedDesktopFrame(bitmap, true), "A transparent overlay failed the desktop check");
                graphics.FillRectangle(Brushes.Black, 160, 100, 1, 1);
                Check(!IsExpectedDesktopFrame(bitmap, true), "An opaque overlay passed the desktop check");
            }
        }

        private static void CheckGreen(Color pixel)
        {
            Check(pixel.A == 255 && pixel.R == 0 && pixel.G == 255 && pixel.B == 0, "Expected green pixel, got " + pixel);
        }

        private static async Task TestWebSocket()
        {
            var container = new TinyIoCContainer();
            var logger = new Logger();
            logger.RegisterListener(entry => Console.WriteLine("Server: " + entry.Level + " " + entry.Message));
            container.Register<ILogger>(logger);
            var config = new PluginConfig(Path.Combine(state, "config.json"), container)
            {
                WSServerIP = "127.0.0.1",
                WSServerPort = 18766,
                WSServerSSL = false,
            };
            container.Register<IPluginConfig>(config);
            var dispatcher = new EventDispatcher(container);
            container.Register(dispatcher);
            dispatcher.RegisterEventType("CombatData");
            dispatcher.RegisterHandler("smokeEcho", request => new JObject { ["value"] = request["value"] });
            using (var server = new WSServer(container))
            using (var timer = new Timer { Interval = 200 })
            {
                timer.Tick += (sender, e) => dispatcher.DispatchEvent(new JObject { ["type"] = "CombatData", ["damage"] = 123 });
                server.Start();
                Check(server.IsRunning() && !server.IsFailed(), "WebSocket listener failed");
                timer.Start();
                File.WriteAllText(Path.Combine(state, "websocket-ready"), "ready");
                var deadline = DateTime.UtcNow.AddSeconds(60);
                while (!File.Exists(Path.Combine(state, "websocket-stop")))
                {
                    Check(DateTime.UtcNow < deadline, "External browser timed out");
                    await Task.Delay(100);
                }
                timer.Stop();
                server.Stop();
            }
        }

        private static async Task<T> Within<T>(Task<T> task)
        {
            Check(await Task.WhenAny(task, Task.Delay(15000)) == task, "Browser operation timed out");
            return await task;
        }

        private static void Check(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }
    }

    public sealed class TestApi
    {
        public TaskCompletionSource<string> Reported = new TaskCompletionSource<string>();

        public string Echo(string value)
        {
            return value;
        }

        public void Report(string json)
        {
            Reported.TrySetResult(json);
        }

        public void Reset()
        {
            Reported = new TaskCompletionSource<string>();
        }
    }
}
