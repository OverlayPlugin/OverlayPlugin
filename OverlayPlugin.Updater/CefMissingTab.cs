using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Forms;

namespace RainbowMage.OverlayPlugin.Updater
{
    public partial class CefMissingTab : UserControl
    {
        private string _cefPath;
        private object _pluginLoader;

        public CefMissingTab(string cefPath, object pluginLoader)
        {
            InitializeComponent();

            _cefPath = cefPath;
            _pluginLoader = pluginLoader;
            lnkManual.Text = CefInstaller.GetUrl();

            TinyIoCContainer.Current.Resolve<ILogger>().OnLog += HandleOnLog;
        }

        private void HandleOnLog(object sender, IReadOnlyCollection<LogEntry> e)
        {
            Advanced_Combat_Tracker.ActGlobals.oFormActMain.Invoke((Action)(() =>
            {
                var newText = @"{\rtf1\ansi";

                foreach (var entry in e)
                {
                    newText += entry.ToRtfString();
                }

                newText += "}";

                logBox.Rtf = newText;
            }));
        }

        private async void btnOpenManual_Click(object sender, EventArgs e)
        {
            var dialog = new OpenFileDialog();
            dialog.Filter = "CEF bundle|*.7z";
            var result = dialog.ShowDialog();

            if (result != DialogResult.OK)
                return;

            if (await CefInstaller.InstallCef(_cefPath, dialog.FileName))
            {
                Parent.Controls.Remove(this);
                _pluginLoader.GetType().GetMethod("FinishInit").Invoke(_pluginLoader, new object[] { });
            }
        }

        private async void btnStartAuto_Click(object sender, EventArgs e)
        {
            if (await CefInstaller.EnsureCef(_cefPath))
            {
                Parent.Controls.Remove(this);
                _pluginLoader.GetType().GetMethod("FinishInit").Invoke(_pluginLoader, new object[] { });
            }
        }

        private void lnkManual_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Process.Start(lnkManual.Text);
        }
    }
}
