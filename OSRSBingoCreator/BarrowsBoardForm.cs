using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Diagnostics;

public class BarrowsBoardForm : Form
{
    private const int ROWS = 4;
    private const int COLUMNS = 6;
    private const float X_THICKNESS = 4f;
    private static readonly Color X_COLOR = Color.Red;

    private static readonly string ImagesFolderPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Images");

    private readonly Dictionary<(int Row, int Column), string> _barrowsPieceImages = new Dictionary<(int, int), string>
    {
        { (0, 0), "Ahrim's_hood.png"  }, { (1, 0), "Ahrim's_robetop.png"    }, { (2, 0), "Ahrim's_robeskirt.png"   }, { (3, 0), "Ahrim's_staff.png"     },
        { (0, 1), "Dharok's_helm.png" }, { (1, 1), "Dharok's_platebody.png"  }, { (2, 1), "Dharok's_platelegs.png"   }, { (3, 1), "Dharok's_greataxe.png" },
        { (0, 2), "Guthan's_helm.png" }, { (1, 2), "Guthan's_platebody.png"  }, { (2, 2), "Guthan's_chainskirt.png"  }, { (3, 2), "Guthan's_warspear.png"    },
        { (0, 3), "Karil's_coif.png"  }, { (1, 3), "Karil's_leathertop.png"  }, { (2, 3), "Karil's_leatherskirt.png" }, { (3, 3), "Karil's_crossbow.png"  },
        { (0, 4), "Torag's_helm.png"  }, { (1, 4), "Torag's_platebody.png"   }, { (2, 4), "Torag's_platelegs.png"    }, { (3, 4), "Torag's_hammers.png"   },
        { (0, 5), "Verac's_helm.png"  }, { (1, 5), "Verac's_brassard.png"    }, { (2, 5), "Verac's_plateskirt.png"   }, { (3, 5), "Verac's_flail.png"     }
    };

    public event Action<int> ColumnCompleted;

    public event Action<bool, int> BarrowsTileCompletionChanged;

    public bool[][] CompletionState { get; set; } = new bool[ROWS][];

    public BarrowsBoardForm()
    {
        InitializeComponent();
        
        string imagesDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Images");
        if (!Directory.Exists(imagesDir))
        {
            Directory.CreateDirectory(imagesDir);
        }
        
        string placeholderPath = Path.Combine(imagesDir, "placeholder.png");
        if (!File.Exists(placeholderPath))
        {
            using (Bitmap placeholder = new Bitmap(64, 64))
            {
                using (Graphics g = Graphics.FromImage(placeholder))
                {
                    g.Clear(Color.LightGray);
                    using (Pen pen = new Pen(Color.Black, 1))
                    {
                        g.DrawRectangle(pen, 0, 0, 63, 63);
                        g.DrawLine(pen, 0, 0, 63, 63);
                        g.DrawLine(pen, 0, 63, 63, 0);
                    }
                }
                placeholder.Save(placeholderPath, System.Drawing.Imaging.ImageFormat.Png);
            }
        }
        
        this.Text = "Barrows Set Progress";
        this.Size = new Size(600, 400);
        this.FormBorderStyle = FormBorderStyle.FixedDialog;
        this.MaximizeBox = false;
        this.MinimizeBox = false;

        for (int i = 0; i < ROWS; i++)
        {
            CompletionState[i] = new bool[COLUMNS];
        }

        var tableLayoutPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = COLUMNS,
            RowCount = ROWS,
            AutoSize = false
        };

