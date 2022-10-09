using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace RainbowMage.OverlayPlugin.Overlays
{
    public partial class LogParseConfigPanel : UserControl
    {
        private LogParseOverlayConfig config;
        private LogParseOverlay overlay;

        static readonly List<KeyValuePair<string, GlobalHotkeyType>> hotkeyTypeDict = new List<KeyValuePair<string, GlobalHotkeyType>>()
        {
            new KeyValuePair<string, GlobalHotkeyType>(Resources.HotkeyActionToggleVisible, GlobalHotkeyType.ToggleVisible),
            new KeyValuePair<string, GlobalHotkeyType>(Resources.HotkeyActionToggleClickthrough, GlobalHotkeyType.ToggleClickthru),
            new KeyValuePair<string, GlobalHotkeyType>(Resources.HotkeyActionToggleLock, GlobalHotkeyType.ToggleLock)
        };

        public LogParseConfigPanel(LogParseOverlay overlay)
        {
            InitializeComponent();

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
            this.textLogParseUrl.Text = config.Url;
            this.nudMaxFrameRate.Value = config.MaxFrameRate;
            this.checkEnableGlobalHotkey.Checked = config.GlobalHotkeys[0].Enabled;
            this.textGlobalHotkey.Enabled = this.checkEnableGlobalHotkey.Checked;
            this.textGlobalHotkey.Text = Util.GetHotkeyString(config.GlobalHotkeys[0].Modifiers, config.GlobalHotkeys[0].Key);
            this.comboHotkeyType.DisplayMember = "Key";
            this.comboHotkeyType.ValueMember = "Value";
            this.comboHotkeyType.DataSource = hotkeyTypeDict;
            this.comboHotkeyType.SelectedValue = config.GlobalHotkeys[0].Type;
            this.comboHotkeyType.SelectedIndexChanged += ComboHotkeyMode_SelectedIndexChanged;
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
                    this.textLogParseUrl.Text = e.NewUrl;
                });
            };
            this.config.MaxFrameRateChanged += (o, e) =>
            {
                this.InvokeIfRequired(() =>
                {
                    this.nudMaxFrameRate.Value = e.NewFrameRate;
                });
            };
            this.config.GlobalHotkeyChanged += (o, e) =>
            {
                this.InvokeIfRequired(() =>
                {
                    checkEnableGlobalHotkey.Checked = config.GlobalHotkeys[0].Enabled;
                    textGlobalHotkey.Enabled = checkEnableGlobalHotkey.Checked;
                    textGlobalHotkey.Text = Util.GetHotkeyString(config.GlobalHotkeys[0].Modifiers, config.GlobalHotkeys[0].Key);
                    comboHotkeyType.SelectedValue = config.GlobalHotkeys[0].Type;
                });
            };
            this.config.LockChanged += (o, e) =>
            {
                this.InvokeIfRequired(() =>
                {
                    this.checkLock.Checked = e.IsLocked;
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
            //this.config.Url = textLogParseUrl.Text;
        }

        private void textLogParseUrl_Leave(object sender, EventArgs e)
        {
            this.config.Url = textLogParseUrl.Text;
        }

        private void ComboHotkeyMode_SelectedIndexChanged(object sender, EventArgs e)
        {
            var value = (GlobalHotkeyType)this.comboHotkeyType.SelectedValue;
            config.GlobalHotkeys[0].Type = value;
            config.TriggerGlobalHotkeyChanged();
        }

        private void nudMaxFrameRate_ValueChanged(object sender, EventArgs e)
        {
            this.config.MaxFrameRate = (int)nudMaxFrameRate.Value;
        }

        private void buttonReloadBrowser_Click(object sender, EventArgs e)
        {
            this.overlay.Navigate(this.config.Url);
        }

        private void buttonSelectFile_Click(object sender, EventArgs e)
        {
            var ofd = new OpenFileDialog();

            if (ofd.ShowDialog() == DialogResult.OK)
            {
                this.config.Url = new Uri(ofd.FileName).ToString();
            }
        }

        private void buttonLogParseOpenDevTools_Click(object sender, EventArgs e)
        {
            this.overlay.Overlay.Renderer.showDevTools();
        }

        private void buttonLogParseOpenDevTools_RClick(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
                this.overlay.Overlay.Renderer.showDevTools(false);
        }

        private void buttonCopyActXiv_Click(object sender, EventArgs e)
        {
            var json = overlay.CreateJsonData();
            if (!string.IsNullOrWhiteSpace(json))
            {
                Clipboard.SetText(json);
            }
        }

        private void checkBoxEnableGlobalHotkey_CheckedChanged(object sender, EventArgs e)
        {
            config.GlobalHotkeys[0].Enabled = checkEnableGlobalHotkey.Checked;
            textGlobalHotkey.Enabled = config.GlobalHotkeys[0].Enabled;
            config.TriggerGlobalHotkeyChanged();
        }

        private void textBoxGlobalHotkey_KeyDown(object sender, KeyEventArgs e)
        {
            e.SuppressKeyPress = true;
            var key = Util.RemoveModifiers(e.KeyCode, e.Modifiers);
            config.GlobalHotkeys[0].Key = key;
            config.GlobalHotkeys[0].Modifiers = e.Modifiers;
            config.TriggerGlobalHotkeyChanged();
        }

        private void checkLock_CheckedChanged(object sender, EventArgs e)
        {
            this.config.IsLocked = this.checkLock.Checked;
        }
    }
}
