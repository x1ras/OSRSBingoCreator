using System;
using System.Windows.Forms;
using OSRSBingoCreator;

namespace OsrsBingoCreator
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new BingoCreatorForm());
        }
    }
}