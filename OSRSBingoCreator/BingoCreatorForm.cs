using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Text.Json;
using System.Text;
using System.Windows.Forms;
using System.Linq;
using System.Threading.Tasks;
using System.Diagnostics;
using OSRSBingoCreator;
using OSRSBingoCreator.Models;

namespace OsrsBingoCreator
{
    public partial class BingoCreatorForm : Form
    {
        private const int MIN_WINDOW_WIDTH = 650;
        private const float HEADER_SIZE = 45F;
        private const float TILE_SIZE = 120F;
        private const int NUD_WIDTH = 35;
        private const int WINDOW_PADDING = 40;
        private const string TITLE_PLACEHOLDER = "Enter Title";
        private const string POINTS_PLACEHOLDER = "0";
        private const int MAX_CACHE_SIZE_MB = 100;
        private const string CACHE_FOLDER_NAME = "WikiImageCache";
        private const int LAYOUT_UPDATE_DELAY = 100;
        private const string TOTAL_PREFIX = "Total: ";
        private const float X_THICKNESS = 4f;
        private static readonly Color X_COLOR = Color.Red;
        private static readonly Color X_COLOR_COMPLETE = Color.Gold;
        private static readonly Color X_COLOR_NORMAL = Color.Red;
        private static readonly string[] SUPPORTED_IMAGE_EXTENSIONS = { ".jpg", ".jpeg", ".png", ".bmp" };
        private static readonly string CACHE_PATH = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "OsrsBingoCreator",
            CACHE_FOLDER_NAME
        );

        private const int BarrowsTileColumn = 0;
        private const int BarrowsTileRow = 0;

        private static readonly int[] CustomColors = new int[16];

        private readonly System.Windows.Forms.Timer _layoutUpdateTimer;
        private readonly Queue<Func<Task>> _uiOperationQueue = new Queue<Func<Task>>();
        private bool _isProcessingQueue;
        private BingoTile[,] currentBoardData;
        private int[] rowBonuses;
        private int[] columnBonuses;
        private readonly ImageCache _imageCache = new ImageCache();
        private BingoBoardState boardState;

        public BingoCreatorForm()
        {
            _layoutUpdateTimer = new System.Windows.Forms.Timer
            {
                Interval = LAYOUT_UPDATE_DELAY,
                Enabled = false
            };
            _layoutUpdateTimer.Tick += HandleDelayedLayoutUpdate;

            InitializeComponent();
            
            this.FormBorderStyle = FormBorderStyle.Sizable;
            this.AutoScroll = true;
            this.AutoSize = false;
            this.MaximizeBox = true;

            topPanel.Dock = DockStyle.Top;
            topPanel.Height = 80;
            topPanel.MinimumSize = new Size(0, 80);
            
            tableLayoutPanelBoard.AutoSize = true;
            tableLayoutPanelBoard.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            tableLayoutPanelBoard.Dock = DockStyle.None;
            tableLayoutPanelBoard.Padding = new Padding(WINDOW_PADDING);
            tableLayoutPanelBoard.Location = new Point(WINDOW_PADDING, topPanel.Height + 10);
        }


        private void btnCreateBoard_Click(object sender, EventArgs e)
        {
            int newSize = (int)numBoardSize.Value;
            
            if (currentBoardData == null)
            {
                CreateBoardGrid();
            }
            else
            {
                UpdateBoardState(newSize);
            }
        }

        private void btnClearBoard_Click(object sender, EventArgs e)
        {
            ClearBoardInputs();
        }

