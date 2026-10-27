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
    class PresetDialog : Form
    {
        public string Selected;
        public PresetDialog()
        {
            Text = "Escolha seu ponto de partida";
            BackColor = T.Bg;
            ForeColor = T.Text;
            ClientSize = new Size(530, 365);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = MinimizeBox = false;
            Controls.Add(T.Label("Um perfil pronto para o seu estilo.", 19, T.Text, 24, 21, 480, 40));
            string[] names =
            {
                "Precisão FPS",
                "Exploração",
                "Corrida"
            };
            string[] notes =
            {
                "Mira mais delicada, curva progressiva e suavização curta.",
                "Câmera mais fluida para explorar com teclado e mouse.",
                "W acelera, S freia, A/D viram. Mouse livre."
            };
            for (int i = 0; i < 3; i++)
            {
                string kind = names[i];
                ActionButton b = new ActionButton
                {
                    Text = kind,
                    Location = new Point(24, 83 + i * 87),
                    Width = 170
                };
                b.Click += (s, e) =>
                {
                    Selected = kind;
                    DialogResult = DialogResult.OK;
                };
                Controls.Add(b);
                Controls.Add(T.Label(notes[i], 10, T.Muted, 211, 88 + i * 87, 290, 61));
            }
        }
    }

    class NameDialog : Form
    {
        TextBox input;
        public string Value
        {
            get
            {
                return input.Text.Trim();
            }
        }

        public NameDialog(string title, string initial)
        {
            Text = title;
            BackColor = T.Bg;
            ForeColor = T.Text;
            ClientSize = new Size(420, 170);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = MinimizeBox = false;
            StartPosition = FormStartPosition.CenterParent;
            Controls.Add(T.Label("Como você quer chamar este perfil?", 11, T.Text, 24, 22, 375, 30));
            input = T.Input(initial, 370);
            input.MaxLength = 60;
            input.Location = new Point(24, 65);
            Controls.Add(input);
            ActionButton save = new ActionButton
            {
                Text = "Salvar perfil",
                Primary = true,
                Location = new Point(239, 111)
            };
            Controls.Add(save);
            save.Click += (s, e) =>
            {
                if (Value.Length == 0)
                {
                    input.Focus();
                    return;
                }

                DialogResult = DialogResult.OK;
            };
            ActionButton cancel = new ActionButton
            {
                Text = "Cancelar",
                Location = new Point(24, 111)
            };
            cancel.Click += (s, e) => DialogResult = DialogResult.Cancel;
            Controls.Add(cancel);
            AcceptButton = save;
            CancelButton = cancel;
            Shown += (s, e) =>
            {
                input.Focus();
                input.SelectAll();
            };
        }
    }

    class CaptureDialog : Form
    {
        public int Captured;
        Label note;
        public CaptureDialog(string action)
        {
            Text = "Escolher entrada";
            BackColor = T.Bg;
            ForeColor = T.Text;
            ClientSize = new Size(470, 237);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            MaximizeBox = MinimizeBox = false;
            KeyPreview = true;
            Controls.Add(T.Label(action, 17, T.Text, 25, 22, 420, 35));
            note = T.Label("Pressione uma tecla ou clique em um botão do mouse\nna área abaixo. F8 e F12 são reservados.", 10, T.Muted, 25, 69, 420, 52);
            Controls.Add(note);
            Panel area = new Panel
            {
                Location = new Point(25, 129),
                Size = new Size(420, 44),
                BackColor = T.Card,
                Cursor = Cursors.Hand,
                TabStop = true
            };
            area.Controls.Add(T.Label("CLIQUE OU ROLE A RODA NESTA ÁREA", 9, T.Mint, 24, 13, 380, 24));
            area.MouseDown += CaptureMouse;
            area.MouseEnter += (s, e) => area.Focus();
            area.MouseWheel += (s, e) =>
            {
                Captured = e.Delta > 0 ? 256 : 257;
                DialogResult = DialogResult.OK;
            };
            foreach (Control c in area.Controls)
            {
                c.MouseDown += CaptureMouse;
                c.MouseEnter += (s, e) => area.Focus();
            }

            Controls.Add(area);
            ActionButton clear = new ActionButton
            {
                Text = "Remover tecla",
                Location = new Point(25, 184),
                Width = 170
            };
            clear.Click += (s, e) =>
            {
                Captured = 0;
                DialogResult = DialogResult.OK;
            };
            Controls.Add(clear);
            ActionButton cancel = new ActionButton
            {
                Text = "Cancelar",
                Location = new Point(275, 184),
                Width = 170
            };
            cancel.Click += (s, e) => DialogResult = DialogResult.Cancel;
            Controls.Add(cancel);
            KeyDown += (s, e) =>
            {
                e.SuppressKeyPress = true;
                e.Handled = true;
                int k = (int)e.KeyCode;
                if (k == 119 || k == 123)
                {
                    note.Text = "F8 e F12 são reservados. Escolha outra tecla.";
                    return;
                }

                if (k == 16)
                    k = Native.GetAsyncKeyState(161) < 0 ? 161 : 160;
                if (k == 17)
                    k = Native.GetAsyncKeyState(163) < 0 ? 163 : 162;
                if (k == 18)
                    k = Native.GetAsyncKeyState(165) < 0 ? 165 : 164;
                Captured = k;
                DialogResult = DialogResult.OK;
            };
        }

        void CaptureMouse(object sender, MouseEventArgs e)
        {
            Captured = e.Button == MouseButtons.Left ? 1 : e.Button == MouseButtons.Right ? 2 : e.Button == MouseButtons.Middle ? 4 : e.Button == MouseButtons.XButton1 ? 5 : 6;
            DialogResult = DialogResult.OK;
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            Keys k = keyData & Keys.KeyCode;
            if (k == Keys.Tab || k == Keys.Escape || k == Keys.Enter || k == Keys.Left || k == Keys.Right || k == Keys.Up || k == Keys.Down)
            {
                Captured = (int)k;
                DialogResult = DialogResult.OK;
                return true;
            }

            return base.ProcessCmdKey(ref msg, keyData);
        }
    }
}
