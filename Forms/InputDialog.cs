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
            ClientSize = new Size(360, 132);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            Padding = new Padding(16);
            AppIconProvider.Apply(this);

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3
            };
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            var lbl = new Label
            {
                Text = prompt,
                AutoSize = true,
                Margin = new Padding(0, 0, 0, 8)
            };
            _txtInput = new TextBox
            {
                Text = defaultValue,
                Dock = DockStyle.Top,
                Margin = new Padding(0, 0, 0, 14)
            };
            var buttonPanel = new FlowLayoutPanel
            {
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Dock = DockStyle.Top,
                FlowDirection = FlowDirection.RightToLeft,
                Margin = new Padding(0)
            };
            var btnOk = new Button
            {
                Text = "Подтвердить",
                DialogResult = DialogResult.OK,
                AutoSize = true,
                MinimumSize = new Size(110, 28),
                Margin = new Padding(8, 0, 0, 0)
            };
            var btnCancel = new Button
            {
                Text = "Отмена",
                DialogResult = DialogResult.Cancel,
                AutoSize = true,
                MinimumSize = new Size(86, 28),
                Margin = new Padding(0)
            };

            buttonPanel.Controls.Add(btnCancel);
            buttonPanel.Controls.Add(btnOk);
            layout.Controls.Add(lbl, 0, 0);
            layout.Controls.Add(_txtInput, 0, 1);
            layout.Controls.Add(buttonPanel, 0, 2);
            Controls.Add(layout);
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
