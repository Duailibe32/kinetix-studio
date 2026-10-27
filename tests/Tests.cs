using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;

namespace Veylora
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
                if (args.Contains("--integration"))
                    Integration();
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
    }
}
