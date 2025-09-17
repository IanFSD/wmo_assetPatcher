namespace WMO.UI.Forms
{
    partial class ConsoleOutputForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;
        
        private TextBox txtOutput;
        private Button btnClose;
        private Button btnClear;
        private Button btnCopy;

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
            this.txtOutput = new TextBox();
            this.btnClose = new Button();
            this.btnClear = new Button();
            this.btnCopy = new Button();
            this.SuspendLayout();
            
            // 
            // txtOutput
            // 
            this.txtOutput.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            this.txtOutput.BackColor = Color.Black;
            this.txtOutput.ForeColor = Color.White;
            this.txtOutput.Font = new Font("Consolas", 9F);
            this.txtOutput.Location = new Point(12, 12);
            this.txtOutput.Multiline = true;
            this.txtOutput.Name = "txtOutput";
            this.txtOutput.ReadOnly = true;
            this.txtOutput.ScrollBars = ScrollBars.Vertical;
            this.txtOutput.Size = new Size(760, 500);
            this.txtOutput.TabIndex = 0;
            
            // 
            // btnClear
            // 
            this.btnClear.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            this.btnClear.Location = new Point(12, 525);
            this.btnClear.Name = "btnClear";
            this.btnClear.Size = new Size(75, 30);
            this.btnClear.TabIndex = 1;
            this.btnClear.Text = "Clear";
            this.btnClear.UseVisualStyleBackColor = true;
            this.btnClear.Click += this.btnClear_Click;
            
            // 
            // btnCopy
            // 
            this.btnCopy.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            this.btnCopy.Location = new Point(93, 525);
            this.btnCopy.Name = "btnCopy";
            this.btnCopy.Size = new Size(75, 30);
            this.btnCopy.TabIndex = 2;
            this.btnCopy.Text = "Copy All";
            this.btnCopy.UseVisualStyleBackColor = true;
            this.btnCopy.Click += this.btnCopy_Click;
            
            // 
            // btnClose
            // 
            this.btnClose.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            this.btnClose.Location = new Point(697, 525);
            this.btnClose.Name = "btnClose";
            this.btnClose.Size = new Size(75, 30);
            this.btnClose.TabIndex = 3;
            this.btnClose.Text = "Close";
            this.btnClose.UseVisualStyleBackColor = true;
            this.btnClose.Click += this.btnClose_Click;
            
            // 
            // ConsoleOutputForm
            // 
            this.AutoScaleDimensions = new SizeF(7F, 15F);
            this.AutoScaleMode = AutoScaleMode.Font;
            this.ClientSize = new Size(784, 567);
            this.Controls.Add(this.btnClose);
            this.Controls.Add(this.btnCopy);
            this.Controls.Add(this.btnClear);
            this.Controls.Add(this.txtOutput);
            this.Name = "ConsoleOutputForm";
            this.Text = "Console Output";
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        #endregion
    }
}
