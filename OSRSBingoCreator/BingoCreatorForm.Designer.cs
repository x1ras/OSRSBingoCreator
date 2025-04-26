using System.Drawing;
using System.Windows.Forms;

namespace OsrsBingoCreator // Make sure this namespace matches your project name
{
    // Ensure 'partial' keyword is present
    partial class BingoCreatorForm
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
                if (this.tableLayoutPanelBoard != null)
                {
                    foreach (Control control in this.tableLayoutPanelBoard.Controls)
                    {
                        // Check panels in the main grid area (skip headers)
                        if (control is Panel cellPanel && cellPanel.Tag is Point)
                        {
                            foreach (Control innerControl in cellPanel.Controls)
                            {
                                if (innerControl is PictureBox pb)
                                {
                                    pb.Image?.Dispose(); // Safely dispose the image
                                }
                            }
                        }
                        // Added check for PictureBox directly in TLP (though we don't have any)
                        else if (control is PictureBox pbDirect)
                        {
                            pbDirect.Image?.Dispose();
                        }
                        // Added check for PictureBox inside FlowLayoutPanel (headers, no images expected currently)
                        else if (control is FlowLayoutPanel flp)
                        {
                            foreach (Control innerFlpControl in flp.Controls)
                            {
                                if (innerFlpControl is PictureBox pbInFlp)
                                {
                                    pbInFlp.Image?.Dispose();
                                }
                            }
                        }
                    }
                }
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
            this.components = new System.ComponentModel.Container();
            this.topPanel = new System.Windows.Forms.Panel();
            this.btnExport = new System.Windows.Forms.Button();
            this.btnLoadBoard = new System.Windows.Forms.Button();
            this.btnSaveBoard = new System.Windows.Forms.Button();
            this.btnClearBoard = new System.Windows.Forms.Button();
            this.btnCreateBoard = new System.Windows.Forms.Button();
            this.numBoardSize = new System.Windows.Forms.NumericUpDown();
            this.labelBoardSize = new System.Windows.Forms.Label();
            this.tableLayoutPanelBoard = new System.Windows.Forms.TableLayoutPanel();
            this.tileContextMenuStrip = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.setColourMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.topPanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numBoardSize)).BeginInit();
            this.tileContextMenuStrip.SuspendLayout();
            this.SuspendLayout();
            // 
            // topPanel
            // 
            this.topPanel.AutoSize = true;
            this.topPanel.Controls.Add(this.btnExport);
            this.topPanel.Controls.Add(this.btnLoadBoard);
            this.topPanel.Controls.Add(this.btnSaveBoard);
            this.topPanel.Controls.Add(this.btnClearBoard);
            this.topPanel.Controls.Add(this.btnCreateBoard);
            this.topPanel.Controls.Add(this.numBoardSize);
            this.topPanel.Controls.Add(this.labelBoardSize);
            this.topPanel.Dock = System.Windows.Forms.DockStyle.Top;
            this.topPanel.Location = new System.Drawing.Point(0, 0);
            this.topPanel.Name = "topPanel";
            this.topPanel.Size = new System.Drawing.Size(784, 49);
            this.topPanel.TabIndex = 0;
            // 
            // btnExport
            // 
            this.btnExport.Location = new System.Drawing.Point(567, 13);
            this.btnExport.Name = "btnExport";
            this.btnExport.Size = new System.Drawing.Size(89, 33);
            this.btnExport.TabIndex = 6;
            this.btnExport.Text = "Export";
            this.btnExport.UseVisualStyleBackColor = true;
            this.btnExport.Click += new System.EventHandler(this.btnExport_Click);
            // 
            // btnLoadBoard
            // 
            this.btnLoadBoard.Location = new System.Drawing.Point(472, 12);
            this.btnLoadBoard.Name = "btnLoadBoard";
            this.btnLoadBoard.Size = new System.Drawing.Size(89, 34);
            this.btnLoadBoard.TabIndex = 5;
            this.btnLoadBoard.Text = "Load";
            this.btnLoadBoard.UseVisualStyleBackColor = true;
            this.btnLoadBoard.Click += new System.EventHandler(this.btnLoadBoard_Click);
            // 
            // btnSaveBoard
            // 
            this.btnSaveBoard.Location = new System.Drawing.Point(377, 12);
            this.btnSaveBoard.Name = "btnSaveBoard";
            this.btnSaveBoard.Size = new System.Drawing.Size(89, 34);
            this.btnSaveBoard.TabIndex = 4;
            this.btnSaveBoard.Text = "Save";
            this.btnSaveBoard.UseVisualStyleBackColor = true;
            this.btnSaveBoard.Click += new System.EventHandler(this.btnSaveBoard_Click);
            // 
            // btnClearBoard
            // 
            this.btnClearBoard.Location = new System.Drawing.Point(246, 12);
            this.btnClearBoard.Name = "btnClearBoard";
            this.btnClearBoard.Size = new System.Drawing.Size(89, 34);
            this.btnClearBoard.TabIndex = 3;
            this.btnClearBoard.Text = "Clear";
            this.btnClearBoard.UseVisualStyleBackColor = true;
            this.btnClearBoard.Click += new System.EventHandler(this.btnClearBoard_Click);
            // 
            // btnCreateBoard
            // 
            this.btnCreateBoard.Location = new System.Drawing.Point(151, 12);
            this.btnCreateBoard.Name = "btnCreateBoard";
            this.btnCreateBoard.Size = new System.Drawing.Size(89, 34);
            this.btnCreateBoard.TabIndex = 2;
            this.btnCreateBoard.Text = "Create/Update Board";
            this.btnCreateBoard.UseVisualStyleBackColor = true;
            this.btnCreateBoard.Click += new System.EventHandler(this.btnCreateBoard_Click);
            // 
            // numBoardSize
            // 
            this.numBoardSize.Location = new System.Drawing.Point(100, 16);
            this.numBoardSize.Maximum = new decimal(new int[] {
            15,
            0,
            0,
            0});
            this.numBoardSize.Minimum = new decimal(new int[] {
            3,
            0,
            0,
            0});
            this.numBoardSize.Name = "numBoardSize";
            this.numBoardSize.Size = new System.Drawing.Size(36, 20);
            this.numBoardSize.TabIndex = 1;
            this.numBoardSize.Value = new decimal(new int[] {
            5,
            0,
            0,
            0});
            // 
            // labelBoardSize
            // 
            this.labelBoardSize.AutoSize = true;
            this.labelBoardSize.Location = new System.Drawing.Point(12, 18);
            this.labelBoardSize.Name = "labelBoardSize";
            this.labelBoardSize.Size = new System.Drawing.Size(91, 13);
            this.labelBoardSize.TabIndex = 0;
            this.labelBoardSize.Text = "Board Size (3-15):";
            // 
            // tableLayoutPanelBoard
            // 
            this.tableLayoutPanelBoard.ColumnCount = 1;
            this.tableLayoutPanelBoard.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanelBoard.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanelBoard.GrowStyle = System.Windows.Forms.TableLayoutPanelGrowStyle.FixedSize;
            this.tableLayoutPanelBoard.Location = new System.Drawing.Point(0, 49);
            this.tableLayoutPanelBoard.Name = "tableLayoutPanelBoard";
            this.tableLayoutPanelBoard.RowCount = 1;
            this.tableLayoutPanelBoard.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanelBoard.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 553F));
            this.tableLayoutPanelBoard.Size = new System.Drawing.Size(784, 553);
            this.tableLayoutPanelBoard.TabIndex = 1;
            // 
            // tileContextMenuStrip
            // 
            this.tileContextMenuStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.setColourMenuItem});
            this.tileContextMenuStrip.Name = "tileContextMenuStrip";
            this.tileContextMenuStrip.Size = new System.Drawing.Size(197, 26);
            // 
            // setColourMenuItem
            // 
            this.setColourMenuItem.Name = "setColourMenuItem";
            this.setColourMenuItem.Size = new System.Drawing.Size(196, 22);
            this.setColourMenuItem.Text = "Set Background Colour";
            this.setColourMenuItem.Click += new System.EventHandler(this.setColourMenuItem_Click);
            // 
            // BingoCreatorForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoSize = true;
            this.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.ClientSize = new System.Drawing.Size(784, 602);
            this.Controls.Add(this.tableLayoutPanelBoard);
            this.Controls.Add(this.topPanel);
            this.MinimumSize = new System.Drawing.Size(700, 200);
            this.Name = "BingoCreatorForm";
            this.Text = "OSRS Bingo Board Creator";
            this.Load += new System.EventHandler(this.BingoCreatorForm_Load);
            this.topPanel.ResumeLayout(false);
            this.topPanel.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numBoardSize)).EndInit();
            this.tileContextMenuStrip.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Panel topPanel;
        private System.Windows.Forms.NumericUpDown numBoardSize;
        private System.Windows.Forms.Label labelBoardSize;
        private System.Windows.Forms.Button btnClearBoard;
        private System.Windows.Forms.Button btnCreateBoard;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanelBoard;
        private System.Windows.Forms.Button btnSaveBoard;
        private System.Windows.Forms.Button btnLoadBoard;
        private System.Windows.Forms.ContextMenuStrip tileContextMenuStrip;
        private System.Windows.Forms.ToolStripMenuItem setColourMenuItem;
        private Button btnExport;
    }
}