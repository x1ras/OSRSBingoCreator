using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Diagnostics;
using System.Net.Http;
using System.Threading.Tasks;

public class BarrowsBoardForm : Form
{
    private const int ROWS = 4;
    private const int COLUMNS = 6;
    private const float X_THICKNESS = 4f;
    private static readonly Color X_COLOR = Color.Red;

    private readonly Dictionary<(int Row, int Column), string> _barrowsPieceImageUrls = new Dictionary<(int, int), string>
    {
        { (0, 0), "https://oldschool.runescape.wiki/images/thumb/Ahrim%27s_hood_detail.png/800px-Ahrim%27s_hood_detail.png"  },
        { (1, 0), "https://oldschool.runescape.wiki/images/thumb/Ahrim%27s_robetop_detail.png/1280px-Ahrim%27s_robetop_detail.png" },
        { (2, 0), "https://oldschool.runescape.wiki/images/thumb/Ahrim%27s_robeskirt_detail.png/800px-Ahrim%27s_robeskirt_detail.png" },
        { (3, 0), "https://oldschool.runescape.wiki/images/thumb/Ahrim%27s_staff_detail.png/800px-Ahrim%27s_staff_detail.png" },

        { (0, 1), "https://oldschool.runescape.wiki/images/thumb/Dharok%27s_helm_detail.png/800px-Dharok%27s_helm_detail.png" },
        { (1, 1), "https://oldschool.runescape.wiki/images/thumb/Dharok%27s_platebody_detail.png/1024px-Dharok%27s_platebody_detail.png" },
        { (2, 1), "https://oldschool.runescape.wiki/images/Dharok%27s_platelegs_detail.png" },
        { (3, 1), "https://oldschool.runescape.wiki/images/thumb/Dharok%27s_greataxe_detail.png/1024px-Dharok%27s_greataxe_detail.png" },

        { (0, 2), "https://oldschool.runescape.wiki/images/thumb/Guthan%27s_helm_detail.png/1024px-Guthan%27s_helm_detail.png" },
        { (1, 2), "https://oldschool.runescape.wiki/images/thumb/Guthan%27s_platebody_detail.png/800px-Guthan%27s_platebody_detail.png" },
        { (2, 2), "https://oldschool.runescape.wiki/images/Guthan%27s_chainskirt_detail.png" },
        { (3, 2), "https://oldschool.runescape.wiki/images/thumb/Guthan%27s_warspear_detail.png/800px-Guthan%27s_warspear_detail.png" },

        { (0, 3), "https://oldschool.runescape.wiki/images/Karil%27s_coif_detail.png" },
        { (1, 3), "https://oldschool.runescape.wiki/images/thumb/Karil%27s_leathertop_detail.png/1280px-Karil%27s_leathertop_detail.png" },
        { (2, 3), "https://oldschool.runescape.wiki/images/thumb/Karil%27s_leatherskirt_detail.png/800px-Karil%27s_leatherskirt_detail.png" },
        { (3, 3), "https://oldschool.runescape.wiki/images/thumb/Karil%27s_crossbow_detail.png/1024px-Karil%27s_crossbow_detail.png" },

        { (0, 4), "https://oldschool.runescape.wiki/images/thumb/Torag%27s_helm_detail.png/1280px-Torag%27s_helm_detail.png" },
        { (1, 4), "https://oldschool.runescape.wiki/images/thumb/Torag%27s_platebody_detail.png/1024px-Torag%27s_platebody_detail.png" },
        { (2, 4), "https://oldschool.runescape.wiki/images/Torag%27s_platelegs_detail.png" },
        { (3, 4), "https://oldschool.runescape.wiki/images/thumb/Torag%27s_hammers_detail.png/1280px-Torag%27s_hammers_detail.png" },

        { (0, 5), "https://oldschool.runescape.wiki/images/thumb/Verac%27s_helm_detail.png/800px-Verac%27s_helm_detail.png" },
        { (1, 5), "https://oldschool.runescape.wiki/images/thumb/Verac%27s_brassard_detail.png/800px-Verac%27s_brassard_detail.png" },
        { (2, 5), "https://oldschool.runescape.wiki/images/thumb/Verac%27s_plateskirt_detail.png/1024px-Verac%27s_plateskirt_detail.png" },
        { (3, 5), "https://oldschool.runescape.wiki/images/thumb/Verac%27s_flail_detail.png/800px-Verac%27s_flail_detail.png" }
    };

