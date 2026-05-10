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
    public partial class RenameOverlayDialog : Form
    {
        public string OverlayName { get => txtName.Text; }

        public RenameOverlayDialog(string name)
        {
            InitializeComponent();

            txtName.Text = name;

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
                txtName.BorderStyle = BorderStyle.FixedSingle;
            }
            else
            {
                // TextBoxes
                txtName.BorderStyle = BorderStyle.Fixed3D;
            }
            // TextBoxes
            txtName.BackColor = ActGlobals.oFormActMain.ActColorSettings.InternalWindowColors.BackColorSetting;
            txtName.ForeColor = ActGlobals.oFormActMain.ActColorSettings.InternalWindowColors.ForeColorSetting;
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
        }

        private void btnOk_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.OK;
        }
    }
}
