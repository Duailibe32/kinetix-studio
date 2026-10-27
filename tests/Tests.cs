using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;

namespace KinetixStudio
{
    public static class Tests
    {
        static int passed;
        static void Assert(bool condition, string name)
        {
            if (!condition)
                throw new Exception("FAIL: " + name);
            passed++;
            Console.WriteLine("PASS: " + name);
        }

        [STAThread]
        public static int Main(string[] args)
        {
            Environment.SetEnvironmentVariable("KINETIX_DATA_DIR", Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data"));
            if (args.Length == 2 && args[0] == "--fixture")
            {
                Profile fixture = Profile.Create();
                fixture.Name = "Installer validation";
                Directory.CreateDirectory(Path.Combine(args[1], "profiles"));
                File.WriteAllText(Path.Combine(args[1], "profiles", fixture.Id + ".json"), Store.Serialize(fixture));
                File.WriteAllText(Path.Combine(args[1], "selected.txt"), fixture.Id);
                File.WriteAllText(Path.Combine(args[1], "desktop.json"), "{\"MinimizeToTray\":true}");
                return 0;
            }

            try
            {
                Profile p = Profile.Create();
                p.Validate();
                bool[] keys = new bool[258];
                double sx = 0, sy = 0;
                keys[32] = true;
                PadState s = Mapper.Map(p, keys, 0, 0, .008, ref sx, ref sy);
                Assert(s.Has("Cross"), "ESPAÇO -> Cross");
                Assert((VirtualPad.ToDS4(s).Buttons & 32) != 0, "DS4 Cross bit 5");
                keys[32] = false;
                s = Mapper.Map(p, keys, 0, 0, .008, ref sx, ref sy);
                Assert(!s.Has("Cross") && (VirtualPad.ToDS4(s).Buttons & 32) == 0, "soltar ESPAÇO neutraliza Cross");
                Assert(VirtualPad.ToDS4(s).Buttons == 8, "DS4 direcional neutro 8");
                keys[87] = keys[68] = true;
                s = Mapper.Map(p, keys, 0, 0, .008, ref sx, ref sy);
                Assert(Math.Abs(s.LX * s.LX + s.LY * s.LY - 1) < .00001, "W+D diagonal normalizada");
                keys[65] = true;
                keys[83] = true;
                s = Mapper.Map(p, keys, 0, 0, .008, ref sx, ref sy);
                Assert(s.LX == 0 && s.LY == 0, "teclas opostas se cancelam");
                Array.Clear(keys, 0, keys.Length);
                p.Smoothing = 0;
                p.Deadzone = 0;
                s = Mapper.Map(p, keys, 30, -20, .008, ref sx, ref sy);
                Assert(s.RX > 0 && s.RY > 0, "mouse direita/cima -> RS direita/cima");
                p.InvertY = true;
                s = Mapper.Map(p, keys, 0, -20, .008, ref sx, ref sy);
                Assert(s.RY < 0, "inversão Y");
                AimRegression();
                p.MouseEnabled = false;
                s = Mapper.Map(p, keys, 100, 100, .008, ref sx, ref sy);
                Assert(s.RX == 0 && s.RY == 0, "mouse desativado neutraliza RS");
                p = Profile.Create();
                for (int i = 0; i < 600; i++)
                    s = Mapper.Map(p, keys, i == 0 ? 80 : 0, 0, .008, ref sx, ref sy);
                Assert(s.RX == 0 && s.RY == 0, "mouse volta ao centro sem movimento");
                keys[1] = keys[2] = true;
                s = Mapper.Map(p, keys, 0, 0, .008, ref sx, ref sy);
                Assert(VirtualPad.ToDS4(s).L2 == 255 && VirtualPad.ToDS4(s).R2 == 255, "mouse L/R -> gatilhos DS4");
                Assert(Marshal.SizeOf(typeof(Native.DS4Report)) == 10, "ABI DS4_REPORT: 10 bytes");
                Assert(Marshal.SizeOf(typeof(Native.XReport)) == 12, "ABI XUSB_REPORT: 12 bytes");
                Profile restored = Store.Deserialize(Store.Serialize(p));
                Assert(restored.Bindings.Count == 22 && restored.Name == p.Name, "perfil JSON round-trip");
                bool rejected = false;
                try
                {
                    restored.Bindings[0].Key = 123;
                    restored.Validate();
                }
                catch (InvalidDataException)
                {
                    rejected = true;
                }

                Assert(rejected, "F12 reservado");
                rejected = false;
                try
                {
                    restored = p.Copy();
                    restored.Bindings[0].Action = restored.Bindings[1].Action;
                    restored.Validate();
                }
                catch (InvalidDataException)
                {
                    rejected = true;
                }

                Assert(rejected, "ações duplicadas rejeitadas na importação");
                p = Profile.Create();
                p.Controller = "X360";
                Array.Clear(keys, 0, keys.Length);
                keys[84] = true;
                s = Mapper.Map(p, keys, 0, 0, .008, ref sx, ref sy);
                Assert(!s.Has("Touch"), "touchpad desativado no Xbox");
                string file = Path.Combine(Store.Root, "test-roundtrip.json");
                Directory.CreateDirectory(Store.Root);
                Store.AtomicWrite(file, Store.Serialize(p));
                Store.AtomicWrite(file, Store.Serialize(p));
                Assert(Store.Deserialize(File.ReadAllText(file)).Controller == "X360", "gravação atômica e substituição");
                File.Delete(file);
                p = Profile.Create();
                Array.Clear(keys, 0, keys.Length);
                keys[87] = keys[164] = true;
                s = Mapper.Map(p, keys, 0, 0, .008, ref sx, ref sy);
                Assert(Math.Abs(s.LY - .45) < .00001, "ALT + W caminha a 45%");
                p.WalkKey = 0;
                s = Mapper.Map(p, keys, 0, 0, .008, ref sx, ref sy);
                Assert(s.LY == 1, "caminhada pode ser desativada");
                Array.Clear(keys, 0, keys.Length);
                p = Profile.Create();
                p.Bindings.First(b => b.Action == "Triangle").Key = 256;
                keys[256] = true;
                s = Mapper.Map(p, keys, 0, 0, .008, ref sx, ref sy);
                Assert(s.Has("Triangle"), "roda para cima pode acionar Triangle");
                keys[256] = false;
                s = Mapper.Map(p, keys, 0, 0, .008, ref sx, ref sy);
                Assert(!s.Has("Triangle"), "pulso da roda pode ser neutralizado");
                p = Profile.Create();
                p.Smoothing = 0;
                p.Deadzone = 0;
                p.MouseStick = "Left";
                s = Mapper.Map(p, keys, 20, 0, .008, ref sx, ref sy);
                Assert(s.LX > 0 && s.RX == 0, "mouse pode controlar o analógico esquerdo");
                p.MouseStick = "Right";
                p.VerticalSensitivity = 50;
                sx = sy = 0;
                s = Mapper.Map(p, keys, 10, -10, .008, ref sx, ref sy);
                Assert(Math.Abs(s.RY / s.RX - .5) < .001, "proporção vertical independente");
                Profile racing = Profile.Preset("Corrida");
                racing.Validate();
                Array.Clear(keys, 0, keys.Length);
                keys[87] = true;
                s = Mapper.Map(racing, keys, 0, 0, .008, ref sx, ref sy);
                Assert(s.Has("R2") && s.LY == 0 && !racing.MouseEnabled, "preset Corrida: W acelera sem eixo vertical");
                Profile fps = Profile.Preset("Precisão FPS");
                fps.Validate();
                Assert(fps.Smoothing == 12 && fps.Curve == 125, "preset FPS preserva curva e suavização");
                string legacy = Store.Serialize(Profile.Create());
                foreach (string fieldName in new string[]
                {
                    "UpdateInterval",
                    "VerticalSensitivity",
                    "MouseStick",
                    "WalkKey",
                    "WalkPercent"
                }

                )
                    legacy = System.Text.RegularExpressions.Regex.Replace(legacy, ",\"" + fieldName + "\":(?:\"[^\"]*\"|[0-9]+)", "");
                Profile migrated = Store.Deserialize(legacy);
                Assert(migrated.UpdateInterval == 8 && migrated.MouseStick == "Right" && migrated.VerticalSensitivity == 100 && migrated.WalkKey == 0, "perfis 1.0 preservam ALT livre na migração");
                rejected = false;
                try
                {
                    p = Profile.Create();
                    p.UpdateInterval = 1;
                    p.Validate();
                }
                catch (InvalidDataException)
                {
                    rejected = true;
                }

                Assert(rejected, "intervalo fora de 4/8/16 ms rejeitado");
                rejected = false;
                try
                {
                    p = Profile.Create();
                    p.Id = "../escape";
                    Store.Delete(p);
                }
                catch (InvalidDataException)
                {
                    rejected = true;
                }

                Assert(rejected, "exclusão rejeita identificador com caminho");
                PadState first = new PadState();
                first.Buttons.Add("Cross");
                PadState second = first.Copy();
                Assert(InputEngine.Equivalent(first, second), "relatórios idênticos são reconhecidos");
                second.Buttons.Clear();
                Assert(!InputEngine.Equivalent(first, second), "soltura não é descartada pela otimização");
                PadState hat = new PadState();
                hat.Buttons.Add("Up");
                hat.Buttons.Add("Right");
                Assert((VirtualPad.ToDS4(hat).Buttons & 15) == 1, "DS4 diagonal cima/direita usa hat 1");
                string storageCase = Path.Combine(Store.Root, "storage-" + Guid.NewGuid().ToString("N")), legacyCase = Path.Combine(storageCase, "legacy"), newCase = Path.Combine(storageCase, "appdata");
                Directory.CreateDirectory(Path.Combine(legacyCase, "profiles"));
                Profile storageProfile = Profile.Create();
                storageProfile.Name = "Meu perfil preservado";
                string legacyProfile = Path.Combine(legacyCase, "profiles", storageProfile.Id + ".json");
                File.WriteAllText(legacyProfile, Store.Serialize(storageProfile));
                File.WriteAllText(Path.Combine(legacyCase, "selected.txt"), storageProfile.Id);
                File.WriteAllText(Path.Combine(legacyCase, "desktop.json"), "{\"MinimizeToTray\":true}");
                Assert(StoragePaths.Migrate(legacyCase, newCase) == 3, "migração AppData importa perfil, seleção e preferências");
                string imported = Path.Combine(newCase, "profiles", storageProfile.Id + ".json");
                Assert(File.ReadAllText(imported) == File.ReadAllText(legacyProfile), "migração mantém conteúdo e origem do perfil");
                File.WriteAllText(Path.Combine(newCase, "desktop.json"), "{\"MinimizeToTray\":false}");
                Assert(StoragePaths.Migrate(legacyCase, newCase) == 0 && File.ReadAllText(Path.Combine(newCase, "desktop.json")).Contains("false"), "migração é idempotente e preserva preferências novas");
                string conflictCase = Path.Combine(storageCase, "conflict");
                Directory.CreateDirectory(Path.Combine(conflictCase, "profiles"));
                Profile updated = storageProfile.Copy();
                updated.Name = "Atualizado";
                File.WriteAllText(Path.Combine(conflictCase, "profiles", updated.Id + ".json"), Store.Serialize(updated));
                StoragePaths.Migrate(legacyCase, conflictCase);
                Assert(Store.Deserialize(File.ReadAllText(Path.Combine(conflictCase, "profiles", updated.Id + ".json"))).Name == "Atualizado", "migração nunca sobrescreve um perfil existente");
                Assert(Directory.GetFiles(Path.Combine(conflictCase, "migration-backups")).Length > 0, "conflitos de migração ficam em backup separado");
                Assert(StoragePaths.Migrate(newCase, newCase) == 0, "migração não copia uma pasta sobre ela mesma");
                Assert(Object.ReferenceEquals(T.Font(10, System.Drawing.FontStyle.Regular), T.Font(10, System.Drawing.FontStyle.Regular)), "fontes GDI são reutilizadas");
                PadState empty = new PadState();
                Assert(empty.ButtonCount == 0 && !empty.Has("Cross") && empty.Copy().ButtonCount == 0, "estado neutro não aloca botões desnecessários");
                bool emptyRejected = false;
                try
                {
                    Store.Deserialize("null");
                }
                catch (InvalidDataException)
                {
                    emptyRejected = true;
                }

                Assert(emptyRejected, "perfil JSON nulo é rejeitado");
                Profile preserved = Profile.Create();
                preserved.Name = "Meu perfil anterior";
                preserved.Sensitivity = 83;
                preserved.Bindings.First(b => b.Action == "Cross").Key = 74;
                Profile renamedRoundTrip = Store.Deserialize(Store.Serialize(preserved));
                Assert(renamedRoundTrip.Id == preserved.Id && renamedRoundTrip.Name == preserved.Name && renamedRoundTrip.Sensitivity == 83 && renamedRoundTrip.Bindings.First(b => b.Action == "Cross").Key == 74, "troca de marca preserva identidade e mapeamentos do perfil");
                string upgrade = Path.Combine(Store.Root, "upgrade-test-" + Guid.NewGuid().ToString("N"));
                Directory.CreateDirectory(upgrade);
                string oldLog = Path.Combine(upgrade, "veylora.log"), newLog = Path.Combine(upgrade, Store.LogFileName);
                try
                {
                    File.WriteAllText(oldLog, "histórico anterior");
                    Store.MigrateLegacyLog(upgrade);
                    Assert(File.ReadAllText(oldLog) == "histórico anterior" && File.ReadAllText(newLog) == "histórico anterior", "migração copia histórico sem apagar original");
                    File.WriteAllText(newLog, "histórico atual");
                    Store.MigrateLegacyLog(upgrade);
                    Assert(File.ReadAllText(newLog) == "histórico atual", "migração não sobrescreve histórico existente");
                }
                finally
                {
                    File.Delete(oldLog);
                    File.Delete(newLog);
                    Directory.Delete(upgrade);
                }

                if (args.Contains("--integration"))
                    Integration();
                if (args.Contains("--input-integration"))
                    InputIntegration();
                Console.WriteLine("TOTAL: " + passed + " testes passaram.");
                return 0;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex);
                return 1;
            }
        }

