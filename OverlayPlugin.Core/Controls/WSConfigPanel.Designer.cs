namespace RainbowMage.OverlayPlugin
{
    partial class WSConfigPanel
    {
        /// <summary> 
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary> 
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
                if (_ngrok != null && !_ngrok.HasExited)
                {
                    _ngrok.Kill();
                }
            }
            base.Dispose(disposing);
        }

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(WSConfigPanel));
            this.ipTxt = new System.Windows.Forms.TextBox();
            this.portTxt = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.genSslBtn = new System.Windows.Forms.Button();
            this.sslBox = new System.Windows.Forms.CheckBox();
            this.logDisplay = new System.Windows.Forms.TextBox();
            this.startBtn = new System.Windows.Forms.Button();
            this.stopBtn = new System.Windows.Forms.Button();
            this.label4 = new System.Windows.Forms.Label();
            this.label5 = new System.Windows.Forms.Label();
            this.cbOverlay = new System.Windows.Forms.ComboBox();
            this.lblUrlConfidentWarning = new System.Windows.Forms.Label();
            this.txtOverlayUrl = new System.Windows.Forms.TextBox();
            this.urlGeneratorBox = new System.Windows.Forms.GroupBox();
            this.tabControl = new System.Windows.Forms.TabControl();
            this.introPage = new System.Windows.Forms.TabPage();
            this.lblWsserverIntro = new System.Windows.Forms.Label();
            this.settingsPage = new System.Windows.Forms.TabPage();
            this.label3 = new System.Windows.Forms.Label();
            this.statusLabel = new System.Windows.Forms.Label();
            this.tunnelPage = new System.Windows.Forms.TabPage();
            this.tokenLabel = new System.Windows.Forms.Label();
            this.txtNgrokToken = new System.Windows.Forms.TextBox();
            this.simpStopBtn = new System.Windows.Forms.Button();
            this.simpStartBtn = new System.Windows.Forms.Button();
            this.simpLogBox = new System.Windows.Forms.TextBox();
            this.simpStatusLbl = new System.Windows.Forms.Label();
            this.regionCb = new System.Windows.Forms.ComboBox();
            this.regionLabel = new System.Windows.Forms.Label();
            this.simpStatusLabel = new System.Windows.Forms.Label();
            this.urlGeneratorBox.SuspendLayout();
            this.tabControl.SuspendLayout();
            this.introPage.SuspendLayout();
            this.settingsPage.SuspendLayout();
            this.tunnelPage.SuspendLayout();
            this.SuspendLayout();
            // 
            // ipTxt
            // 
            resources.ApplyResources(this.ipTxt, "ipTxt");
            this.ipTxt.Name = "ipTxt";
            this.ipTxt.Leave += new System.EventHandler(this.ipTxt_Leave);
            // 
            // portTxt
            // 
            resources.ApplyResources(this.portTxt, "portTxt");
            this.portTxt.Name = "portTxt";
            this.portTxt.Leave += new System.EventHandler(this.portTxt_Leave);
            // 
            // label1
            // 
            resources.ApplyResources(this.label1, "label1");
            this.label1.Name = "label1";
            // 
            // label2
            // 
            resources.ApplyResources(this.label2, "label2");
            this.label2.Name = "label2";
            // 
            // genSslBtn
            // 
            resources.ApplyResources(this.genSslBtn, "genSslBtn");
            this.genSslBtn.Name = "genSslBtn";
            this.genSslBtn.UseVisualStyleBackColor = true;
            this.genSslBtn.Click += new System.EventHandler(this.genSslBtn_Click);
            // 
            // sslBox
            // 
            resources.ApplyResources(this.sslBox, "sslBox");
            this.sslBox.Name = "sslBox";
            this.sslBox.UseVisualStyleBackColor = true;
            this.sslBox.CheckedChanged += new System.EventHandler(this.sslBox_CheckedChanged);
            // 
            // logDisplay
            // 
            resources.ApplyResources(this.logDisplay, "logDisplay");
            this.logDisplay.BackColor = System.Drawing.SystemColors.Control;
            this.logDisplay.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.logDisplay.Name = "logDisplay";
            this.logDisplay.ReadOnly = true;
            // 
            // startBtn
            // 
            resources.ApplyResources(this.startBtn, "startBtn");
            this.startBtn.Name = "startBtn";
            this.startBtn.UseVisualStyleBackColor = true;
            this.startBtn.Click += new System.EventHandler(this.startBtn_Click);
            // 
            // stopBtn
            // 
            resources.ApplyResources(this.stopBtn, "stopBtn");
            this.stopBtn.Name = "stopBtn";
            this.stopBtn.UseVisualStyleBackColor = true;
            this.stopBtn.Click += new System.EventHandler(this.stopBtn_Click);
            // 
            // label4
            // 
            resources.ApplyResources(this.label4, "label4");
            this.label4.Name = "label4";
            // 
            // label5
            // 
            resources.ApplyResources(this.label5, "label5");
            this.label5.Name = "label5";
            // 
            // cbOverlay
            // 
            this.cbOverlay.DisplayMember = "label";
            this.cbOverlay.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbOverlay.FormattingEnabled = true;
            resources.ApplyResources(this.cbOverlay, "cbOverlay");
            this.cbOverlay.Name = "cbOverlay";
            this.cbOverlay.ValueMember = "overlay";
            this.cbOverlay.SelectedIndexChanged += new System.EventHandler(this.cbOverlay_SelectedIndexChanged);
            // 
            // lblUrlConfidentWarning
            // 
            resources.ApplyResources(this.lblUrlConfidentWarning, "lblUrlConfidentWarning");
            this.lblUrlConfidentWarning.Name = "lblUrlConfidentWarning";
            // 
            // txtOverlayUrl
            // 
            resources.ApplyResources(this.txtOverlayUrl, "txtOverlayUrl");
            this.txtOverlayUrl.Name = "txtOverlayUrl";
            this.txtOverlayUrl.ReadOnly = true;
            this.txtOverlayUrl.Click += new System.EventHandler(this.txtOverlayUrl_Click);
            // 
            // urlGeneratorBox
            // 
            resources.ApplyResources(this.urlGeneratorBox, "urlGeneratorBox");
            this.urlGeneratorBox.Controls.Add(this.lblUrlConfidentWarning);
            this.urlGeneratorBox.Controls.Add(this.txtOverlayUrl);
            this.urlGeneratorBox.Controls.Add(this.label5);
            this.urlGeneratorBox.Controls.Add(this.label4);
            this.urlGeneratorBox.Controls.Add(this.cbOverlay);
            this.urlGeneratorBox.Name = "urlGeneratorBox";
            this.urlGeneratorBox.TabStop = false;
            // 
            // tabControl
            // 
            resources.ApplyResources(this.tabControl, "tabControl");
            this.tabControl.Controls.Add(this.introPage);
            this.tabControl.Controls.Add(this.settingsPage);
            this.tabControl.Controls.Add(this.tunnelPage);
            this.tabControl.Name = "tabControl";
            this.tabControl.SelectedIndex = 0;
            // 
            // introPage
            // 
            this.introPage.Controls.Add(this.lblWsserverIntro);
            resources.ApplyResources(this.introPage, "introPage");
            this.introPage.Name = "introPage";
            this.introPage.UseVisualStyleBackColor = true;
            // 
            // lblWsserverIntro
            // 
            resources.ApplyResources(this.lblWsserverIntro, "lblWsserverIntro");
            this.lblWsserverIntro.Name = "lblWsserverIntro";
            // 
            // settingsPage
            // 
            this.settingsPage.Controls.Add(this.logDisplay);
            this.settingsPage.Controls.Add(this.label1);
            this.settingsPage.Controls.Add(this.label3);
            this.settingsPage.Controls.Add(this.ipTxt);
            this.settingsPage.Controls.Add(this.portTxt);
            this.settingsPage.Controls.Add(this.genSslBtn);
            this.settingsPage.Controls.Add(this.sslBox);
            this.settingsPage.Controls.Add(this.statusLabel);
            this.settingsPage.Controls.Add(this.label2);
            this.settingsPage.Controls.Add(this.startBtn);
            this.settingsPage.Controls.Add(this.stopBtn);
            resources.ApplyResources(this.settingsPage, "settingsPage");
            this.settingsPage.Name = "settingsPage";
            this.settingsPage.UseVisualStyleBackColor = true;
            // 
            // label3
            // 
            resources.ApplyResources(this.label3, "label3");
            this.label3.Name = "label3";
            // 
            // statusLabel
            // 
            resources.ApplyResources(this.statusLabel, "statusLabel");
            this.statusLabel.Name = "statusLabel";
            // 
            // tunnelPage
            // 
            this.tunnelPage.Controls.Add(this.tokenLabel);
            this.tunnelPage.Controls.Add(this.txtNgrokToken);
            this.tunnelPage.Controls.Add(this.simpStopBtn);
            this.tunnelPage.Controls.Add(this.simpStartBtn);
            this.tunnelPage.Controls.Add(this.simpLogBox);
            this.tunnelPage.Controls.Add(this.simpStatusLbl);
            this.tunnelPage.Controls.Add(this.regionCb);
            this.tunnelPage.Controls.Add(this.regionLabel);
            this.tunnelPage.Controls.Add(this.simpStatusLabel);
            resources.ApplyResources(this.tunnelPage, "tunnelPage");
            this.tunnelPage.Name = "tunnelPage";
            this.tunnelPage.UseVisualStyleBackColor = true;
            // 
            // tokenLabel
            // 
            resources.ApplyResources(this.tokenLabel, "tokenLabel");
            this.tokenLabel.Name = "tokenLabel";
            // 
            // txtNgrokToken
            // 
            resources.ApplyResources(this.txtNgrokToken, "txtNgrokToken");
            this.txtNgrokToken.Name = "txtNgrokToken";
            this.txtNgrokToken.TextChanged += new System.EventHandler(this.txtNgrokTokenChanged);
            // 
            // simpStopBtn
            // 
            resources.ApplyResources(this.simpStopBtn, "simpStopBtn");
            this.simpStopBtn.Name = "simpStopBtn";
            this.simpStopBtn.UseVisualStyleBackColor = true;
            this.simpStopBtn.Click += new System.EventHandler(this.simpStopBtn_Click);
            // 
            // simpStartBtn
            // 
            resources.ApplyResources(this.simpStartBtn, "simpStartBtn");
            this.simpStartBtn.Name = "simpStartBtn";
            this.simpStartBtn.UseVisualStyleBackColor = true;
            this.simpStartBtn.Click += new System.EventHandler(this.simpStartBtn_Click);
            // 
            // simpLogBox
            // 
            resources.ApplyResources(this.simpLogBox, "simpLogBox");
            this.simpLogBox.Name = "simpLogBox";
            this.simpLogBox.ReadOnly = true;
            // 
            // simpStatusLbl
            // 
            resources.ApplyResources(this.simpStatusLbl, "simpStatusLbl");
            this.simpStatusLbl.Name = "simpStatusLbl";
            // 
            // regionCb
            // 
            this.regionCb.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.regionCb.FormattingEnabled = true;
            this.regionCb.Items.AddRange(new object[] {
            resources.GetString("regionCb.Items"),
            resources.GetString("regionCb.Items1"),
            resources.GetString("regionCb.Items2"),
            resources.GetString("regionCb.Items3"),
            resources.GetString("regionCb.Items4"),
            resources.GetString("regionCb.Items5"),
            resources.GetString("regionCb.Items6")});
            resources.ApplyResources(this.regionCb, "regionCb");
            this.regionCb.Name = "regionCb";
            this.regionCb.SelectedIndexChanged += new System.EventHandler(this.regionCb_SelectedIndexChanged);
            // 
            // regionLabel
            // 
            resources.ApplyResources(this.regionLabel, "regionLabel");
            this.regionLabel.Name = "regionLabel";
            // 
            // simpStatusLabel
            // 
            resources.ApplyResources(this.simpStatusLabel, "simpStatusLabel");
            this.simpStatusLabel.Name = "simpStatusLabel";
            // 
            // WSConfigPanel
            // 
            resources.ApplyResources(this, "$this");
            this.BackColor = System.Drawing.SystemColors.ControlLightLight;
            this.Controls.Add(this.urlGeneratorBox);
            this.Controls.Add(this.tabControl);
            this.Name = "WSConfigPanel";
            this.urlGeneratorBox.ResumeLayout(false);
            this.urlGeneratorBox.PerformLayout();
            this.tabControl.ResumeLayout(false);
            this.introPage.ResumeLayout(false);
            this.settingsPage.ResumeLayout(false);
            this.settingsPage.PerformLayout();
            this.tunnelPage.ResumeLayout(false);
            this.tunnelPage.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TextBox ipTxt;
        private System.Windows.Forms.TextBox portTxt;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Button genSslBtn;
        private System.Windows.Forms.CheckBox sslBox;
        private System.Windows.Forms.TextBox logDisplay;
        private System.Windows.Forms.Button startBtn;
        private System.Windows.Forms.Button stopBtn;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.ComboBox cbOverlay;
        private System.Windows.Forms.Label lblUrlConfidentWarning;
        private System.Windows.Forms.TextBox txtOverlayUrl;
        private System.Windows.Forms.GroupBox urlGeneratorBox;
        private System.Windows.Forms.TabControl tabControl;
        private System.Windows.Forms.TabPage settingsPage;
        private System.Windows.Forms.TabPage tunnelPage;
        private System.Windows.Forms.Button simpStopBtn;
        private System.Windows.Forms.Button simpStartBtn;
        private System.Windows.Forms.TextBox simpLogBox;
        private System.Windows.Forms.Label simpStatusLbl;
        private System.Windows.Forms.ComboBox regionCb;
        private System.Windows.Forms.Label regionLabel;
        private System.Windows.Forms.Label simpStatusLabel;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label statusLabel;
        private System.Windows.Forms.TabPage introPage;
        private System.Windows.Forms.Label lblWsserverIntro;
        private System.Windows.Forms.Label tokenLabel;
        private System.Windows.Forms.TextBox txtNgrokToken;
    }
}