        private void btnSaveBoard_Click(object sender, EventArgs e)
        {
            if (currentBoardData == null || rowBonuses == null || columnBonuses == null)
            {
                MessageBox.Show("Please create a board before saving.", "No Board", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            using (SaveFileDialog sfd = new SaveFileDialog())
            {
                sfd.Filter = "Bingo Board JSON|*.bingo.json|All Files|*.*";
                sfd.Title = "Save Bingo Board";
                sfd.DefaultExt = "bingo.json";
                sfd.AddExtension = true;

                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        BingoBoardState stateToSave = GetCurrentBoardState();

                        var options = new JsonSerializerOptions { WriteIndented = true };
                        string jsonString = JsonSerializer.Serialize(stateToSave, options);

                        File.WriteAllText(sfd.FileName, jsonString);
                        MessageBox.Show($"Board saved successfully to:\n{sfd.FileName}", "Save Successful", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error saving board:\n{ex.Message}", "Save Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void btnLoadBoard_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog ofd = new OpenFileDialog())
            {
                ofd.Filter = "Bingo Board JSON|*.bingo.json|All Files|*.*";
                ofd.Title = "Load Bingo Board";
                ofd.CheckFileExists = true;
                ofd.CheckPathExists = true;

                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        string jsonString = File.ReadAllText(ofd.FileName);

                        BingoBoardState loadedState = JsonSerializer.Deserialize<BingoBoardState>(jsonString);

                        if (!ValidateBoardState(loadedState))
                        {
                            MessageBox.Show("Failed to load valid board data from file (invalid structure or mismatched sizes).", "Load Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            return;
                        }

                        int rows = loadedState.Rows;
                        int cols = loadedState.Columns;
                        BingoTile[,] loadedTileData = new BingoTile[rows, cols];
                        for (int r = 0; r < rows; r++)
                        {
                            for (int c = 0; c < cols; c++)
                            {
                                loadedTileData[r, c] = loadedState.Tiles[r][c];
                            }
                        }

                        currentBoardData = loadedTileData;
                        rowBonuses = loadedState.RowBonuses;
                        columnBonuses = loadedState.ColumnBonuses;

                        CreateBoardGrid();
                        PopulateUIFromData();
                        UpdateAllTotals();

                        MessageBox.Show($"Board loaded successfully from:\n{ofd.FileName}", "Load Successful", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch (JsonException jsonEx)
                    {
                        MessageBox.Show($"Error reading board data (invalid JSON format or structure):\n{jsonEx.Message}", "Load Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error loading board file:\n{ex.Message}", "Load Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private async void BingoCreatorForm_Load(object sender, EventArgs e)
        {
            try
            {
                await Task.Run(() => CleanCacheIfNeeded());
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error initiating cache cleanup: {ex.Message}");
            }
        }

        private async Task CleanCacheIfNeeded()
        {
            if (!Directory.Exists(CACHE_PATH)) return;

            try
            {
                long maxSizeBytes = MAX_CACHE_SIZE_MB * 1024L * 1024L;
                long currentSize = await GetDirectorySizeAsync(CACHE_PATH);

                if (currentSize > maxSizeBytes)
                {
                    var files = Directory.GetFiles(CACHE_PATH)
                        .Select(f => new FileInfo(f))
                        .OrderBy(f => f.LastAccessTimeUtc)
                        .ToList();

                    long bytesToDelete = currentSize - maxSizeBytes;
                    long bytesDeleted = 0;

                    foreach (var file in files)
                    {
                        if (bytesDeleted >= bytesToDelete) break;

                        try
                        {
                            long size = file.Length;
                            file.Delete();
                            bytesDeleted += size;
                            
                            Console.WriteLine($"Deleted cached file: {file.Name}");
                        }
                        catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
                        {
                            Console.WriteLine($"Error deleting cache file {file.Name}: {ex.Message}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during cache cleanup: {ex.Message}");
            }
        }

        private async Task<long> GetDirectorySizeAsync(string path)
        {
            return await Task.Run(() =>
            {
                return new DirectoryInfo(path)
                    .GetFiles("*", SearchOption.AllDirectories)
                    .Sum(file => file.Length);
            });
        }

        private async Task EnqueueUIOperation(Func<Task> operation)
        {
            _uiOperationQueue.Enqueue(operation);
            if (!_isProcessingQueue)
            {
                await ProcessUIOperationQueue();
            }
        }

        private async Task ProcessUIOperationQueue()
        {
            if (_isProcessingQueue) return;
            
            _isProcessingQueue = true;
            try
            {
                while (_uiOperationQueue.Count > 0)
                {
                    var operation = _uiOperationQueue.Dequeue();
                    await operation();
                }
            }
            finally
            {
                _isProcessingQueue = false;
            }
        }

        private string GetColumnLetter(int colNumber)
        {
            if (colNumber < 1 || colNumber > 26) { return "?"; }
            return ((char)('A' + colNumber - 1)).ToString();
        }

        private void CreateBoardGrid()
        {
            Point currentScroll = this.AutoScrollPosition;
            
            tableLayoutPanelBoard.SuspendLayout();

            int boardSize = currentBoardData?.GetLength(0) ?? (int)numBoardSize.Value;

            if (currentBoardData == null)
            {
                InitializeBoardArrays(boardSize);
            }

            tableLayoutPanelBoard.Controls.Clear();
            tableLayoutPanelBoard.RowStyles.Clear();
            tableLayoutPanelBoard.ColumnStyles.Clear();

            tableLayoutPanelBoard.RowCount = boardSize + 2;
            tableLayoutPanelBoard.ColumnCount = boardSize + 2;

            float headerAbsSize = HEADER_SIZE;
            float tileSize = TILE_SIZE;

            tableLayoutPanelBoard.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, headerAbsSize + 5));
            tableLayoutPanelBoard.RowStyles.Add(new RowStyle(SizeType.Absolute, headerAbsSize));

            for (int i = 0; i < boardSize; i++)
            {
                tableLayoutPanelBoard.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, tileSize));
            }
            for (int i = 0; i < boardSize; i++)
            {
                tableLayoutPanelBoard.RowStyles.Add(new RowStyle(SizeType.Absolute, tileSize));
            }

            tableLayoutPanelBoard.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, headerAbsSize + 5));

            tableLayoutPanelBoard.RowStyles.Add(new RowStyle(SizeType.Absolute, headerAbsSize));

            Font headerFont = new Font(this.Font, FontStyle.Bold);

            for (int col = 1; col <= boardSize; col++)
            {
                FlowLayoutPanel headerPanel = new FlowLayoutPanel
                {
                    Dock = DockStyle.Fill,
                    FlowDirection = FlowDirection.LeftToRight,
                    WrapContents = false
                };

                Label colLabel = new Label
                {
                    Text = GetColumnLetter(col),
                    Font = headerFont,
                    AutoSize = true,
                    TextAlign = ContentAlignment.MiddleCenter,
                    Margin = new Padding(0, 4, 2, 0)
                };

                NumericUpDown colBonus = new NumericUpDown
                {
                    Name = $"nudColBonus_{col - 1}",
                    Width = NUD_WIDTH,
                    Minimum = 0,
                    Maximum = 999,
                    Value = columnBonuses?[col - 1] ?? 0,
                    Tag = col - 1
                };
                colBonus.ValueChanged += BonusNud_ValueChanged;

                headerPanel.Controls.Add(colLabel);
                headerPanel.Controls.Add(colBonus);
                tableLayoutPanelBoard.Controls.Add(headerPanel, col, 0);
            }

            for (int row = 1; row <= boardSize; row++)
            {
                FlowLayoutPanel headerPanel = new FlowLayoutPanel
                {
                    Dock = DockStyle.Fill,
                    FlowDirection = FlowDirection.TopDown,
                    WrapContents = false,
                    AutoSize = true,
                    Margin = new Padding(0)
                };

                Label rowLabel = new Label
                {
                    Text = row.ToString(),
                    Font = headerFont,
                    AutoSize = true,
                    TextAlign = ContentAlignment.MiddleCenter,
                    Margin = new Padding(0, 4, 0, 2)
                };

                NumericUpDown rowBonus = new NumericUpDown
                {
                    Name = $"nudRowBonus_{row - 1}",
                    Width = NUD_WIDTH,
                    Minimum = 0,
                    Maximum = 999,
                    Value = rowBonuses?[row - 1] ?? 0,
                    Tag = row - 1
                };
                rowBonus.ValueChanged += BonusNud_ValueChanged;

                headerPanel.Controls.Add(rowLabel);
                headerPanel.Controls.Add(rowBonus);
                tableLayoutPanelBoard.Controls.Add(headerPanel, 0, row);
            }

            for (int row = 1; row <= boardSize; row++)
            {
                Label rowTotalLabel = new Label
                {
                    Name = $"lblRowTotal_{row - 1}",
                    Text = "0",
                    Dock = DockStyle.Fill,
                    TextAlign = ContentAlignment.MiddleCenter,
                    AutoSize = true,
                    Font = new Font(this.Font, FontStyle.Bold)
                };
                tableLayoutPanelBoard.Controls.Add(rowTotalLabel, boardSize + 1, row);
            }

            for (int col = 1; col <= boardSize; col++)
            {
                Label colTotalLabel = new Label
                {
                    Name = $"lblColTotal_{col - 1}",
                    Text = "0",
                    Dock = DockStyle.Fill,
                    TextAlign = ContentAlignment.MiddleCenter,
                    AutoSize = true,
                    Font = new Font(this.Font, FontStyle.Bold)
                };
                tableLayoutPanelBoard.Controls.Add(colTotalLabel, col, boardSize + 1);
            }

            Label rowTotalsHeader = new Label
            {
                Text = "",
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                AutoSize = true,
                Font = new Font(this.Font, FontStyle.Bold)
            };
            tableLayoutPanelBoard.Controls.Add(rowTotalsHeader, boardSize + 1, 0);

            Label colTotalsHeader = new Label
            {
                Text = "",
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                AutoSize = true,
                Font = new Font(this.Font, FontStyle.Bold)
            };
            tableLayoutPanelBoard.Controls.Add(colTotalsHeader, 0, boardSize + 1);

            for (int row = 1; row <= boardSize; row++)
            {
                for (int col = 1; col <= boardSize; col++)
                {
                    Panel tilePanel = CreateTilePanel(row, col);

                    tableLayoutPanelBoard.Controls.Add(tilePanel, col, row);
                }
            }

            tableLayoutPanelBoard.ResumeLayout(true);
            UpdateFormSize(boardSize);
            
            this.AutoScrollPosition = new Point(-currentScroll.X, -currentScroll.Y);
            
            this.PerformLayout();
            this.Refresh();
        }

        private void UpdateFormSize(int boardSize)
        {
            Point currentScroll = this.AutoScrollPosition;
            
            Rectangle workingArea = Screen.FromControl(this).WorkingArea;
            
            int requiredWidth = (int)(
                WINDOW_PADDING * 2 +
                (HEADER_SIZE + 5) +
                (boardSize * TILE_SIZE) +
                (HEADER_SIZE + 5)
            );
            
            int requiredHeight = (int)(
                topPanel.Height + 10 +
                WINDOW_PADDING +
                HEADER_SIZE +
                (boardSize * TILE_SIZE) +
                HEADER_SIZE +
                WINDOW_PADDING
            );
            
            requiredWidth = Math.Max(MIN_WINDOW_WIDTH, requiredWidth);
            
            int formWidth = Math.Min(requiredWidth + 40, workingArea.Width - 20);
            int formHeight = Math.Min(requiredHeight + 40, workingArea.Height - 20);
            
            this.MinimumSize = new Size(
                MIN_WINDOW_WIDTH,
                topPanel.Height + 100
            );

            this.Size = new Size(formWidth, formHeight);
            
            tableLayoutPanelBoard.Location = new Point(
                WINDOW_PADDING,
                topPanel.Height + 10
            );
            
            for (int i = 0; i < tableLayoutPanelBoard.ColumnStyles.Count; i++)
            {
                tableLayoutPanelBoard.ColumnStyles[i].SizeType = SizeType.Absolute;
                if (i == 0)
                    tableLayoutPanelBoard.ColumnStyles[i].Width = HEADER_SIZE + 5;
                else if (i == tableLayoutPanelBoard.ColumnStyles.Count - 1)
                    tableLayoutPanelBoard.ColumnStyles[i].Width = HEADER_SIZE + 5;
                else
                    tableLayoutPanelBoard.ColumnStyles[i].Width = TILE_SIZE;
            }

            for (int i = 0; i < tableLayoutPanelBoard.RowStyles.Count; i++)
            {
                tableLayoutPanelBoard.RowStyles[i].SizeType = SizeType.Absolute;
                tableLayoutPanelBoard.RowStyles[i].Height = i == 0 ? HEADER_SIZE : 
                    (i == tableLayoutPanelBoard.RowStyles.Count - 1 ? HEADER_SIZE : TILE_SIZE);
            }

            this.PerformLayout();
            CenterBoardInForm();
            
            this.AutoScrollPosition = new Point(-currentScroll.X, -currentScroll.Y);
        }

        private void CenterBoardInForm()
        {
            if (tableLayoutPanelBoard.Width < this.ClientSize.Width)
            {
                tableLayoutPanelBoard.Left = (this.ClientSize.Width - tableLayoutPanelBoard.Width) / 2;
            }
            else
            {
                tableLayoutPanelBoard.Left = WINDOW_PADDING;
            }
            
            tableLayoutPanelBoard.Top = topPanel.Height + 10;
        }

        private void InitializeBoardArrays(int size)
        {
            currentBoardData = new BingoTile[size, size];
            rowBonuses = new int[size];
            columnBonuses = new int[size];

            for (int r = 0; r < size; r++)
            {
                for (int c = 0; c < size; c++)
                {
                    currentBoardData[r, c] = new OSRSBingoCreator.Models.BingoTile(r, c);
                }
            }
        }

        private void PopulateUIFromData()
        {
            if (currentBoardData == null) return;

            int rows = currentBoardData.GetLength(0);
            int cols = currentBoardData.GetLength(1);

            foreach (Control control in tableLayoutPanelBoard.Controls)
            {
                if (control is Panel cellPanel && cellPanel.Tag is Point coordinates)
                {
                    var tile = GetTileAt(coordinates);
                    if (tile != null)
                    {
                        UpdateTilePanel(cellPanel, coordinates);
                    }
                }
            }
        }

        private void ClearTilePanel(Panel panel)
        {
            foreach (Control control in panel.Controls)
            {
                if (control is TextBox tb)
                {
                    string placeholder = tb.Tag as string;
                    if (!string.IsNullOrEmpty(placeholder))
                    {
                        HandleTextInput(tb, "", true);
                    }
                    else 
                    {
                        tb.Clear();
                        tb.ForeColor = SystemColors.WindowText;
                    }
                }
                else if (control is PictureBox pb)
                {
                    pb.Image?.Dispose();
                    pb.Image = null;
                    pb.Tag = null;
                    pb.BackColor = Color.Transparent;
                }
                else if (control is NumericUpDown nud && nud.Name.StartsWith("nudPoints_"))
                {
                    nud.Value = 0;
                }
            }
        }

        private void ResetBonusControls()
        {
            foreach (Control control in tableLayoutPanelBoard.Controls)
            {
                if (control is FlowLayoutPanel flowPanel)
                {
                    foreach (NumericUpDown nud in flowPanel.Controls.OfType<NumericUpDown>())
                    {
                        nud.Value = 0;
                    }
                }
            }
        }

        private void ClearBoardInputs()
        {
            if (currentBoardData == null) return;

            foreach (Control control in tableLayoutPanelBoard.Controls)
            {
                if (control is Panel panel && panel.Tag is Point)
                {
                    ClearTilePanel(panel);
                }
            }

            ResetBonusControls();

            UpdateAllTotals();
        }

        private void UpdateBoardState(int newBoardSize)
        {
            if (currentBoardData != null && newBoardSize < currentBoardData.GetLength(0))
            {
                bool hasDataInRemovingTiles = false;
                for (int r = 0; r < currentBoardData.GetLength(0); r++)
                {
                    for (int c = 0; c < currentBoardData.GetLength(1); c++)
                    {
                        if (r >= newBoardSize || c >= newBoardSize)
                        {
                            var tile = currentBoardData[r, c];
                            if (!string.IsNullOrEmpty(tile.Title) || 
                                tile.Points != 0 || 
                                !string.IsNullOrEmpty(tile.ImagePath) ||
                                tile.BackgroundColourArgb != Color.Transparent.ToArgb())
                            {
                                hasDataInRemovingTiles = true;
                                break;
                            }
                        }
                    }
                    if (hasDataInRemovingTiles) break;
                }

                if (hasDataInRemovingTiles)
                {
                    var result = MessageBox.Show(
                        "Reducing the board size will delete tiles containing data. Do you want to continue?",
                        "Warning - Data Loss",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Warning);

                    if (result == DialogResult.No)
                    {
                        numBoardSize.Value = currentBoardData.GetLength(0);
                        return;
                    }
                }
            }

            var oldBoardData = currentBoardData;
            var oldRowBonuses = rowBonuses?.ToArray();
            var oldColumnBonuses = columnBonuses?.ToArray();

            tableLayoutPanelBoard.SuspendLayout();
            try
            {
                var newBoardData = new BingoTile[newBoardSize, newBoardSize];
                var newRowBonuses = new int[newBoardSize];
                var newColumnBonuses = new int[newBoardSize];

                if (oldBoardData != null)
                {
                    int minRows = Math.Min(newBoardSize, oldBoardData.GetLength(0));
                    int minCols = Math.Min(newBoardSize, oldBoardData.GetLength(1));

                    for (int r = 0; r < newBoardSize; r++)
                    {
                        for (int c = 0; c < newBoardSize; c++)
                        {
                            if (r < minRows && c < minCols)
                            {
                                newBoardData[r, c] = oldBoardData[r, c].Clone();
                            }
                            else
                            {
                                newBoardData[r, c] = new BingoTile(r, c);
                            }
                        }
                    }

                    if (oldRowBonuses != null)
                    {
                        Array.Copy(oldRowBonuses, newRowBonuses, Math.Min(newBoardSize, oldRowBonuses.Length));
                    }
                    if (oldColumnBonuses != null)
                    {
                        Array.Copy(oldColumnBonuses, newColumnBonuses, Math.Min(newBoardSize, oldColumnBonuses.Length));
                    }
                }

                tableLayoutPanelBoard.Controls.Clear();
                tableLayoutPanelBoard.RowStyles.Clear();
                tableLayoutPanelBoard.ColumnStyles.Clear();

                tableLayoutPanelBoard.RowCount = newBoardSize + 2;
                tableLayoutPanelBoard.ColumnCount = newBoardSize + 2;

                tableLayoutPanelBoard.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, HEADER_SIZE + 5));
                tableLayoutPanelBoard.RowStyles.Add(new RowStyle(SizeType.Absolute, HEADER_SIZE));

                for (int i = 0; i < newBoardSize; i++)
                {
                    tableLayoutPanelBoard.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, TILE_SIZE));
                }
                for (int i = 0; i < newBoardSize; i++)
                {
                    tableLayoutPanelBoard.RowStyles.Add(new RowStyle(SizeType.Absolute, TILE_SIZE));
                }

                tableLayoutPanelBoard.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, HEADER_SIZE + 5));

                tableLayoutPanelBoard.RowStyles.Add(new RowStyle(SizeType.Absolute, HEADER_SIZE));
                 
                Font headerFont = new Font(this.Font, FontStyle.Bold);
                
                for (int col = 1; col <= newBoardSize; col++)
                {
                    FlowLayoutPanel headerPanel = new FlowLayoutPanel
                    {
                        Dock = DockStyle.Fill,
                        FlowDirection = FlowDirection.LeftToRight,
                        WrapContents = false
                    };

                    Label colLabel = new Label
                    {
                        Text = GetColumnLetter(col),
                        Font = headerFont,
                        AutoSize = true,
                        TextAlign = ContentAlignment.MiddleCenter,
                        Margin = new Padding(0, 4, 2, 0)
                    };

                    NumericUpDown colBonus = new NumericUpDown
                    {
                        Name = $"nudColBonus_{col - 1}",
                        Width = NUD_WIDTH,
                        Minimum = 0,
                        Maximum = 999,
                        Value = newColumnBonuses[col - 1],
                        Tag = col - 1
                    };
                    colBonus.ValueChanged += BonusNud_ValueChanged;

                    headerPanel.Controls.Add(colLabel);
                    headerPanel.Controls.Add(colBonus);
                    tableLayoutPanelBoard.Controls.Add(headerPanel, col, 0);
                }

                for (int row = 1; row <= newBoardSize; row++)
                {
                    FlowLayoutPanel headerPanel = new FlowLayoutPanel
                    {
                        Dock = DockStyle.Fill,
                        FlowDirection = FlowDirection.TopDown,
                        WrapContents = false,
                        AutoSize = true,
                        Margin = new Padding(0)
                    };

                    Label rowLabel = new Label
                    {
                        Text = row.ToString(),
                        Font = headerFont,
                        AutoSize = true,
                        TextAlign = ContentAlignment.MiddleCenter,
                        Margin = new Padding(0, 4, 0, 2)
                    };

                    NumericUpDown rowBonus = new NumericUpDown
                    {
                        Name = $"nudRowBonus_{row - 1}",
                        Width = NUD_WIDTH,
                        Minimum = 0,
                        Maximum = 999,
                        Value = newRowBonuses[row - 1],
                        Tag = row - 1
                    };
                    rowBonus.ValueChanged += BonusNud_ValueChanged;

                    headerPanel.Controls.Add(rowLabel);
                    headerPanel.Controls.Add(rowBonus);
                    tableLayoutPanelBoard.Controls.Add(headerPanel, 0, row);
                }

                for (int row = 1; row <= newBoardSize; row++)
                {
                    Label rowTotalLabel = new Label
                    {
                        Name = $"lblRowTotal_{row - 1}",
                        Text = "0",
                        Dock = DockStyle.Fill,
                        TextAlign = ContentAlignment.MiddleCenter,
                        AutoSize = true,
                        Font = new Font(this.Font, FontStyle.Bold)
                    };
                    tableLayoutPanelBoard.Controls.Add(rowTotalLabel, newBoardSize + 1, row);
                }

                for (int col = 1; col <= newBoardSize; col++)
                {
                    Label colTotalLabel = new Label
                    {
                        Name = $"lblColTotal_{col - 1}",
                        Text = "0",
                        Dock = DockStyle.Fill,
                        TextAlign = ContentAlignment.MiddleCenter,
                        AutoSize = true,
                        Font = new Font(this.Font, FontStyle.Bold)
                    };
                    tableLayoutPanelBoard.Controls.Add(colTotalLabel, col, newBoardSize + 1);
                }

                Label rowTotalsHeader = new Label
                {
                    Text = "",
                    Dock = DockStyle.Fill,
                    TextAlign = ContentAlignment.MiddleCenter,
                    AutoSize = true,
                    Font = new Font(this.Font, FontStyle.Bold)
                };
                tableLayoutPanelBoard.Controls.Add(rowTotalsHeader, newBoardSize + 1, 0);

                Label colTotalsHeader = new Label
                {
                    Text = "",
                    Dock = DockStyle.Fill,
                    TextAlign = ContentAlignment.MiddleCenter,
                    AutoSize = true,
                    Font = new Font(this.Font, FontStyle.Bold)
                };
                tableLayoutPanelBoard.Controls.Add(colTotalsHeader, 0, newBoardSize + 1);

                for (int row = 1; row <= newBoardSize; row++)
                {
                    for (int col = 1; col <= newBoardSize; col++)
                    {
                        Panel tilePanel = CreateTilePanel(row, col);
                        tableLayoutPanelBoard.Controls.Add(tilePanel, col, row);
                    }
                }

                currentBoardData = newBoardData;
                rowBonuses = newRowBonuses;
                columnBonuses = newColumnBonuses;

                PopulateUIFromData();
                
                UpdateFormSize(newBoardSize);
                CenterBoardInForm();

                UpdateAllTotals();
            }
            finally
            {
                tableLayoutPanelBoard.ResumeLayout(true);
                this.PerformLayout();
                this.Refresh();
            }
        }