        static void Integration()
        {
            // The independent consumer is XInput, not the serialization code under test.
            HashSet<uint> occupied = new HashSet<uint>();
            for (uint i = 0; i < 4; i++)
            {
                Native.XState before;
                if (Native.XInputGetState(i, out before) == 0)
                    occupied.Add(i);
            }

            using (VirtualPad pad = new VirtualPad("X360"))
            {
                uint slot = 99;
                for (int wait = 0; wait < 50 && slot == 99; wait++)
                {
                    for (uint i = 0; i < 4; i++)
                    {
                        Native.XState found;
                        if (!occupied.Contains(i) && Native.XInputGetState(i, out found) == 0)
                        {
                            slot = i;
                            break;
                        }
                    }

                    if (slot == 99)
                        Thread.Sleep(100);
                }

                Assert(slot < 4, "controle virtual Xbox enumerado via XInput");
                Profile p = Profile.Create();
                bool[] keys = new bool[258];
                keys[32] = keys[87] = true;
                double sx = 0, sy = 0;
                PadState s = Mapper.Map(p, keys, 0, 0, .008, ref sx, ref sy);
                pad.Send(s);
                Thread.Sleep(60);
                Native.XState read;
                Assert(Native.XInputGetState(slot, out read) == 0 && (read.Report.Buttons & 0x1000) != 0 && read.Report.LY == 32767, "XInput recebe ESPAÇO/A e W/LS cima");
                pad.Send(new PadState());
                Thread.Sleep(60);
                Assert(Native.XInputGetState(slot, out read) == 0 && read.Report.Buttons == 0 && read.Report.LY == 0, "XInput confirma soltura e centro");
            }

            using (VirtualPad pad = new VirtualPad("PS4"))
            {
                PadState s = new PadState();
                s.Buttons.Add("Cross");
                pad.Send(s);
                pad.Send(new PadState());
                Assert(true, "driver aceitou DS4 Cross e relatório neutro");
            }
        }

