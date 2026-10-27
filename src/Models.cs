using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Windows.Forms;

namespace Veylora
{
    [DataContract]
    public class DesktopSettings
    {
        [DataMember]
        public bool MinimizeToTray;
        public static DesktopSettings Load()
        {
            try
            {
                string file = Path.Combine(Store.Root, "desktop.json");
                if (!File.Exists(file))
                    return new DesktopSettings();
                using (MemoryStream s = new MemoryStream(File.ReadAllBytes(file)))
                    return (DesktopSettings)new DataContractJsonSerializer(typeof(DesktopSettings)).ReadObject(s);
            }
            catch
            {
                return new DesktopSettings();
            }
        }

        public void Save()
        {
            Directory.CreateDirectory(Store.Root);
            using (MemoryStream s = new MemoryStream())
            {
                new DataContractJsonSerializer(typeof(DesktopSettings)).WriteObject(s, this);
                Store.AtomicWrite(Path.Combine(Store.Root, "desktop.json"), Encoding.UTF8.GetString(s.ToArray()));
            }
        }
    }

    [DataContract]
    public class Binding
    {
        [DataMember]
        public string Action;
        [DataMember]
        public int Key;
        public Binding()
        {
        }

        public Binding(string action, int key)
        {
            Action = action;
            Key = key;
        }
    }

    [DataContract]
    public class Profile
    {
        [DataMember]
        public int Version = 1;
        [DataMember]
        public string Id = Guid.NewGuid().ToString("N");
        [DataMember]
        public string Name = "Meu primeiro perfil";
        [DataMember]
        public string Controller = "PS4";
        [DataMember]
        public List<Binding> Bindings = new List<Binding>();
        [DataMember]
        public bool MouseEnabled = true;
        [DataMember]
        public bool BlockMappedKeys = true;
        [DataMember]
        public bool BlockMouse = true;
        [DataMember]
        public bool InvertY;
        [DataMember]
        public int Sensitivity = 65;
        [DataMember]
        public int Smoothing = 30;
        [DataMember]
        public int Deadzone = 5;
        [DataMember]
        public int Curve = 100;
        [DataMember]
        public string GamePath = "";
        [DataMember]
        public int UpdateInterval = 8;
        [DataMember]
        public int VerticalSensitivity = 100;
        [DataMember]
        public string MouseStick = "Right";
        [DataMember]
        public int WalkKey = 164;
        [DataMember]
        public int WalkPercent = 45;
        [OnDeserializing]
        void Defaults(StreamingContext context)
        {
            UpdateInterval = 8;
            VerticalSensitivity = 100;
            MouseStick = "Right";
            WalkKey = 0;
            WalkPercent = 45;
        }

        public static Profile Preset(string kind)
        {
            Profile p = Create();
            p.Name = kind;
            if (kind == "Precisão FPS")
            {
                p.Sensitivity = 45;
                p.Smoothing = 12;
                p.Curve = 125;
                p.Deadzone = 7;
            }

            if (kind == "Exploração")
            {
                p.Sensitivity = 75;
                p.Smoothing = 40;
                p.Curve = 100;
            }

            if (kind == "Corrida")
            {
                p.MouseEnabled = false;
                p.Bindings.First(b => b.Action == "LY+").Key = 0;
                p.Bindings.First(b => b.Action == "LY-").Key = 0;
                p.Bindings.First(b => b.Action == "R2").Key = 87;
                p.Bindings.First(b => b.Action == "L2").Key = 83;
            }

            return p;
        }

        public static Profile Create()
        {
            Profile p = new Profile();
            string[] actions =
            {
                "LX-",
                "LX+",
                "LY+",
                "LY-",
                "Cross",
                "Circle",
                "Square",
                "Triangle",
                "L1",
                "R1",
                "L2",
                "R2",
                "L3",
                "R3",
                "Share",
                "Options",
                "Up",
                "Down",
                "Left",
                "Right",
                "PS",
                "Touch"
            };
            int[] keys =
            {
                65,
                68,
                87,
                83,
                32,
                162,
                82,
                69,
                81,
                70,
                2,
                1,
                160,
                67,
                9,
                27,
                38,
                40,
                37,
                39,
                36,
                84
            };
            for (int i = 0; i < actions.Length; i++)
                p.Bindings.Add(new Binding(actions[i], keys[i]));
            return p;
        }

