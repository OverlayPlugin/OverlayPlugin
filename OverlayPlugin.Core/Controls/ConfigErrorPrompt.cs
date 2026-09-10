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

namespace RainbowMage.OverlayPlugin.Controls
{
    public partial class ConfigErrorPrompt : Form
    {
        public ConfigErrorPrompt()
        {
            InitializeComponent();

            if (ActGlobals.oFormActMain != null)
            {
                ActGlobals.oFormActMain.ActColorSettings.MainWindowColors.BackColorSettingChanged += ActColorSettings_ColorSettingChanged;
                ActGlobals.oFormActMain.ActColorSettings.MainWindowColors.ForeColorSettingChanged += ActColorSettings_ColorSettingChanged;
                UpdateActColorSettings();
            }
        }

        private void ActColorSettings_ColorSettingChanged(Color NewColor)
        {
            UpdateActColorSettings();
        }
        private void UpdateActColorSettings()
        {
            this.BackColor = ActGlobals.oFormActMain.ActColorSettings.MainWindowColors.BackColorSetting;
            this.ForeColor = ActGlobals.oFormActMain.ActColorSettings.MainWindowColors.ForeColorSetting;
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }

        private void btnReset_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Yes;
            Close();
        }
    }
}