        [DllImport("user32.dll")]
        static extern void keybd_event(byte key, byte scan, uint flags, UIntPtr extra);
        [DllImport("user32.dll")]
        static extern void mouse_event(uint flags, int dx, int dy, uint data, UIntPtr extra);
        static void Pump(int milliseconds)
        {
            var watch = System.Diagnostics.Stopwatch.StartNew();
            while (watch.ElapsedMilliseconds < milliseconds)
            {
                System.Windows.Forms.Application.DoEvents();
                Thread.Sleep(2);
            }
        }

        static void AimRegression()
        {
            var p = Profile.Create();
            p.Smoothing = 0;
            p.Deadzone = 7;
            var keys = new bool[258];
            double sx = 0, sy = 0;
            var small = Mapper.Map(p, keys, .1, 0, .008, ref sx, ref sy);
            Assert(small.RX > 0 && small.RX < .01, "micromovimento não salta diretamente para 7% do analógico");
            sx = sy = 0;
            var diagonal = Mapper.Map(p, keys, 10, -.02, .008, ref sx, ref sy);
            Assert(diagonal.RX > 0 && diagonal.RY > 0, "direção rasa mantém eixo menor sem recorte independente");
            var filter = new MouseVelocityWindow();
            double min = Double.MaxValue, max = 0;
            bool diagonalPreserved = true;
            for (int i = 0; i < 40; i++)
            {
                double x = i % 2 == 0 ? 8 : 0, y = -x;
                filter.Apply(ref x, ref y, .004, true);
                if (i > 8) { min = Math.Min(min, x); max = Math.Max(max, x); }
                diagonalPreserved &= Math.Abs(x + y) < .000001;
            }
            Assert(diagonalPreserved, "filtro preserva proporção diagonal em toda a sequência");
            Assert(max - min < .000001 && min > 0, "mouse 125 Hz não alterna mira e neutro no motor de 250 Hz");
            for (int i = 0; i < 4; i++) { double x = 0, y = 0; filter.Apply(ref x, ref y, .004, true); if (i == 3) Assert(x == 0 && y == 0, "janela de movimento zera após 16 ms sem entrada"); }
            filter.Reset();
            double directX = 8, directY = -4;
            filter.Apply(ref directX, ref directY, .004, false);
            Assert(directX == 8 && directY == -4, "suavização zero preserva resposta direta sem janela adicional");
            filter.Reset();
            bool timeWeighted = true;
            foreach (double dt in new double[] { .005, .011, .004, .007, .009 })
            {
                double x = 1000 * dt, y = -500 * dt;
                filter.Apply(ref x, ref y, dt, true);
                timeWeighted &= Math.Abs(x / dt - 1000) < .000001 && Math.Abs(y / dt + 500) < .000001;
            }
            Assert(timeWeighted, "velocidade constante permanece igual com intervalos irregulares");
            filter.Reset();
            double resetX = 0, resetY = 0;
            filter.Apply(ref resetX, ref resetY, .004, true);
            Assert(resetX == 0 && resetY == 0, "reinício do filtro não reaproveita movimento anterior");
            sx = sy = 0;
            var full = Mapper.Map(p, keys, 10000, 0, .008, ref sx, ref sy);
            Assert(full.RX == 1 && full.RY == 0, "compensação gradual mantém alcance completo do analógico");
            p.Smoothing = 30;
            filter.Reset(); sx = sy = 0;
            PadState stopped = null;
            for (int i = 0; i < 70; i++)
            {
                double x = i < 8 ? 8 : 0, y = -x;
                filter.Apply(ref x, ref y, .008, true);
                stopped = Mapper.Map(p, keys, x, y, .008, ref sx, ref sy);
            }
            Assert(stopped.RX == 0 && stopped.RY == 0, "janela e suavização juntas voltam ao neutro sem entrada");
        }