        private void AttachTileEvents(Control tileControl)
        {
            if (tileControl is TextBox tb)
            {
                tb.Enter += TextBox_Enter;
                tb.Leave += TextBox_Leave;
                tb.TextChanged += TextBox_TextChanged;
            }
            else if (tileControl is PictureBox pb)
            {
                pb.DragEnter += PictureBox_DragEnter;
                pb.DragDrop += PictureBox_DragDrop;
                pb.DoubleClick += PictureBox_DoubleClick;
            }
            else if (tileControl is NumericUpDown nud && nud.Name.StartsWith("nudPoints_"))
            {
                nud.ValueChanged += PointsNud_ValueChanged;
            }
            else if (tileControl is Panel panel)
            {
                panel.Click += Panel_Click;
            }
        }

        private void DetachTileEvents(Control tileControl)
        {
            if (tileControl is TextBox tb)
            {
                tb.Enter -= TextBox_Enter;
                tb.Leave -= TextBox_Leave;
                tb.TextChanged -= TextBox_TextChanged;
            }
            else if (tileControl is PictureBox pb)
            {
                pb.DragEnter -= PictureBox_DragEnter;
                pb.DragDrop -= PictureBox_DragDrop;
                pb.DoubleClick -= PictureBox_DoubleClick;
            }
            else if (tileControl is NumericUpDown nud && nud.Name.StartsWith("nudPoints_"))
            {
                nud.ValueChanged -= PointsNud_ValueChanged;
            }
            else if (tileControl is Panel panel)
            {
                panel.Click -= Panel_Click;
            }
        }

