namespace WMO.UI.Forms
{
    partial class SetupForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;
        
        private Label lblTitle;
        private Label lblDescription;
        private Label lblGamePath;
        private TextBox txtGamePath;
        private Button btnBrowse;
        private Label lblPathStatus;
        private Label lblGameVersion;
        private ComboBox cmbGameVersion;
        private CheckBox chkShowConsole;
        private Button btnFinish;
        private Button btnCancel;

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
            this.lblTitle = new Label();
            this.lblDescription = new Label();
            this.lblGamePath = new Label();
            this.txtGamePath = new TextBox();
            this.btnBrowse = new Button();
            this.lblPathStatus = new Label();
            this.lblGameVersion = new Label();
            this.cmbGameVersion = new ComboBox();
            this.chkShowConsole = new CheckBox();
            this.btnFinish = new Button();
            this.btnCancel = new Button();
            this.SuspendLayout();
            
            // 
            // lblTitle
            // 
            this.lblTitle.AutoSize = true;
            this.lblTitle.Font = new Font("Segoe UI", 14F, FontStyle.Bold);
            this.lblTitle.Location = new Point(20, 20);
            this.lblTitle.Name = "lblTitle";
            this.lblTitle.Size = new Size(300, 25);
            this.lblTitle.TabIndex = 0;
            this.lblTitle.Text = "WMO Asset Patcher - Setup";
            
            // 
            // lblDescription
            // 
            this.lblDescription.Location = new Point(20, 55);
            this.lblDescription.Name = "lblDescription";
            this.lblDescription.Size = new Size(450, 60);
            this.lblDescription.TabIndex = 1;
            this.lblDescription.Text = "Welcome to WMO Asset Patcher! This appears to be your first time running the application. Please configure the following settings to get started:";
            
            // 
            // lblGamePath
            // 
            this.lblGamePath.AutoSize = true;
            this.lblGamePath.Location = new Point(20, 130);
            this.lblGamePath.Name = "lblGamePath";
            this.lblGamePath.Size = new Size(200, 15);
            this.lblGamePath.TabIndex = 2;
            this.lblGamePath.Text = "Whisper Mountain Outbreak Location:";
            
            // 
            // txtGamePath
            // 
            this.txtGamePath.Location = new Point(20, 150);
            this.txtGamePath.Name = "txtGamePath";
            this.txtGamePath.Size = new Size(350, 23);
            this.txtGamePath.TabIndex = 3;
            this.txtGamePath.TextChanged += this.txtGamePath_TextChanged;
            
            // 
            // btnBrowse
            // 
            this.btnBrowse.Location = new Point(380, 149);
            this.btnBrowse.Name = "btnBrowse";
            this.btnBrowse.Size = new Size(75, 25);
            this.btnBrowse.TabIndex = 4;
            this.btnBrowse.Text = "Browse...";
            this.btnBrowse.UseVisualStyleBackColor = true;
            this.btnBrowse.Click += this.btnBrowse_Click;
            
            // 
            // lblPathStatus
            // 
            this.lblPathStatus.AutoSize = true;
            this.lblPathStatus.Location = new Point(20, 180);
            this.lblPathStatus.Name = "lblPathStatus";
            this.lblPathStatus.Size = new Size(120, 15);
            this.lblPathStatus.TabIndex = 5;
            this.lblPathStatus.Text = "Please select a path";
            this.lblPathStatus.ForeColor = Color.Gray;
            
            // 
            // lblGameVersion
            // 
            this.lblGameVersion.AutoSize = true;
            this.lblGameVersion.Location = new Point(20, 210);
            this.lblGameVersion.Name = "lblGameVersion";
            this.lblGameVersion.Size = new Size(90, 15);
            this.lblGameVersion.TabIndex = 6;
            this.lblGameVersion.Text = "Game Version:";
            
            // 
            // cmbGameVersion
            // 
            this.cmbGameVersion.DropDownStyle = ComboBoxStyle.DropDownList;
            this.cmbGameVersion.Location = new Point(20, 230);
            this.cmbGameVersion.Name = "cmbGameVersion";
            this.cmbGameVersion.Size = new Size(200, 23);
            this.cmbGameVersion.TabIndex = 7;

            // 
            // chkShowConsole
            // 
            this.chkShowConsole.AutoSize = true;
            this.chkShowConsole.Location = new Point(20, 270);
            this.chkShowConsole.Name = "chkShowConsole";
            this.chkShowConsole.Size = new Size(140, 19);
            this.chkShowConsole.TabIndex = 8;
            this.chkShowConsole.Text = "Show debug console";
            this.chkShowConsole.UseVisualStyleBackColor = true;
            
            // 
            // btnFinish
            // 
            this.btnFinish.Location = new Point(300, 310);
            this.btnFinish.Name = "btnFinish";
            this.btnFinish.Size = new Size(75, 30);
            this.btnFinish.TabIndex = 9;
            this.btnFinish.Text = "Finish";
            this.btnFinish.UseVisualStyleBackColor = true;
            this.btnFinish.Click += this.btnFinish_Click;
            this.btnFinish.Enabled = false;
            
            // 
            // btnCancel
            // 
            this.btnCancel.Location = new Point(380, 310);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new Size(75, 30);
            this.btnCancel.TabIndex = 10;
            this.btnCancel.Text = "Cancel";
            this.btnCancel.UseVisualStyleBackColor = true;
            this.btnCancel.Click += this.btnCancel_Click;
            
            // 
            // SetupForm
            // 
            this.AutoScaleDimensions = new SizeF(7F, 15F);
            this.AutoScaleMode = AutoScaleMode.Font;
            this.ClientSize = new Size(484, 360);
            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.btnFinish);
            this.Controls.Add(this.chkShowConsole);
            this.Controls.Add(this.cmbGameVersion);
            this.Controls.Add(this.lblGameVersion);
            this.Controls.Add(this.lblPathStatus);
            this.Controls.Add(this.btnBrowse);
            this.Controls.Add(this.txtGamePath);
            this.Controls.Add(this.lblGamePath);
            this.Controls.Add(this.lblDescription);
            this.Controls.Add(this.lblTitle);
            this.Name = "SetupForm";
            this.Text = "Setup";
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        #endregion
    }
}
