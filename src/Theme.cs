using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace Veylora
{
    public static class T
    {
        public static Color Bg = Color.FromArgb(10, 15, 24), Side = Color.FromArgb(13, 20, 31), Card = Color.FromArgb(18, 28, 42), Line = Color.FromArgb(35, 52, 71), Text = Color.FromArgb(240, 245, 252), Muted = Color.FromArgb(148, 169, 190), Mint = Color.FromArgb(255, 171, 124), Purple = Color.FromArgb(112, 227, 226);
        public static Font Font(float size, FontStyle style)
        {
            return new Font("Segoe UI", size, style);
        }

        public static GraphicsPath Round(RectangleF r, float radius)
        {
            GraphicsPath p = new GraphicsPath();
            float d = radius * 2;
            p.AddArc(r.X, r.Y, d, d, 180, 90);
            p.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            p.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            p.CloseFigure();
            return p;
        }

        public static void Box(Graphics g, RectangleF r, Color color, int radius)
        {
            using (GraphicsPath p = Round(r, radius))
            using (Brush b = new SolidBrush(color))
                g.FillPath(b, p);
        }

        public static void Border(Graphics g, RectangleF r, Color color, int radius)
        {
            using (GraphicsPath p = Round(r, radius))
            using (Pen pen = new Pen(color))
                g.DrawPath(pen, p);
        }

        public static void TextAt(Graphics g, string text, float size, Color color, float x, float y, FontStyle style)
        {
            using (Font f = Font(size, style))
            using (Brush b = new SolidBrush(color))
                g.DrawString(text, f, b, x, y);
        }

        public static Label Label(string text, int size, Color color, int x, int y, int width, int height)
        {
            return new Label
            {
                Text = text,
                Font = Font(size, FontStyle.Regular),
                ForeColor = color,
                BackColor = Color.Transparent,
                AutoEllipsis = true,
                Location = new Point(x, y),
                Size = new Size(width, height)
            };
        }

        public static TextBox Input(string value, int width)
        {
            return new TextBox
            {
                Text = value,
                Width = width,
                Font = Font(11, FontStyle.Regular),
                BackColor = Card,
                ForeColor = Text,
                BorderStyle = BorderStyle.FixedSingle
            };
        }
    }

    public class SoftPanel : Panel
    {
        public Color Fill = T.Card;
        public bool Outline = true, Decorative;
        public SoftPanel()
        {
            DoubleBuffered = true;
            BackColor = Color.Transparent;
            Padding = new Padding(22);
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            e.Graphics.Clear(Parent == null ? T.Bg : Parent.BackColor);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            T.Box(e.Graphics, new RectangleF(0, 0, Width - 1, Height - 1), Fill, 18);
            if (Decorative)
            {
                GraphicsState state = e.Graphics.Save();
                using (GraphicsPath clip = T.Round(new RectangleF(1, 1, Width - 2, Height - 2), 18))
                    e.Graphics.SetClip(clip);
                using (Pen p = new Pen(Color.FromArgb(26, 54, 72)))
                    for (int x = Width / 2; x < Width + 300; x += 38)
                        e.Graphics.DrawLine(p, x, 0, x - 120, Height);
                using (Pen p = new Pen(Color.FromArgb(38, 72, 92), 2))
                    e.Graphics.DrawEllipse(p, Width - 190, -100, 340, 340);
                e.Graphics.Restore(state);
            }

            if (Outline)
                T.Border(e.Graphics, new RectangleF(.5f, .5f, Width - 1, Height - 1), T.Line, 18);
        }
    }

    public class ActionButton : Button
    {
        public bool Primary, Selected, Navigation;
        bool hover;
        public ActionButton()
        {
            FlatStyle = FlatStyle.Flat;
            FlatAppearance.BorderSize = 0;
            Cursor = Cursors.Hand;
            Font = T.Font(10, FontStyle.Bold);
            Height = 43;
            Width = 155;
            ForeColor = T.Text;
            BackColor = T.Card;
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            hover = true;
            Invalidate();
            base.OnMouseEnter(e);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            hover = false;
            Invalidate();
            base.OnMouseLeave(e);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            e.Graphics.Clear(Parent is SoftPanel ? ((SoftPanel)Parent).Fill : Parent == null ? T.Bg : Parent.BackColor);
            Color fill = Primary ? T.Mint : Selected ? Color.FromArgb(41, 42, 48) : hover ? Color.FromArgb(29, 45, 61) : Navigation ? T.Side : T.Card;
            if (!Enabled)
                fill = T.Line;
            T.Box(e.Graphics, new RectangleF(0, 0, Width - 1, Height - 1), fill, 11);
            if (!Primary && !Navigation)
                T.Border(e.Graphics, new RectangleF(.5f, .5f, Width - 1, Height - 1), Selected ? T.Purple : T.Line, 11);
            if (Navigation)
            {
                using (Brush b = new SolidBrush(Selected ? T.Mint : T.Muted))
                    e.Graphics.FillEllipse(b, 15, Height / 2 - 3, 6, 6);
                if (Selected)
                    T.Box(e.Graphics, new RectangleF(1, 10, 3, Height - 20), T.Mint, 1);
            }

            Rectangle text = Navigation ? new Rectangle(34, 0, Width - 39, Height) : ClientRectangle;
            TextRenderer.DrawText(e.Graphics, Text, Font, text, Primary ? T.Bg : Selected ? T.Mint : Enabled ? T.Text : T.Muted, (Navigation ? TextFormatFlags.Left : TextFormatFlags.HorizontalCenter) | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
            if (Focused)
                ControlPaint.DrawFocusRectangle(e.Graphics, new Rectangle(4, 4, Width - 8, Height - 8), T.Purple, fill);
        }
    }

    public class ControllerView : Control
    {
        public PadState State = new PadState();
        public string Kind = "PS4";
        public ControllerView()
        {
            DoubleBuffered = true;
            BackColor = T.Card;
            Size = new Size(480, 285);
        }

        void Disc(Graphics g, float x, float y, float radius, bool active, string text, Color tint)
        {
            using (Brush b = new SolidBrush(active ? tint : Color.FromArgb(30, 39, 56)))
                g.FillEllipse(b, x - radius, y - radius, radius * 2, radius * 2);
            using (Pen pen = new Pen(active ? tint : T.Line, 1.5f))
                g.DrawEllipse(pen, x - radius, y - radius, radius * 2, radius * 2);
            if (text != null)
                using (Font f = T.Font(12, FontStyle.Bold))
                using (Brush b = new SolidBrush(active ? T.Bg : tint))
                {
                    StringFormat sf = new StringFormat
                    {
                        Alignment = StringAlignment.Center,
                        LineAlignment = StringAlignment.Center
                    };
                    g.DrawString(text, f, b, new RectangleF(x - radius, y - radius, radius * 2, radius * 2), sf);
                    sf.Dispose();
                }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            float scale = Math.Min(Width / 480f, Height / 285f);
            g.TranslateTransform((Width - 480 * scale) / 2, (Height - 285 * scale) / 2);
            g.ScaleTransform(scale, scale);
            using (LinearGradientBrush glow = new LinearGradientBrush(new Rectangle(50, 25, 380, 220), Color.FromArgb(28, 39, 55), T.Card, 90))
                g.FillEllipse(glow, 35, 20, 410, 245);
            GraphicsPath body = new GraphicsPath();
            body.AddBezier(133, 53, 93, 48, 70, 61, 58, 98);
            body.AddBezier(58, 98, 47, 135, 25, 210, 58, 231);
            body.AddBezier(58, 231, 79, 248, 109, 208, 133, 180);
            body.AddLine(133, 180, 347, 180);
            body.AddBezier(347, 180, 371, 208, 401, 248, 422, 231);
            body.AddBezier(422, 231, 455, 210, 433, 135, 422, 98);
            body.AddBezier(422, 98, 410, 61, 387, 48, 347, 53);
            body.CloseFigure();
            using (LinearGradientBrush b = new LinearGradientBrush(new Rectangle(40, 50, 400, 190), Color.FromArgb(47, 58, 81), Color.FromArgb(23, 31, 46), 90))
                g.FillPath(b, body);
            using (Pen p = new Pen(Color.FromArgb(75, 88, 114), 2))
                g.DrawPath(p, body);
            body.Dispose();
            T.Box(g, new RectangleF(177, 63, 126, 68), State.Has("Touch") ? Color.FromArgb(80, 76, 113) : Color.FromArgb(24, 32, 46), 10);
            T.Border(g, new RectangleF(177, 63, 126, 68), State.Has("Touch") ? T.Purple : Color.FromArgb(55, 66, 88), 10);
            using (Pen p = new Pen(T.Mint, 3))
                g.DrawLine(p, 197, 58, 283, 58);
            Disc(g, 124, 89, 12, State.Has("Up"), "↑", T.Muted);
            Disc(g, 124, 133, 12, State.Has("Down"), "↓", T.Muted);
            Disc(g, 102, 111, 12, State.Has("Left"), "←", T.Muted);
            Disc(g, 146, 111, 12, State.Has("Right"), "→", T.Muted);
            Disc(g, 365, 88, 15, State.Has("Triangle"), Kind == "PS4" ? "△" : "Y", T.Mint);
            Disc(g, 365, 140, 15, State.Has("Cross"), Kind == "PS4" ? "X" : "A", Color.FromArgb(141, 189, 255));
            Disc(g, 339, 114, 15, State.Has("Square"), Kind == "PS4" ? "□" : "X", T.Purple);
            Disc(g, 391, 114, 15, State.Has("Circle"), Kind == "PS4" ? "○" : "B", Color.FromArgb(250, 151, 175));
            Disc(g, 176, 165, 28, State.Has("L3"), null, T.Mint);
            Disc(g, 304, 165, 28, State.Has("R3"), null, T.Purple);
            Disc(g, 176 + (float)State.LX * 17, 165 - (float)State.LY * 17, 18, Math.Abs(State.LX) + Math.Abs(State.LY) > .05, null, T.Mint);
            Disc(g, 304 + (float)State.RX * 17, 165 - (float)State.RY * 17, 18, Math.Abs(State.RX) + Math.Abs(State.RY) > .05, null, T.Purple);
            Disc(g, 240, 155, 9, State.Has("PS"), "·", T.Muted);
            Disc(g, 157, 90, 7, State.Has("Share"), null, T.Mint);
            Disc(g, 323, 90, 7, State.Has("Options"), null, T.Mint);
            T.Box(g, new RectangleF(87, 39, 68, 9), State.Has("L1") ? T.Mint : T.Line, 4);
            T.Box(g, new RectangleF(325, 39, 68, 9), State.Has("R1") ? T.Mint : T.Line, 4);
            T.Box(g, new RectangleF(96, 23, 50, 10), State.Has("L2") ? T.Mint : Color.FromArgb(39, 49, 68), 4);
            T.Box(g, new RectangleF(334, 23, 50, 10), State.Has("R2") ? T.Purple : Color.FromArgb(39, 49, 68), 4);
            T.TextAt(g, "L2", 8, T.Muted, 109, 4, FontStyle.Regular);
            T.TextAt(g, "R2", 8, T.Muted, 347, 4, FontStyle.Regular);
        }
    }
}