        private async Task UpdateTileImageAsync(PictureBox pb, BingoTile tile, string imagePath)
        {
            if (!IsValidImageFile(imagePath))
            {
                MessageBox.Show("Please select a valid image file (JPG, PNG, BMP).", 
                    "Invalid File Type", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            { 
                string cachedPath = await CacheImageAsync(imagePath);
                if (string.IsNullOrEmpty(cachedPath)) return;

                pb.Image?.Dispose();
                pb.Image = null;

                pb.Image = await LoadImageAsync(cachedPath);
                if (pb.Image != null)
                {
                    tile.ImagePath = imagePath;

                    if (pb.Tag is PictureBoxTag tag)
                    {
                        pb.Tag = new PictureBoxTag
                        {
                            Coordinates = tag.Coordinates,
                            FilePath = imagePath
                        };
                    }
                    else
                    {
                        pb.Tag = new PictureBoxTag
                        {
                            Coordinates = new Point(tile.Column, tile.Row),
                            FilePath = imagePath
                        };
                    }

                    _imageCache.CacheImage(imagePath, pb.Image);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error setting image:\n{ex.Message}", 
                    "Image Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                ClearTileImage(pb, tile);
            }
        }

        private async Task<Image> LoadImageAsync(string path)
        {
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return null;

            return await Task.Run(() =>
            {
                try
                {
                    using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read))
                    {
                        return Image.FromStream(stream);
                    }
                }
                catch
                {
                    return null;
                }
            });
        }

        private async Task<string> CacheImageAsync(string originalPath)
        {
            if (!IsValidImageFile(originalPath)) return null;

            try
            {
                Directory.CreateDirectory(CACHE_PATH);

                string ext = Path.GetExtension(originalPath);
                using (var md5 = System.Security.Cryptography.MD5.Create())
                {
                    byte[] hash = md5.ComputeHash(Encoding.UTF8.GetBytes(originalPath));
                    string hashString = BitConverter.ToString(hash).Replace("-", "");
                    string cachedPath = Path.Combine(CACHE_PATH, $"{hashString}{ext}");

                    if (!File.Exists(cachedPath))
                    {
                        await Task.Run(() => File.Copy(originalPath, cachedPath, true));
                    }

                    File.SetLastAccessTimeUtc(cachedPath, DateTime.UtcNow);
                    return cachedPath;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error caching image: {ex.Message}");
                return null;
            }
        }

        private bool IsValidImageFile(string filePath)
        {
            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath)) 
                return false;

            string ext = Path.GetExtension(filePath).ToLowerInvariant();
            return SUPPORTED_IMAGE_EXTENSIONS.Contains(ext);
        }

