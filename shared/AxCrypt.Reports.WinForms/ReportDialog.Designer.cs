namespace AxCrypt.Reports.WinForms
{
    partial class ReportDialog
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
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.toMonthPicker = new System.Windows.Forms.DateTimePicker();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.fromMonthPicker = new System.Windows.Forms.DateTimePicker();
            this.saveAsButton = new System.Windows.Forms.Button();
            this.groupBox2.SuspendLayout();
            this.groupBox1.SuspendLayout();
            this.SuspendLayout();
            // 
            // groupBox2
            // 
            this.groupBox2.Controls.Add(this.toMonthPicker);
            this.groupBox2.Location = new System.Drawing.Point(172, 12);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(147, 46);
            this.groupBox2.TabIndex = 6;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "To";
            // 
            // toMonthPicker
            // 
            this.toMonthPicker.CustomFormat = "MMMM yyyy";
            this.toMonthPicker.Format = System.Windows.Forms.DateTimePickerFormat.Custom;
            this.toMonthPicker.Location = new System.Drawing.Point(6, 19);
            this.toMonthPicker.Name = "toMonthPicker";
            this.toMonthPicker.ShowUpDown = true;
            this.toMonthPicker.Size = new System.Drawing.Size(128, 20);
            this.toMonthPicker.TabIndex = 2;
            this.toMonthPicker.ValueChanged += new System.EventHandler(this.toMonthPicker_ValueChanged);
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.fromMonthPicker);
            this.groupBox1.Location = new System.Drawing.Point(14, 11);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(147, 46);
            this.groupBox1.TabIndex = 5;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "From";
            // 
            // fromMonthPicker
            // 
            this.fromMonthPicker.CustomFormat = "MMMM yyyy";
            this.fromMonthPicker.Format = System.Windows.Forms.DateTimePickerFormat.Custom;
            this.fromMonthPicker.Location = new System.Drawing.Point(6, 19);
            this.fromMonthPicker.Name = "fromMonthPicker";
            this.fromMonthPicker.ShowUpDown = true;
            this.fromMonthPicker.Size = new System.Drawing.Size(128, 20);
            this.fromMonthPicker.TabIndex = 2;
            this.fromMonthPicker.ValueChanged += new System.EventHandler(this.fromMonthPicker_ValueChanged);
            // 
            // saveAsButton
            // 
            this.saveAsButton.Location = new System.Drawing.Point(325, 28);
            this.saveAsButton.Name = "saveAsButton";
            this.saveAsButton.Size = new System.Drawing.Size(75, 23);
            this.saveAsButton.TabIndex = 7;
            this.saveAsButton.Text = "Save As...";
            this.saveAsButton.UseVisualStyleBackColor = true;
            this.saveAsButton.Click += new System.EventHandler(this.saveAsButton_Click);
            // 
            // ReportDialog
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(411, 69);
            this.Controls.Add(this.saveAsButton);
            this.Controls.Add(this.groupBox2);
            this.Controls.Add(this.groupBox1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.Fixed3D;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "ReportDialog";
            this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Hide;
            this.Text = "Create Reports";
            this.Load += new System.EventHandler(this.ReportDialog_Load);
            this.groupBox2.ResumeLayout(false);
            this.groupBox1.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.DateTimePicker toMonthPicker;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.DateTimePicker fromMonthPicker;
        private System.Windows.Forms.Button saveAsButton;
    }
}