        public Profile Copy()
        {
            return Store.Deserialize(Store.Serialize(this));
        }

        public void Validate()
        {
            if (Version != 1)
                throw new InvalidDataException("Versão de perfil não compatível.");
            if (String.IsNullOrWhiteSpace(Name) || Name.Length > 60)
                throw new InvalidDataException("O nome deve ter de 1 a 60 caracteres.");
            if (Controller != "PS4" && Controller != "X360")
                throw new InvalidDataException("Controle desconhecido.");
            if (Bindings == null || Bindings.Count != Catalog.Actions.Length)
                throw new InvalidDataException("Lista de mapeamentos incompleta.");
            HashSet<string> found = new HashSet<string>();
            foreach (Binding b in Bindings)
            {
                if (b == null || !Catalog.Actions.Contains(b.Action) || !found.Add(b.Action) || b.Key < 0 || b.Key > 257 || b.Key == 255 || b.Key == 119 || b.Key == 123)
                    throw new InvalidDataException("Mapeamento inválido. F8 e F12 são reservados.");
            }

            if (Sensitivity < 5 || Sensitivity > 200 || Smoothing < 0 || Smoothing > 150 || Deadzone < 0 || Deadzone > 30 || Curve < 50 || Curve > 200)
                throw new InvalidDataException("Ajustes do mouse fora dos limites.");
            if (GamePath == null)
                GamePath = "";
            if (GamePath.Length > 512)
                throw new InvalidDataException("Caminho de jogo inválido.");
            if ((UpdateInterval != 4 && UpdateInterval != 8 && UpdateInterval != 16) || VerticalSensitivity < 25 || VerticalSensitivity > 200 || (MouseStick != "Right" && MouseStick != "Left") || WalkPercent < 10 || WalkPercent > 90 || WalkKey < 0 || WalkKey > 254 || WalkKey == 119 || WalkKey == 123)
                throw new InvalidDataException("Ajustes avançados inválidos.");
        }
    }

    public static class Catalog
    {
        public static readonly string[] Actions =
        {
            "LX-",
            "LX+",
            "LY+",
            "LY-",
            "Cross",
            "Circle",
            "Square",
            "Triangle",
            "L1",
            "R1",
            "L2",
            "R2",
            "L3",
            "R3",
            "Share",
            "Options",
            "Up",
            "Down",
            "Left",
            "Right",
            "PS",
            "Touch"
        };
        static readonly string[] Labels =
        {
            "Mover para a esquerda",
            "Mover para a direita",
            "Mover para a frente",
            "Mover para trás",
            "Pular / confirmar",
            "Voltar / agachar",
            "Recarregar / interagir",
            "Trocar / interagir",
            "Ombro esquerdo",
            "Ombro direito",
            "Gatilho esquerdo",
            "Gatilho direito",
            "Clique do analógico esquerdo",
            "Clique do analógico direito",
            "Compartilhar / selecionar",
            "Menu / pausar",
            "Direcional para cima",
            "Direcional para baixo",
            "Direcional para a esquerda",
            "Direcional para a direita",
            "Botão central",
            "Touchpad"
        };
        public static string Label(string a)
        {
            int i = Array.IndexOf(Actions, a);
            return i < 0 ? a : Labels[i];
        }

        public static string Output(string a, string type)
        {
            string[] ds =
            {
                "LS ←",
                "LS →",
                "LS ↑",
                "LS ↓",
                "X",
                "○",
                "□",
                "△",
                "L1",
                "R1",
                "L2",
                "R2",
                "L3",
                "R3",
                "SHARE",
                "OPTIONS",
                "D-PAD ↑",
                "D-PAD ↓",
                "D-PAD ←",
                "D-PAD →",
                "PS",
                "TOUCH"
            };
            string[] xb =
            {
                "LS ←",
                "LS →",
                "LS ↑",
                "LS ↓",
                "A",
                "B",
                "X",
                "Y",
                "LB",
                "RB",
                "LT",
                "RT",
                "LS",
                "RS",
                "BACK",
                "START",
                "D-PAD ↑",
                "D-PAD ↓",
                "D-PAD ←",
                "D-PAD →",
                "GUIDE",
                "—"
            };
            int i = Array.IndexOf(Actions, a);
            return i < 0 ? a : (type == "PS4" ? ds[i] : xb[i]);
        }