        private void HandleDelayedLayoutUpdate(object sender, EventArgs e)
        {
            _layoutUpdateTimer.Stop();
            UpdateLayout();
        }

        private void UpdateLayout()
        {
            if (this.InvokeRequired)
            {
                this.BeginInvoke(new MethodInvoker(UpdateLayout));
                return;
            }

            Point currentScroll = this.AutoScrollPosition;

            tableLayoutPanelBoard.SuspendLayout();
            try
            {
                CenterBoardInForm();
                this.PerformLayout();
            }
            finally
            {
                tableLayoutPanelBoard.ResumeLayout(true);
                this.AutoScrollPosition = new Point(-currentScroll.X, -currentScroll.Y);
            }
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            _layoutUpdateTimer.Stop();
            _layoutUpdateTimer.Start();
        }

        private void BatchUpdateControls(Action<Control> updateAction, IEnumerable<Control> controls)
        {
            tableLayoutPanelBoard.SuspendLayout();
            try
            {
                foreach (var control in controls)
                {
                    if (control is Panel panel)
                        panel.SuspendLayout();
                    
                    updateAction(control);
                    
                    if (control is Panel p)
                        p.ResumeLayout();
                }
            }
            finally
            {
                tableLayoutPanelBoard.ResumeLayout(true);
            }
        }

        private void UpdateAllTilePanels()
        {
            var tilePanels = tableLayoutPanelBoard.Controls.OfType<Panel>()
                .Where(p => p.Tag is Point);
            
            BatchUpdateControls(panel => 
            {
                if (panel.Tag is Point coords)
                {
                    UpdateTilePanel((Panel)panel, coords);
                }
            }, tilePanels);
        }

        private bool IsValidCoordinate(Point coordinates)
        {
            return currentBoardData != null && 
                   coordinates.Y >= 0 && 
                   coordinates.Y < currentBoardData.GetLength(0) && 
                   coordinates.X >= 0 && 
                   coordinates.X < currentBoardData.GetLength(1);
        }

        private BingoTile GetTileAt(Point coordinates)
        {
            return IsValidCoordinate(coordinates) ? currentBoardData[coordinates.Y, coordinates.X] : null;
        }

        private BingoTile[,] GetCurrentBoardData()
        {
            return currentBoardData;
        }

        private BingoBoardState GetCurrentBoardState()
        {
            if (currentBoardData == null) return null;

            var state = new BingoBoardState
            {
                Tiles = new List<List<BingoTile>>(),
                RowBonuses = (int[])rowBonuses?.Clone() ?? Array.Empty<int>(),
                ColumnBonuses = (int[])columnBonuses?.Clone() ?? Array.Empty<int>(),
                Rows = currentBoardData.GetLength(0),
                Columns = currentBoardData.GetLength(1)
            };

            for (int r = 0; r < currentBoardData.GetLength(0); r++)
            {
                var row = new List<BingoTile>();
                for (int c = 0; c < currentBoardData.GetLength(1); c++)
                {
                    row.Add(currentBoardData[r, c]?.Clone());
                }
                state.Tiles.Add(row);
            }

            return state;
        }

        private void UpdateTilePanel(Panel panel, Point coordinates)
        {
            var tile = GetTileAt(coordinates);
            if (tile == null) return;

            foreach (Control control in panel.Controls)
            {
                if (control is TextBox tb)
                {
                    if (tb.Name.StartsWith("txtTitle_"))
                    {
                        HandleTextInput(tb, tile.Title, string.IsNullOrEmpty(tile.Title));
                    }
                }
                else if (control is NumericUpDown nud && nud.Name.StartsWith("nudPoints_"))
                {
                    nud.Value = tile.Points;
                }
                else if (control is PictureBox pb)
                {
                    UpdatePictureBoxFromTile(pb, tile);
                }
            }
        }

        private void UpdatePictureBoxFromTile(PictureBox pb, BingoTile tile)
        {
            pb.Image?.Dispose();
            pb.Image = null;
            pb.Tag = null;
            pb.BackColor = Color.FromArgb(tile.BackgroundColourArgb);

            if (!string.IsNullOrEmpty(tile.ImagePath) && File.Exists(tile.ImagePath))
            {
                try
                {
                    var cachedImage = _imageCache.RetrieveImage(tile.ImagePath);
                    if (cachedImage != null)
                    {
                        pb.Image = cachedImage;
                    }
                    else
                    {
                        pb.Image = Image.FromFile(tile.ImagePath);
                        _imageCache.CacheImage(tile.ImagePath, pb.Image);
                    }

                    var existingTag = pb.Tag as PictureBoxTag;
                    pb.Tag = new PictureBoxTag
                    {
                        Coordinates = existingTag?.Coordinates ?? new Point(tile.Column, tile.Row),
                        FilePath = tile.ImagePath
                    };
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error loading image {tile.ImagePath}: {ex.Message}");
                }
            }

            if (tile.IsCompleted)
            {
                DrawCompletionX(pb);
            }
        }

        private void HandleTextInput(TextBox textBox, string value, bool isPlaceholder = false)
        {
            if (string.IsNullOrEmpty(value) || isPlaceholder)
            {
                string placeholder = textBox.Tag as string;
                textBox.Text = placeholder;
                textBox.ForeColor = SystemColors.GrayText;
            }
            else
            {
                textBox.Text = value;
                textBox.ForeColor = SystemColors.WindowText;
            }
            textBox.TextAlign = HorizontalAlignment.Center;
        }

