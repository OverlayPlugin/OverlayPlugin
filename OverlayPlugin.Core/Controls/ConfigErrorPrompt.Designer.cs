namespace RainbowMage.OverlayPlugin.Controls
{
    partial class ConfigErrorPrompt
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
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ConfigErrorPrompt));
			this.errorExplanation = new System.Windows.Forms.Label();
			this.panel1 = new System.Windows.Forms.Panel();
			this.btnCancel = new Advanced_Combat_Tracker.ButtonPainted();
			this.btnReset = new Advanced_Combat_Tracker.ButtonPainted();
			this.panel1.SuspendLayout();
			this.SuspendLayout();
			// 
			// errorExplanation
			// 
			this.errorExplanation.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.errorExplanation.Location = new System.Drawing.Point(12, 9);
			this.errorExplanation.Name = "errorExplanation";
			this.errorExplanation.Size = new System.Drawing.Size(472, 116);
			this.errorExplanation.TabIndex = 0;
			this.errorExplanation.Text = resources.GetString("errorExplanation.Text");
			// 
			// panel1
			// 
			this.panel1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.panel1.Controls.Add(this.btnCancel);
			this.panel1.Controls.Add(this.btnReset);
			this.panel1.Location = new System.Drawing.Point(15, 116);
			this.panel1.Name = "panel1";
			this.panel1.Size = new System.Drawing.Size(469, 30);
			this.panel1.TabIndex = 1;
			// 
			// btnCancel
			// 
			this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.btnCancel.Dock = System.Windows.Forms.DockStyle.Right;
			this.btnCancel.Location = new System.Drawing.Point(250, 0);
			this.btnCancel.Margin = new System.Windows.Forms.Padding(1);
			this.btnCancel.Name = "btnCancel";
			this.btnCancel.Size = new System.Drawing.Size(219, 30);
			this.btnCancel.TabIndex = 1;
			this.btnCancel.Text = "Disable OverlayPlugin";
			this.btnCancel.UseVisualStyleBackColor = true;
			this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click);
			// 
			// btnReset
			// 
			this.btnReset.Dock = System.Windows.Forms.DockStyle.Left;
			this.btnReset.Location = new System.Drawing.Point(0, 0);
			this.btnReset.Margin = new System.Windows.Forms.Padding(1);
			this.btnReset.Name = "btnReset";
			this.btnReset.Size = new System.Drawing.Size(209, 30);
			this.btnReset.TabIndex = 0;
			this.btnReset.Text = "Reset config and continue";
			this.btnReset.UseVisualStyleBackColor = true;
			this.btnReset.Click += new System.EventHandler(this.btnReset_Click);
			// 
			// ConfigErrorPrompt
			// 
			this.AcceptButton = this.btnReset;
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.CancelButton = this.btnCancel;
			this.ClientSize = new System.Drawing.Size(496, 158);
			this.ControlBox = false;
			this.Controls.Add(this.panel1);
			this.Controls.Add(this.errorExplanation);
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "ConfigErrorPrompt";
			this.ShowIcon = false;
			this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Hide;
			this.Text = "OverlayPlugin";
			this.panel1.ResumeLayout(false);
			this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Label errorExplanation;
        private System.Windows.Forms.Panel panel1;
        private Advanced_Combat_Tracker.ButtonPainted btnCancel;
        private Advanced_Combat_Tracker.ButtonPainted btnReset;
    }
}