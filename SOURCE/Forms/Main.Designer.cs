
namespace UserDataBackup.Forms {
    partial class Main {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing) {
            if (disposing && (components != null)) {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent() {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Main));
            this.statusImage = new System.Windows.Forms.PictureBox();
            this.backup = new System.Windows.Forms.Button();
            this.progressBar2 = new System.Windows.Forms.ProgressBar();
            this.progressBar3 = new System.Windows.Forms.ProgressBar();
            this.restore = new System.Windows.Forms.Button();
            this.statusLabel = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.statusImage)).BeginInit();
            this.SuspendLayout();
            // 
            // statusImage
            // 
            this.statusImage.InitialImage = ((System.Drawing.Image)(resources.GetObject("statusImage.InitialImage")));
            this.statusImage.Location = new System.Drawing.Point(104, 12);
            this.statusImage.Name = "statusImage";
            this.statusImage.Size = new System.Drawing.Size(174, 99);
            this.statusImage.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
            this.statusImage.TabIndex = 0;
            this.statusImage.TabStop = false;
            // 
            // backup
            // 
            this.backup.Location = new System.Drawing.Point(50, 169);
            this.backup.Name = "backup";
            this.backup.Size = new System.Drawing.Size(92, 48);
            this.backup.TabIndex = 2;
            this.backup.Text = "Backup to OneDrive";
            this.backup.UseVisualStyleBackColor = true;
            this.backup.Click += new System.EventHandler(this.Backup_Click);
            // 
            // progressBar2
            // 
            this.progressBar2.Location = new System.Drawing.Point(12, 140);
            this.progressBar2.Name = "progressBar2";
            this.progressBar2.Size = new System.Drawing.Size(169, 23);
            this.progressBar2.TabIndex = 3;
            this.progressBar2.Visible = false;
            // 
            // progressBar3
            // 
            this.progressBar3.Location = new System.Drawing.Point(202, 140);
            this.progressBar3.Name = "progressBar3";
            this.progressBar3.Size = new System.Drawing.Size(169, 23);
            this.progressBar3.TabIndex = 4;
            this.progressBar3.Visible = false;
            // 
            // restore
            // 
            this.restore.Location = new System.Drawing.Point(240, 169);
            this.restore.Name = "restore";
            this.restore.Size = new System.Drawing.Size(92, 48);
            this.restore.TabIndex = 5;
            this.restore.Text = "Restore from OneDrive";
            this.restore.UseVisualStyleBackColor = true;
            this.restore.Click += new System.EventHandler(this.Restore_Click);
            // 
            // statusLabel
            // 
            this.statusLabel.Location = new System.Drawing.Point(9, 114);
            this.statusLabel.Name = "statusLabel";
            this.statusLabel.Size = new System.Drawing.Size(359, 23);
            this.statusLabel.TabIndex = 6;
            this.statusLabel.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // Main
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(383, 225);
            this.Controls.Add(this.statusLabel);
            this.Controls.Add(this.restore);
            this.Controls.Add(this.progressBar3);
            this.Controls.Add(this.progressBar2);
            this.Controls.Add(this.backup);
            this.Controls.Add(this.statusImage);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "Main";
            this.Text = "User Data Backup and Restore";
            this.TopMost = true;
            this.Load += new System.EventHandler(this.Main_Load);
            ((System.ComponentModel.ISupportInitialize)(this.statusImage)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.PictureBox statusImage;
        private System.Windows.Forms.Button backup;
        private System.Windows.Forms.ProgressBar progressBar2;
        private System.Windows.Forms.ProgressBar progressBar3;
        private System.Windows.Forms.Button restore;
        private System.Windows.Forms.Label statusLabel;
    }
}