        private Panel CreateTilePanel(int row, int col)
        {
            Panel panel = new Panel
            {
                Dock = DockStyle.Fill,
                Tag = new Point(col - 1, row - 1),
                BorderStyle = BorderStyle.FixedSingle
            };

            PictureBox pictureBox = new PictureBox
            {
                Dock = DockStyle.Fill,
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = Color.Transparent,
                AllowDrop = true,
                Tag = new PictureBoxTag { Coordinates = new Point(col - 1, row - 1) }
            };
            AttachTileEvents(pictureBox);

            ContextMenuStrip contextMenu = new ContextMenuStrip();

            ToolStripMenuItem toggleCompleteItem = new ToolStripMenuItem("Toggle Completion");
            toggleCompleteItem.Click += (sender, e) => ToggleTileCompletion(pictureBox);
            contextMenu.Items.Add(toggleCompleteItem);

            ToolStripMenuItem changeBackgroundColorItem = new ToolStripMenuItem("Change Background Color");
            changeBackgroundColorItem.Click += (sender, e) => ChangeTileBackgroundColor(pictureBox);
            contextMenu.Items.Add(changeBackgroundColorItem);

            ToolStripMenuItem toggleBarrowsTileItem = new ToolStripMenuItem("Toggle Barrows Tile");
            toggleBarrowsTileItem.Click += (sender, e) => ToggleBarrowsTile(pictureBox);
            contextMenu.Items.Add(toggleBarrowsTileItem);

            pictureBox.ContextMenuStrip = contextMenu;

            panel.Controls.Add(pictureBox);

            TextBox titleBox = new TextBox
            {
                Name = $"txtTitle_{row}_{col}",
                Dock = DockStyle.Top,
                Tag = TITLE_PLACEHOLDER,
                Text = TITLE_PLACEHOLDER,
                ForeColor = SystemColors.GrayText,
                TextAlign = HorizontalAlignment.Center
            };
            AttachTileEvents(titleBox);

            NumericUpDown pointsBox = new NumericUpDown
            {
                Name = $"nudPoints_{row}_{col}",
                Dock = DockStyle.Bottom,
                Width = NUD_WIDTH,
                Minimum = 0,
                Maximum = 999,
                Value = 0,
                Tag = new Point(col - 1, row - 1)
            };
            pointsBox.ValueChanged += PointsNud_ValueChanged;

            panel.Controls.Add(titleBox);
            panel.Controls.Add(pointsBox);

            return panel;
        }

        private void ChangeTileBackgroundColor(PictureBox pb)
        {
            var parentPanel = pb.Parent as Panel;
            if (parentPanel?.Tag is Point coordinates)
            {
                var tile = GetTileAt(coordinates);
                if (tile != null)
                {
                    using (var colorDialog = new ColorDialog())
                    {
                        colorDialog.Color = Color.FromArgb(tile.BackgroundColourArgb);
                        colorDialog.FullOpen = true;
                        colorDialog.CustomColors = CustomColors;

                        if (colorDialog.ShowDialog() == DialogResult.OK)
                        {
                            tile.BackgroundColourArgb = colorDialog.Color.ToArgb();
                            pb.BackColor = colorDialog.Color;
                            colorDialog.CustomColors.CopyTo(CustomColors, 0);
                        }
                    }
                }
            }
        }