        for (int i = 0; i < COLUMNS; i++)
        {
            tableLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f / COLUMNS));
        }
        for (int i = 0; i < ROWS; i++)
        {
            tableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100f / ROWS));
        }

        for (int row = 0; row < ROWS; row++)
        {
            for (int col = 0; col < COLUMNS; col++)
            {
                var pictureBox = new PictureBox
                {
                    Dock = DockStyle.Fill,
                    SizeMode = PictureBoxSizeMode.Zoom,
                    BorderStyle = BorderStyle.FixedSingle,
                    Tag = new BarrowsPieceTag { Row = row, Column = col, IsCompleted = false },
                    Name = $"PictureBox_{row}_{col}"
                };

                if (_barrowsPieceImages.TryGetValue((row, col), out string imageName))
                {
                    string imagePath = Path.Combine(ImagesFolderPath, imageName);
                    if (File.Exists(imagePath))
                    {
                        pictureBox.Image = Image.FromFile(imagePath);
                    }
                    else
                    {
                        pictureBox.Image = Image.FromFile(Path.Combine(ImagesFolderPath, "placeholder.png"));
                    }
                }

                pictureBox.Click += PictureBox_Click;
                pictureBox.Paint += PictureBox_Paint;

                tableLayoutPanel.Controls.Add(pictureBox, col, row);
            }
        }

        this.Controls.Add(tableLayoutPanel);
    }

    private void PictureBox_Click(object sender, EventArgs e)
    {
        if (sender is PictureBox pb && pb.Tag is BarrowsPieceTag tag)
        {
            tag.IsCompleted = !tag.IsCompleted;
            CompletionState[tag.Row][tag.Column] = tag.IsCompleted;

            pb.Invalidate();

            int completedColumn = GetCompletedColumn();

            bool isAnyColumnComplete = completedColumn >= 0;
            BarrowsTileCompletionChanged?.Invoke(isAnyColumnComplete, completedColumn);
        }
    }

    private void UpdateBarrowsSetImage(int completedColumn)
    {
        var barrowsSetImages = new Dictionary<int, string>
        {
            { 0, "Ahrim's_set.png" },
            { 1, "Dharok's_set.png" },
            { 2, "Guthan's_set.png" },
            { 3, "Karil's_set.png" },
            { 4, "Torag's_set.png" },
            { 5, "Verac's_set.png" }
        };

        if (barrowsSetImages.TryGetValue(completedColumn, out string imageName))
        {
            string imagePath = Path.Combine(ImagesFolderPath, imageName);
            if (File.Exists(imagePath))
            {
                var tableLayoutPanel = this.Controls.OfType<TableLayoutPanel>().FirstOrDefault();
                if (tableLayoutPanel != null)
                {
                    for (int row = 0; row < ROWS; row++)
                    {
                        var pictureBox = tableLayoutPanel.GetControlFromPosition(completedColumn, row) as PictureBox;
                        if (pictureBox != null)
                        {
                            pictureBox.Image?.Dispose();
                            pictureBox.Image = Image.FromFile(imagePath);
                        }
                    }
                }
            }
        }
    }

    private void ResetColumnImages(int column)
    {
        var tableLayoutPanel = this.Controls.OfType<TableLayoutPanel>().FirstOrDefault();
        if (tableLayoutPanel != null)
        {
            for (int row = 0; row < ROWS; row++)
            {
                var pictureBox = tableLayoutPanel.GetControlFromPosition(column, row) as PictureBox;
                if (pictureBox != null && _barrowsPieceImages.TryGetValue((row, column), out string imageName))
                {
                    string imagePath = Path.Combine(ImagesFolderPath, imageName);
                    if (File.Exists(imagePath))
                    {
                        pictureBox.Image?.Dispose();
                        pictureBox.Image = Image.FromFile(imagePath);
                    }
                }
            }
        }
    }

    private bool IsColumnComplete(int column)
    {
        for (int row = 0; row < ROWS; row++)
        {
            if (!CompletionState[row][column])
            {
                return false;
            }
        }
        return true;
    }

    private int GetCompletedColumn()
    {
        for (int col = 0; col < COLUMNS; col++)
        {
            if (IsColumnComplete(col))
            {
                return col;
            }
        }
        return -1;
    }

    private void PictureBox_Paint(object sender, PaintEventArgs e)
    {
        if (sender is PictureBox pb && pb.Tag is BarrowsPieceTag tag && tag.IsCompleted)
        {
            using (var pen = new Pen(X_COLOR, X_THICKNESS))
            {
                e.Graphics.DrawLine(pen, 0, 0, pb.Width, pb.Height);
                e.Graphics.DrawLine(pen, 0, pb.Height, pb.Width, 0);
            }
        }
    }

    private void UpdateColumnCompletionState()
    {
        for (int col = 0; col < CompletionState[0].Length; col++)
        {
            Debug.WriteLine($"Column {col}: {string.Join(", ", CompletionState.Select(row => row[col]))}");
        }
    }

    private void ToggleTileCompletion(int row, int col)
    {
        CompletionState[row][col] = !CompletionState[row][col];
        Debug.WriteLine($"Toggled tile at ({row}, {col}). New state: {CompletionState[row][col]}");
        UpdateColumnCompletionState();
    }

    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);

        for (int row = 0; row < ROWS; row++)
        {
            for (int col = 0; col < COLUMNS; col++)
            {
                var pictureBox = this.Controls
                    .OfType<TableLayoutPanel>()
                    .FirstOrDefault()?
                    .GetControlFromPosition(col, row) as PictureBox;

                if (pictureBox?.Tag is BarrowsPieceTag tag)
                {
                    tag.IsCompleted = CompletionState[row][col];
                    pictureBox.Invalidate();
                }
            }
        }
    }

    private class BarrowsPieceTag
    {
        public int Row { get; set; }
        public int Column { get; set; }
        public bool IsCompleted { get; set; }
    }

    private void InitializeComponent()
    {
            this.SuspendLayout();
            this.ClientSize = new System.Drawing.Size(284, 261);
            this.Name = "BarrowsBoardForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.ResumeLayout(false);

    }
}
