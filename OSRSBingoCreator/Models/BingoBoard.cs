using System;

namespace OSRSBingoCreator.Models
{
    // Represents a bingo board with a 2D array of tiles.
    public class BingoBoard
    {
        private readonly BingoTile[,] tiles;

        // Creates a new bingo board with the specified dimensions.
        // <param name="rows">Number of rows in the board</param>
        // <param name="cols">Number of columns in the board</param>
        public BingoBoard(int rows, int cols)
        {
            tiles = new BingoTile[rows, cols];
        }

        // Gets the tile at the specified coordinates.
        // <param name="x">Row index</param>
        // <param name="y">Column index</param>
        // Returns the bingo tile at the specified position
        public BingoTile GetTile(int x, int y)
        {
            if (x < 0 || x >= tiles.GetLength(0) || y < 0 || y >= tiles.GetLength(1))
            {
                throw new ArgumentOutOfRangeException(nameof(x) + ", " + nameof(y), "Coordinates are out of bounds.");
            }
            return tiles[x, y];
        }

        // Sets the tile at the specified coordinates.
        // <param name="x">Row index</param>
        // <param name="y">Column index</param>
        // <param name="tile">The tile to place at the position</param>
        public void SetTile(int x, int y, BingoTile tile)
        {
            if (x < 0 || x >= tiles.GetLength(0) || y < 0 || y >= tiles.GetLength(1))
            {
                throw new ArgumentOutOfRangeException(nameof(x) + ", " + nameof(y), "Coordinates are out of bounds.");
            }
            tiles[x, y] = tile;
        }
    }
}
