namespace OSRSBingoCreator
{
    partial class ExportOptionsForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;


        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.label1 = new System.Windows.Forms.Label();
            this.btnExportCsvOption = new System.Windows.Forms.Button();
            this.btnExportExcelOption = new System.Windows.Forms.Button();
            this.btnExportSheetsOption = new System.Windows.Forms.Button();
            this.btnCancelExport = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(9, 6);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(105, 13);
            this.label1.TabIndex = 0;
            this.label1.Text = "Select Export Format";
            this.label1.TextAlign = System.Drawing.ContentAlignment.TopCenter;
            // 
            // btnExportCsvOption
            // 
            this.btnExportCsvOption.Location = new System.Drawing.Point(12, 40);
            this.btnExportCsvOption.Name = "btnExportCsvOption";
            this.btnExportCsvOption.Size = new System.Drawing.Size(102, 23);
            this.btnExportCsvOption.TabIndex = 1;
            this.btnExportCsvOption.Text = "CSV";
            this.btnExportCsvOption.UseVisualStyleBackColor = true;
            this.btnExportCsvOption.Click += new System.EventHandler(this.btnExportCsvOption_Click);
            // 
            // btnExportExcelOption
            // 
            this.btnExportExcelOption.Location = new System.Drawing.Point(12, 69);
            this.btnExportExcelOption.Name = "btnExportExcelOption";
            this.btnExportExcelOption.Size = new System.Drawing.Size(102, 23);
            this.btnExportExcelOption.TabIndex = 2;
            this.btnExportExcelOption.Text = "Excel (.xlsx)";
            this.btnExportExcelOption.UseVisualStyleBackColor = true;
            // 
            // btnExportSheetsOption
            // 
            this.btnExportSheetsOption.Location = new System.Drawing.Point(12, 98);
            this.btnExportSheetsOption.Name = "btnExportSheetsOption";
            this.btnExportSheetsOption.Size = new System.Drawing.Size(102, 23);
            this.btnExportSheetsOption.TabIndex = 3;
            this.btnExportSheetsOption.Text = "Google Sheets";
            this.btnExportSheetsOption.UseVisualStyleBackColor = true;
            // 
            // btnCancelExport
            // 
            this.btnCancelExport.Location = new System.Drawing.Point(12, 147);
            this.btnCancelExport.Name = "btnCancelExport";
            this.btnCancelExport.Size = new System.Drawing.Size(102, 23);
            this.btnCancelExport.TabIndex = 4;
            this.btnCancelExport.Text = "Cancel";
            this.btnCancelExport.UseVisualStyleBackColor = true;
            this.btnCancelExport.Click += new System.EventHandler(this.btnCancelExport_Click);
            // 
            // ExportOptionsForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(128, 179);
            this.Controls.Add(this.btnCancelExport);
            this.Controls.Add(this.btnExportSheetsOption);
            this.Controls.Add(this.btnExportExcelOption);
            this.Controls.Add(this.btnExportCsvOption);
            this.Controls.Add(this.label1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "ExportOptionsForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Button btnExportCsvOption;
        private System.Windows.Forms.Button btnExportExcelOption;
        private System.Windows.Forms.Button btnExportSheetsOption;
        private System.Windows.Forms.Button btnCancelExport;
    }
}