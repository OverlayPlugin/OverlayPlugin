using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Globalization;
using System.Threading;

namespace RainbowMage.OverlayPlugin
{
    public partial class ControlPanel : UserControl
    {
        TinyIoCContainer _container;
        ILogger _logger;
        PluginMain _pluginMain;
        IPluginConfig _config;
        Registry _registry;
        TabPage _generalTab, _eventTab;
        bool logResized = false;
        bool logConnected = false;

        static Dictionary<string, string> esNames = new Dictionary<string, string>
        {
            { "MiniParseEventSource", Resources.MapESMiniParse },
        };
        static Dictionary<string, string> overlayNames = new Dictionary<string, string>
        {
            { "LabelOverlay", Resources.MapOverlayLabel },
            { "MiniParseOverlay", Resources.MapOverlayMiniParse },
            { "SpellTimerOverlay", Resources.MapOverlaySpellTimer },
        };

        public ControlPanel(TinyIoCContainer container)
        {
            InitializeComponent();
            tableLayoutPanel0.PerformLayout();
            // Make the log box big until we load the overlays since the log is going to be *very*
            // important if we never make it that far.
            splitContainer1.SplitterDistance = 5;

            _container = container;
            _logger = container.Resolve<ILogger>();
            _pluginMain = container.Resolve<PluginMain>();
            _config = container.Resolve<IPluginConfig>();
            _registry = container.Resolve<Registry>();

            this.checkBoxFollowLog.Checked = _config.FollowLatestLog;

            _generalTab = new ConfigTabPage
            {
                Name = Resources.GeneralTab,
                Text = "",
            };
            _generalTab.Controls.Add(new GeneralConfigTab(container));

            _eventTab = new ConfigTabPage
            {
                Name = Resources.EventConfigTab,
                Text = "",
            };
            _eventTab.Controls.Add(new EventSources.BuiltinEventConfigPanel(container));

            logBox.Text = Resources.LogNotConnectedError;
            _logger.RegisterListener(AddLogEntry);
            _registry.EventSourceRegistered += (o, e) => Invoke((Action)(() => AddEventSourceTab(o, e)));

            Resize += (o, e) =>
            {
                if (!logResized && Height > 500 && tabControl.TabCount > 0)
                {
                    ResizeLog();
                }
            };
        }

        public void ResizeLog()
        {
            if (!logResized)
            {
                // Only make this the final resize if we have enough height to make this layout usable.
                logResized = Height > 500;

                // Overlay tabs have been initialised, everything is fine; make the log small again.
                splitContainer1.SplitterDistance = (int)Math.Round(Height * 0.75);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (components != null) components.Dispose();
                _registry.EventSourceRegistered -= AddEventSourceTab;
                _logger.ClearListener();
            }

            base.Dispose(disposing);
        }

        private void AddLogEntry(LogEntry entry)
        {
            var msg = $"[{entry.Time}] {entry.Level}: {entry.Message}" + Environment.NewLine;

            if (!logConnected)
            {
                // Remove the error message about the log not being connected since it is now.
                logConnected = true;
                logBox.Text = "";
            }
            else if (logBox.TextLength > 200 * 1024)
            {
                logBox.Text = "============ LOG TRUNCATED ==============\nThe log was truncated to reduce memory usage.\n=========================================\n" + msg;
                return;
            }

            if (checkBoxFollowLog.Checked)
            {
                logBox.AppendText(msg);
            }
            else
            {
                // This is based on https://stackoverflow.com/q/1743448
                bool bottomFlag = false;
                int sbOffset;
                int savedVpos;

                // Win32 magic to keep the textbox scrolling to the newest append to the textbox unless
                // the user has moved the scrollbox up
                sbOffset = (int)((logBox.ClientSize.Height - SystemInformation.HorizontalScrollBarHeight) / (logBox.Font.Height));
                savedVpos = NativeMethods.GetScrollPos(logBox.Handle, NativeMethods.SB_VERT);
                NativeMethods.GetScrollRange(logBox.Handle, NativeMethods.SB_VERT, out _, out int VSmax);

                if (savedVpos >= (VSmax - sbOffset - 1))
                    bottomFlag = true;

                logBox.AppendText(msg);

                if (bottomFlag)
                {
                    NativeMethods.GetScrollRange(logBox.Handle, NativeMethods.SB_VERT, out _, out VSmax);
                    savedVpos = VSmax - sbOffset;
                }
                NativeMethods.SetScrollPos(logBox.Handle, NativeMethods.SB_VERT, savedVpos, true);
                NativeMethods.PostMessageA(logBox.Handle, NativeMethods.WM_VSCROLL, NativeMethods.SB_THUMBPOSITION + 0x10000 * savedVpos, 0);
            }
        }

        private void AddEventSourceTab(object sender, EventSourceRegisteredEventArgs e)
        {
            AddConfigTab(e.EventSource);
        }

        public void InitializeOverlayConfigTabs()
        {
            tabControl.TabPages.Clear();
            tabControl.TabPages.Add(_generalTab);
            tabControl.TabPages.Add(_eventTab);

            foreach (var source in _registry.EventSources)
            {
                AddConfigTab(source);
            }

            foreach (var overlay in _pluginMain.Overlays)
            {
                AddConfigTab(overlay);
            }

            if (_pluginMain.Overlays.Count == 0)
            {
                ((GeneralConfigTab)_generalTab.Controls[0]).SetReadmeVisible(true);
            }
            else
            {
                ((GeneralConfigTab)_generalTab.Controls[0]).SetReadmeVisible(false);
            }
        }

