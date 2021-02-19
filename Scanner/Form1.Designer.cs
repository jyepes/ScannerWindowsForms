
namespace Scanner
{
    partial class Form1
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
            this.ScannerButton = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // ScannerButton
            // 
            this.ScannerButton.Location = new System.Drawing.Point(263, 148);
            this.ScannerButton.Name = "ScannerButton";
            this.ScannerButton.Size = new System.Drawing.Size(277, 72);
            this.ScannerButton.TabIndex = 0;
            this.ScannerButton.Text = "Adquirir Imágenes";
            this.ScannerButton.UseVisualStyleBackColor = true;
            this.ScannerButton.Click += new System.EventHandler(this.ScannerButton_Click);
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(11F, 24F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 450);
            this.Controls.Add(this.ScannerButton);
            this.Name = "Form1";
            this.Text = "Form1";
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Button ScannerButton;
    }
}

