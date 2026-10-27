using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

namespace KinetixStudio
{
    public static class Native
    {
        public const uint OK = 0x20000000;
        [DllImport("ViGEmClient.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr vigem_alloc();
        [DllImport("ViGEmClient.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void vigem_free(IntPtr client);
        [DllImport("ViGEmClient.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern uint vigem_connect(IntPtr client);
        [DllImport("ViGEmClient.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void vigem_disconnect(IntPtr client);
        [DllImport("ViGEmClient.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr vigem_target_ds4_alloc();
        [DllImport("ViGEmClient.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr vigem_target_x360_alloc();
        [DllImport("ViGEmClient.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void vigem_target_free(IntPtr target);
        [DllImport("ViGEmClient.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern uint vigem_target_add(IntPtr client, IntPtr target);
        [DllImport("ViGEmClient.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern uint vigem_target_remove(IntPtr client, IntPtr target);
        [DllImport("ViGEmClient.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern uint vigem_target_ds4_update(IntPtr client, IntPtr target, DS4Report report);
        [DllImport("ViGEmClient.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern uint vigem_target_x360_update(IntPtr client, IntPtr target, XReport report);
        [StructLayout(LayoutKind.Sequential)]
        public struct DS4Report
        {
            public byte LX, LY, RX, RY;
            public ushort Buttons;
            public byte Special, L2, R2;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct XReport
        {
            public ushort Buttons;
            public byte L2, R2;
            public short LX, LY, RX, RY;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct XState
        {
            public uint Packet;
            public XReport Report;
        }

        [DllImport("xinput1_4.dll")]
        public static extern uint XInputGetState(uint index, out XState state);
        public delegate IntPtr HookProc(int code, IntPtr w, IntPtr l);
        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr SetWindowsHookEx(int id, HookProc fn, IntPtr module, uint thread);
        [DllImport("user32.dll")]
        public static extern bool UnhookWindowsHookEx(IntPtr hook);
        [DllImport("user32.dll")]
        public static extern IntPtr CallNextHookEx(IntPtr hook, int code, IntPtr w, IntPtr l);
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        public static extern IntPtr GetModuleHandle(string name);
        [DllImport("user32.dll")]
        public static extern IntPtr GetForegroundWindow();
        [DllImport("user32.dll")]
        public static extern uint GetWindowThreadProcessId(IntPtr h, out uint pid);
        [DllImport("user32.dll")]
        public static extern bool GetCursorPos(out Point p);
        [DllImport("user32.dll")]
        public static extern short GetAsyncKeyState(int key);
        [DllImport("user32.dll")]
        public static extern uint MapVirtualKey(uint key, uint type);
        [DllImport("user32.dll")]
        public static extern bool SetProcessDPIAware();
        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        public static extern IntPtr SendMessage(IntPtr window, uint message, IntPtr wParam, string text);
        [StructLayout(LayoutKind.Sequential)]
        public struct Point
        {
            public int X, Y;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct KeyboardData
        {
            public uint Key, Scan, Flags, Time;
            public UIntPtr Extra;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct MouseData
        {
            public Point Pt;
            public uint Data, Flags, Time;
            public UIntPtr Extra;
        }
    }

    public sealed class VirtualPad : IDisposable
    {
        IntPtr client, target;
        bool added;
        public readonly string Kind;
        public VirtualPad(string kind)
        {
            Kind = kind;
            try
            {
                client = Native.vigem_alloc();
                if (client == IntPtr.Zero)
                    throw new Exception("Não foi possível reservar o cliente virtual.");
                Check(Native.vigem_connect(client), "conectar ao driver");
                target = kind == "PS4" ? Native.vigem_target_ds4_alloc() : Native.vigem_target_x360_alloc();
                if (target == IntPtr.Zero)
                    throw new Exception("Não foi possível criar o controle.");
                Check(Native.vigem_target_add(client, target), "adicionar o controle");
                added = true;
                Send(new PadState());
            }
            catch
            {
                Dispose();
                throw;
            }
        }

        public static void Check(uint error, string step)
        {
            if (error == Native.OK)
                return;
            string hint = error == 0xE0000001 ? "Driver ViGEmBus não encontrado." : error == 0xE0000008 ? "A versão do driver não é compatível." : error == 0xE0000009 ? "Acesso ao driver negado." : "Falha no controle virtual.";
            throw new InvalidOperationException(hint + " Não foi possível " + step + " (0x" + error.ToString("X8") + ").");
        }

        static byte Axis(double x)
        {
            return (byte)Math.Max(0, Math.Min(255, Math.Round((x + 1) * 127.5)));
        }

        static short Signed(double x)
        {
            return (short)Math.Round(Math.Max(-1, Math.Min(1, x)) * (x < 0 ? 32768 : 32767));
        }

        public static Native.DS4Report ToDS4(PadState s)
        {
            Native.DS4Report r = new Native.DS4Report
            {
                LX = Axis(s.LX),
                LY = Axis(-s.LY),
                RX = Axis(s.RX),
                RY = Axis(-s.RY)
            };
            int dx = (s.Has("Right") ? 1 : 0) - (s.Has("Left") ? 1 : 0), dy = (s.Has("Down") ? 1 : 0) - (s.Has("Up") ? 1 : 0);
            r.Buttons = (ushort)(dx == 0 && dy == 0 ? 8 : dx == 0 ? (dy < 0 ? 0 : 4) : dy == 0 ? (dx > 0 ? 2 : 6) : dx > 0 ? (dy < 0 ? 1 : 3) : (dy < 0 ? 7 : 5));
            string[] names =
            {
                "Square",
                "Cross",
                "Circle",
                "Triangle",
                "L1",
                "R1",
                "L2",
                "R2",
                "Share",
                "Options",
                "L3",
                "R3"
            };
            for (int i = 0; i < names.Length; i++)
                if (s.Has(names[i]))
                    r.Buttons |= (ushort)(1 << (i + 4));
            if (s.Has("PS"))
                r.Special |= 1;
            if (s.Has("Touch"))
                r.Special |= 2;
            r.L2 = s.Has("L2") ? (byte)255 : (byte)0;
            r.R2 = s.Has("R2") ? (byte)255 : (byte)0;
            return r;
        }

        public static Native.XReport ToXbox(PadState s)
        {
            Native.XReport r = new Native.XReport
            {
                LX = Signed(s.LX),
                LY = Signed(s.LY),
                RX = Signed(s.RX),
                RY = Signed(s.RY),
                L2 = s.Has("L2") ? (byte)255 : (byte)0,
                R2 = s.Has("R2") ? (byte)255 : (byte)0
            };
            string[] names =
            {
                "Up",
                "Down",
                "Left",
                "Right",
                "Options",
                "Share",
                "L3",
                "R3",
                "L1",
                "R1",
                "PS",
                "",
                "Cross",
                "Circle",
                "Square",
                "Triangle"
            };
            for (int i = 0; i < names.Length; i++)
                if (s.Has(names[i]))
                    r.Buttons |= (ushort)(1 << i);
            return r;
        }

        public void Send(PadState s)
        {
            if (added)
                Check(Kind == "PS4" ? Native.vigem_target_ds4_update(client, target, ToDS4(s)) : Native.vigem_target_x360_update(client, target, ToXbox(s)), "enviar os comandos");
        }

        public void Dispose()
        {
            if (added)
            {
                try
                {
                    Send(new PadState());
                }
                catch
                {
                }

                Native.vigem_target_remove(client, target);
                added = false;
            }

            if (target != IntPtr.Zero)
            {
                Native.vigem_target_free(target);
                target = IntPtr.Zero;
            }

            if (client != IntPtr.Zero)
            {
                Native.vigem_disconnect(client);
                Native.vigem_free(client);
                client = IntPtr.Zero;
            }
        }
    }

    public sealed class InputEngine : IDisposable
    {
        readonly object sync = new object ();
        readonly bool[] keys = new bool[258], mapped = new bool[258];
        readonly uint ownPid = (uint)Process.GetCurrentProcess().Id;
        readonly Queue<bool[]> edges = new Queue<bool[]>();
        string lastCommand = "nenhum", lastButton = "nenhum";
        Native.HookProc keyProc, mouseProc;
        IntPtr keyHook, mouseHook;
        VirtualPad pad;
        System.Threading.Timer timer;
        Profile profile;
        PadState state = new PadState();
        double dx, dy, sx, sy;
        long lastTick;
        int ticks;
        long rateStart;
        bool running, allowed;
        string gameName = "", gamePath = "";
        Native.Point lastMouse;
        int generation, tickBusy;
        IntPtr cachedWindow;
        uint cachedPid;
        bool cachedGameAllowed;
        long wheelUpUntil, wheelDownUntil, lastSend;
        PadState lastSent;
        public long ReportsSent { get; private set; }

        public event Action<string> Fault;
        public volatile bool TestInApp;
        public bool Running
        {
            get
            {
                lock (sync)
                    return running;
            }
        }

        public bool Capturing
        {
            get
            {
                lock (sync)
                    return running && allowed;
            }
        }

        public int ActualHz { get; private set; }

        public string LastCommand
        {
            get
            {
                lock (sync)
                    return lastCommand;
            }
        }

        public string LastButton
        {
            get
            {
                lock (sync)
                    return lastButton;
            }
        }

        public PadState State
        {
            get
            {
                lock (sync)
                    return state.Copy();
            }
        }

        public void Start(Profile p)
        {
            Stop();
            p.Validate();
            Profile copy = p.Copy();
            lock (sync)
            {
                profile = copy;
                gameName = String.IsNullOrEmpty(p.GamePath) ? "" : System.IO.Path.GetFileNameWithoutExtension(p.GamePath);
                gamePath = p.GamePath;
                Array.Clear(mapped, 0, mapped.Length);
                foreach (Binding b in profile.Bindings)
                    if (b.Key > 0 && !(p.Controller == "X360" && b.Action == "Touch"))
                        mapped[b.Key] = true;
                if (profile.WalkKey > 0)
                    mapped[profile.WalkKey] = true;
                try
                {
                    pad = new VirtualPad(p.Controller);
                    keyProc = Keyboard;
                    mouseProc = Mouse;
                    IntPtr module = Native.GetModuleHandle(null);
                    keyHook = Native.SetWindowsHookEx(13, keyProc, module, 0);
                    mouseHook = Native.SetWindowsHookEx(14, mouseProc, module, 0);
                    if (keyHook == IntPtr.Zero || mouseHook == IntPtr.Zero)
                        throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error(), "Não foi possível capturar teclado e mouse.");
                    Array.Clear(keys, 0, keys.Length);
                    dx = dy = sx = sy = 0;
                    Native.GetCursorPos(out lastMouse);
                    lastTick = rateStart = Stopwatch.GetTimestamp();
                    running = true;
                    allowed = false;
                    cachedWindow = IntPtr.Zero;
                    cachedPid = 0;
                    lastSent = null;
                    lastSend = 0;
                    ReportsSent = 0;
                    int run = ++generation;
                    timer = new System.Threading.Timer(delegate (object unused)
                    {
                        Tick(run);
                    }, null, 0, p.UpdateInterval);
                    Store.Log("Remapeamento iniciado: " + p.Controller);
                }
                catch
                {
                    Cleanup();
                    throw;
                }
            }
        }

        bool ForegroundAllowed()
        {
            IntPtr window = Native.GetForegroundWindow();
            if (window != cachedWindow)
            {
                cachedWindow = window;
                Native.GetWindowThreadProcessId(window, out cachedPid);
                cachedGameAllowed = false;
                if (cachedPid != 0 && cachedPid != ownPid)
                {
                    if (gameName.Length == 0)
                        cachedGameAllowed = true;
                    else
                        try
                        {
                            using (Process proc = Process.GetProcessById((int)cachedPid))
                                cachedGameAllowed = String.Equals(proc.ProcessName, gameName, StringComparison.OrdinalIgnoreCase) && String.Equals(System.IO.Path.GetFullPath(proc.MainModule.FileName), System.IO.Path.GetFullPath(gamePath), StringComparison.OrdinalIgnoreCase);
                        }
                        catch
                        {
                            cachedGameAllowed = false;
                        }
                }
            }

            return cachedPid == ownPid ? TestInApp : cachedGameAllowed;
        }

        bool IsMapped(int key)
        {
            return key > 0 && key < mapped.Length && mapped[key];
        }

        void QueueEdge()
        {
            if (edges.Count >= 512)
                edges.Clear();
            edges.Enqueue((bool[])keys.Clone());
        }

        void Submit(PadState report)
        {
            long now = Stopwatch.GetTimestamp();
            bool changed = lastSent == null || !Equivalent(lastSent, report);
            if (!changed && now - lastSend < Stopwatch.Frequency / 4)
                return;
            pad.Send(report);
            lastSend = now;
            lastSent = report;
            ReportsSent++;
            if (changed)
            {
                if (report.Buttons.Count > 0)
                {
                    List<string> labels = new List<string>();
                    foreach (string b in report.Buttons)
                        labels.Add(Catalog.Output(b, profile.Controller));
                    lastCommand = String.Join(" + ", labels.ToArray());
                    lastButton = lastCommand;
                }
                else if (Math.Abs(report.LX) + Math.Abs(report.LY) > .05)
                    lastCommand = "Analógico esquerdo";
                else if (Math.Abs(report.RX) + Math.Abs(report.RY) > .05)
                    lastCommand = "Analógico direito";
            }
        }

        public static bool Equivalent(PadState a, PadState b)
        {
            return a.LX == b.LX && a.LY == b.LY && a.RX == b.RX && a.RY == b.RY && a.Buttons.SetEquals(b.Buttons);
        }

        bool OwnForeground()
        {
            return cachedPid == ownPid;
        }

        IntPtr Keyboard(int code, IntPtr w, IntPtr l)
        {
            if (code >= 0)
            {
                Native.KeyboardData data = (Native.KeyboardData)Marshal.PtrToStructure(l, typeof(Native.KeyboardData));
                int k = (int)data.Key;
                if (k == 16)
                    k = (int)Native.MapVirtualKey(data.Scan, 3);
                if (k == 17)
                    k = (data.Flags & 1) != 0 ? 163 : 162;
                if (k == 18)
                    k = (data.Flags & 1) != 0 ? 165 : 164;
                int message = w.ToInt32();
                bool down = message == 0x100 || message == 0x104;
                bool up = message == 0x101 || message == 0x105;
                lock (sync)
                    if (running && k >= 0 && k < 256 && (down || up))
                    {
                        bool gate = ForegroundAllowed();
                        // Reserved hotkeys reach the independent safety hook.
                        if (k != 119 && k != 123)
                        {
                            bool was = keys[k];
                            keys[k] = gate && down;
                            if (gate && was != keys[k] && IsMapped(k))
                                QueueEdge();
                            if (gate && (profile.BlockMappedKeys || OwnForeground()) && IsMapped(k))
                                return (IntPtr)1;
                        }
                    }
            }

            return Native.CallNextHookEx(IntPtr.Zero, code, w, l);
        }

        IntPtr Mouse(int code, IntPtr w, IntPtr l)
        {
            if (code >= 0)
            {
                Native.MouseData data = (Native.MouseData)Marshal.PtrToStructure(l, typeof(Native.MouseData));
                int message = w.ToInt32();
                lock (sync)
                    if (running)
                    {
                        bool gate = ForegroundAllowed();
                        if (message == 0x200)
                        {
                            if (gate && profile.MouseEnabled)
                            {
                                Native.Point origin = lastMouse;
                                if (profile.BlockMouse && !OwnForeground())
                                    Native.GetCursorPos(out origin);
                                dx += data.Pt.X - origin.X;
                                dy += data.Pt.Y - origin.Y;
                            }

                            lastMouse = data.Pt;
                            if (gate && profile.MouseEnabled && profile.BlockMouse && !OwnForeground())
                                return (IntPtr)1;
                        }
                        else if (message == 0x20A)
                        {
                            int k = unchecked((short)(data.Data >> 16)) > 0 ? 256 : 257;
                            if (gate && IsMapped(k))
                            {
                                if (keys[k])
                                {
                                    keys[k] = false;
                                    QueueEdge();
                                }

                                keys[k] = true;
                                QueueEdge();
                                long until = Stopwatch.GetTimestamp() + Stopwatch.Frequency * 60 / 1000;
                                if (k == 256)
                                    wheelUpUntil = until;
                                else
                                    wheelDownUntil = until;
                                if (profile.BlockMappedKeys && !OwnForeground())
                                    return (IntPtr)1;
                            }
                        }
                        else
                        {
                            int k = 0;
                            bool down = false;
                            switch (message)
                            {
                                case 0x201:
                                    k = 1;
                                    down = true;
                                    break;
                                case 0x202:
                                    k = 1;
                                    break;
                                case 0x204:
                                    k = 2;
                                    down = true;
                                    break;
                                case 0x205:
                                    k = 2;
                                    break;
                                case 0x207:
                                    k = 4;
                                    down = true;
                                    break;
                                case 0x208:
                                    k = 4;
                                    break;
                                case 0x20B:
                                    k = (data.Data >> 16) == 1 ? 5 : 6;
                                    down = true;
                                    break;
                                case 0x20C:
                                    k = (data.Data >> 16) == 1 ? 5 : 6;
                                    break;
                            }

                            if (k > 0)
                            {
                                bool was = keys[k];
                                keys[k] = gate && down;
                                if (gate && was != keys[k] && IsMapped(k))
                                    QueueEdge();
                                if (gate && profile.BlockMappedKeys && IsMapped(k) && !OwnForeground())
                                    return (IntPtr)1;
                            }
                        }
                    }
            }

            return Native.CallNextHookEx(IntPtr.Zero, code, w, l);
        }

        void Tick(int run)
        {
            if (Interlocked.Exchange(ref tickBusy, 1) != 0)
                return;
            string error = null;
            try
            {
                lock (sync)
                {
                    if (!running || run != generation)
                        return;
                    try
                    {
                        long now = Stopwatch.GetTimestamp();
                        double dt = (now - lastTick) / (double)Stopwatch.Frequency;
                        lastTick = now;
                        bool gate = ForegroundAllowed();
                        if (gate != allowed)
                        {
                            Array.Clear(keys, 0, keys.Length);
                            edges.Clear();
                            dx = dy = sx = sy = 0;
                            Native.GetCursorPos(out lastMouse);
                        }

                        allowed = gate;
                        if (keys[256] && now >= wheelUpUntil)
                        {
                            keys[256] = false;
                            QueueEdge();
                        }

                        if (keys[257] && now >= wheelDownUntil)
                        {
                            keys[257] = false;
                            QueueEdge();
                        }

                        while (edges.Count > 0)
                        {
                            double ex = 0, ey = 0;
                            PadState edge = Mapper.Map(profile, edges.Dequeue(), 0, 0, dt, ref ex, ref ey);
                            edge.RX = state.RX;
                            edge.RY = state.RY;
                            Submit(edge);
                        }

                        state = gate ? Mapper.Map(profile, keys, dx, dy, dt, ref sx, ref sy) : new PadState();
                        dx = dy = 0;
                        Submit(state);
                        ticks++;
                        if (now - rateStart >= Stopwatch.Frequency)
                        {
                            ActualHz = (int)Math.Round(ticks * Stopwatch.Frequency / (double)(now - rateStart));
                            ticks = 0;
                            rateStart = now;
                        }
                    }
                    catch (Exception ex)
                    {
                        running = false;
                        allowed = false;
                        state = new PadState();
                        error = ex.Message;
                    }
                }
            }
            finally
            {
                Interlocked.Exchange(ref tickBusy, 0);
            }

            if (error != null)
            {
                Store.Log(error);
                Action<string> f = Fault;
                if (f != null)
                    f(error);
            }
        }

        void Cleanup()
        {
            running = false;
            allowed = false;
            generation++;
            if (timer != null)
            {
                timer.Dispose();
                timer = null;
            }

            if (keyHook != IntPtr.Zero)
            {
                Native.UnhookWindowsHookEx(keyHook);
                keyHook = IntPtr.Zero;
            }

            if (mouseHook != IntPtr.Zero)
            {
                Native.UnhookWindowsHookEx(mouseHook);
                mouseHook = IntPtr.Zero;
            }

            if (pad != null)
            {
                pad.Dispose();
                pad = null;
            }

            Array.Clear(keys, 0, keys.Length);
            edges.Clear();
            lastCommand = "nenhum";
            lastButton = "nenhum";
            state = new PadState();
            ActualHz = 0;
            ticks = 0;
            dx = dy = sx = sy = 0;
            wheelUpUntil = wheelDownUntil = 0;
        }

        public void Stop()
        {
            lock (sync)
                Cleanup();
        }

        public void Dispose()
        {
            Stop();
            GC.KeepAlive(keyProc);
            GC.KeepAlive(mouseProc);
        }
    }
}
