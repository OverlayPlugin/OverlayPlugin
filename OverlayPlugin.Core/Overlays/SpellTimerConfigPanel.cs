using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Advanced_Combat_Tracker;

namespace RainbowMage.OverlayPlugin.Overlays
{
    public partial class SpellTimerConfigPanel : UserControl
    {
        private SpellTimerOverlay overlay;
        private SpellTimerOverlayConfig config;

        static readonly List<KeyValuePair<string, GlobalHotkeyType>> hotkeyTypeDict = new List<KeyValuePair<string, GlobalHotkeyType>>()
        {
            new KeyValuePair<string, GlobalHotkeyType>(Resources.HotkeyActionToggleVisible, GlobalHotkeyType.ToggleVisible),
            new KeyValuePair<string, GlobalHotkeyType>(Resources.HotkeyActionToggleClickthrough, GlobalHotkeyType.ToggleClickthru),
            new KeyValuePair<string, GlobalHotkeyType>(Resources.HotkeyActionToggleLock, GlobalHotkeyType.ToggleLock)
        };

        public SpellTimerConfigPanel(SpellTimerOverlay overlay)
        {
            InitializeComponent();

            this.overlay = overlay;
            this.config = overlay.Config;

            SetupConfigEventHandlers();
            SetupControlProperties();
            if (ActGlobals.oFormActMain != null)
            {
                ActGlobals.oFormActMain.ActColorSettings.MainWindowColors.BackColorSettingChanged += ActColorSettings_ColorSettingChanged;
                ActGlobals.oFormActMain.ActColorSettings.MainWindowColors.ForeColorSettingChanged += ActColorSettings_ColorSettingChanged;
                ActGlobals.oFormActMain.ActColorSettings.InternalWindowColors.BackColorSettingChanged += ActColorSettings_ColorSettingChanged;
                ActGlobals.oFormActMain.ActColorSettings.InternalWindowColors.ForeColorSettingChanged += ActColorSettings_ColorSettingChanged;
                ActGlobals.oFormActMain.ActColorSettings.InvertColorsChanged += ActColorSettings_InvertColorsChanged;
                UpdateActColorSettings();
            }
        }

        private void ActColorSettings_InvertColorsChanged(object sender, EventArgs e)
        {
            UpdateActColorSettings();
        }
        private void ActColorSettings_ColorSettingChanged(Color NewColor)
        {
            UpdateActColorSettings();
        }
        private void UpdateActColorSettings()
        {
            this.BackColor = ActGlobals.oFormActMain.ActColorSettings.MainWindowColors.BackColorSetting;
            this.ForeColor = ActGlobals.oFormActMain.ActColorSettings.MainWindowColors.ForeColorSetting;
            if (ActGlobals.oFormActMain.ActColorSettings.InvertColors)
            {
                // TextBoxes
                textBoxUrl.BorderStyle = BorderStyle.FixedSingle;
                textGlobalHotkey.BorderStyle = BorderStyle.FixedSingle;
                // ComboBoxes
                comboHotkeyType.FlatStyle = FlatStyle.Flat;
            }
            else
            {
                // TextBoxes
                textBoxUrl.BorderStyle = BorderStyle.Fixed3D;
                textGlobalHotkey.BorderStyle = BorderStyle.Fixed3D;
                // ComboBoxes
                comboHotkeyType.FlatStyle = FlatStyle.Standard;
            }
            // Text Boxes
            textBoxUrl.BackColor = ActGlobals.oFormActMain.ActColorSettings.InternalWindowColors.BackColorSetting;
            textBoxUrl.ForeColor = ActGlobals.oFormActMain.ActColorSettings.InternalWindowColors.ForeColorSetting;
            textGlobalHotkey.BackColor = ActGlobals.oFormActMain.ActColorSettings.InternalWindowColors.BackColorSetting;
            textGlobalHotkey.ForeColor = ActGlobals.oFormActMain.ActColorSettings.InternalWindowColors.ForeColorSetting;
            // ComboBoxes
            comboHotkeyType.BackColor = ActGlobals.oFormActMain.ActColorSettings.InternalWindowColors.BackColorSetting;
            comboHotkeyType.ForeColor = ActGlobals.oFormActMain.ActColorSettings.InternalWindowColors.ForeColorSetting;
            // NumericUpDowns
            nudMaxFrameRate.BackColor = ActGlobals.oFormActMain.ActColorSettings.InternalWindowColors.BackColorSetting;
            nudMaxFrameRate.ForeColor = ActGlobals.oFormActMain.ActColorSettings.InternalWindowColors.ForeColorSetting;
        }
        private void SetupControlProperties()
        {
            if (config.GlobalHotkeys.Count < 1)
                config.GlobalHotkeys.Add(new GlobalHotkey());

            this.checkBoxVisible.Checked = this.config.IsVisible;
            this.checkBoxClickThru.Checked = this.config.IsClickThru;
            this.checkLock.Checked = config.IsLocked;
            this.textBoxUrl.Text = this.config.Url;
            this.nudMaxFrameRate.Value = this.config.MaxFrameRate;
            this.checkEnableGlobalHotkey.Checked = config.GlobalHotkeys[0].Enabled;
            this.textGlobalHotkey.Enabled = this.checkEnableGlobalHotkey.Checked;
            this.textGlobalHotkey.Text = Util.GetHotkeyString(config.GlobalHotkeys[0].Modifiers, config.GlobalHotkeys[0].Key);
            this.comboHotkeyType.DisplayMember = "Key";
            this.comboHotkeyType.ValueMember = "Value";
            this.comboHotkeyType.DataSource = hotkeyTypeDict;
            this.comboHotkeyType.SelectedValue = config.GlobalHotkeys[0].Type;
            this.comboHotkeyType.SelectedIndexChanged += ComboHotkeyMode_SelectedIndexChanged;
        }

