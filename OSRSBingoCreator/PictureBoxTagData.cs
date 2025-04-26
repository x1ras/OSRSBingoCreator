using System.Drawing;
using System.Windows.Forms;
using System.Threading.Tasks;
using System;
using System.Linq;

namespace OsrsBingoCreator
{
    public class PictureBoxTagData
    {
        public Point? Coordinates { get; set; }
        public string FilePath { get; set; }

        public PictureBoxTagData(Point? coordinates = null, string filePath = null)
        {
            Coordinates = coordinates;
            FilePath = filePath;
        }
    }
}
