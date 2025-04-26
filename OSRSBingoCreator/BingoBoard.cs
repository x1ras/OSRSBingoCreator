using System;
using OSRSBingoCreator;
using OSRSBingoCreator.Core.Models; // Ensure the correct namespace for BingoTile is included

namespace OsrsBingoCreator
{
    public class BingoBoard
    {
        private BingoTile[,] tiles;

        public BingoBoard(int rows, int cols)
        {
            tiles = new BingoTile[rows, cols];
        }

        public BingoTile GetTile(int x, int y)
        {
            if (x < 0 || x >= tiles.GetLength(0) || y < 0 || y >= tiles.GetLength(1))
            {
                throw new ArgumentOutOfRangeException("Coordinates are out of bounds.");
            }
            return tiles[x, y];
        }

        public void SetTile(int x, int y, BingoTile tile)
        {
            if (x < 0 || x >= tiles.GetLength(0) || y < 0 || y >= tiles.GetLength(1))
            {
                throw new ArgumentOutOfRangeException("Coordinates are out of bounds.");
            }
            tiles[x, y] = tile;
        }
    }
}