        private void ToggleBarrowsTile(PictureBox pb)
        {
            var parentPanel = pb.Parent as Panel;
            if (parentPanel?.Tag is Point coordinates)
            {
                var tile = GetTileAt(coordinates);
                if (tile != null)
                {
                    if (tile.Title == "Barrows Set")
                    {
                        tile.Title = TITLE_PLACEHOLDER;
                        tile.IsCompleted = false;
                        tile.ImagePath = null;
                        pb.Image?.Dispose();
                        pb.Image = null;
                        pb.BackColor = Color.Transparent;
                        MessageBox.Show("This tile has been reverted to a normal tile.", "Normal Tile", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    else
                    {
                        tile.Title = "Barrows Set";
                        tile.IsCompleted = false;

                        string barrowsImagePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "25700.png");
                        if (File.Exists(barrowsImagePath))
                        {
                            pb.Image?.Dispose();
                            pb.Image = Image.FromFile(barrowsImagePath);
                            tile.ImagePath = barrowsImagePath;
                        }
                        else
                        {
                            MessageBox.Show("The Barrows tile image (25700.png) could not be found.", "Image Not Found", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        }

                        pb.Tag = new PictureBoxTag
                        {
                            Coordinates = coordinates,
                            FilePath = tile.ImagePath
                        };

                        MessageBox.Show("This tile has been set as a Barrows tile.", "Barrows Tile", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
            }
        }

        private bool ValidateBoardState(BingoBoardState state)
        {
            return state != null &&
                   state.Tiles != null &&
                   state.RowBonuses != null &&
                   state.ColumnBonuses != null &&
                   state.Rows > 0 &&
                   state.Columns > 0 &&
                   state.RowBonuses.Length == state.Rows &&
                   state.ColumnBonuses.Length == state.Columns &&
                   state.Tiles.All(row => row.All(tile => tile.CompletionState != null && tile.CompletionState.All(col => col != null)));
        }

        private int CalculateRowTotal(int row)
        {
            if (currentBoardData == null) return 0;
            
            int total = 0;
            for (int col = 0; col < currentBoardData.GetLength(1); col++)
            {
                total += currentBoardData[row, col].Points;
            }
            return total + (rowBonuses != null ? rowBonuses[row] : 0);
        }

        private int CalculateColumnTotal(int col)
        {
            if (currentBoardData == null) return 0;
            
            int total = 0;
            for (int row = 0; row < currentBoardData.GetLength(0); row++)
            {
                total += currentBoardData[row, col].Points;
            }
            return total + (columnBonuses != null ? columnBonuses[col] : 0);
        }

        private int CalculateRowGainedPoints(int row)
        {
            if (currentBoardData == null) return 0;
            
            int gained = 0;
            for (int col = 0; col < currentBoardData.GetLength(1); col++)
            {
                var tile = currentBoardData[row, col];
                if (tile.IsCompleted)
                {
                    gained += tile.Points;
                }
            }
            
            if (IsRowComplete(row) && rowBonuses != null && row < rowBonuses.Length)
            {
                gained += rowBonuses[row];
            }
            
            return gained;
        }

        private int CalculateColumnGainedPoints(int col)
        {
            if (currentBoardData == null) return 0;
            
            int gained = 0;
            for (int row = 0; row < currentBoardData.GetLength(0); row++)
            {
                var tile = currentBoardData[row, col];
                if (tile.IsCompleted)
                {
                    gained += tile.Points;
                }
            }
            
            if (IsColumnComplete(col) && columnBonuses != null && col < columnBonuses.Length)
            {
                gained += columnBonuses[col];
            }
            
            return gained;
        }

        private void UpdateAllTotals()
        {
            int boardSize = currentBoardData.GetLength(0);
            
            for (int row = 0; row < boardSize; row++)
            {
                var totalLabel = tableLayoutPanelBoard.Controls.OfType<Label>()
                    .FirstOrDefault(l => l.Name == $"lblRowTotal_{row}");
                if (totalLabel != null)
                {
                    int gained = CalculateRowGainedPoints(row);
                    int total = CalculateRowTotal(row);
                    totalLabel.Text = $"{gained}/{total}";
                }
            }
            
            for (int col = 0; col < boardSize; col++)
            {
                var totalLabel = tableLayoutPanelBoard.Controls.OfType<Label>()
                    .FirstOrDefault(l => l.Name == $"lblColTotal_{col}");
                if (totalLabel != null)
                {
                    int gained = CalculateColumnGainedPoints(col);
                    int total = CalculateColumnTotal(col);
                    totalLabel.Text = $"{gained}/{total}";
                }
            }
        }

        private void PointsNud_ValueChanged(object sender, EventArgs e)
        {
            if (!(sender is NumericUpDown nud) || !(nud.Tag is Point coordinates)) return;
            
            var tile = GetTileAt(coordinates);
            if (tile != null)
            {
                tile.Points = (int)nud.Value;
                UpdateAllTotals();
            }
        }

        private void BonusNud_ValueChanged(object sender, EventArgs e)
        {
            if (!(sender is NumericUpDown nud) || !(nud.Tag is int index)) return;

            if (nud.Name.StartsWith("nudRowBonus_"))
            {
                if (index < rowBonuses?.Length)
                {
                    rowBonuses[index] = (int)nud.Value;
                    UpdateAllTotals();
                }
            }
            else if (nud.Name.StartsWith("nudColBonus_"))
            {
                if (index < columnBonuses?.Length)
                {
                    columnBonuses[index] = (int)nud.Value;
                    UpdateAllTotals();
                }
            }
        }

        private void setColourMenuItem_Click(object sender, EventArgs e)
        {
            if (!(sender is ToolStripMenuItem menuItem)) return;

            var menu = menuItem.Owner as ContextMenuStrip;
            var panel = menu?.SourceControl as Panel;
            
            if (panel?.Tag is Point coordinates)
            {
                var tile = GetTileAt(coordinates);
                if (tile != null)
                {
                    using (var colorDialog = new ColorDialog())
                    {
                        colorDialog.Color = Color.FromArgb(tile.BackgroundColourArgb);
                        colorDialog.FullOpen = true;

                        if (colorDialog.ShowDialog() == DialogResult.OK)
                        {
                            tile.BackgroundColourArgb = colorDialog.Color.ToArgb();
                            var pb = panel.Controls.OfType<PictureBox>().FirstOrDefault();
                            if (pb != null)
                            {
                                pb.BackColor = colorDialog.Color;
                            }
                        }
                    }
                }
            }
        }

        private void btnExport_Click(object sender, EventArgs e)
        {
            this.boardState = GetCurrentBoardState();
            if (boardState == null || !ValidateBoardState(boardState))
            {
                MessageBox.Show("Please create a valid board before exporting.", 
                    "Export Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            using (var exportOptionsForm = new ExportOptionsForm(boardState))
            {
                exportOptionsForm.ShowDialog();
            }
        }

        private void TextBox_Enter(object sender, EventArgs e)
        {
            if (sender is TextBox tb)
            {
                string placeholder = tb.Tag as string;
                if (!string.IsNullOrEmpty(placeholder) && tb.Text == placeholder && tb.ForeColor == SystemColors.GrayText)
                {
                    tb.Text = "";
                    tb.ForeColor = SystemColors.WindowText;
                }
            }
        }

        private void TextBox_Leave(object sender, EventArgs e)
        {
            if (sender is TextBox tb)
            {
                string placeholder = tb.Tag as string;
                if (!string.IsNullOrEmpty(placeholder) && string.IsNullOrWhiteSpace(tb.Text))
                {
                    tb.Text = placeholder;
                    tb.ForeColor = SystemColors.GrayText;
                }
            }
        }

        private void TextBox_TextChanged(object sender, EventArgs e)
        {
            if (!(sender is TextBox tb)) return;

            var panel = tb.Parent as Panel;
            if (panel?.Tag is Point coordinates)
            {
                var tile = GetTileAt(coordinates);
                if (tile != null && tb.Name.StartsWith("txtTitle_"))
                {
                    string placeholder = tb.Tag as string;
                    bool isPlaceholder = !string.IsNullOrEmpty(placeholder) && 
                                       tb.Text == placeholder && 
                                       tb.ForeColor == SystemColors.GrayText;

                    tile.Title = isPlaceholder ? "" : tb.Text;
                }
            }
        }

        private void PictureBox_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effect = DragDropEffects.Copy;
            }
        }

        private void PictureBox_DragDrop(object sender, DragEventArgs e)
        {
            if (!(sender is PictureBox pb) || !e.Data.GetDataPresent(DataFormats.FileDrop)) 
                return;

            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
            if (files?.Length > 0)
            {
                string imagePath = files[0];
                var parentPanel = pb.Parent as Panel;
                if (parentPanel?.Tag is Point coordinates)
                {
                    var tile = GetTileAt(coordinates);
                    if (tile != null)
                    {
                        _ = UpdateTileImageAsync(pb, tile, imagePath);
                    }
                }
            }
        }

        private void PictureBox_DoubleClick(object sender, EventArgs e)
        {
            if (!(sender is PictureBox pb)) return;

            var parentPanel = pb.Parent as Panel;
            if (parentPanel?.Tag is Point coordinates)
            {
                var tile = GetTileAt(coordinates);
                if (tile != null)
                {
                    if (tile.Title == "Barrows Set")
                    {
                        using (var barrowsForm = new BarrowsBoardForm())
                        {
                            barrowsForm.CompletionState = tile.CompletionState ?? new bool[4][].Select(_ => new bool[6]).ToArray();

                            string originalImagePath = tile.ImagePath;

                            barrowsForm.BarrowsTileCompletionChanged += (isAnyColumnComplete, completedColumn) =>
                            {
                                Debug.WriteLine($"BarrowsTileCompletionChanged: isAnyColumnComplete = {isAnyColumnComplete}, completedColumn = {completedColumn}");

                                bool hasCompletedColumn = false;
                                int lastCompletedColumn = -1;

                                for (int col = 0; col < 6; col++)
                                {
                                    bool columnComplete = true;
                                    for (int row = 0; row < 4; row++)
                                    {
                                        if (!barrowsForm.CompletionState[row][col])
                                        {
                                            columnComplete = false;
                                            break;
                                        }
                                    }
                                    if (columnComplete)
                                    {
                                        hasCompletedColumn = true;
                                        lastCompletedColumn = col;
                                        break;
                                    }
                                }

                                if (hasCompletedColumn)
                                {
                                    tile.IsCompleted = true;
                                    UpdateBarrowsTileImage(pb, tile, lastCompletedColumn);
                                    DrawCompletionX(pb);
                                    UpdateAllTotals();
                                }
                                else
                                {
                                    if (File.Exists(originalImagePath))
                                    {
                                        pb.Image?.Dispose();
                                        pb.Image = Image.FromFile(originalImagePath);
                                        tile.ImagePath = originalImagePath;
                                        
                                        if (pb.Tag is PictureBoxTag tag)
                                        {
                                            pb.Tag = new PictureBoxTag
                                            {
                                                Coordinates = tag.Coordinates,
                                                FilePath = originalImagePath
                                            };
                                        }
                                    }
                                    tile.IsCompleted = false;
                                    pb.Paint -= PictureBox_Paint;
                                    pb.Invalidate();
                                    UpdateAllTotals();
                                }
                            };

                            barrowsForm.ShowDialog();

                            tile.CompletionState = barrowsForm.CompletionState;
                        }
                    }
                    else
                    {
                        using (var imageSelectorForm = new ImageSelectorForm())
                        {
                            if (imageSelectorForm.ShowDialog() == DialogResult.OK && !string.IsNullOrEmpty(imageSelectorForm.SelectedImagePath))
                            {
                                _ = UpdateTileImageAsync(pb, tile, imageSelectorForm.SelectedImagePath);
                            }
                        }
                    }
                }
            }
        }

        private void Panel_Click(object sender, EventArgs e)
        {
            if (!(sender is Panel panel)) return;
            var pb = panel.Controls.OfType<PictureBox>().FirstOrDefault();
            if (pb != null)
            {
                ToggleTileCompletion(pb);
            }
        }

        private void ToggleTileCompletion(PictureBox pb)
        {
            var parentPanel = pb.Parent as Panel;
            if (parentPanel?.Tag is Point coordinates)
            {
                var tile = GetTileAt(coordinates);
                if (tile != null)
                {
                    tile.IsCompleted = !tile.IsCompleted;
                    
                    if (tile.IsCompleted)
                    {
                        DrawCompletionX(pb);
                    }
                    else
                    {
                        pb.Paint -= PictureBox_Paint;
                        pb.Invalidate();
                    }

                    RefreshRowAndColumnTiles(tile.Row, tile.Column);
                    
                    UpdateAllTotals();
                }
            }
        }

        private void DrawCompletionX(PictureBox pb)
        {
            pb.Paint -= PictureBox_Paint;
            pb.Paint += PictureBox_Paint;
            pb.Invalidate();
        }

        private void PictureBox_Paint(object sender, PaintEventArgs e)
        {
            if (sender is PictureBox pb && pb.Tag is PictureBoxTag tag && tag.Coordinates.HasValue)
            {
                var tile = GetTileAt(tag.Coordinates.Value);
                if (tile?.IsCompleted == true)
                {
                    bool isInCompletedLine = IsRowComplete(tile.Row) || IsColumnComplete(tile.Column);
                    Color xColor = isInCompletedLine ? X_COLOR_COMPLETE : X_COLOR_NORMAL;

                    using (var pen = new Pen(xColor, X_THICKNESS))
                    {
                        e.Graphics.DrawLine(pen, 0, 0, pb.Width, pb.Height);
                        e.Graphics.DrawLine(pen, 0, pb.Height, pb.Width, 0);
                    }
                }
            }
        }

        private void RefreshRowAndColumnTiles(int row, int col)
        {
            foreach (Control control in tableLayoutPanelBoard.Controls)
            {
                if (control is Panel panel && panel.Tag is Point coords)
                {
                    if (coords.Y == row || coords.X == col)
                    {
                        var pb = panel.Controls.OfType<PictureBox>().FirstOrDefault();
                        if (pb != null)
                        {
                            pb.Invalidate();
                        }
                    }
                }
            }
        }

        private bool IsRowComplete(int row)
        {
            if (currentBoardData == null) return false;
            
            for (int col = 0; col < currentBoardData.GetLength(1); col++)
            {
                if (!currentBoardData[row, col].IsCompleted)
                    return false;
            }
            return true;
        }

        private bool IsColumnComplete(int col)
        {
            if (currentBoardData == null) return false;

            for (int row = 0; row < currentBoardData.GetLength(0); row++)
            {
                if (!currentBoardData[row, col].IsCompleted)
                {
                    return false;
                }
            }
            return true;
        }

        private void UpdateBarrowsTileImage(PictureBox pb, BingoTile tile, int completedColumn)
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
                try
                {
                    string imagePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Images", imageName);
                    if (File.Exists(imagePath))
                    {
                        pb.Image?.Dispose();
                        pb.Image = null;

                        using (var stream = new FileStream(imagePath, FileMode.Open, FileAccess.Read))
                        {
                            pb.Image = Image.FromStream(stream);
                        }
                        
                        tile.ImagePath = imagePath;
                        
                        if (pb.Tag is PictureBoxTag tag)
                        {
                            pb.Tag = new PictureBoxTag
                            {
                                Coordinates = tag.Coordinates,
                                FilePath = imagePath
                            };
                        }

                        _imageCache.CacheImage(imagePath, pb.Image);

                        pb.Invalidate();
                    }
                    else
                    {
                        Debug.WriteLine($"Image not found for completed column {completedColumn}: {imagePath}");
                        MessageBox.Show($"Could not find Barrows set image: {imageName}", "Image Not Found", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error updating Barrows tile image: {ex.Message}");
                    MessageBox.Show($"Error updating Barrows tile image: {ex.Message}", "Update Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void OnBarrowsTileCompletionChanged(bool isAnyColumnComplete, int completedColumn)
        {
            var barrowsTile = GetTileAt(new Point(BarrowsTileColumn, BarrowsTileRow));
            if (barrowsTile == null) return;

            if (isAnyColumnComplete && completedColumn >= 0)
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
                    string imagePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Images", imageName);
                    if (File.Exists(imagePath))
                    {
                        barrowsTile.ImagePath = imagePath;
                        UpdateTileImage(barrowsTile, imagePath);
                    }
                }
            }
            else
            {
                barrowsTile.ImagePath = null;
                ClearTileImage(GetPictureBoxForTile(barrowsTile), barrowsTile);
            }
        }

        private PictureBox GetPictureBoxForTile(BingoTile tile)
        {
            foreach (Control control in tableLayoutPanelBoard.Controls)
            {
                if (control is Panel panel && panel.Tag is Point coordinates)
                {
                    if (coordinates.X == tile.Column && coordinates.Y == tile.Row)
                    {
                        return panel.Controls.OfType<PictureBox>().FirstOrDefault();
                    }
                }
            }
            return null;
        }

        private void LoadBingoJson(string filePath)
        {
            this.boardState = LoadBoardFromJson(filePath);

            RefreshBoardDisplay();
        }

        private BingoBoardState LoadBoardFromJson(string filePath)
        {
            try
            {
                string jsonString = File.ReadAllText(filePath);
                BingoBoardState loadedState = JsonSerializer.Deserialize<BingoBoardState>(jsonString);

                if (!ValidateBoardState(loadedState))
                {
                    throw new InvalidDataException("Invalid board data structure or mismatched sizes.");
                }

                return loadedState;
            }
            catch (JsonException ex)
            {
                MessageBox.Show($"Error reading board data (invalid JSON format or structure):\n{ex.Message}", 
                    "Load Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return null;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading board file:\n{ex.Message}", 
                    "Load Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return null;
            }
        }

        private void RefreshBoardDisplay()
        {
            if (boardState == null || !ValidateBoardState(boardState)) return;

            int rows = boardState.Rows;
            int cols = boardState.Columns;

            InitializeBoardArrays(rows);
            currentBoardData = new BingoTile[rows, cols];

            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < cols; c++)
                {
                    currentBoardData[r, c] = boardState.Tiles[r][c].Clone();
                }
            }

            rowBonuses = boardState.RowBonuses;
            columnBonuses = boardState.ColumnBonuses;

            CreateBoardGrid();
            PopulateUIFromData();
            UpdateAllTotals();
        }

        private void UpdateTileImage(BingoTile tile, string imagePath)
        {
            if (tile == null || string.IsNullOrEmpty(imagePath)) return;

            try
            {
                string cachedPath = CacheImage(imagePath);
                if (string.IsNullOrEmpty(cachedPath)) return;

                tile.ImagePath = cachedPath;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error updating tile image:\n{ex.Message}", "Image Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private string CacheImage(string originalPath)
        {
            if (!IsValidImageFile(originalPath)) return null;

            try
            {
                Directory.CreateDirectory(CACHE_PATH);

                string ext = Path.GetExtension(originalPath);
                using (var md5 = System.Security.Cryptography.MD5.Create())
                {
                    byte[] hash = md5.ComputeHash(Encoding.UTF8.GetBytes(originalPath));
                    string hashString = BitConverter.ToString(hash).Replace("-", "");
                    string cachedPath = Path.Combine(CACHE_PATH, $"{hashString}{ext}");

                    if (!File.Exists(cachedPath))
                    {
                        File.Copy(originalPath, cachedPath, true);
                    }

                    File.SetLastAccessTimeUtc(cachedPath, DateTime.UtcNow);
                    return cachedPath;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error caching image: {ex.Message}");
                return null;
            }
        }

        private void ClearTileImage(PictureBox pb, BingoTile tile)
        {
            if (pb == null || tile == null) return;

            pb.Image?.Dispose();
            pb.Image = null;
            tile.ImagePath = null;
            pb.BackColor = Color.Transparent;
            pb.Paint -= PictureBox_Paint;
            pb.Invalidate();
        }
    }

    public class PictureBoxTag
    {
        public Point? Coordinates { get; set; }
        public string FilePath { get; set; }
    }   
}