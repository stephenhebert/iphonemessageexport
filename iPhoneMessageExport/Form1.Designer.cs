namespace iPhoneMessageExport
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form1));
            this.comboBackups = new System.Windows.Forms.ComboBox();
            this.panel1 = new System.Windows.Forms.Panel();
            this.lbMessageGroup = new System.Windows.Forms.ListBox();
            this.labelMessageGroup = new System.Windows.Forms.Label();
            this.labelBackupFiles = new System.Windows.Forms.Label();
            this.btnLoad = new System.Windows.Forms.Button();
            this.btnExport = new System.Windows.Forms.Button();
            this.panel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // comboBackups
            // 
            this.comboBackups.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBackups.Enabled = false;
            this.comboBackups.FormattingEnabled = true;
            this.comboBackups.ItemHeight = 13;
            this.comboBackups.Location = new System.Drawing.Point(12, 29);
            this.comboBackups.Name = "comboBackups";
            this.comboBackups.Size = new System.Drawing.Size(586, 21);
            this.comboBackups.TabIndex = 2;
            // 
            // panel1
            // 
            this.panel1.Controls.Add(this.lbMessageGroup);
            this.panel1.Controls.Add(this.labelMessageGroup);
            this.panel1.Controls.Add(this.labelBackupFiles);
            this.panel1.Controls.Add(this.btnLoad);
            this.panel1.Controls.Add(this.btnExport);
            this.panel1.Controls.Add(this.comboBackups);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panel1.Location = new System.Drawing.Point(0, 0);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(645, 359);
            this.panel1.TabIndex = 4;
            // 
            // lbMessageGroup
            // 
            this.lbMessageGroup.FormattingEnabled = true;
            this.lbMessageGroup.Location = new System.Drawing.Point(12, 78);
            this.lbMessageGroup.Name = "lbMessageGroup";
            this.lbMessageGroup.Size = new System.Drawing.Size(621, 238);
            this.lbMessageGroup.TabIndex = 7;
            this.lbMessageGroup.SelectedIndexChanged += new System.EventHandler(this.lbMessageGroup_SelectedIndexChanged);
            // 
            // labelMessageGroup
            // 
            this.labelMessageGroup.AutoSize = true;
            this.labelMessageGroup.Location = new System.Drawing.Point(12, 62);
            this.labelMessageGroup.Name = "labelMessageGroup";
            this.labelMessageGroup.Size = new System.Drawing.Size(104, 13);
            this.labelMessageGroup.TabIndex = 6;
            this.labelMessageGroup.Text = "Message Group List:";
            // 
            // labelBackupFiles
            // 
            this.labelBackupFiles.AutoSize = true;
            this.labelBackupFiles.Location = new System.Drawing.Point(12, 13);
            this.labelBackupFiles.Name = "labelBackupFiles";
            this.labelBackupFiles.Size = new System.Drawing.Size(71, 13);
            this.labelBackupFiles.TabIndex = 5;
            this.labelBackupFiles.Text = "Backup Files:";
            // 
            // btnLoad
            // 
            this.btnLoad.Image = ((System.Drawing.Image)(resources.GetObject("btnLoad.Image")));
            this.btnLoad.Location = new System.Drawing.Point(604, 24);
            this.btnLoad.Name = "btnLoad";
            this.btnLoad.Size = new System.Drawing.Size(29, 29);
            this.btnLoad.TabIndex = 4;
            this.btnLoad.UseVisualStyleBackColor = true;
            this.btnLoad.Click += new System.EventHandler(this.btnLoad_Click);
            // 
            // btnExport
            // 
            this.btnExport.Enabled = false;
            this.btnExport.Location = new System.Drawing.Point(12, 324);
            this.btnExport.Name = "btnExport";
            this.btnExport.Size = new System.Drawing.Size(621, 23);
            this.btnExport.TabIndex = 0;
            this.btnExport.Text = "Export Messages for Selected Message Group to HTML";
            this.btnExport.UseVisualStyleBackColor = true;
            this.btnExport.Click += new System.EventHandler(this.btnExport_Click);
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(645, 359);
            this.Controls.Add(this.panel1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.Name = "Form1";
            this.Text = "iPhone Message Exporter";
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion
        private System.Windows.Forms.ComboBox comboBackups;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.Button btnExport;
        private System.Windows.Forms.Button btnLoad;
        private System.Windows.Forms.Label labelBackupFiles;
        private System.Windows.Forms.ListBox lbMessageGroup;
        private System.Windows.Forms.Label labelMessageGroup;
    }
}

