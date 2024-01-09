using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace RainbowMage.OverlayPlugin.Overlays
{
    public partial class LabelOverlayConfigPanel : UserControl
    {
        private LabelOverlayConfig config;
        private LabelOverlay overlay;

        [System.Diagnostics.CodeAnalysis.SuppressMessage(
            "Usage",
            "CA2213:Disposable fields should be disposed",
            Justification = "keyboardHook is disposed of by TinyIoCContainer")]
        private readonly KeyboardHook keyboardHook;

        public LabelOverlayConfigPanel(TinyIoCContainer container, LabelOverlay overlay)
        {
            InitializeComponent();

            this.keyboardHook = container.Resolve<KeyboardHook>();
            this.overlay = overlay;
            this.config = overlay.Config;

            SetupControlProperties();
            SetupConfigEventHandlers();
        }

        private void SetupControlProperties()
        {
            if (config.GlobalHotkeys.Count < 1)
                config.GlobalHotkeys.Add(new GlobalHotkey());

            this.checkMiniParseVisible.Checked = config.IsVisible;
            this.checkMiniParseClickthru.Checked = config.IsClickThru;
            this.checkLock.Checked = config.IsLocked;
            this.textUrl.Text = config.Url;
            this.checkEnableGlobalHotkey.Checked = config.GlobalHotkeys[0].Enabled;
            this.textGlobalHotkey.Enabled = this.checkEnableGlobalHotkey.Checked;
            this.textGlobalHotkey.Text = Util.GetHotkeyString(config.GlobalHotkeys[0].Modifiers, config.GlobalHotkeys[0].Key);
            this.textBox.Text = config.Text;
            this.checkHTML.Checked = config.HtmlModeEnabled;
        }

        private void SetupConfigEventHandlers()
        {
            this.config.VisibleChanged += (o, e) =>
            {
                this.InvokeIfRequired(() =>
                {
                    this.checkMiniParseVisible.Checked = e.IsVisible;
                });
            };
            this.config.ClickThruChanged += (o, e) =>
            {
                this.InvokeIfRequired(() =>
                {
                    this.checkMiniParseClickthru.Checked = e.IsClickThru;
                });
            };
            this.config.UrlChanged += (o, e) =>
            {
                this.InvokeIfRequired(() =>
                {
                    this.textUrl.Text = e.NewUrl;
                });
            };
            this.config.GlobalHotkeyChanged += (o, e) =>
            {
                this.InvokeIfRequired(() =>
                {
                    this.checkEnableGlobalHotkey.Checked = config.GlobalHotkeys[0].Enabled;
                    this.textGlobalHotkey.Enabled = this.checkEnableGlobalHotkey.Checked;
                    this.textGlobalHotkey.Text = Util.GetHotkeyString(config.GlobalHotkeys[0].Modifiers, config.GlobalHotkeys[0].Key);
                });
            };
            this.config.LockChanged += (o, e) =>
            {
                this.InvokeIfRequired(() =>
                {
                    this.checkLock.Checked = e.IsLocked;
                });
            };
            this.config.TextChanged += (o, e) =>
            {
                this.InvokeIfRequired(() =>
                {
                    this.textBox.Text = e.Text;
                });
            };
            this.config.HTMLModeChanged += (o, e) =>
            {
                this.InvokeIfRequired(() =>
                {
                    this.checkHTML.Checked = e.NewState;
                });
            };
        }

        private void InvokeIfRequired(Action action)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(action);
            }
            else
            {
                action();
            }
        }

        private void checkWindowVisible_CheckedChanged(object sender, EventArgs e)
        {
            this.config.IsVisible = checkMiniParseVisible.Checked;
        }

        private void checkMouseClickthru_CheckedChanged(object sender, EventArgs e)
        {
            this.config.IsClickThru = checkMiniParseClickthru.Checked;
        }

        private void textUrl_TextChanged(object sender, EventArgs e)
        {
            //this.config.Url = textMiniParseUrl.Text;
        }

        private void buttonReloadBrowser_Click(object sender, EventArgs e)
        {
            this.overlay.Navigate(this.config.Url);
        }

        private void buttonOpenDevTools_Click(object sender, EventArgs e)
        {
            this.overlay.Overlay.Renderer.showDevTools();
        }

        private void buttonOpenDevTools_RClick(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
                this.overlay.Overlay.Renderer.showDevTools(false);
        }

        private void buttonCopyActXiv_Click(object sender, EventArgs e)
        {
            var json = overlay.CreateJson();
            if (!string.IsNullOrWhiteSpace(json))
            {
                Clipboard.SetText(json);
            }
        }

        private void checkBoxEnableGlobalHotkey_CheckedChanged(object sender, EventArgs e)
        {
            this.config.GlobalHotkeys[0].Enabled = this.checkEnableGlobalHotkey.Checked;
            this.textGlobalHotkey.Enabled = this.config.GlobalHotkeys[0].Enabled;
            this.config.TriggerGlobalHotkeyChanged();
        }

        private void checkLock_CheckedChanged(object sender, EventArgs e)
        {
            this.config.IsLocked = this.checkLock.Checked;
        }

        private void checkHTML_CheckedChanged(object sender, EventArgs e)
        {
            this.config.HtmlModeEnabled = checkHTML.Checked;
        }

        private void buttonSelectFile_Click(object sender, EventArgs e)
        {
            var ofd = new OpenFileDialog();

            if (ofd.ShowDialog() == DialogResult.OK)
            {
                this.config.Url = new Uri(ofd.FileName).ToString();
            }
        }

        private void textUrl_Leave(object sender, EventArgs e)
        {
            this.config.Url = textUrl.Text;
        }

        private void textBox_TextChanged(object sender, EventArgs e)
        {
            this.config.Text = textBox.Text;
        }

        private void textGlobalHotkey_Enter(object sender, EventArgs e)
        {
            keyboardHook.DisableHotKeys();
        }

        private void textGlobalHotkey_Leave(object sender, EventArgs e)
        {
            keyboardHook.EnableHotKeys();
        }

        private void textGlobalHotkey_KeyDown(object sender, KeyEventArgs e)
        {
            e.SuppressKeyPress = true;
            var key = Util.RemoveModifiers(e.KeyCode, e.Modifiers);
            this.config.GlobalHotkeys[0].Key = key;
            this.config.GlobalHotkeys[0].Modifiers = e.Modifiers;
            this.config.TriggerGlobalHotkeyChanged();
        }
    }
}
