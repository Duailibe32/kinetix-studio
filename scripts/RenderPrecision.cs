using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
namespace KinetixStudio
{
    public static class RenderPrecision
    {
        [STAThread] public static void Main(string[] args)
        {
            Environment.SetEnvironmentVariable("KINETIX_DATA_DIR", Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "preview-data"));
            Native.SetProcessDPIAware(); Application.EnableVisualStyles(); Application.SetCompatibleTextRenderingDefault(false);
            Directory.CreateDirectory(args[0]);
            using (var window = new MainWindow(true))
            {
                window.Show(); Application.DoEvents(); window.Navigate(2); Application.DoEvents();
                var panel = (Panel)typeof(MainWindow).GetField("content", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(window);
                var precision = panel.Controls.Cast<Control>().Single(c => c.Controls.Cast<Control>().Any(label => label.Text == "Filtro de ruído"));
                precision.Controls.OfType<TrackBar>().Single(c => c.Minimum == 0 && c.Maximum == 30).Value = 12;
                precision.Controls.OfType<TrackBar>().Single(c => c.Minimum == 10 && c.Maximum == 100).Value = 70;
                var saved = Store.Current();
                if (saved.NoiseFilter != 12 || saved.AdsSensitivity != 70) throw new Exception("Sliders não salvaram os ajustes.");
                panel.AutoScrollPosition = new Point(0, 780); Application.DoEvents();
                using (var bitmap = new Bitmap(window.Width, window.Height)) { window.DrawToBitmap(bitmap, new Rectangle(0, 0, window.Width, window.Height)); bitmap.Save(Path.Combine(args[0], "precision-settings.png")); }
                window.Close();
            }
            using (var dialog = new PresetDialog())
            {
                dialog.Show(); Application.DoEvents();
                using (var bitmap = new Bitmap(dialog.Width, dialog.Height)) { dialog.DrawToBitmap(bitmap, new Rectangle(0, 0, dialog.Width, dialog.Height)); bitmap.Save(Path.Combine(args[0], "precision-presets.png")); }
                dialog.Controls.OfType<ActionButton>().Single(button => button.Text == "Mira estável").PerformClick();
                if (dialog.Selected != "Mira estável") throw new Exception("Preset não selecionado pelo botão.");
                dialog.Close();
            }
            T.DisposeFonts();
            File.WriteAllText(Path.Combine(args[0], "ui-validation.json"), "{\"sliderPersistence\":true,\"presetSelection\":true}");
        }
    }
}