        private void AddConfigTab(IOverlay overlay)
        {
            var label = overlay.GetType().Name;
            if (overlayNames.ContainsKey(label))
                label = overlayNames[label];

            var tabPage = new ConfigTabPage
            {
                Name = overlay.Name,
                Text = label,
                IsOverlay = true,
            };

            var control = overlay.CreateConfigControl();
            if (control != null)
            {
                control.Dock = DockStyle.Fill;
                control.BackColor = SystemColors.ControlLightLight;
                tabPage.Controls.Add(control);

                this.tabControl.TabPages.Add(tabPage);
                ((GeneralConfigTab)_generalTab.Controls[0]).SetReadmeVisible(false);
            }
        }

        private void AddConfigTab(IEventSource source)
        {
            var label = source.GetType().Name;
            if (esNames.ContainsKey(label))
                label = esNames[label];

            var tabPage = new ConfigTabPage
            {
                Name = source.Name,
                Text = "",
                IsEventSource = true,
            };

            var control = source.CreateConfigControl();
            if (control != null)
            {
                control.Dock = DockStyle.Fill;
                control.BackColor = SystemColors.ControlLightLight;
                tabPage.Controls.Add(control);

                var index = 0;
                foreach (var page in this.tabControl.TabPages)
                {
                    if (index == 0 || ((ConfigTabPage)page).IsEventSource)
                    {
                        index++;
                    }
                    else
                    {
                        break;
                    }
                }

                this.tabControl.TabPages.Insert(index, tabPage);
            }
        }

        private void buttonNewOverlay_Click(object sender, EventArgs e)
        {
            var newOverlayDialog = new NewOverlayDialog(_container);
            newOverlayDialog.NameValidator = (name) =>
                {
                    // 空もしくは空白文字のみの名前は許容しない
                    if (string.IsNullOrWhiteSpace(name))
                    {
                        MessageBox.Show(Resources.ErrorOverlayNameEmpty);
                        return false;
                    }
                    // 名前の重複も許容しない
                    else if (_config.Overlays.Where(x => x.Name == name).Any())
                    {
                        MessageBox.Show(Resources.ErrorOverlayNameNotUnique);
                        return false;
                    }
                    else
                    {
                        return true;
                    }
                };

            if (newOverlayDialog.ShowDialog(this.ParentForm) == DialogResult.OK)
            {
                if (this.tabControl.TabCount == 1 && this.tabControl.TabPages[0].Equals(this.tabPageMain))
                {
                    this.tabControl.TabPages.Remove(this.tabPageMain);
                }
                CreateAndRegisterOverlay(newOverlayDialog.SelectedOverlay);
            }

            newOverlayDialog.Dispose();
        }

        private IOverlay CreateAndRegisterOverlay(IOverlay overlay)
        {
            _config.Overlays.Add(overlay.Config);
            _pluginMain.RegisterOverlay(overlay);

            AddConfigTab(overlay);

            return overlay;
        }

        private void buttonRemoveOverlay_Click(object sender, EventArgs e)
        {
            if (tabControl.SelectedTab == null) // ???
                tabControl.SelectedTab = tabControl.TabPages[0];

            var subLabel = tabControl.SelectedTab.Text;
            if (!((ConfigTabPage)tabControl.SelectedTab).IsOverlay)
            {
                return;
            }

            string selectedOverlayName = tabControl.SelectedTab.Name;
            int selectedOverlayIndex = tabControl.TabPages.IndexOf(tabControl.SelectedTab);

            // コンフィグ削除
            var configs = _config.Overlays.Where(x => x.Name == selectedOverlayName);
            foreach (var config in configs.ToArray())
            {
                _config.Overlays.Remove(config);
            }

            // 動作中のオーバーレイを停止して削除
            var overlays = _pluginMain.Overlays.Where(x => x.Name == selectedOverlayName);
            foreach (var overlay in overlays)
            {
                overlay.Dispose();
            }
            foreach (var overlay in overlays.ToArray())
            {
                _pluginMain.Overlays.Remove(overlay);
            }

            // タブページを削除
            this.tabControl.TabPages.Remove(tabControl.SelectedTab);

            // タープカントロールが
            if (this.tabControl.TabCount == 0)
            {
                this.tabControl.TabPages.Add(this.tabPageMain);
            }
            // 
            if (selectedOverlayIndex > 0)
            {
                this.tabControl.SelectTab(selectedOverlayIndex - 1);
            }

            // タープを更新
            this.tabControl.Update();
            if (_pluginMain.Overlays.Count == 0)
            {
                ((GeneralConfigTab)_generalTab.Controls[0]).SetReadmeVisible(true);
            }
        }

        private void buttonRename_Click(object sender, EventArgs e)
        {
            if (tabControl.SelectedTab == null) // ???
                tabControl.SelectedTab = tabControl.TabPages[0];

            if (!((ConfigTabPage)tabControl.SelectedTab).IsOverlay)
                return;

            string selectedOverlayName = tabControl.SelectedTab.Name;
            int selectedOverlayIndex = tabControl.TabPages.IndexOf(tabControl.SelectedTab);

            var config = _config.Overlays.Where(x => x.Name == selectedOverlayName).FirstOrDefault();
            if (config == null)
                return;

            var dialog = new Controls.RenameOverlayDialog(config.Name);
            if (dialog.ShowDialog(ParentForm) == DialogResult.OK)
            {
                config.Name = dialog.OverlayName;
            }

            tabControl.SelectedTab.Name = config.Name;
            tabControl.Update();
        }

        private void CheckBoxFollowLog_CheckedChanged(object sender, EventArgs e)
        {
            _config.FollowLatestLog = checkBoxFollowLog.Checked;
        }

        private void ButtonClearLog_Click(object sender, EventArgs e)
        {
            logBox.Clear();
        }

        private class ConfigTabPage : TabPage
        {
            public bool IsOverlay = false;
            public bool IsEventSource = false;
        }
    }
}
