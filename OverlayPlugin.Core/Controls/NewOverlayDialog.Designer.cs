namespace RainbowMage.OverlayPlugin
{
    partial class NewOverlayDialog
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(NewOverlayDialog));
			this.buttonOK = new Advanced_Combat_Tracker.ButtonPainted();
			this.buttonCancel = new Advanced_Combat_Tracker.ButtonPainted();
			this.textBox1 = new System.Windows.Forms.TextBox();
			this.label1 = new System.Windows.Forms.Label();
			this.lblType = new System.Windows.Forms.Label();
			this.cbType = new System.Windows.Forms.ComboBox();
			this.lblTypeDesc = new System.Windows.Forms.Label();
			this.lblPreset = new System.Windows.Forms.Label();
			this.cbPreset = new System.Windows.Forms.ComboBox();
			this.lblPresetDescription = new System.Windows.Forms.Label();
			this.SuspendLayout();
			// 
			// buttonOK
			// 
			resources.ApplyResources(this.buttonOK, "buttonOK");
			this.buttonOK.DialogResult = System.Windows.Forms.DialogResult.OK;
			this.buttonOK.Name = "buttonOK";
			this.buttonOK.UseVisualStyleBackColor = true;
			this.buttonOK.Click += new System.EventHandler(this.buttonOK_Click);
			// 
			// buttonCancel
			// 
			resources.ApplyResources(this.buttonCancel, "buttonCancel");
			this.buttonCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.buttonCancel.Name = "buttonCancel";
			this.buttonCancel.UseVisualStyleBackColor = true;
			// 
			// textBox1
			// 
			resources.ApplyResources(this.textBox1, "textBox1");
			this.textBox1.Name = "textBox1";
			// 
			// label1
			// 
			resources.ApplyResources(this.label1, "label1");
			this.label1.Name = "label1";
			// 
			// lblType
			// 
			resources.ApplyResources(this.lblType, "lblType");
			this.lblType.Name = "lblType";
			// 
			// cbType
			// 
			resources.ApplyResources(this.cbType, "cbType");
			this.cbType.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.cbType.FormattingEnabled = true;
			this.cbType.Name = "cbType";
			// 
			// lblTypeDesc
			// 
			resources.ApplyResources(this.lblTypeDesc, "lblTypeDesc");
			this.lblTypeDesc.Name = "lblTypeDesc";
			// 
			// lblPreset
			// 
			resources.ApplyResources(this.lblPreset, "lblPreset");
			this.lblPreset.Name = "lblPreset";
			// 
			// cbPreset
			// 
			this.cbPreset.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.cbPreset.FormattingEnabled = true;
			resources.ApplyResources(this.cbPreset, "cbPreset");
			this.cbPreset.Name = "cbPreset";
			this.cbPreset.SelectedIndexChanged += new System.EventHandler(this.cbPreset_SelectedIndexChanged);
			// 
			// lblPresetDescription
			// 
			resources.ApplyResources(this.lblPresetDescription, "lblPresetDescription");
			this.lblPresetDescription.Name = "lblPresetDescription";
			// 
			// NewOverlayDialog
			// 
			this.AcceptButton = this.buttonOK;
			resources.ApplyResources(this, "$this");
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.CancelButton = this.buttonCancel;
			this.Controls.Add(this.lblPresetDescription);
			this.Controls.Add(this.cbPreset);
			this.Controls.Add(this.lblPreset);
			this.Controls.Add(this.lblTypeDesc);
			this.Controls.Add(this.cbType);
			this.Controls.Add(this.label1);
			this.Controls.Add(this.textBox1);
			this.Controls.Add(this.buttonCancel);
			this.Controls.Add(this.buttonOK);
			this.Controls.Add(this.lblType);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "NewOverlayDialog";
			this.ShowIcon = false;
			this.ShowInTaskbar = false;
			this.ResumeLayout(false);
			this.PerformLayout();

        }

        #endregion

        private Advanced_Combat_Tracker.ButtonPainted buttonOK;
        private Advanced_Combat_Tracker.ButtonPainted buttonCancel;
        private System.Windows.Forms.TextBox textBox1;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label lblType;
        private System.Windows.Forms.ComboBox cbType;
        private System.Windows.Forms.Label lblTypeDesc;
        private System.Windows.Forms.Label lblPreset;
        private System.Windows.Forms.ComboBox cbPreset;
        private System.Windows.Forms.Label lblPresetDescription;
    }
}