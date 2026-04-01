namespace GbaUploadGUI
{
    partial class GbaUploadGuiForm
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
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.OpenSerialPortButton = new System.Windows.Forms.Button();
            this.serialPortSelection = new System.Windows.Forms.ComboBox();
            this.label1 = new System.Windows.Forms.Label();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.RomCRC32 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.BrowseROMFileButton = new System.Windows.Forms.Button();
            this.RomTitleLabel = new System.Windows.Forms.Label();
            this.SelectedFileLabel = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.groupBox3 = new System.Windows.Forms.GroupBox();
            this.UploadButton = new System.Windows.Forms.Button();
            this.progressBar1 = new System.Windows.Forms.ProgressBar();
            this.groupBox1.SuspendLayout();
            this.groupBox2.SuspendLayout();
            this.groupBox3.SuspendLayout();
            this.SuspendLayout();
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.OpenSerialPortButton);
            this.groupBox1.Controls.Add(this.serialPortSelection);
            this.groupBox1.Controls.Add(this.label1);
            this.groupBox1.Location = new System.Drawing.Point(8, 8);
            this.groupBox1.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Padding = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.groupBox1.Size = new System.Drawing.Size(512, 61);
            this.groupBox1.TabIndex = 0;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Serial port configuration";
            // 
            // OpenSerialPortButton
            // 
            this.OpenSerialPortButton.Location = new System.Drawing.Point(413, 20);
            this.OpenSerialPortButton.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.OpenSerialPortButton.Name = "OpenSerialPortButton";
            this.OpenSerialPortButton.Size = new System.Drawing.Size(89, 24);
            this.OpenSerialPortButton.TabIndex = 2;
            this.OpenSerialPortButton.Text = "Open";
            this.OpenSerialPortButton.UseVisualStyleBackColor = true;
            this.OpenSerialPortButton.Click += new System.EventHandler(this.OpenSerialPortButton_Click);
            // 
            // serialPortSelection
            // 
            this.serialPortSelection.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.serialPortSelection.FormattingEnabled = true;
            this.serialPortSelection.Location = new System.Drawing.Point(38, 15);
            this.serialPortSelection.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.serialPortSelection.Name = "serialPortSelection";
            this.serialPortSelection.Size = new System.Drawing.Size(108, 21);
            this.serialPortSelection.TabIndex = 1;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(5, 20);
            this.label1.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(29, 13);
            this.label1.TabIndex = 0;
            this.label1.Text = "Port:";
            this.label1.Click += new System.EventHandler(this.label1_Click);
            // 
            // groupBox2
            // 
            this.groupBox2.Controls.Add(this.RomCRC32);
            this.groupBox2.Controls.Add(this.label3);
            this.groupBox2.Controls.Add(this.BrowseROMFileButton);
            this.groupBox2.Controls.Add(this.RomTitleLabel);
            this.groupBox2.Controls.Add(this.SelectedFileLabel);
            this.groupBox2.Controls.Add(this.label2);
            this.groupBox2.Location = new System.Drawing.Point(8, 73);
            this.groupBox2.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Padding = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.groupBox2.Size = new System.Drawing.Size(512, 93);
            this.groupBox2.TabIndex = 1;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "Multiboot Image";
            // 
            // RomCRC32
            // 
            this.RomCRC32.AutoSize = true;
            this.RomCRC32.Location = new System.Drawing.Point(5, 70);
            this.RomCRC32.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.RomCRC32.Name = "RomCRC32";
            this.RomCRC32.Size = new System.Drawing.Size(47, 13);
            this.RomCRC32.TabIndex = 5;
            this.RomCRC32.Text = "CRC32: ";
            // 
            // label3
            // 
            this.label3.Location = new System.Drawing.Point(5, 52);
            this.label3.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(118, 13);
            this.label3.TabIndex = 4;
            this.label3.Text = "Game code:";
            // 
            // BrowseROMFileButton
            // 
            this.BrowseROMFileButton.Location = new System.Drawing.Point(413, 17);
            this.BrowseROMFileButton.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.BrowseROMFileButton.Name = "BrowseROMFileButton";
            this.BrowseROMFileButton.Size = new System.Drawing.Size(89, 26);
            this.BrowseROMFileButton.TabIndex = 2;
            this.BrowseROMFileButton.Text = "Browse...";
            this.BrowseROMFileButton.UseVisualStyleBackColor = true;
            this.BrowseROMFileButton.Click += new System.EventHandler(this.BrowseROMFileButton_Click);
            // 
            // RomTitleLabel
            // 
            this.RomTitleLabel.AutoSize = true;
            this.RomTitleLabel.Location = new System.Drawing.Point(5, 35);
            this.RomTitleLabel.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.RomTitleLabel.Name = "RomTitleLabel";
            this.RomTitleLabel.Size = new System.Drawing.Size(30, 13);
            this.RomTitleLabel.TabIndex = 3;
            this.RomTitleLabel.Text = "Title:";
            // 
            // SelectedFileLabel
            // 
            this.SelectedFileLabel.Location = new System.Drawing.Point(35, 17);
            this.SelectedFileLabel.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.SelectedFileLabel.Name = "SelectedFileLabel";
            this.SelectedFileLabel.Size = new System.Drawing.Size(203, 18);
            this.SelectedFileLabel.TabIndex = 1;
            this.SelectedFileLabel.Text = "No file selected";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(5, 17);
            this.label2.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(26, 13);
            this.label2.TabIndex = 0;
            this.label2.Text = "File:";
            // 
            // groupBox3
            // 
            this.groupBox3.Controls.Add(this.UploadButton);
            this.groupBox3.Controls.Add(this.progressBar1);
            this.groupBox3.Location = new System.Drawing.Point(8, 170);
            this.groupBox3.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.groupBox3.Name = "groupBox3";
            this.groupBox3.Padding = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.groupBox3.Size = new System.Drawing.Size(512, 63);
            this.groupBox3.TabIndex = 2;
            this.groupBox3.TabStop = false;
            this.groupBox3.Text = "Upload";
            // 
            // UploadButton
            // 
            this.UploadButton.Location = new System.Drawing.Point(413, 21);
            this.UploadButton.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.UploadButton.Name = "UploadButton";
            this.UploadButton.Size = new System.Drawing.Size(89, 26);
            this.UploadButton.TabIndex = 3;
            this.UploadButton.Text = "Upload!";
            this.UploadButton.UseVisualStyleBackColor = true;
            this.UploadButton.Click += new System.EventHandler(this.UploadButton_Click);
            // 
            // progressBar1
            // 
            this.progressBar1.Location = new System.Drawing.Point(8, 25);
            this.progressBar1.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.progressBar1.Name = "progressBar1";
            this.progressBar1.Size = new System.Drawing.Size(380, 18);
            this.progressBar1.TabIndex = 0;
            // 
            // GbaUploadGuiForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(531, 249);
            this.Controls.Add(this.groupBox3);
            this.Controls.Add(this.groupBox2);
            this.Controls.Add(this.groupBox1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "GbaUploadGuiForm";
            this.Text = "gba-usb-multiboot uploader";
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.groupBox2.ResumeLayout(false);
            this.groupBox2.PerformLayout();
            this.groupBox3.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Button OpenSerialPortButton;
        private System.Windows.Forms.ComboBox serialPortSelection;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.Button BrowseROMFileButton;
        private System.Windows.Forms.Label SelectedFileLabel;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label RomTitleLabel;
        private System.Windows.Forms.Label RomCRC32;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.GroupBox groupBox3;
        private System.Windows.Forms.Button UploadButton;
        private System.Windows.Forms.ProgressBar progressBar1;
    }
}

