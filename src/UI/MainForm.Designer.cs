namespace WMO.UI
{
    partial class MainForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;
        
        private MenuStrip menuStrip;
        private ToolStripMenuItem menuFile;
        private ToolStripMenuItem menuFileExit;
        
        private TabControl tabControl;
        private TabPage tabMods;
        private TabPage tabSettings;
        private GroupBox grpGeneral;
        private GroupBox grpLogging;
        private ComboBox cmbLogLevel;
        private Label lblLogLevel;
        private GroupBox grpInterface;
        private CheckBox chkRememberWindowSize;
        private CheckBox chkDarkMode;
        private Button btnBrowseGamePath;
        private TextBox txtGamePath;
        private Label lblGamePath;
        
        // Mods tab controls
        private ListView lstMods;
        private Label lblModCount;
        private Label lblGamePathStatus;
        private Button btnPatchGame;
        private Button btnLaunchGame;
        private Button btnRefreshMods;
        
        // Status bar
        private StatusStrip statusStrip;
        private ToolStripStatusLabel statusLabel;

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
            this.menuStrip = new MenuStrip();
            this.menuFile = new ToolStripMenuItem();
            this.menuFileExit = new ToolStripMenuItem();
            
            this.tabControl = new TabControl();
            this.tabMods = new TabPage();
            this.tabSettings = new TabPage();
            this.grpGeneral = new GroupBox();
            this.lblGamePath = new Label();
            this.txtGamePath = new TextBox();
            this.btnBrowseGamePath = new Button();
            this.grpLogging = new GroupBox();
            this.lblLogLevel = new Label();
            this.cmbLogLevel = new ComboBox();
            this.grpInterface = new GroupBox();
            this.chkRememberWindowSize = new CheckBox();
            this.chkDarkMode = new CheckBox();
            
            this.lstMods = new ListView();
            this.lblModCount = new Label();
            this.lblGamePathStatus = new Label();
            this.btnPatchGame = new Button();
            this.btnLaunchGame = new Button();
            this.btnRefreshMods = new Button();
            
            this.statusStrip = new StatusStrip();
            this.statusLabel = new ToolStripStatusLabel();
            
            this.menuStrip.SuspendLayout();
            this.tabControl.SuspendLayout();
            this.tabMods.SuspendLayout();
            this.statusStrip.SuspendLayout();
            this.SuspendLayout();
            
            // 
            // menuStrip
            // 
            this.menuStrip.Items.AddRange(new ToolStripItem[] {
                this.menuFile});
            this.menuStrip.Location = new Point(0, 0);
            this.menuStrip.Name = "menuStrip";
            this.menuStrip.Size = new Size(884, 24);
            this.menuStrip.TabIndex = 0;
            this.menuStrip.Text = "menuStrip";
            
            // 
            // menuFile
            // 
            this.menuFile.DropDownItems.AddRange(new ToolStripItem[] {
                this.menuFileExit});
            this.menuFile.Name = "menuFile";
            this.menuFile.Size = new Size(37, 20);
            this.menuFile.Text = "&File";
            
            // 
            // menuFileExit
            // 
            this.menuFileExit.Name = "menuFileExit";
            this.menuFileExit.Size = new Size(93, 22);
            this.menuFileExit.Text = "E&xit";
            this.menuFileExit.Click += this.menuFileExit_Click;
            
            // 
            // tabControl
            // 
            this.tabControl.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            this.tabControl.Controls.Add(this.tabMods);
            this.tabControl.Controls.Add(this.tabSettings);
            this.tabControl.Location = new Point(12, 27);
            this.tabControl.Name = "tabControl";
            this.tabControl.SelectedIndex = 0;
            this.tabControl.Size = new Size(860, 536);
            this.tabControl.TabIndex = 1;
            
            // 
            // tabMods
            // 
            this.tabMods.Controls.Add(this.lstMods);
            this.tabMods.Controls.Add(this.lblModCount);
            this.tabMods.Controls.Add(this.lblGamePathStatus);
            this.tabMods.Controls.Add(this.btnPatchGame);
            this.tabMods.Controls.Add(this.btnLaunchGame);
            this.tabMods.Controls.Add(this.btnRefreshMods);
            this.tabMods.Location = new Point(4, 24);
            this.tabMods.Name = "tabMods";
            this.tabMods.Padding = new Padding(3);
            this.tabMods.Size = new Size(852, 508);
            this.tabMods.TabIndex = 0;
            this.tabMods.Text = "Mods";
            this.tabMods.UseVisualStyleBackColor = true;
            
            // 
            // lstMods
            // 
            this.lstMods.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            this.lstMods.CheckBoxes = true;
            this.lstMods.FullRowSelect = true;
            this.lstMods.GridLines = true;
            this.lstMods.Location = new Point(6, 35);
            this.lstMods.Name = "lstMods";
            this.lstMods.Size = new Size(840, 380);
            this.lstMods.TabIndex = 1;
            this.lstMods.UseCompatibleStateImageBehavior = false;
            this.lstMods.View = View.Details;
            this.lstMods.ItemChecked += this.lstMods_ItemChecked;
            
            // Set up columns
            this.lstMods.Columns.Add("Mod Name", 300);
            this.lstMods.Columns.Add("Types", 150);
            this.lstMods.Columns.Add("Files", 200);
            
            // 
            // lblModCount
            // 
            this.lblModCount.AutoSize = true;
            this.lblModCount.Location = new Point(6, 10);
            this.lblModCount.Name = "lblModCount";
            this.lblModCount.Size = new Size(79, 15);
            this.lblModCount.TabIndex = 0;
            this.lblModCount.Text = "Mods found: 0";
            
            // 
            // lblGamePathStatus
            // 
            this.lblGamePathStatus.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            this.lblGamePathStatus.Location = new Point(6, 425);
            this.lblGamePathStatus.Name = "lblGamePathStatus";
            this.lblGamePathStatus.Size = new Size(840, 15);
            this.lblGamePathStatus.TabIndex = 2;
            this.lblGamePathStatus.Text = "Game path status";
            
            // 
            // btnRefreshMods
            // 
            this.btnRefreshMods.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            this.btnRefreshMods.Location = new Point(6, 450);
            this.btnRefreshMods.Name = "btnRefreshMods";
            this.btnRefreshMods.Size = new Size(100, 35);
            this.btnRefreshMods.TabIndex = 3;
            this.btnRefreshMods.Text = "Refresh Mods";
            this.btnRefreshMods.UseVisualStyleBackColor = true;
            this.btnRefreshMods.Click += this.btnRefreshMods_Click;
            
            // 
            // btnPatchGame
            // 
            this.btnPatchGame.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            this.btnPatchGame.Location = new Point(636, 450);
            this.btnPatchGame.Name = "btnPatchGame";
            this.btnPatchGame.Size = new Size(100, 35);
            this.btnPatchGame.TabIndex = 4;
            this.btnPatchGame.Text = "Patch Game";
            this.btnLaunchGame.UseVisualStyleBackColor = true;
            this.btnPatchGame.Click += this.btnPatchGame_Click;
            
            // 
            // btnLaunchGame
            // 
            this.btnLaunchGame.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            this.btnLaunchGame.Location = new Point(746, 450);
            this.btnLaunchGame.Name = "btnLaunchGame";
            this.btnLaunchGame.Size = new Size(100, 35);
            this.btnLaunchGame.TabIndex = 5;
            this.btnLaunchGame.Text = "Launch Game";
            this.btnLaunchGame.UseVisualStyleBackColor = true;
            this.btnLaunchGame.Click += this.btnLaunchGame_Click;
            
            // 
            // tabSettings
            // 
            this.tabSettings.Controls.Add(this.grpGeneral);
            this.tabSettings.Controls.Add(this.grpLogging);
            this.tabSettings.Controls.Add(this.grpInterface);
            this.tabSettings.Location = new Point(4, 24);
            this.tabSettings.Name = "tabSettings";
            this.tabSettings.Padding = new Padding(3);
            this.tabSettings.Size = new Size(852, 508);
            this.tabSettings.TabIndex = 1;
            this.tabSettings.Text = "Settings";
            this.tabSettings.UseVisualStyleBackColor = true;
            
            // 
            // grpGeneral
            // 
            this.grpGeneral.Controls.Add(this.lblGamePath);
            this.grpGeneral.Controls.Add(this.txtGamePath);
            this.grpGeneral.Controls.Add(this.btnBrowseGamePath);
            this.grpGeneral.Location = new Point(6, 6);
            this.grpGeneral.Name = "grpGeneral";
            this.grpGeneral.Size = new Size(840, 160);
            this.grpGeneral.TabIndex = 0;
            this.grpGeneral.TabStop = false;
            this.grpGeneral.Text = "General Settings";
            
            // 
            // lblGamePath
            // 
            this.lblGamePath.AutoSize = true;
            this.lblGamePath.Location = new Point(10, 25);
            this.lblGamePath.Name = "lblGamePath";
            this.lblGamePath.Size = new Size(70, 15);
            this.lblGamePath.TabIndex = 0;
            this.lblGamePath.Text = "Game Path:";
            
            // 
            // txtGamePath
            // 
            this.txtGamePath.Location = new Point(90, 22);
            this.txtGamePath.Name = "txtGamePath";
            this.txtGamePath.Size = new Size(650, 23);
            this.txtGamePath.TabIndex = 1;
            
            // 
            // btnBrowseGamePath
            // 
            this.btnBrowseGamePath.Location = new Point(750, 21);
            this.btnBrowseGamePath.Name = "btnBrowseGamePath";
            this.btnBrowseGamePath.Size = new Size(75, 25);
            this.btnBrowseGamePath.TabIndex = 2;
            this.btnBrowseGamePath.Text = "Browse...";
            this.btnBrowseGamePath.UseVisualStyleBackColor = true;
            
            // 
            // grpLogging
            // 
            this.grpLogging.Controls.Add(this.lblLogLevel);
            this.grpLogging.Controls.Add(this.cmbLogLevel);
            this.grpLogging.Location = new Point(6, 180);
            this.grpLogging.Name = "grpLogging";
            this.grpLogging.Size = new Size(840, 70);
            this.grpLogging.TabIndex = 1;
            this.grpLogging.TabStop = false;
            this.grpLogging.Text = "Logging Settings";
            
            // 
            // lblLogLevel
            // 
            this.lblLogLevel.AutoSize = true;
            this.lblLogLevel.Location = new Point(10, 25);
            this.lblLogLevel.Name = "lblLogLevel";
            this.lblLogLevel.Size = new Size(62, 15);
            this.lblLogLevel.TabIndex = 0;
            this.lblLogLevel.Text = "Log Level:";
            
            // 
            // cmbLogLevel
            // 
            this.cmbLogLevel.DropDownStyle = ComboBoxStyle.DropDownList;
            this.cmbLogLevel.FormattingEnabled = true;
            this.cmbLogLevel.Location = new Point(80, 22);
            this.cmbLogLevel.Name = "cmbLogLevel";
            this.cmbLogLevel.Size = new Size(150, 23);
            this.cmbLogLevel.TabIndex = 1;
            
            // 
            // grpInterface
            // 
            this.grpInterface.Controls.Add(this.chkRememberWindowSize);
            this.grpInterface.Controls.Add(this.chkDarkMode);
            this.grpInterface.Location = new Point(6, 260);
            this.grpInterface.Name = "grpInterface";
            this.grpInterface.Size = new Size(840, 80);
            this.grpInterface.TabIndex = 2;
            this.grpInterface.TabStop = false;
            this.grpInterface.Text = "Interface Settings";
            
            // 
            // chkRememberWindowSize
            // 
            this.chkRememberWindowSize.AutoSize = true;
            this.chkRememberWindowSize.Location = new Point(10, 25);
            this.chkRememberWindowSize.Name = "chkRememberWindowSize";
            this.chkRememberWindowSize.Size = new Size(140, 19);
            this.chkRememberWindowSize.TabIndex = 0;
            this.chkRememberWindowSize.Text = "Remember window size";
            this.chkRememberWindowSize.UseVisualStyleBackColor = true;
            
            // 
            // chkDarkMode
            // 
            this.chkDarkMode.AutoSize = true;
            this.chkDarkMode.Location = new Point(10, 50);
            this.chkDarkMode.Name = "chkDarkMode";
            this.chkDarkMode.Size = new Size(145, 19);
            this.chkDarkMode.TabIndex = 1;
            this.chkDarkMode.Text = "Enable dark mode (experimental)";
            this.chkDarkMode.UseVisualStyleBackColor = true;
            
            // 
            // statusStrip
            // 
            this.statusStrip.Items.AddRange(new ToolStripItem[] {
                this.statusLabel});
            this.statusStrip.Location = new Point(0, 574);
            this.statusStrip.Name = "statusStrip";
            this.statusStrip.Size = new Size(884, 22);
            this.statusStrip.TabIndex = 2;
            this.statusStrip.Text = "statusStrip";
            
            // 
            // statusLabel
            // 
            this.statusLabel.Name = "statusLabel";
            this.statusLabel.Size = new Size(39, 17);
            this.statusLabel.Text = "Ready";
            
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new SizeF(7F, 15F);
            this.AutoScaleMode = AutoScaleMode.Font;
            this.ClientSize = new Size(884, 596);
            this.Controls.Add(this.statusStrip);
            this.Controls.Add(this.tabControl);
            this.Controls.Add(this.menuStrip);
            try
            {
                this.Icon = new Icon(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "RER_Icon.ico"));
            }
            catch
            {
                // If icon loading fails, continue without it
            }
            this.MainMenuStrip = this.menuStrip;
            this.Name = "MainForm";
            this.Text = "WMO Asset Patcher";
            this.menuStrip.ResumeLayout(false);
            this.menuStrip.PerformLayout();
            this.tabControl.ResumeLayout(false);
            this.tabMods.ResumeLayout(false);
            this.tabMods.PerformLayout();
            this.statusStrip.ResumeLayout(false);
            this.statusStrip.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        #endregion
    }
}