        private void ComboHotkeyMode_SelectedIndexChanged(object sender, EventArgs e)
        {
            var value = (GlobalHotkeyType)comboHotkeyType.SelectedValue;
            config.GlobalHotkeys[0].Type = value;
            config.TriggerGlobalHotkeyChanged();
        }

        private void SetupConfigEventHandlers()
        {
            this.config.VisibleChanged += (o, e) =>
            {
                this.InvokeIfRequired(() =>
                {
                    this.checkBoxVisible.Checked = e.IsVisible;
                });
            };
            this.config.ClickThruChanged += (o, e) =>
            {
                this.InvokeIfRequired(() =>
                {
                    this.checkBoxClickThru.Checked = e.IsClickThru;
                });
            };
            this.config.UrlChanged += (o, e) =>
            {
                this.InvokeIfRequired(() =>
                {
                    this.textBoxUrl.Text = e.NewUrl;
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

        private void checkBoxVisible_CheckedChanged(object sender, EventArgs e)
        {
            this.config.IsVisible = this.checkBoxVisible.Checked;
        }

        private void checkBoxClickThru_CheckedChanged(object sender, EventArgs e)
        {
            this.config.IsClickThru = this.checkBoxClickThru.Checked;
        }

        private void textBoxUrl_TextChanged(object sender, EventArgs e)
        {
            //this.config.Url = this.textBoxUrl.Text;
        }

        private void textBoxUrl_Leave(object sender, EventArgs e)
        {
            this.config.Url = this.textBoxUrl.Text;
        }

        private void buttonSelectFile_Click(object sender, EventArgs e)
        {
            var ofd = new OpenFileDialog();

            if (ofd.ShowDialog() == DialogResult.OK)
            {
                this.config.Url = new Uri(ofd.FileName).ToString();
            }
        }

        private void buttonCopyVariable_Click(object sender, EventArgs e)
        {
            var json = this.overlay.CreateJsonData();
            if (!string.IsNullOrWhiteSpace(json))
            {
                Clipboard.SetText(json);
            }
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

        private void buttonSpellTimerReloadBrowser_Click(object sender, EventArgs e)
        {
            this.overlay.Navigate(this.config.Url);
        }

        private void nudMaxFrameRate_ValueChanged(object sender, EventArgs e)
        {
            this.config.MaxFrameRate = (int)nudMaxFrameRate.Value;
        }

        private void checkEnableGlobalHotkey_CheckedChanged(object sender, EventArgs e)
        {
            this.config.GlobalHotkeys[0].Enabled = this.checkEnableGlobalHotkey.Checked;
            this.textGlobalHotkey.Enabled = this.config.GlobalHotkeys[0].Enabled;
        }

        private void textGlobalHotkey_KeyDown(object sender, KeyEventArgs e)
        {
            e.SuppressKeyPress = true;
            var key = Util.RemoveModifiers(e.KeyCode, e.Modifiers);
            this.config.GlobalHotkeys[0].Key = key;
            this.config.GlobalHotkeys[0].Modifiers = e.Modifiers;
        }

        private void checkLock_CheckedChanged(object sender, EventArgs e)
        {
            this.config.IsLocked = this.checkLock.Checked;
        }
    }
}