        static void InputIntegration()
        {
            var occupied = new HashSet<uint>();
            for (uint i = 0; i < 4; i++)
            {
                Native.XState old;
                if (Native.XInputGetState(i, out old) == 0)
                    occupied.Add(i);
            }

            using (var form = new System.Windows.Forms.Form
            {
                Text = "Kinetix Studio input validation",
                Width = 320,
                Height = 160,
                TopMost = true,
                StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen
            }

            )
            using (var engine = new InputEngine())
            {
                form.Show();
                form.Activate();
                Pump(180);
                uint pid;var focusWait=System.Diagnostics.Stopwatch.StartNew();
                do{Native.GetWindowThreadProcessId(Native.GetForegroundWindow(),out pid);if(pid==(uint)System.Diagnostics.Process.GetCurrentProcess().Id)break;Pump(50);}while(focusWait.ElapsedMilliseconds<45000);
                if (pid != (uint)System.Diagnostics.Process.GetCurrentProcess().Id)
                    throw new InvalidOperationException("Teste de entrada não executado: a janela de validação não recebeu foco.");
                Profile p = Profile.Create();
                p.Controller = "X360";
                p.Smoothing = 30;
                p.Deadzone = 0;
                p.BlockMouse = false;
                System.Windows.Forms.Cursor.Position = form.PointToScreen(new System.Drawing.Point(80, 80));
                Pump(30);
                engine.TestInApp = true;
                engine.Start(p);
                Pump(180);
                uint slot = 99;
                for (uint i = 0; i < 4; i++)
                {
                    Native.XState found;
                    if (!occupied.Contains(i) && Native.XInputGetState(i, out found) == 0)
                    {
                        slot = i;
                        break;
                    }
                }

                if (slot > 3)
                    throw new Exception("Slot XInput não encontrado no teste de hooks.");
                try
                {
                    keybd_event(32, 0, 0, UIntPtr.Zero);
                    Pump(60);
                    Native.XState read;
                    Assert(Native.XInputGetState(slot, out read) == 0 && (read.Report.Buttons & 0x1000) != 0, "hook real de teclado converte ESPAÇO em A lido por XInput");
                    keybd_event(32, 0, 2, UIntPtr.Zero);
                    Pump(60);
                    Assert(Native.XInputGetState(slot, out read) == 0 && (read.Report.Buttons & 0x1000) == 0, "soltura real de teclado neutraliza o botão virtual");
                    bool moved = false;
                    int maxRX = 0, maxRY = 0;
                    var clock = System.Diagnostics.Stopwatch.StartNew();
                    while (clock.ElapsedMilliseconds < 300)
                    {
                        Native.GetWindowThreadProcessId(Native.GetForegroundWindow(), out pid);
                        if (pid != (uint)System.Diagnostics.Process.GetCurrentProcess().Id)
                            throw new InvalidOperationException("A janela de validação perdeu o foco durante o teste de mouse.");
                        mouse_event(1, 4, -4, 0, UIntPtr.Zero);
                        System.Windows.Forms.Application.DoEvents();
                        Native.XInputGetState(slot, out read);
                        maxRX = Math.Max(maxRX, read.Report.RX);
                        maxRY = Math.Max(maxRY, read.Report.RY);
                        if (Native.XInputGetState(slot, out read) == 0 && read.Report.RX > 0 && read.Report.RY > 0)
                        {
                            moved = true;
                            break;
                        }

                        Thread.Sleep(4);
                    }

                    Assert(moved, "hook real de mouse produz analógico direito lido por XInput (" + maxRX + ", " + maxRY + ")");
                }
                finally
                {
                    keybd_event(32, 0, 2, UIntPtr.Zero);
                    engine.Stop();
                    form.Close();
                }
            }
        }
    }
}
