using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Runtime.InteropServices;

namespace KinetixStudio
{
    public static class Brand
    {
        public const string Version = "2.2.1";
        [DllImport("user32.dll")]
        static extern bool DestroyIcon(IntPtr handle);
        public static void Draw(Graphics graphics, RectangleF bounds, bool tile)
        {
            GraphicsState saved = graphics.Save();
            graphics.SmoothingMode = SmoothingMode.AntiAlias;
            graphics.TranslateTransform(bounds.X, bounds.Y);
            graphics.ScaleTransform(bounds.Width / 512f, bounds.Height / 512f);
            if (tile)
                T.Box(graphics, new RectangleF(0, 0, 512, 512), Color.FromArgb(12, 17, 27), 112);
            using (Pen p = new Pen(Color.FromArgb(36, 53, 73), 3))
                graphics.DrawArc(p, 53, 53, 406, 406, 202, 285);
            PointF[] left =
            {
                new PointF(96, 116),
                new PointF(174, 116),
                new PointF(174, 396),
                new PointF(96, 396)
            };
            PointF[] right =
            {
                new PointF(196, 233),
                new PointF(309, 116),
                new PointF(418, 116),
                new PointF(277, 262),
                new PointF(196, 262)
            };
            using (LinearGradientBrush b = new LinearGradientBrush(new Point(85, 130), new Point(265, 400), Color.FromArgb(255, 185, 117), Color.FromArgb(255, 98, 125)))
                graphics.FillPolygon(b, left);
            using (LinearGradientBrush b = new LinearGradientBrush(new Point(420, 96), new Point(250, 400), Color.FromArgb(127, 248, 237), Color.FromArgb(67, 137, 249)))
                graphics.FillPolygon(b, right);
            using (LinearGradientBrush b = new LinearGradientBrush(new Point(196, 269), new Point(421, 396), Color.FromArgb(127, 248, 237), Color.FromArgb(67, 137, 249)))
                graphics.FillPolygon(b, new PointF[] { new PointF(196, 286), new PointF(259, 269), new PointF(421, 396), new PointF(310, 396) });
            graphics.Restore(saved);
        }

        public static void Export(string directory)
        {
            Directory.CreateDirectory(directory);
            using (Bitmap bitmap = new Bitmap(512, 512))
            {
                using (Graphics g = Graphics.FromImage(bitmap))
                {
                    g.Clear(Color.Transparent);
                    Draw(g, new RectangleF(0, 0, 512, 512), true);
                }

                bitmap.Save(Path.Combine(directory, "kinetix-logo.png"), System.Drawing.Imaging.ImageFormat.Png);
            }

            using (Bitmap bitmap = new Bitmap(64, 64))
            {
                using (Graphics g = Graphics.FromImage(bitmap))
                {
                    g.Clear(Color.Transparent);
                    Draw(g, new RectangleF(0, 0, 64, 64), true);
                }

                IntPtr handle = bitmap.GetHicon();
                try
                {
                    using (Icon icon = Icon.FromHandle(handle))
                    using (FileStream output = File.Create(Path.Combine(directory, "kinetix.ico")))
                        icon.Save(output);
                }
                finally
                {
                    DestroyIcon(handle);
                }
            }

            using (Bitmap banner = new Bitmap(1440, 500))
            {
                using (Graphics g = Graphics.FromImage(banner))
                {
                    g.Clear(T.Bg);
                    using (LinearGradientBrush b = new LinearGradientBrush(new Rectangle(0, 0, 1440, 500), T.Bg, Color.FromArgb(24, 43, 65), 0f))
                        g.FillRectangle(b, 0, 0, 1440, 500);
                    using (Pen p = new Pen(Color.FromArgb(29, 46, 64)))
                        for (int x = 0; x < 1440; x += 64)
                            g.DrawLine(p, x, 0, x - 200, 500);
                    Draw(g, new RectangleF(80, 70, 350, 350), false);
                    T.TextAt(g, "KINETIX", 68, T.Text, 460, 101, FontStyle.Bold);
                    T.TextAt(g, "STUDIO", 22, T.Purple, 470, 211, FontStyle.Bold);
                    T.TextAt(g, "SEU MOVIMENTO. SUA ASSINATURA.", 15, T.Mint, 470, 285, FontStyle.Bold);
                    T.TextAt(g, "Teclado + mouse  →  Controle virtual", 17, T.Muted, 470, 332, FontStyle.Regular);
                }

                banner.Save(Path.Combine(directory, "kinetix-banner.png"), System.Drawing.Imaging.ImageFormat.Png);
            }
        }
    }
}
