namespace WMO.UI.Forms
{
    partial class AssetPreviewForm
    {
        private System.ComponentModel.IContainer components = null;
        
        private Panel pnlMain;
        private Panel pnlPreview;
        private Panel pnlProperties;
        private Panel pnlImageContainer;
        private PictureBox picPreview;
        private Label lblAssetTitle;
        private RichTextBox txtProperties;
        private Button btnClose;
        private Splitter splitter;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.pnlMain = new Panel();
            this.pnlPreview = new Panel();
            this.pnlProperties = new Panel();
            this.pnlImageContainer = new Panel();
            this.picPreview = new PictureBox();
            this.lblAssetTitle = new Label();
            this.txtProperties = new RichTextBox();
            this.btnClose = new Button();
            this.splitter = new Splitter();
            
            this.pnlMain.SuspendLayout();
            this.pnlPreview.SuspendLayout();
            this.pnlImageContainer.SuspendLayout();
            this.pnlProperties.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.picPreview)).BeginInit();
            this.SuspendLayout();
            
            // 
            // AssetPreviewForm
            // 
            this.AutoScaleDimensions = new SizeF(7F, 15F);
            this.AutoScaleMode = AutoScaleMode.Font;
            this.ClientSize = new Size(800, 600);
            this.Controls.Add(this.pnlMain);
            this.Controls.Add(this.btnClose);
            this.MinimumSize = new Size(600, 400);
            this.Name = "AssetPreviewForm";
            this.StartPosition = FormStartPosition.CenterParent;
            this.Text = "Asset Preview";
            
            // 
            // pnlMain
            // 
            this.pnlMain.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            this.pnlMain.Controls.Add(this.pnlProperties);
            this.pnlMain.Controls.Add(this.splitter);
            this.pnlMain.Controls.Add(this.pnlPreview);
            this.pnlMain.Location = new Point(10, 10);
            this.pnlMain.Name = "pnlMain";
            this.pnlMain.Size = new Size(780, 545);
            this.pnlMain.TabIndex = 0;
            
            // 
            // pnlPreview
            // 
            this.pnlPreview.BorderStyle = BorderStyle.FixedSingle;
            this.pnlPreview.Controls.Add(this.pnlImageContainer);
            this.pnlPreview.Controls.Add(this.lblAssetTitle);
            this.pnlPreview.Dock = DockStyle.Left;
            this.pnlPreview.Location = new Point(0, 0);
            this.pnlPreview.Name = "pnlPreview";
            this.pnlPreview.Size = new Size(400, 545);
            this.pnlPreview.TabIndex = 0;
            
            // 
            // lblAssetTitle
            // 
            this.lblAssetTitle.Dock = DockStyle.Top;
            this.lblAssetTitle.Font = new Font("Microsoft Sans Serif", 10F, FontStyle.Bold);
            this.lblAssetTitle.Location = new Point(0, 0);
            this.lblAssetTitle.Name = "lblAssetTitle";
            this.lblAssetTitle.Padding = new Padding(5);
            this.lblAssetTitle.Size = new Size(398, 30);
            this.lblAssetTitle.TabIndex = 0;
            this.lblAssetTitle.Text = "Asset Name";
            
            // 
            // pnlImageContainer
            // 
            this.pnlImageContainer.AutoScroll = true;
            this.pnlImageContainer.BackColor = SystemColors.ControlDark;
            this.pnlImageContainer.Controls.Add(this.picPreview);
            this.pnlImageContainer.Dock = DockStyle.Fill;
            this.pnlImageContainer.Location = new Point(0, 30);
            this.pnlImageContainer.Name = "pnlImageContainer";
            this.pnlImageContainer.Size = new Size(398, 513);
            this.pnlImageContainer.TabIndex = 1;
            
            // 
            // picPreview
            // 
            this.picPreview.Location = new Point(0, 0);
            this.picPreview.Name = "picPreview";
            this.picPreview.Size = new Size(100, 50);
            this.picPreview.SizeMode = PictureBoxSizeMode.AutoSize;
            this.picPreview.TabIndex = 0;
            this.picPreview.TabStop = false;
            
            // 
            // splitter
            // 
            this.splitter.Location = new Point(400, 0);
            this.splitter.Name = "splitter";
            this.splitter.Size = new Size(3, 545);
            this.splitter.TabIndex = 1;
            this.splitter.TabStop = false;
            
            // 
            // pnlProperties
            // 
            this.pnlProperties.BorderStyle = BorderStyle.FixedSingle;
            this.pnlProperties.Controls.Add(this.txtProperties);
            this.pnlProperties.Dock = DockStyle.Fill;
            this.pnlProperties.Location = new Point(403, 0);
            this.pnlProperties.Name = "pnlProperties";
            this.pnlProperties.Size = new Size(377, 545);
            this.pnlProperties.TabIndex = 2;
            
            // 
            // txtProperties
            // 
            this.txtProperties.BackColor = SystemColors.Window;
            this.txtProperties.Dock = DockStyle.Fill;
            this.txtProperties.Font = new Font("Consolas", 9F);
            this.txtProperties.Location = new Point(0, 0);
            this.txtProperties.Name = "txtProperties";
            this.txtProperties.ReadOnly = true;
            this.txtProperties.Size = new Size(375, 543);
            this.txtProperties.TabIndex = 0;
            this.txtProperties.Text = "";
            
            // 
            // btnClose
            // 
            this.btnClose.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            this.btnClose.Location = new Point(715, 565);
            this.btnClose.Name = "btnClose";
            this.btnClose.Size = new Size(75, 25);
            this.btnClose.TabIndex = 1;
            this.btnClose.Text = "Close";
            this.btnClose.UseVisualStyleBackColor = true;
            this.btnClose.Click += new EventHandler(this.btnClose_Click);
            
            this.pnlMain.ResumeLayout(false);
            this.pnlPreview.ResumeLayout(false);
            this.pnlImageContainer.ResumeLayout(false);
            this.pnlImageContainer.PerformLayout();
            this.pnlProperties.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.picPreview)).EndInit();
            this.ResumeLayout(false);
        }
    }
}
