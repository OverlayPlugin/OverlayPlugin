using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Forms;
using Advanced_Combat_Tracker;
using RainbowMage.OverlayPlugin.Updater;

namespace RainbowMage.OverlayPlugin
{
    public class PluginLoader : IActPluginV1, IDisposable
    {
        PluginMain pluginMain;
        ILogger logger;
        static AssemblyResolver asmResolver;
        string pluginDirectory;
        TabPage pluginScreenSpace;
        Label pluginStatusText;
        bool initFailed = false;
        private bool _disposed;

        public TinyIoCContainer Container { get; private set; }

        public void InitPlugin(TabPage pluginScreenSpace, Label pluginStatusText)
        {
#if DEBUG
            Control.CheckForIllegalCrossThreadCalls = true;
#endif
            pluginDirectory = GetPluginDirectory();

            if (asmResolver == null)
            {
                asmResolver = new AssemblyResolver(new List<string>
                {
                    Path.Combine(pluginDirectory, "libs"),
                    Path.Combine(pluginDirectory, "addons"),
#if TRACEPERF
                    Path.Combine(pluginDirectory, "libs", Environment.Is64BitProcess ? "x64" : "x86"),
#else
                    GetCefPath()
#endif
                });
            }

            this.pluginScreenSpace = pluginScreenSpace;
            this.pluginStatusText = pluginStatusText;

            /*
             * We explicitly load OverlayPlugin.Common here for two reasons:
             *  * To prevent a stack overflow in the assembly loaded handler when we use the logger interface.
             *  * To check that the loaded version matches.
             * OverlayPlugin.Core is needed due to the Logger class used in Initialize().
             * OverlayPlugin.Updater is necessary for the CefInstaller class.
             */
            if (!SanityChecker.LoadSaneAssembly("OverlayPlugin.Common") || !SanityChecker.LoadSaneAssembly("OverlayPlugin.Core") ||
                !SanityChecker.LoadSaneAssembly("OverlayPlugin.Updater"))
            {
                pluginStatusText.Text = Resources.FailedToLoadCommon;
                return;
            }

            pluginStatusText.Text = Resources.InitRuntime;

            Initialize();
        }

        // To prevent PluginMain from being resolved before we add our custom resolver in AssemblyResolver,
        // we separate the reference to its own method and prevent inlining.
        [MethodImpl(MethodImplOptions.NoInlining)]
        private async void Initialize()
        {
            // Use TinyIoC's singleton since we don't use multiple IoC containers
            // This won't cause issues with other plugins, as our TinyIoCContainer is namespaced to `RainbowMage.OverlayPlugin`
            Container = TinyIoCContainer.Current;

            TinyIoCAutoHelper.RegisterAssemblies(SanityChecker.GetAssemblies());

            TinyIoCAutoHelper.AutoRegisterPreInit();
            TinyIoCAutoHelper.AutoConstructPreInit();

            logger = Container.Resolve<ILogger>();

            asmResolver.ExceptionOccured += (o, e) => logger.Log(LogLevel.Error, Resources.AssemblyResolverError, e.Exception);
            asmResolver.AssemblyLoaded += (o, e) => logger.Log(LogLevel.Info, Resources.AssemblyResolverLoaded, e.LoadedAssembly.FullName);

            pluginMain = new PluginMain(pluginDirectory);

            pluginStatusText.Text = Resources.InitCef;

            SanityChecker.CheckDependencyVersions();

            await FinishInit();
        }

        public async Task FinishInit()
        {
            if (await CefInstaller.EnsureCef(GetCefPath()))
            {
                // Finally, load the html renderer. We load it here since HtmlRenderer depends on CEF which we can't load these before
                // the CefInstaller is done.
                if (SanityChecker.LoadSaneAssembly("HtmlRenderer"))
                {
                    // Since this is an async method, we could have switched threds. Make sure InitPlugin() runs on the ACT main thread.
                    await InvokeOnUIThread(async () =>
                    {
                        try
                        {
                            await pluginMain.InitPlugin(pluginScreenSpace, pluginStatusText);
                            initFailed = false;
                        }
                        catch (Exception ex)
                        {
                            // TODO: Add a log box to CefMissingTab and while CEF missing is the most likely
                            // cause for an exception here, it is not necessarily the case.
                            // logger.Log(LogLevel.Error, "Failed to init plugin: " + ex.ToString());

                            initFailed = true;

                            MessageBox.Show("Failed to init OverlayPlugin: " + ex.ToString(), "OverlayPlugin Error");
                            pluginScreenSpace.Controls.Add(new CefMissingTab(GetCefPath(), this));
                        }
                    });
                }
                else
                {
                    pluginStatusText.Text = Resources.CoreOrHtmlRendererInsane;
                }
            }
            else
            {
                pluginScreenSpace.Controls.Add(new CefMissingTab(GetCefPath(), this));
            }
        }

        public void DeInitPlugin()
        {
            if (pluginMain != null && !initFailed)
            {
                pluginMain.DeInitPlugin();
            }

            if (ActGlobals.oFormActMain.IsActClosing)
            {
                // We can only dispose the resolver once the HtmlRenderer is shut down. HtmlRenderer is only shut down if ACT is closing.
                asmResolver.Dispose();
            }
        }

        private string GetPluginDirectory()
        {
            // ACT のプラグインリストからパスを取得する
            // Assembly.CodeBase からはパスを取得できない
            var plugin = ActGlobals.oFormActMain.ActPlugins.Where(x => x.pluginObj == this).FirstOrDefault();
            if (plugin != null)
            {
                return Path.GetDirectoryName(plugin.pluginFile.FullName);
            }
            else
            {
                throw new Exception("Could not find ourselves in the plugin list!");
            }
        }

        private string GetCefPath()
        {
            return Path.Combine(ActGlobals.oFormActMain.AppDataFolder.FullName, "OverlayPluginCef", Environment.Is64BitProcess ? "x64" : "x86");
        }

        public async void SetPluginStatusText(string text)
        {
            await InvokeOnUIThread(() =>
            {
                pluginStatusText.Text = text;
            });
        }

        public static Task<T> InvokeOnUIThread<T>(Func<T> p)
        {
            return Task.Run(new Func<T>(() =>
            {
                if (ActGlobals.oFormActMain.InvokeRequired)
                {
                    return (T)ActGlobals.oFormActMain.EndInvoke(ActGlobals.oFormActMain.BeginInvoke(p));
                }
                else
                {
                    return p();
                }
            }));
        }

        public static Task InvokeOnUIThread(Action p)
        {
            return Task.Run(() =>
            {
                if (ActGlobals.oFormActMain.InvokeRequired)
                {
                    ActGlobals.oFormActMain.EndInvoke(ActGlobals.oFormActMain.BeginInvoke(p));
                }
                else
                {
                    p();
                }
            });
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    pluginMain.Dispose();
                    Container.Dispose();
                }
                _disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
