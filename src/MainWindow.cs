using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Forms;

namespace KinetixStudio
{
    public class MainWindow : Form
    {
        Profile profile;
        InputEngine engine = new InputEngine();
        Panel sidebar, header, content;
        Label subtitle, statusLabel, footer, hzLabel, activityLabel, profileLabel;
        ActionButton power;
        List<ActionButton> nav = new List<ActionButton>();
        ControllerView controller;
        System.Windows.Forms.Timer refresh;
        int page;
        bool renderOnly;
        string driverStatus = "Verificando driver…";
        bool driverReady;
        DataGridView grid;
        Native.HookProc safetyProc;
        IntPtr safetyHook;
        bool f8Held, f12Held;
        DesktopSettings desktop = DesktopSettings.Load();
        NotifyIcon tray;
        System.Windows.Forms.Timer layoutTimer;
        int layoutWidth;
        Icon ownedIcon;
        int probing;
        public MainWindow(bool render)
        {
            renderOnly = render;
            Text = "Kinetix Studio — Seu movimento. Sua assinatura.";
            BackColor = T.Bg;
            ForeColor = T.Text;
            Font = T.Font(10, FontStyle.Regular);
            Size = new Size(1300, 870);
            MinimumSize = new Size(1024, 720);
            Rectangle desktopArea = Screen.FromPoint(Cursor.Position).WorkingArea;
            Size = new Size(Math.Min(1300, desktopArea.Width - 32), Math.Min(870, desktopArea.Height - 32));
            StartPosition = FormStartPosition.CenterScreen;
            AutoScaleMode = AutoScaleMode.Dpi;
            try
            {
                ownedIcon = EmbeddedIcon.Load();
                Icon = ownedIcon;
            }
            catch
            {
            }

            profile = Store.Current();
            sidebar = new Panel
            {
                Dock = DockStyle.Left,
                Width = 224,
                BackColor = T.Side,
                AutoScroll = true
            };
            Controls.Add(sidebar);
            Panel main = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = T.Bg
            };
            Controls.Add(main);
            main.BringToFront();
            header = new Panel
            {
                Dock = DockStyle.Top,
                Height = 93,
                BackColor = T.Bg
            };
            main.Controls.Add(header);
            content = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = T.Bg,
                AutoScroll = true,
                Padding = new Padding(28, 0, 28, 22)
            };
            main.Controls.Add(content);
            content.BringToFront();
            sidebar.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                Brand.Draw(e.Graphics, new RectangleF(15, 21, 62, 62), true);
                T.TextAt(e.Graphics, "KINETIX", 16, T.Text, 83, 24, FontStyle.Bold);
                T.TextAt(e.Graphics, "STUDIO", 9, T.Purple, 85, 56, FontStyle.Bold);
                T.TextAt(e.Graphics, "INPUT STUDIO", 7, T.Purple, 24, 102, FontStyle.Bold);
                using (Pen p = new Pen(T.Line))
                    e.Graphics.DrawLine(p, 24, 128, 200, 128);
            };
            string[] names =
            {
                "Meu estúdio",
                "Mapeamentos",
                "Mouse e mira",
                "Meus perfis",
                "Testar controle",
                "Preferências",
                "Diagnóstico"
            };
            for (int i = 0; i < names.Length; i++)
            {
                int index = i;
                ActionButton b = new ActionButton
                {
                    Text = names[i],
                    Navigation = true,
                    Location = new Point(14, 148 + i * 50),
                    Size = new Size(196, 43),
                    TabIndex = i
                };
                b.Click += (s, e) => Navigate(index);
                sidebar.Controls.Add(b);
                nav.Add(b);
            }

            SoftPanel tip = new SoftPanel
            {
                Location = new Point(16, 527),
                Size = new Size(192, 145),
                Fill = Color.FromArgb(17, 29, 42)
            };
            tip.Controls.Add(T.Label("NO SEU RITMO", 8, T.Mint, 15, 18, 150, 20));
            tip.Controls.Add(T.Label("F8   Ativar / pausar\n\nF12  Desligar agora", 9, T.Text, 15, 51, 150, 82));
            sidebar.Controls.Add(tip);
            sidebar.Controls.Add(T.Label("KINETIX STUDIO  /  " + Brand.Version, 7, T.Muted, 24, 704, 184, 22));
            Label title = T.Label("Estúdio de controle", 17, T.Text, 28, 22, 470, 31);
            title.Font = T.Font(17, FontStyle.Bold);
            header.Controls.Add(title);
            subtitle = T.Label("Teclado + mouse. Uma nova forma de jogar.", 9, T.Muted, 29, 55, 550, 22);
            header.Controls.Add(subtitle);
            power = new ActionButton
            {
                Text = "Ativar controle  ·  F8",
                Primary = true,
                Size = new Size(196, 43),
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            header.Controls.Add(power);
            power.Click += (s, e) => Toggle();
            header.Resize += (s, e) => power.Location = new Point(header.Width - 224, 25);
            engine.Fault += (message) =>
            {
                OnUi(() =>
                {
                    engine.Stop();
                    SetPower();
                    Notify(message, true);
                });
            };
            refresh = new System.Windows.Forms.Timer
            {
                Interval = 50
            };
            refresh.Tick += (s, e) => RefreshLive();
            layoutTimer = new System.Windows.Forms.Timer
            {
                Interval = 180
            };
            layoutTimer.Tick += (s, e) =>
            {
                layoutTimer.Stop();
                if (Math.Abs(content.ClientSize.Width - layoutWidth) > 28)
                    Navigate(page);
            };
            SizeChanged += (s, e) =>
            {
                if (!renderOnly)
                {
                    layoutTimer.Stop();
                    layoutTimer.Start();
                }

                if (WindowState == FormWindowState.Minimized && desktop.MinimizeToTray && !renderOnly)
                    Hide();
                RefreshLive();
                UpdateRefresh();
            };
            VisibleChanged += (s, e) => UpdateRefresh();
            FormClosed += (s, e) =>
            {
                if (ownedIcon != null)
                {
                    ownedIcon.Dispose();
                    ownedIcon = null;
                }
            };
            FormClosing += (s, e) =>
            {
                engine.Dispose();
                refresh.Stop();
                refresh.Dispose();
                layoutTimer.Dispose();
                if (tray != null)
                {
                    ContextMenuStrip menu = tray.ContextMenuStrip;
                    tray.Visible = false;
                    tray.Dispose();
                    if (menu != null)
                        menu.Dispose();
                }

                if (safetyHook != IntPtr.Zero)
                {
                    Native.UnhookWindowsHookEx(safetyHook);
                    safetyHook = IntPtr.Zero;
                }

                try
                {
                    Store.Save(profile);
                }
                catch (Exception ex)
                {
                    Store.Log(ex.Message);
                }

                GC.KeepAlive(safetyProc);
            };
            Shown += (s, e) =>
            {
                power.Location = new Point(header.Width - 224, 25);
                if (!renderOnly)
                {
                    safetyProc = SafetyKeyboard;
                    safetyHook = Native.SetWindowsHookEx(13, safetyProc, Native.GetModuleHandle(null), 0);
                    if (safetyHook == IntPtr.Zero)
                    {
                        power.Enabled = false;
                        Notify("Não foi possível preparar os atalhos de segurança. Reabra o Kinetix Studio.", true);
                    }

                    Probe();
                }

                UpdateRefresh();
            };
            if (!renderOnly)
            {
                ContextMenuStrip menu = new ContextMenuStrip();
                menu.Items.Add("Abrir Kinetix Studio", null, (s, e) => RestoreWindow());
                menu.Items.Add("Ativar / pausar · F8", null, (s, e) => Toggle());
                menu.Items.Add("Desligar controle · F12", null, (s, e) =>
                {
                    engine.Stop();
                    SetPower();
                });
                menu.Items.Add(new ToolStripSeparator());
                menu.Items.Add("Sair", null, (s, e) => Close());
                tray = new NotifyIcon
                {
                    Icon = Icon,
                    Text = "Kinetix Studio · Controle desativado",
                    Visible = true,
                    ContextMenuStrip = menu
                };
                tray.DoubleClick += (s, e) => RestoreWindow();
            }

            Navigate(0);
        }

        void RestoreWindow()
        {
            Show();
            WindowState = FormWindowState.Normal;
            Activate();
            RefreshLive();
            UpdateRefresh();
        }

        void UpdateRefresh()
        {
            if (refresh == null || IsDisposed || Disposing)
                return;
            if (renderOnly || !Visible || WindowState == FormWindowState.Minimized || !engine.Running || (page != 0 && page != 4))
            {
                refresh.Stop();
                return;
            }

            refresh.Interval = page == 4 ? 50 : 200;
            refresh.Start();
        }

        void OnUi(Action action)
        {
            if (IsDisposed || Disposing || !IsHandleCreated)
                return;
            try
            {
                BeginInvoke((Action)(() =>
                {
                    if (!IsDisposed && !Disposing)
                        action();
                }));
            }
            catch (InvalidOperationException)
            {
            }
        }

        IntPtr SafetyKeyboard(int code, IntPtr w, IntPtr l)
        {
            if (code >= 0)
            {
                Native.KeyboardData d = (Native.KeyboardData)System.Runtime.InteropServices.Marshal.PtrToStructure(l, typeof(Native.KeyboardData));
                int msg = w.ToInt32();
                bool down = msg == 0x100 || msg == 0x104, up = msg == 0x101 || msg == 0x105;
                if (d.Key == 119 || d.Key == 123)
                {
                    bool held = d.Key == 119 ? f8Held : f12Held;
                    if (down && !held && !IsDisposed)
                        BeginInvoke((Action)(() =>
                        {
                            if (d.Key == 123)
                            {
                                engine.Stop();
                                SetPower();
                                Notify("Controle desligado. Teclado e mouse liberados.", false);
                            }
                            else if (OwnedForms.Length == 0)
                                Toggle();
                        }));
                    if (down || up)
                    {
                        if (d.Key == 119)
                            f8Held = down;
                        else
                            f12Held = down;
                    }

                    return (IntPtr)1;
                }
            }

            return Native.CallNextHookEx(IntPtr.Zero, code, w, l);
        }

        void Probe()
        {
            if (Interlocked.Exchange(ref probing, 1) != 0)
                return;
            System.Threading.Tasks.Task.Run(() =>
            {
                string result;
                bool ready = false;
                IntPtr c = IntPtr.Zero;
                try
                {
                    c = Native.vigem_alloc();
                    if (c == IntPtr.Zero)
                        throw new Exception("Falha ao reservar o cliente.");
                    VirtualPad.Check(Native.vigem_connect(c), "verificar o driver");
                    ready = true;
                    result = "Driver conectado";
                }
                catch (DllNotFoundException)
                {
                    result = "Biblioteca ViGEmClient.dll ausente";
                }
                catch (Exception ex)
                {
                    result = ex.Message;
                }
                finally
                {
                    if (c != IntPtr.Zero)
                    {
                        Native.vigem_disconnect(c);
                        Native.vigem_free(c);
                    }
                }

                Interlocked.Exchange(ref probing, 0);
                OnUi(() =>
                {
                    driverReady = ready;
                    driverStatus = result;
                    if (page == 0 || page == 6)
                        Navigate(page);
                });
            });
        }

        void Toggle()
        {
            if (!power.Enabled)
                return;
            try
            {
                if (engine.Running)
                {
                    engine.Stop();
                    Notify("Remapeamento pausado. Suas teclas estão livres.", false);
                }
                else
                {
                    Store.Save(profile);
                    engine.TestInApp = page == 4;
                    engine.Start(profile);
                    Notify(page == 4 ? "Teste ativo. Pressione as teclas para ver a resposta." : "Controle ativado. Volte ao jogo para usar. F8 pausa; F12 desliga.", false);
                }
            }
            catch (Exception ex)
            {
                engine.Stop();
                Store.Log(ex.ToString());
                Notify(ex is DllNotFoundException ? "ViGEmClient.dll não encontrada. Use a pasta completa do Kinetix Studio." : ex.Message, true);
            }

            SetPower();
        }

        void SetPower()
        {
            power.Text = engine.Running ? "Pausar controle  ·  F8" : "Ativar controle  ·  F8";
            power.Primary = !engine.Running;
            power.Invalidate();
            if (tray != null)
                tray.Text = engine.Running ? "Kinetix Studio · Controle ativo" : "Kinetix Studio · Controle desativado";
            RefreshLive();
            UpdateRefresh();
        }

        void Changed()
        {
            if (engine.Running)
            {
                engine.Stop();
                SetPower();
                Notify("Ajuste salvo. Ative o controle para usar a nova configuração.", false);
            }

            try
            {
                Store.Save(profile);
            }
            catch (Exception ex)
            {
                Notify("Não foi possível salvar: " + ex.Message, true);
            }
        }

        void Notify(string message, bool error)
        {
            subtitle.Text = message;
            subtitle.ForeColor = error ? Color.FromArgb(250, 151, 175) : T.Mint;
            Store.Log(message);
        }

        public void Navigate(int index)
        {
            page = index;
            layoutWidth = content.ClientSize.Width;
            engine.TestInApp = index == 4;
            content.SuspendLayout();
            content.AutoScrollMinSize = Size.Empty;
            content.AutoScrollPosition = Point.Empty;
            controller = null;
            statusLabel = null;
            hzLabel = null;
            activityLabel = null;
            footer = null;
            profileLabel = null;
            grid = null;
            foreach (Control child in content.Controls.Cast<Control>().ToArray())
                child.Dispose();
            for (int i = 0; i < nav.Count; i++)
            {
                nav[i].Selected = i == index;
                nav[i].Invalidate();
            }

            switch (index)
            {
                case 0:
                    Dashboard();
                    break;
                case 1:
                    Mappings();
                    break;
                case 2:
                    MouseSettings();
                    break;
                case 3:
                    Profiles();
                    break;
                case 4:
                    Tester();
                    break;
                case 5:
                    Preferences();
                    break;
                case 6:
                    Diagnostics();
                    break;
            }

            content.ResumeLayout();
            RefreshLive();
            UpdateRefresh();
        }

        int Available
        {
            get
            {
                return Math.Max(790, content.ClientSize.Width - 56);
            }
        }

        SoftPanel Card(int x, int y, int w, int h)
        {
            SoftPanel c = new SoftPanel
            {
                Location = new Point(x, y),
                Size = new Size(w, h)
            };
            content.Controls.Add(c);
            return c;
        }

        void Heading(string kicker, string title, string note)
        {
            content.Controls.Add(T.Label(kicker, 8, T.Mint, 28, 6, Available, 20));
            int titleSize = Available < 850 ? 21 : 25;
            Label h = T.Label(title, titleSize, T.Text, 25, 30, Available, 48);
            h.Font = T.Font(titleSize, FontStyle.Bold);
            content.Controls.Add(h);
            content.Controls.Add(T.Label(note, 10, T.Muted, 28, 86, Available, 30));
        }

        ActionButton Button(Control parent, string text, int x, int y, int w, Action click, bool primary)
        {
            ActionButton b = new ActionButton
            {
                Text = text,
                Location = new Point(x, y),
                Width = w,
                Primary = primary
            };
            parent.Controls.Add(b);
            b.Click += (s, e) => click();
            return b;
        }

        void Dashboard()
        {
            int w = Available;
            Heading("BEM-VINDO AO KINETIX STUDIO", "Cada movimento tem a sua assinatura.", "Um estúdio para transformar teclado e mouse no controle que combina com você.");
            int left = (int)(w * .56);
            SoftPanel hero = Card(28, 132, left, 299);
            hero.Decorative = true;
            hero.Controls.Add(T.Label("SEU SETUP. SUAS REGRAS.", 8, T.Purple, 23, 22, left - 46, 20));
            Label lead = T.Label("Seu movimento.\nSua assinatura.", 25, T.Text, 20, 59, left - 40, 106);
            lead.Font = T.Font(25, FontStyle.Bold);
            lead.AutoSize = true;
            hero.Controls.Add(lead);
            hero.Controls.Add(T.Label("ESPAÇO vira " + (profile.Controller == "PS4" ? "X" : "A") + ". WASD vira o analógico.\nO mouse assume a câmera. Você assume o jogo.", 10, T.Muted, 23, 168, left - 46, 53));
            Button(hero, "Personalizar teclas  →", 23, 238, 225, () => Navigate(1), true);
            SoftPanel pad = Card(28 + left + 18, 132, w - left - 18, 299);
            pad.Controls.Add(T.Label(profile.Controller == "PS4" ? "DUALSHOCK 4 VIRTUAL" : "XBOX 360 VIRTUAL", 8, T.Muted, 22, 22, pad.Width - 44, 20));
            controller = new ControllerView
            {
                Location = new Point(8, 56),
                Size = new Size(pad.Width - 16, 213),
                Kind = profile.Controller
            };
            pad.Controls.Add(controller);
            int gap = 16, cw = (w - gap * 2) / 3;
            SoftPanel p = Card(28, 449, cw, 127);
            p.Controls.Add(T.Label("PERFIL ATUAL", 8, T.Muted, 20, 17, cw - 40, 19));
            profileLabel = T.Label(profile.Name, 14, T.Text, 19, 48, cw - 38, 29);
            profileLabel.Font = T.Font(14, FontStyle.Bold);
            p.Controls.Add(profileLabel);
            p.Controls.Add(T.Label(profile.Controller == "PS4" ? "PlayStation 4  ·  " + profile.Bindings.Count + " ações" : "Xbox 360  ·  " + (profile.Bindings.Count - 1) + " ações", 9, T.Purple, 20, 87, cw - 40, 20));
            SoftPanel s = Card(28 + cw + gap, 449, cw, 127);
            s.Controls.Add(T.Label("CONTROLE VIRTUAL", 8, T.Muted, 20, 17, cw - 40, 19));
            statusLabel = T.Label("Desativado", 14, T.Text, 19, 48, cw - 38, 29);
            statusLabel.Font = T.Font(14, FontStyle.Bold);
            s.Controls.Add(statusLabel);
            s.Controls.Add(T.Label(renderOnly ? "Verificação ao abrir o aplicativo" : driverReady ? "●  Driver pronto para jogar" : driverStatus, 8, driverReady ? T.Mint : T.Muted, 20, 87, cw - 40, 30));
            SoftPanel rate = Card(28 + (cw + gap) * 2, 449, w - (cw + gap) * 2, 127);
            rate.Controls.Add(T.Label("RESPOSTA DO MOTOR", 8, T.Muted, 20, 17, cw - 40, 19));
            hzLabel = T.Label("— Hz", 20, T.Text, 18, 43, cw - 36, 38);
            hzLabel.Font = T.Font(20, FontStyle.Bold);
            rate.Controls.Add(hzLabel);
            rate.Controls.Add(T.Label("Ciclos medidos por segundo", 8, T.Muted, 20, 87, cw - 40, 20));
            footer = T.Label("F8 para ativar ou pausar. F12 para desligar imediatamente. A captura pausa enquanto você edita no Kinetix Studio.", 9, T.Muted, 29, 598, w, 45);
            content.Controls.Add(footer);
        }

        void Mappings()
        {
            int w = Available;
            Heading("CADA TECLA, UMA POSSIBILIDADE", "Seu mapa de movimentos.", "Clique em uma tecla para alterar. F8 e F12 ficam reservados para você manter o controle.");
            SoftPanel top = Card(28, 129, w, 65);
            TextBox search = T.Input("", w - 235);
            search.Location = new Point(20, 20);
            search.HandleCreated += (s, e) => Native.SendMessage(search.Handle, 0x1501, (IntPtr)1, "Buscar ação, tecla ou botão…");
            search.AccessibleName = "Buscar mapeamento";
            top.Controls.Add(search);
            Button(top, "Restaurar padrão", w - 190, 11, 170, () =>
            {
                if (MessageBox.Show(this, "Restaurar todas as teclas deste perfil?", "Kinetix Studio", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    profile.Bindings = Profile.Create().Bindings;
                    Changed();
                    Navigate(1);
                }
            }, false);
            grid = new DataGridView
            {
                Location = new Point(28, 210),
                Size = new Size(w, 394),
                BackgroundColor = T.Card,
                BorderStyle = BorderStyle.None,
                GridColor = T.Line,
                RowHeadersVisible = false,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToResizeRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                EnableHeadersVisualStyles = false,
                ScrollBars = ScrollBars.Vertical
            };
            grid.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = T.Card,
                ForeColor = T.Muted,
                Font = T.Font(9, FontStyle.Bold),
                Padding = new Padding(12, 0, 0, 0)
            };
            grid.ColumnHeadersHeight = 42;
            grid.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.Single;
            grid.DefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = T.Card,
                ForeColor = T.Text,
                SelectionBackColor = Color.FromArgb(44, 45, 70),
                SelectionForeColor = T.Text,
                Font = T.Font(10, FontStyle.Regular),
                Padding = new Padding(12, 0, 0, 0)
            };
            grid.RowTemplate.Height = 46;
            grid.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
            grid.Columns.Add("action", "AÇÃO");
            grid.Columns.Add("key", "ENTRADA · CLIQUE PARA ALTERAR");
            grid.Columns.Add("output", "SAÍDA");
            grid.Columns[0].FillWeight = 45;
            grid.Columns[1].FillWeight = 34;
            grid.Columns[2].FillWeight = 21;
            foreach (Binding b in profile.Bindings)
            {
                int i = grid.Rows.Add(Catalog.Label(b.Action), Catalog.KeyName(b.Key), Catalog.Output(b.Action, profile.Controller));
                grid.Rows[i].Tag = b;
                grid.Rows[i].Cells[1].Style.ForeColor = T.Mint;
                grid.Rows[i].Cells[2].Style.ForeColor = T.Purple;
                if (profile.Controller == "X360" && b.Action == "Touch")
                    grid.Rows[i].DefaultCellStyle.ForeColor = T.Muted;
            }

            search.TextChanged += (s, e) =>
            {
                if (grid == null)
                    return;
                grid.CurrentCell = null;
                string query = search.Text.Trim();
                foreach (DataGridViewRow row in grid.Rows)
                    row.Visible = query.Length == 0 || String.Join(" ", row.Cells.Cast<DataGridViewCell>().Select(cell => Convert.ToString(cell.Value)).ToArray()).IndexOf(query, StringComparison.CurrentCultureIgnoreCase) >= 0;
            };
            grid.CellClick += (s, e) =>
            {
                if (e.RowIndex < 0)
                    return;
                Binding b = (Binding)grid.Rows[e.RowIndex].Tag;
                if (profile.Controller == "X360" && b.Action == "Touch")
                {
                    Notify("O controle Xbox 360 não tem touchpad. Use um perfil PS4 para essa ação.", false);
                    return;
                }

                engine.Stop();
                SetPower();
                using (CaptureDialog d = new CaptureDialog(Catalog.Label(b.Action)))
                {
                    if (d.ShowDialog(this) == DialogResult.OK)
                    {
                        b.Key = d.Captured;
                        Changed();
                        Navigate(1);
                    }
                }
            };
            content.Controls.Add(grid);
            content.Controls.Add(T.Label("Dica: uma tecla pode acionar mais de um botão. Mouse → analógico direito é ajustado em Mouse e analógicos.", 9, T.Muted, 29, 622, w, 42));
        }

        void MouseSettings()
        {
            int w = Available;
            Heading("PRECISÃO QUE ACOMPANHA VOCÊ", "Encontre a sua sensibilidade.", "O mouse controla o analógico direito. WASD controla o esquerdo, com diagonais normalizadas.");
            SoftPanel card = Card(28, 132, w, 435);
            CheckBox enabled = Check(card, "Usar mouse para controlar a câmera", 22, 20, profile.MouseEnabled);
            enabled.CheckedChanged += (s, e) =>
            {
                profile.MouseEnabled = enabled.Checked;
                Changed();
            };
            Slider(card, "Sensibilidade", "Velocidade de resposta do analógico ao movimento do mouse.", 5, 200, profile.Sensitivity, 22, 76, v =>
            {
                profile.Sensitivity = v;
                Changed();
            }, "%");
            Slider(card, "Suavização", "Reduz movimentos bruscos. Valores maiores deixam a resposta mais gradual.", 0, 150, profile.Smoothing, 22, 157, v =>
            {
                profile.Smoothing = v;
                Changed();
            }, " ms");
            Slider(card, "Zona morta de saída", "Compensa jogos que ignoram movimentos pequenos do analógico.", 0, 30, profile.Deadzone, 22, 238, v =>
            {
                profile.Deadzone = v;
                Changed();
            }, "%");
            Slider(card, "Curva de resposta", "100% é linear. Acima disso, movimentos pequenos ficam mais delicados.", 50, 200, profile.Curve, 22, 319, v =>
            {
                profile.Curve = v;
                Changed();
            }, "%");
            SoftPanel bottom = Card(28, 585, w, 73);
            CheckBox inv = Check(bottom, "Inverter eixo vertical (Y)", 22, 23, profile.InvertY);
            inv.CheckedChanged += (s, e) =>
            {
                profile.InvertY = inv.Checked;
                Changed();
            };
            SoftPanel advanced = Card(28, 675, w, 273);
            Slider(advanced, "Proporção vertical", "Ajuste o eixo Y sem mudar a sensibilidade horizontal.", 25, 200, profile.VerticalSensitivity, 22, 20, v =>
            {
                profile.VerticalSensitivity = v;
                Changed();
            }, "%");
            Slider(advanced, "Intensidade ao caminhar", "Segure ALT esquerdo para reduzir o deslocamento de WASD.", 10, 90, profile.WalkPercent, 22, 101, v =>
            {
                profile.WalkPercent = v;
                Changed();
            }, "%");
            advanced.Controls.Add(T.Label("Mouse controla", 10, T.Muted, 22, 201, 170, 25));
            ComboBox stick = Combo(advanced, 198, 197, 270, new string[] { "Analógico direito · câmera", "Analógico esquerdo · movimento" }, profile.MouseStick == "Right" ? 0 : 1);
            stick.SelectedIndexChanged += (s, e) =>
            {
                profile.MouseStick = stick.SelectedIndex == 0 ? "Right" : "Left";
                Changed();
            };
            Button(advanced, "Tecla de caminhada: " + Catalog.KeyName(profile.WalkKey), 22, 233, 330, () =>
            {
                engine.Stop();
                SetPower();
                using (CaptureDialog d = new CaptureDialog("Modificador de caminhada"))
                {
                    if (d.ShowDialog(this) == DialogResult.OK)
                    {
                        if (d.Captured > 254)
                        {
                            Notify("Escolha uma tecla ou botão; a roda não pode manter a caminhada.", true);
                            return;
                        }

                        profile.WalkKey = d.Captured;
                        Changed();
                        Navigate(2);
                    }
                }
            }, false);
            advanced.Height = 300;
            SoftPanel precision = Card(28, 1000, w, 270);
            Slider(precision, "Filtro de ruído", "Atenua pequenas oscilações sem recortar a direção. Zero desativa.", 0, 30, profile.NoiseFilter, 22, 20, v =>
            {
                profile.NoiseFilter = v;
                Changed();
            }, "");
            Slider(precision, "Sensibilidade ao mirar · LT / L2", "Multiplicador do mouse enquanto o gatilho esquerdo está pressionado.", 10, 100, profile.AdsSensitivity, 22, 103, v =>
            {
                profile.AdsSensitivity = v;
                Changed();
            }, "%");
            precision.Controls.Add(T.Label("A redução ao mirar vale apenas para o analógico direito. 100% mantém a sensibilidade normal. O jogo pode aplicar sua própria sensibilidade de mira também.", 9, T.Muted, 22, 192, w - 44, 65));
        }

        CheckBox Check(Control parent, string text, int x, int y, bool value)
        {
            CheckBox c = new CheckBox
            {
                Text = text,
                Checked = value,
                AutoSize = true,
                Font = T.Font(10, FontStyle.Regular),
                ForeColor = T.Text,
                BackColor = T.Card,
                Location = new Point(x, y),
                Cursor = Cursors.Hand
            };
            parent.Controls.Add(c);
            return c;
        }

        void Slider(Control parent, string title, string note, int min, int max, int value, int x, int y, Action<int> change, string unit)
        {
            int width = parent.Width - 44;
            parent.Controls.Add(T.Label(title, 11, T.Text, x, y, width - 100, 23));
            Label val = T.Label(value + unit, 11, T.Mint, x + width - 85, y, 85, 23);
            val.TextAlign = ContentAlignment.TopRight;
            parent.Controls.Add(val);
            parent.Controls.Add(T.Label(note, 8, T.Muted, x, y + 26, width - 230, 34));
            TrackBar bar = new TrackBar
            {
                Minimum = min,
                Maximum = max,
                Value = value,
                TickStyle = TickStyle.None,
                AutoSize = false,
                Size = new Size(210, 32),
                Location = new Point(x + width - 215, y + 25),
                BackColor = T.Card,
                SmallChange = 1,
                LargeChange = 5
            };
            parent.Controls.Add(bar);
            bar.ValueChanged += (s, e) =>
            {
                val.Text = bar.Value + unit;
                change(bar.Value);
            };
        }

        void Profiles()
        {
            int w = Available;
            Heading("UM SETUP PARA CADA MUNDO", "Sua coleção de perfis.", "Crie, duplique ou compartilhe suas configurações. Cada perfil guarda suas teclas e ajustes de mouse.");
            Button(content, "+ Novo perfil", 28, 128, 158, () =>
            {
                string name = AskName("Novo perfil", "Meu perfil");
                if (name != null)
                {
                    Profile p = Profile.Create();
                    p.Name = name;
                    profile = p;
                    Changed();
                    Navigate(3);
                }
            }, true);
            Button(content, "Importar JSON", 198, 128, 158, Import, false);
            Button(content, "Exportar atual", 368, 128, 158, Export, false);
            Button(content, "Presets prontos", 538, 128, 158, () =>
            {
                using (PresetDialog d = new PresetDialog())
                {
                    if (d.ShowDialog(this) == DialogResult.OK)
                    {
                        engine.Stop();
                        profile = Profile.Preset(d.Selected);
                        Changed();
                        SetPower();
                        Navigate(3);
                    }
                }
            }, false);
            int y = 193;
            foreach (Profile p in Store.All())
            {
                Profile item = p;
                SoftPanel c = Card(28, y, w, 104);
                c.Fill = item.Id == profile.Id ? Color.FromArgb(28, 35, 51) : T.Card;
                Label n = T.Label(item.Name, 14, T.Text, 21, 19, w - 430, 29);
                n.Font = T.Font(14, FontStyle.Bold);
                c.Controls.Add(n);
                c.Controls.Add(T.Label((item.Controller == "PS4" ? "PlayStation 4" : "Xbox 360") + "   ·   " + (item.MouseEnabled ? "Mouse + teclado" : "Teclado") + (item.Id == profile.Id ? "   ·   Perfil selecionado" : ""), 9, item.Id == profile.Id ? T.Mint : T.Muted, 22, 60, w - 410, 25));
                Button(c, item.Id == profile.Id ? "Selecionado" : "Selecionar", w - 373, 30, 130, () =>
                {
                    engine.Stop();
                    profile = item;
                    Store.Save(profile);
                    SetPower();
                    Navigate(3);
                }, item.Id == profile.Id);
                Button(c, "Duplicar", w - 231, 30, 100, () =>
                {
                    string name = AskName("Duplicar perfil", item.Name + " (cópia)");
                    if (name != null)
                    {
                        Profile copy = item.Copy();
                        copy.Id = Guid.NewGuid().ToString("N");
                        copy.Name = name;
                        profile = copy;
                        Changed();
                        Navigate(3);
                    }
                }, false);
                Button(c, "Excluir", w - 119, 30, 97, () =>
                {
                    if (Store.All().Count <= 1)
                    {
                        Notify("Mantenha pelo menos um perfil. Você pode restaurar as teclas padrão.", false);
                        return;
                    }

                    if (MessageBox.Show(this, "Excluir o perfil “" + item.Name + "”?", "Kinetix Studio", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                    {
                        if (item.Id == profile.Id)
                            engine.Stop();
                        Store.Delete(item);
                        if (item.Id == profile.Id)
                            profile = Store.Current();
                        SetPower();
                        Navigate(3);
                    }
                }, false);
                y += 120;
            }

            content.AutoScrollMinSize = new Size(0, y + 25);
        }

        string AskName(string title, string initial)
        {
            using (NameDialog d = new NameDialog(title, initial))
            {
                return d.ShowDialog(this) == DialogResult.OK ? d.Value : null;
            }
        }

        void Import()
        {
            using (OpenFileDialog d = new OpenFileDialog
            {
                Filter = "Perfil Kinetix Studio (*.json)|*.json",
                Title = "Importar perfil"
            }

            )
                if (d.ShowDialog(this) == DialogResult.OK)
                {
                    try
                    {
                        if (new FileInfo(d.FileName).Length > 65536)
                            throw new InvalidDataException("O arquivo é grande demais para um perfil.");
                        Profile p = Store.Deserialize(File.ReadAllText(d.FileName));
                        p.Id = Guid.NewGuid().ToString("N");
                        engine.Stop();
                        profile = p;
                        Changed();
                        SetPower();
                        Navigate(3);
                        Notify("Perfil importado.", false);
                    }
                    catch (Exception ex)
                    {
                        Notify("Não foi possível importar: " + ex.Message, true);
                    }
                }
        }

        void Export()
        {
            using (SaveFileDialog d = new SaveFileDialog
            {
                Filter = "Perfil Kinetix Studio (*.json)|*.json",
                FileName = "KinetixStudio-perfil.json",
                Title = "Exportar perfil"
            }

            )
                if (d.ShowDialog(this) == DialogResult.OK)
                {
                    try
                    {
                        File.WriteAllText(d.FileName, Store.Serialize(profile));
                        Notify("Perfil exportado com sucesso.", false);
                    }
                    catch (Exception ex)
                    {
                        Notify(ex.Message, true);
                    }
                }
        }

        void Tester()
        {
            int w = Available;
            Heading("VEJA CADA COMANDO GANHAR VIDA", "Seu controle, em tempo real.", "Ative o controle e pressione suas teclas aqui. F8 pausa; F12 desliga. O cursor fica livre nesta tela.");
            SoftPanel card = Card(28, 130, w, 352);
            controller = new ControllerView
            {
                Location = new Point(20, 25),
                Size = new Size(w - 40, 295),
                Kind = profile.Controller
            };
            card.Controls.Add(controller);
            SoftPanel data = Card(28, 500, w, 133);
            data.Controls.Add(T.Label("SAÍDA AO VIVO", 8, T.Muted, 22, 15, w - 44, 22));
            activityLabel = T.Label("Ative o controle para começar.", 10, T.Mint, 22, 44, w - 44, 84);
            data.Controls.Add(activityLabel);
            footer = T.Label("Esta tela envia comandos ao controle virtual. Nas outras telas, a captura pausa para você editar com tranquilidade.", 9, T.Muted, 29, 650, w, 40);
            content.Controls.Add(footer);
        }

        void Preferences()
        {
            int w = Available;
            Heading("OS DETALHES FAZEM DIFERENÇA", "Seu Kinetix Studio, do seu jeito.", "As preferências abaixo pertencem ao perfil selecionado. Alterações pausam o controle e são salvas automaticamente.");
            SoftPanel c = Card(28, 130, w, 145);
            c.Controls.Add(T.Label("Nome do perfil", 10, T.Muted, 22, 19, 200, 22));
            TextBox name = T.Input(profile.Name, w - 225);
            name.Location = new Point(22, 48);
            c.Controls.Add(name);
            Button(c, "Salvar nome", w - 180, 43, 155, () =>
            {
                if (name.Text.Trim().Length == 0 || name.Text.Trim().Length > 60)
                {
                    Notify("Escolha um nome de 1 a 60 caracteres.", true);
                    return;
                }

                profile.Name = name.Text.Trim();
                Changed();
                Notify("Nome do perfil salvo.", false);
            }, false);
            c.Controls.Add(T.Label("Controle de saída", 10, T.Muted, 22, 101, 170, 22));
            ComboBox type = Combo(c, 198, 97, 270, new string[] { "PlayStation 4 · DualShock 4", "Xbox 360 · XInput" }, profile.Controller == "PS4" ? 0 : 1);
            type.SelectedIndexChanged += (s, e) =>
            {
                profile.Controller = type.SelectedIndex == 0 ? "PS4" : "X360";
                Changed();
            };
            SoftPanel capture = Card(28, 292, w, 130);
            CheckBox keys = Check(capture, "Bloquear no Windows as teclas e os botões remapeados", 22, 21, profile.BlockMappedKeys);
            keys.CheckedChanged += (s, e) =>
            {
                profile.BlockMappedKeys = keys.Checked;
                Changed();
            };
            CheckBox mouse = Check(capture, "Manter o cursor parado enquanto o mouse controla a câmera", 22, 58, profile.BlockMouse);
            mouse.CheckedChanged += (s, e) =>
            {
                profile.BlockMouse = mouse.Checked;
                Changed();
            };
            capture.Controls.Add(T.Label("F8 e F12 continuam disponíveis. No Kinetix Studio, o cursor permanece livre.", 8, T.Muted, 23, 96, w - 46, 22));
            SoftPanel game = Card(28, 439, w, 110);
            game.Controls.Add(T.Label("Onde ativar a captura", 11, T.Text, 22, 16, w - 44, 24));
            Label path = T.Label(String.IsNullOrEmpty(profile.GamePath) ? "Qualquer jogo ou aplicativo em primeiro plano" : Path.GetFileName(profile.GamePath), 9, T.Muted, 22, 49, w - 360, 42);
            game.Controls.Add(path);
            Button(game, "Escolher jogo", w - 325, 45, 154, () =>
            {
                using (OpenFileDialog d = new OpenFileDialog
                {
                    Filter = "Aplicativo (*.exe)|*.exe",
                    Title = "Escolher executável do jogo"
                }

                )
                    if (d.ShowDialog(this) == DialogResult.OK)
                    {
                        profile.GamePath = d.FileName;
                        Changed();
                        Navigate(5);
                    }
            }, false);
            Button(game, "Todos", w - 159, 45, 135, () =>
            {
                profile.GamePath = "";
                Changed();
                Navigate(5);
            }, false);
            SoftPanel perf = Card(28, 566, w, 151);
            perf.Controls.Add(T.Label("Resposta e janela", 11, T.Text, 22, 16, w - 44, 25));
            perf.Controls.Add(T.Label("Intervalo solicitado ao motor", 9, T.Muted, 22, 58, 265, 24));
            ComboBox interval = Combo(perf, 300, 54, 220, new string[] { "4 ms · até 250 Hz", "8 ms · até 125 Hz", "16 ms · até 62,5 Hz" }, profile.UpdateInterval == 4 ? 0 : profile.UpdateInterval == 8 ? 1 : 2);
            interval.SelectedIndexChanged += (s, e) =>
            {
                profile.UpdateInterval = new int[]
                {
                    4,
                    8,
                    16
                }[interval.SelectedIndex];
                Changed();
            };
            CheckBox minimize = Check(perf, "Minimizar para a bandeja do sistema", 22, 106, desktop.MinimizeToTray);
            minimize.CheckedChanged += (s, e) =>
            {
                desktop.MinimizeToTray = minimize.Checked;
                desktop.Save();
            };
            content.Controls.Add(T.Label("O Hz real depende do Windows e aparece no estúdio. Não é uma medida de latência do jogo.", 9, T.Muted, 29, 734, w, 45));
        }

        ComboBox Combo(Control parent, int x, int y, int width, string[] entries, int selected)
        {
            ComboBox box = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Location = new Point(x, y),
                Width = width,
                BackColor = T.Card,
                ForeColor = T.Text,
                FlatStyle = FlatStyle.Flat,
                DrawMode = DrawMode.OwnerDrawFixed,
                ItemHeight = 24
            };
            box.Items.AddRange(entries);
            box.SelectedIndex = selected;
            box.DrawItem += (s, e) =>
            {
                using (Brush brush = new SolidBrush((e.State & DrawItemState.Selected) != 0 ? T.Line : T.Card))
                    e.Graphics.FillRectangle(brush, e.Bounds);
                if (e.Index >= 0)
                    TextRenderer.DrawText(e.Graphics, Convert.ToString(box.Items[e.Index]), Font, e.Bounds, T.Text, TextFormatFlags.Left | TextFormatFlags.VerticalCenter);
            };
            parent.Controls.Add(box);
            return box;
        }

        void Diagnostics()
        {
            int w = Available;
            Heading("TUDO À VISTA", "A saúde do seu setup.", "Verifique a conexão, encontre seus arquivos e consulte os eventos mais recentes.");
            SoftPanel driver = Card(28, 130, w, 116);
            driver.Controls.Add(T.Label("CONTROLE VIRTUAL", 8, T.Purple, 22, 17, w - 44, 22));
            driver.Controls.Add(T.Label(driverStatus, 11, driverReady ? T.Mint : T.Muted, 22, 50, w - 400, 44));
            Button(driver, "Verificar driver", w - 365, 45, 164, Probe, false);
            Button(driver, "Controles Windows", w - 187, 45, 165, () =>
            {
                try
                {
                    Process.Start("control.exe", "joy.cpl");
                }
                catch (Exception ex)
                {
                    Notify(ex.Message, true);
                }
            }, false);
            SoftPanel stats = Card(28, 264, w, 105);
            stats.Controls.Add(T.Label("SESSÃO ATUAL", 8, T.Purple, 22, 17, w - 44, 22));
            stats.Controls.Add(T.Label("Relatórios enviados: " + engine.ReportsSent + "   ·   Perfil: " + profile.Name + "   ·   Intervalo: " + profile.UpdateInterval + " ms", 10, T.Text, 22, 51, w - 44, 35));
            Button(content, "Abrir pasta de perfis", 28, 389, 220, () => OpenFolder(Path.Combine(Store.Root, "profiles")), false);
            Button(content, "Atualizar eventos", 260, 389, 190, () => Navigate(6), false);
            TextBox log = new TextBox
            {
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Vertical,
                Location = new Point(28, 451),
                Size = new Size(w, 220),
                BackColor = T.Card,
                ForeColor = T.Muted,
                BorderStyle = BorderStyle.FixedSingle,
                Font = T.Font(9, FontStyle.Regular)
            };
            try
            {
                string path = Path.Combine(Store.Root, Store.LogFileName);
                log.Text = File.Exists(path) ? String.Join(Environment.NewLine, File.ReadLines(path).Reverse().Take(40).Reverse().ToArray()) : "Nenhum evento registrado.";
            }
            catch (Exception ex)
            {
                log.Text = "Não foi possível ler o log: " + ex.Message;
            }

            content.Controls.Add(log);
        }

        void OpenFolder(string folder)
        {
            try
            {
                Directory.CreateDirectory(folder);
                Process.Start(new ProcessStartInfo("explorer.exe", "\"" + folder + "\"") { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                Notify(ex.Message, true);
            }
        }

        void RefreshLive()
        {
            if (IsDisposed || (!renderOnly && (!Visible || WindowState == FormWindowState.Minimized)))
                return;
            PadState s = controller != null || activityLabel != null ? engine.State : null;
            if (controller != null)
            {
                bool repaint = controller.Kind != profile.Controller || !InputEngine.Equivalent(controller.State, s);
                controller.State = s;
                controller.Kind = profile.Controller;
                if (repaint)
                    controller.Invalidate();
            }

            if (statusLabel != null)
            {
                statusLabel.Text = engine.Running ? (engine.Capturing ? "Em ação" : "Pronto para jogar") : "Desativado";
                statusLabel.ForeColor = engine.Running ? T.Mint : T.Text;
            }

            if (hzLabel != null)
                hzLabel.Text = engine.Running ? engine.ActualHz + " Hz" : "— Hz";
            if (activityLabel != null)
                activityLabel.Text = engine.Running ? "LS  " + s.LX.ToString("+0.00;-0.00;0.00") + ", " + s.LY.ToString("+0.00;-0.00;0.00") + "     RS  " + s.RX.ToString("+0.00;-0.00;0.00") + ", " + s.RY.ToString("+0.00;-0.00;0.00") + "\nBotões agora: " + (s.ButtonCount == 0 ? "nenhum" : String.Join(" · ", s.Buttons.Select(b => Catalog.Output(b, profile.Controller)).ToArray())) + "\nÚltimo botão enviado: " + engine.LastButton : "Ative o controle para começar. ESPAÇO → " + (profile.Controller == "PS4" ? "X" : "A") + ".";
        }
    }
}