    private readonly Dictionary<string, Image> _imageCache = new Dictionary<string, Image>();
    private readonly HttpClient _httpClient;

    public event Action<int> ColumnCompleted;
    public event Action<bool, int> BarrowsTileCompletionChanged;

    public bool[][] CompletionState { get; set; } = new bool[ROWS][];

    public BarrowsBoardForm()
    {
        InitializeComponent();

        _httpClient = new HttpClient();
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "OSRSBingoCreator/1.0 (https://github.com/x1ras/OSRSBingoCreator)");

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

                pictureBox.BackColor = Color.LightGray;

                if (_barrowsPieceImageUrls.TryGetValue((row, col), out string imageUrl))
                {
                    LoadImageAsync(pictureBox, imageUrl);
                }

                pictureBox.Click += PictureBox_Click;
                pictureBox.Paint += PictureBox_Paint;

                tableLayoutPanel.Controls.Add(pictureBox, col, row);
            }
        }

        this.Controls.Add(tableLayoutPanel);
    }

    private async void LoadImageAsync(PictureBox pictureBox, string url)
    {
        try
        {
            if (_imageCache.ContainsKey(url))
            {
                pictureBox.Image = _imageCache[url];
                return;
            }

            var image = await GetImageFromUrlAsync(url);
            if (image != null)
            {
                if (pictureBox.InvokeRequired)
                {
                    pictureBox.Invoke(new Action(() => {
                        pictureBox.Image = image;
                        pictureBox.BackColor = Color.Transparent;
                    }));
                }
                else
                {
                    pictureBox.Image = image;
                    pictureBox.BackColor = Color.Transparent;
                }

                _imageCache[url] = image;
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to load image from URL {url}: {ex.Message}");
        }
    }

    private async Task<Image> GetImageFromUrlAsync(string url)
    {
        try
        {
            var imageBytes = await _httpClient.GetByteArrayAsync(url);
            using (var ms = new MemoryStream(imageBytes))
            {
                return Image.FromStream(ms);
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error downloading image {url}: {ex.Message}");
            return null;
        }
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
        var barrowsSetImageUrls = new Dictionary<int, string>
        {
            { 0, "https://oldschool.runescape.wiki/images/Ahrim%27s_robes_equipped_male.png" },
            { 1, "https://oldschool.runescape.wiki/images/Dharok%27s_armour_equipped_male.png" },
            { 2, "https://oldschool.runescape.wiki/images/Guthan%27s_armour_equipped_male.png" },
            { 3, "https://oldschool.runescape.wiki/images/Karil%27s_armour_equipped_male.png" },
            { 4, "https://oldschool.runescape.wiki/images/Torag%27s_armour_equipped_male.png" },
            { 5, "https://oldschool.runescape.wiki/images/Verac%27s_armour_equipped_male.png" }
        };

        if (barrowsSetImageUrls.TryGetValue(completedColumn, out string imageUrl))
        {
            var tableLayoutPanel = this.Controls.OfType<TableLayoutPanel>().FirstOrDefault();
            if (tableLayoutPanel != null)
            {
                for (int row = 0; row < ROWS; row++)
                {
                    var pictureBox = tableLayoutPanel.GetControlFromPosition(completedColumn, row) as PictureBox;
                    if (pictureBox != null)
                    {
                        LoadImageAsync(pictureBox, imageUrl);
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
                if (pictureBox != null && _barrowsPieceImageUrls.TryGetValue((row, column), out string imageUrl))
                {
                    LoadImageAsync(pictureBox, imageUrl);
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

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            foreach (var image in _imageCache.Values)
            {
                image?.Dispose();
            }
            _imageCache.Clear();

            _httpClient?.Dispose();
        }
        base.Dispose(disposing);
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
