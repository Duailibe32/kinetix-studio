using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Forms;

namespace Veylora
{
    static class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            Native.SetProcessDPIAware();
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.ThreadException += (s, e) =>
            {
                Store.Log(e.Exception.ToString());
                MessageBox.Show(e.Exception.Message, "Veylora", MessageBoxButtons.OK, MessageBoxIcon.Error);
            };
            if (args.Length > 0 && args[0] == "--render")
            {
                using (MainWindow window = new MainWindow(true))
                {
                    window.Show();
                    Application.DoEvents();
                    if (args.Length > 2)
                        window.Navigate(Int32.Parse(args[2]));
                    Application.DoEvents();
                    using (Bitmap bmp = new Bitmap(window.Width, window.Height))
                    {
                        window.DrawToBitmap(bmp, new Rectangle(0, 0, window.Width, window.Height));
                        bmp.Save(args.Length > 1 ? args[1] : "veylora-preview.png");
                    }

                    window.Close();
                }

                return;
            }

            bool fresh;
            using (Mutex mutex = new Mutex(true, "Local\\Veylora.Desktop", out fresh))
            {
                if (!fresh)
                {
                    MessageBox.Show("O Veylora já está aberto.", "Veylora");
                    return;
                }

                Application.Run(new MainWindow(false));
            }
        }
    }
}
