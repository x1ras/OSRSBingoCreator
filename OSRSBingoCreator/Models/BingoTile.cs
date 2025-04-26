using System.Drawing;
using System.Linq;

namespace OSRSBingoCreator.Models
{
    // Represents a single tile on a bingo board.
    public class BingoTile
    {
        public int Row { get; set; }
        
        public int Column { get; set; }
        
        public string Title { get; set; }
        
        public int Points { get; set; }
        
        public string ImagePath { get; set; }
        
        public string ImageUrl { get; set; }
        
        public int BackgroundColourArgb { get; set; }
        
        public bool IsCompleted { get; set; }
        
        public bool[][] CompletionState { get; set; }

        // Creates a new BingoTile with default values.
        public BingoTile()
        {
            Title = string.Empty;
            Points = 0;
            ImagePath = string.Empty;
            ImageUrl = string.Empty;
            BackgroundColourArgb = Color.Transparent.ToArgb();
            IsCompleted = false;
            CompletionState = InitializeCompletionState(4, 6);
        }

        // Creates a new BingoTile with specified position.
        public BingoTile(int row, int column)
        {
            Row = row;
            Column = column;
            Title = string.Empty;
            Points = 0;
            ImagePath = null;
            ImageUrl = null;
            BackgroundColourArgb = Color.Transparent.ToArgb();
            IsCompleted = false;
            CompletionState = InitializeCompletionState(4, 6);
        }

        // Creates a deep copy of this tile.
        // Returns a new BingoTile with the same values.
        public BingoTile Clone()
        {
            return new BingoTile
            {
                Row = Row,
                Column = Column,
                Title = Title,
                Points = Points,
                ImagePath = ImagePath,
                ImageUrl = ImageUrl,
                BackgroundColourArgb = BackgroundColourArgb,
                IsCompleted = IsCompleted,
                CompletionState = CompletionState?.Select(row => row.ToArray()).ToArray()
            };
        }

        // Creates a jagged array for tracking completion state.
        // Returns initialized completion state array.
        private bool[][] InitializeCompletionState(int rows, int columns)
        {
            var state = new bool[rows][];
            for (int i = 0; i < rows; i++)
            {
                state[i] = new bool[columns];
            }
            return state;
        }

        // Draws a red X over the tile to indicate completion.
        public void DrawRedX(Graphics g, Rectangle imageArea)
        {
            using (Pen redPen = new Pen(Color.Red, 2))
            {
                g.DrawLine(redPen, imageArea.Left, imageArea.Top, imageArea.Right, imageArea.Bottom);
                g.DrawLine(redPen, imageArea.Left, imageArea.Bottom, imageArea.Right, imageArea.Top);
            }
        }
    }
}