        public static string KeyName(int k)
        {
            switch (k)
            {
                case 0:
                    return "Sem tecla";
                case 1:
                    return "Mouse esquerdo";
                case 2:
                    return "Mouse direito";
                case 256:
                    return "Roda ↑";
                case 257:
                    return "Roda ↓";
                case 4:
                    return "Mouse central";
                case 5:
                    return "Mouse lateral 1";
                case 6:
                    return "Mouse lateral 2";
                case 32:
                    return "ESPAÇO";
                case 162:
                    return "CTRL esq.";
                case 163:
                    return "CTRL dir.";
                case 160:
                    return "SHIFT esq.";
                case 161:
                    return "SHIFT dir.";
                case 164:
                    return "ALT esq.";
                case 165:
                    return "ALT dir.";
                case 38:
                    return "↑";
                case 40:
                    return "↓";
                case 37:
                    return "←";
                case 39:
                    return "→";
            }

            return ((Keys)k).ToString().ToUpperInvariant();
        }
    }

    public static class Store
    {
        public static readonly string Root = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data");
        static string DirectoryPath
        {
            get
            {
                return Path.Combine(Root, "profiles");
            }
        }

        public static string Serialize(Profile p)
        {
            using (MemoryStream s = new MemoryStream())
            {
                new DataContractJsonSerializer(typeof(Profile)).WriteObject(s, p);
                return Encoding.UTF8.GetString(s.ToArray());
            }
        }

        public static Profile Deserialize(string json)
        {
            using (MemoryStream s = new MemoryStream(Encoding.UTF8.GetBytes(json)))
            {
                Profile p = (Profile)new DataContractJsonSerializer(typeof(Profile)).ReadObject(s);
                p.Validate();
                return p;
            }
        }

        public static void AtomicWrite(string path, string text)
        {
            string tmp = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try
            {
                File.WriteAllText(tmp, text, new UTF8Encoding(false));
                if (File.Exists(path))
                    File.Replace(tmp, path, null);
                else
                    File.Move(tmp, path);
            }
            finally
            {
                if (File.Exists(tmp))
                    File.Delete(tmp);
            }
        }

        public static void Save(Profile p)
        {
            p.Validate();
            Directory.CreateDirectory(DirectoryPath);
            if (String.IsNullOrEmpty(p.Id) || p.Id.Any(c => !Uri.IsHexDigit(c)) || p.Id.Length != 32)
                throw new InvalidDataException("Identificador inválido.");
            AtomicWrite(Path.Combine(DirectoryPath, p.Id + ".json"), Serialize(p));
            AtomicWrite(Path.Combine(Root, "selected.txt"), p.Id);
        }

        public static List<Profile> All()
        {
            Directory.CreateDirectory(DirectoryPath);
            List<Profile> result = new List<Profile>();
            foreach (string file in Directory.GetFiles(DirectoryPath, "*.json"))
            {
                try
                {
                    if (new FileInfo(file).Length > 65536)
                        throw new InvalidDataException("Perfil excede 64 KiB.");
                    Profile p = Deserialize(File.ReadAllText(file));
                    if (Path.GetFileNameWithoutExtension(file) == p.Id)
                        result.Add(p);
                }
                catch (Exception ex)
                {
                    Log("Perfil ignorado: " + Path.GetFileName(file) + " — " + ex.Message);
                }
            }

            return result.OrderBy(p => p.Name).ToList();
        }

