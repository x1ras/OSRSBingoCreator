using System;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;
using OSRSBingoCreator.Models;

namespace OSRSBingoCreator.Models
{
    public class BingoBoardState
    {
        public List<List<BingoTile>> Tiles { get; set; }
        
        public int[] RowBonuses { get; set; }
        
        public int[] ColumnBonuses { get; set; }
        
        public int Rows { get; set; }

        public int Columns { get; set; }
    }
}
