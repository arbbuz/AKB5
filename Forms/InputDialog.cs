using System.Drawing;
using System.Windows.Forms;

namespace AsutpKnowledgeBase
{
    /// <summary>
    /// Универсальное модальное окно для ввода строки.
    /// </summary>
    public class InputDialog : Form
    {
        public string Result { get; private set; } = string.Empty;

        private readonly TextBox _txtInput;

        public InputDialog(string prompt, string defaultValue = "")
        {
            Text = "Ввод данных";
            Size = new Size(350, 180);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;

            var lbl = new Label { Text = prompt, AutoSize = true, Location = new Point(15, 15) };
            _txtInput = new TextBox { Text = defaultValue, Location = new Point(15, 45), Width = 300 };
            var btnOk = new Button { Text = "OK", DialogResult = DialogResult.OK, Location = new Point(130, 90), Width = 80 };
            var btnCancel = new Button { Text = "Отмена", DialogResult = DialogResult.Cancel, Location = new Point(220, 90), Width = 80 };

            Controls.AddRange(new Control[] { lbl, _txtInput, btnOk, btnCancel });
            AcceptButton = btnOk;
            CancelButton = btnCancel;
            Shown += (s, e) =>
            {
                _txtInput.SelectAll();
                _txtInput.Focus();
            };
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (DialogResult == DialogResult.OK)
                Result = _txtInput.Text;

            base.OnFormClosing(e);
        }
    }
}
