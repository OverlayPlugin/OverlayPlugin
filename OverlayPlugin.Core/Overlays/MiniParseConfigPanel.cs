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
    public partial class MiniParseConfigPanel : UserControl
    {
        private MiniParseOverlayConfig config;
        private MiniParseOverlay overlay;
        private readonly KeyboardHook keyboardHook;

        static readonly List<KeyValuePair<string, GlobalHotkeyType>> hotkeyTypeDict = new List<KeyValuePair<string, GlobalHotkeyType>>()
        {
            new KeyValuePair<string, GlobalHotkeyType>(Resources.HotkeyActionToggleVisible, GlobalHotkeyType.ToggleVisible),
            new KeyValuePair<string, GlobalHotkeyType>(Resources.HotkeyActionToggleClickthrough, GlobalHotkeyType.ToggleClickthru),
            new KeyValuePair<string, GlobalHotkeyType>(Resources.HotkeyActionToggleLock, GlobalHotkeyType.ToggleLock),
            new KeyValuePair<string, GlobalHotkeyType>(Resources.HotkeyActionToggleEnabled, GlobalHotkeyType.ToogleEnabled),
        };

        public MiniParseConfigPanel(TinyIoCContainer container, MiniParseOverlay overlay)
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
            this.checkMiniParseVisible.Checked = config.IsVisible;
            this.checkMiniParseClickthru.Checked = config.IsClickThru;
            this.checkLock.Checked = config.IsLocked;
            this.textMiniParseUrl.Text = config.Url;
            this.checkActwsCompatbility.Checked = config.ActwsCompatibility;
            this.lblNoFocus.Visible = config.ActwsCompatibility;
            this.checkNoFocus.Visible = config.ActwsCompatibility;
            this.checkNoFocus.Checked = config.NoFocus;
            this.nudMaxFrameRate.Value = config.MaxFrameRate;
            this.checkLogConsoleMessages.Checked = config.LogConsoleMessages;
            this.tbZoom.Value = config.Zoom;
            this.cbWhiteBg.Checked = config.ForceWhiteBackground;
            this.cbEnableOverlay.Checked = !config.Disabled;
            this.cbMuteHidden.Checked = config.MuteWhenHidden;
            this.cbHideOutOfCombat.Checked = config.HideOutOfCombat;

            hotkeyColAction.DisplayMember = "Key";
            hotkeyColAction.ValueMember = "Value";
            hotkeyColAction.DataSource = hotkeyTypeDict;

            hotkeyGridView.DataSource = new BindingList<GlobalHotkey>(config.GlobalHotkeys);

            // TODO
            applyPresetCombo.Visible = false;
            //presets = NewOverlayDialog.PreparePresetCombo(applyPresetCombo);
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
                    this.textMiniParseUrl.Text = e.NewUrl;
                });
            };
            this.config.ActwsCompatibilityChanged += (o, e) =>
            {
                this.InvokeIfRequired(() =>
                {
                    this.checkActwsCompatbility.Checked = config.ActwsCompatibility;

                    this.lblNoFocus.Visible = config.ActwsCompatibility;
                    this.checkNoFocus.Visible = config.ActwsCompatibility;

                    if (!config.ActwsCompatibility)
                    {
                        config.NoFocus = true;
                    }
                });
            };
            this.config.NoFocusChanged += (o, e) =>
            {
                this.InvokeIfRequired(() =>
                {
                    this.checkNoFocus.Checked = config.NoFocus;
                });
            };
            this.config.MaxFrameRateChanged += (o, e) =>
            {
                this.InvokeIfRequired(() =>
                {
                    this.nudMaxFrameRate.Value = e.NewFrameRate;
                });
            };
            this.config.LockChanged += (o, e) =>
            {
                this.InvokeIfRequired(() =>
                {
                    this.checkLock.Checked = e.IsLocked;
                });
            };
            this.config.ActwsCompatibilityChanged += (o, e) =>
            {
                this.InvokeIfRequired(() =>
                {
                    this.checkActwsCompatbility.Checked = this.config.ActwsCompatibility;
                });
            };
            this.config.LogConsoleMessagesChanged += (o, e) =>
            {
                this.InvokeIfRequired(() =>
                {
                    this.checkLogConsoleMessages.Checked = this.config.LogConsoleMessages;
                });
            };
            this.config.ZoomChanged += (o, e) =>
            {
                this.InvokeIfRequired(() =>
                {
                    this.tbZoom.Value = this.config.Zoom;
                });
            };
            this.config.DisabledChanged += (o, e) =>
            {
                this.InvokeIfRequired(() =>
                {
                    this.cbEnableOverlay.Checked = !this.config.Disabled;
                });
            };
            this.config.MuteWhenHiddenChanged += (o, e) =>
            {
                this.InvokeIfRequired(() =>
                {
                    this.cbMuteHidden.Checked = this.config.MuteWhenHidden;
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

        private void textMiniParseUrl_Leave(object sender, EventArgs e)
        {
            this.config.Url = textMiniParseUrl.Text;
        }

        private void nudMaxFrameRate_ValueChanged(object sender, EventArgs e)
        {
            this.config.MaxFrameRate = (int)nudMaxFrameRate.Value;
        }

        private void buttonReloadBrowser_Click(object sender, EventArgs e)
        {
            this.overlay.Reload();
        }

        private void buttonSelectFile_Click(object sender, EventArgs e)
        {
            var ofd = new OpenFileDialog();

            if (ofd.ShowDialog() == DialogResult.OK)
            {
                this.config.Url = new Uri(ofd.FileName).ToString();
            }
        }

        private void buttonMiniParseOpenDevTools_Click(object sender, EventArgs e)
        {
            this.overlay.Overlay.Renderer.showDevTools();
        }

        private void buttonMiniParseOpenDevTools_RClick(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
                this.overlay.Overlay.Renderer.showDevTools(false);
        }

        private void checkLock_CheckedChanged(object sender, EventArgs e)
        {
            this.config.IsLocked = this.checkLock.Checked;
        }

        private void buttonResetOverlayPosition_Click(object sender, EventArgs e)
        {
            this.overlay.Overlay.Location = new Point(10, 10);
        }

        private void CheckActwsCompatbility_CheckedChanged(object sender, EventArgs e)
        {
            this.config.ActwsCompatibility = checkActwsCompatbility.Checked;
        }

        private void cbWhiteBg_CheckedChanged(object sender, EventArgs e)
        {
            this.config.ForceWhiteBackground = this.cbWhiteBg.Checked;
        }

        private void checkNoFocus_CheckedChanged(object sender, EventArgs e)
        {
            this.config.NoFocus = this.checkNoFocus.Checked;
        }

        private void checkLogConsoleMessages_CheckedChanged(object sender, EventArgs e)
        {
            this.config.LogConsoleMessages = this.checkLogConsoleMessages.Checked;
        }

        private void tbZoom_ValueChanged(object sender, EventArgs e)
        {
            if (this.config.Zoom == 0 && Math.Abs(this.config.Zoom - this.tbZoom.Value) < 10)
            {
                // Don't change the zoom level if we don't want any zoom (see #152 for details).
                return;
            }

            this.config.Zoom = this.tbZoom.Value;
        }

        private void btnResetZoom_Click(object sender, EventArgs e)
        {
            this.config.Zoom = 0;
        }

        private void btnAddHotkey_Click(object sender, EventArgs e)
        {
            var list = (BindingList<GlobalHotkey>)hotkeyGridView.DataSource;
            list.Add(new GlobalHotkey
            {
                Enabled = true
            });
        }

        private void btnRemoveHotkey_Click(object sender, EventArgs e)
        {
            for (var i = hotkeyGridView.SelectedRows.Count - 1; i >= 0; --i)
            {
                var row = hotkeyGridView.SelectedRows[i];
                hotkeyGridView.Rows.Remove(row);
            }
        }

        private void hotkeyGridView_CellMouseClick(object sender, DataGridViewCellMouseEventArgs e)
        {
            if (e.ColumnIndex > 0 && e.RowIndex >= 0)
            {
                // If a user clicks on the hotkey cell, start editing immidiately.
                hotkeyGridView.BeginEdit(false);
            }
        }

        // The next three methods are a bit of a mess but they allow me to completely control how the values
        // are displayed (Formatting), how the edit widget behaves (EditControlShowing) and how the values
        // are stored (CellValidated).
        // There are probably better ways to implement this but this works and is fairly short.
        // If you're reading this and want to improve this section, feel free to submit a PR (or message me
        // so we can discuss your ideas first).
        // -- ngld
        private void hotkeyGridView_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < config.GlobalHotkeys.Count)
            {
                var entry = config.GlobalHotkeys[e.RowIndex];
                switch (e.ColumnIndex)
                {
                    case 0:
                        e.Value = entry.Enabled;
                        break;

                    case 1:
                        e.Value = Util.GetHotkeyString(entry.Modifiers, entry.Key);
                        break;

                    case 2:
                        foreach (var item in hotkeyTypeDict)
                        {
                            if (item.Value == entry.Type)
                            {
                                e.Value = item.Key;
                            }
                        }
                        break;
                }
                e.FormattingApplied = true;
            }
        }

        private void hotkeyGridView_EditingControlShowing(object sender, DataGridViewEditingControlShowingEventArgs ce)
        {
            var entry = config.GlobalHotkeys[hotkeyGridView.CurrentCell.RowIndex];

            switch (hotkeyGridView.CurrentCell.ColumnIndex)
            {
                case 1:
                    keyboardHook.DisableHotKeys();

                    KeyEventHandler keyHandler = (o, e) =>
                    {
                        e.SuppressKeyPress = true;
                        var key = Util.RemoveModifiers(e.KeyCode, e.Modifiers);
                        entry.Modifiers = e.Modifiers;
                        entry.Key = key;
                        ce.Control.Text = Util.GetHotkeyString(entry.Modifiers, entry.Key);
                    };

                    ce.Control.KeyDown += keyHandler;
                    ce.Control.LostFocus += (o, e) =>
                    {
                        ce.Control.KeyDown -= keyHandler;
                    };

                    ce.Control.Disposed += (o, e) =>
                    {
                        keyboardHook.EnableHotKeys();
                    };

                    ce.Control.BackColor = SystemColors.ControlLightLight;
                    break;
            }
        }

        private void hotkeyGridView_CellValidated(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= config.GlobalHotkeys.Count) return;

            var entry = config.GlobalHotkeys[e.RowIndex];
            var cells = hotkeyGridView.Rows[e.RowIndex].Cells;

            switch (e.ColumnIndex)
            {
                case 0:
                    if (cells[0].Value != null)
                    {
                        entry.Enabled = (bool)cells[0].Value;
                    }
                    break;

                case 2:
                    if (cells[2].Value != null)
                    {
                        entry.Type = (GlobalHotkeyType)cells[2].Value;
                    }
                    break;
            }
        }

        private void btnApplyHotkeyChanges_Click(object sender, EventArgs e)
        {
            config.TriggerGlobalHotkeyChanged();
        }

        private void cbEnableOverlay_CheckedChanged(object sender, EventArgs e)
        {
            config.Disabled = !cbEnableOverlay.Checked;
        }

        private void cbMuteHidden_CheckedChanged(object sender, EventArgs e)
        {
            config.MuteWhenHidden = cbMuteHidden.Checked;
        }

        private void cbHideOutOfCombat_CheckedChanged(object sender, EventArgs e)
        {
            config.HideOutOfCombat = cbHideOutOfCombat.Checked;
        }

        private void btnClearCache_Click(object sender, EventArgs e)
        {
            overlay.Overlay.Renderer.ClearCache();
        }
    }
}
