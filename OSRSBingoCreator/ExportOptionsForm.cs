using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows.Forms;
using OSRSBingoCreator;
using OSRSBingoCreator.Models;

namespace OSRSBingoCreator
{
    // Form for exporting bingo board data to various formats.
    public partial class ExportOptionsForm : Form, IDisposable
    {
        private readonly BingoBoardState _boardState;

        public ExportOptionsForm(BingoBoardState boardState)
        {
            _boardState = boardState;
            InitializeComponent();

            if (boardState == null || boardState.Tiles == null)
            {
                MessageBox.Show("No board data provided to export.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                btnExportCsvOption.Enabled = false;
                btnExportExcelOption.Enabled = false;
                btnExportSheetsOption.Enabled = false;
            }
            else
            {
                btnExportExcelOption.Enabled = false;
                btnExportSheetsOption.Enabled = false;
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                components?.Dispose();
            }
            base.Dispose(disposing);
        }

        private void btnExportCsvOption_Click(object sender, EventArgs e)
        {
            if (this._boardState == null) return;

            ExportDataAsCsv(this._boardState);

            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private void btnCancelExport_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }

        private void ExportDataAsCsv(BingoBoardState state)
        {
            using (SaveFileDialog sfd = new SaveFileDialog())
            {
                sfd.Filter = "CSV (Comma delimited)|*.csv|All Files|*.*";
                sfd.Title = "Export Bingo Board to CSV";
                sfd.DefaultExt = "csv";
                sfd.AddExtension = true;
                sfd.FileName = "bingo_board_export.csv";

                if (sfd.ShowDialog(this) == DialogResult.OK)
                {
                    try
                    {
                        int boardSize = state.Tiles.Count;
                        StringBuilder csvContent = new StringBuilder();

                        csvContent.Append(",");
                        for (int c = 1; c <= boardSize; c++)
                        {
                            csvContent.Append(GetColumnLetter(c));
                            if (c < boardSize) csvContent.Append(",");
                        }
                        csvContent.AppendLine();

                        for (int r = 0; r < boardSize; r++)
                        {
                            csvContent.Append(r + 1);
                            csvContent.Append(",");

                            for (int c = 0; c < boardSize; c++)
                            {
                                BingoTile tile = state.Tiles[r][c];
                                string title = tile?.Title ?? "";
                                int points = tile?.Points ?? 0;
                                string cellText = EscapeCsvField($"{title} ({points} Pts)");
                                csvContent.Append(cellText);

                                if (c < boardSize - 1) csvContent.Append(",");
                            }
                            csvContent.AppendLine();
                        }

                        File.WriteAllText(sfd.FileName, csvContent.ToString());
                        MessageBox.Show(this, $"Board data exported successfully to:\n{sfd.FileName}", 
                            "Export Successful", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(this, $"Error exporting board to CSV:\n{ex.Message}", 
                            "Export Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private string EscapeCsvField(string data)
        {
            if (string.IsNullOrEmpty(data)) return "";

            if (data.Contains(",") || data.Contains("\"") || data.Contains("\r") || data.Contains("\n"))
            { 
                return $"\"{data.Replace("\"", "\"\"")}\""; 
            }
            
            return data;
        }

        private string GetColumnLetter(int colNumber)
        {
            if (colNumber < 1 || colNumber > 26) return "?";
            return ((char)('A' + colNumber - 1)).ToString();
        }
    }
}