        public static Profile Current()
        {
            List<Profile> ps = All();
            string id = File.Exists(Path.Combine(Root, "selected.txt")) ? File.ReadAllText(Path.Combine(Root, "selected.txt")) : "";
            Profile p = ps.FirstOrDefault(x => x.Id == id) ?? ps.FirstOrDefault();
            if (p == null)
            {
                p = Profile.Create();
                Save(p);
            }

            return p;
        }

        public static void Delete(Profile p)
        {
            if (p == null || String.IsNullOrEmpty(p.Id) || p.Id.Length != 32 || p.Id.Any(c => !Uri.IsHexDigit(c)))
                throw new InvalidDataException("Identificador inválido.");
            File.Delete(Path.Combine(DirectoryPath, p.Id + ".json"));
        }

        public static void Log(string msg)
        {
            try
            {
                Directory.CreateDirectory(Root);
                string f = Path.Combine(Root, "veylora.log");
                if (File.Exists(f) && new FileInfo(f).Length > 1048576)
                    File.Move(f, f + "." + DateTime.Now.ToString("yyyyMMddHHmmss"));
                File.AppendAllText(f, DateTime.Now.ToString("s") + " " + msg + Environment.NewLine);
            }
            catch
            {
            }
        }
    }

    public class PadState
    {
        public double LX, LY, RX, RY;
        public HashSet<string> Buttons = new HashSet<string>();
        public bool Has(string a)
        {
            return Buttons.Contains(a);
        }

        public PadState Copy()
        {
            return new PadState
            {
                LX = LX,
                LY = LY,
                RX = RX,
                RY = RY,
                Buttons = new HashSet<string>(Buttons)
            };
        }
    }

    public static class Mapper
    {
        public static PadState Map(Profile p, bool[] keys, double dx, double dy, double dt, ref double sx, ref double sy)
        {
            PadState state = new PadState();
            foreach (Binding b in p.Bindings)
                if (b.Key > 0 && keys[b.Key] && !(p.Controller == "X360" && b.Action == "Touch"))
                {
                    switch (b.Action)
                    {
                        case "LX-":
                            state.LX -= 1;
                            break;
                        case "LX+":
                            state.LX += 1;
                            break;
                        case "LY+":
                            state.LY += 1;
                            break;
                        case "LY-":
                            state.LY -= 1;
                            break;
                        default:
                            state.Buttons.Add(b.Action);
                            break;
                    }
                }

            double length = Math.Sqrt(state.LX * state.LX + state.LY * state.LY);
            if (length > 1)
            {
                state.LX /= length;
                state.LY /= length;
            }

            if (p.WalkKey > 0 && keys[p.WalkKey])
            {
                state.LX *= p.WalkPercent / 100.0;
                state.LY *= p.WalkPercent / 100.0;
            }

            if (p.MouseEnabled)
            {
                double x = dx / Math.Max(.001, dt) * p.Sensitivity / 180000.0;
                double y = -dy / Math.Max(.001, dt) * p.Sensitivity / 180000.0 * (p.InvertY ? -1 : 1) * p.VerticalSensitivity / 100.0;
                length = Math.Sqrt(x * x + y * y);
                if (length > 1)
                {
                    x /= length;
                    y /= length;
                }

                double alpha = p.Smoothing == 0 ? 1 : 1 - Math.Exp(-dt * 1000 / p.Smoothing);
                sx += alpha * (x - sx);
                sy += alpha * (y - sy);
                if (Math.Abs(sx) < .002)
                    sx = 0;
                if (Math.Abs(sy) < .002)
                    sy = 0;
                length = Math.Sqrt(sx * sx + sy * sy);
                if (length > 0)
                {
                    double magnitude = p.Deadzone / 100.0 + (1 - p.Deadzone / 100.0) * Math.Pow(Math.Min(1, length), p.Curve / 100.0);
                    double ox = sx / length * magnitude, oy = sy / length * magnitude;
                    if (p.MouseStick == "Left")
                    {
                        state.LX = ox;
                        state.LY = oy;
                    }
                    else
                    {
                        state.RX = ox;
                        state.RY = oy;
                    }
                }
            }
            else
            {
                sx = sy = 0;
            }

            return state;
        }
    }
}
