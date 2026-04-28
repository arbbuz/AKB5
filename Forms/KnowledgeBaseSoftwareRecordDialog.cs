using System.Globalization;
using AsutpKnowledgeBase.Models;

namespace AsutpKnowledgeBase
{
    public sealed class KnowledgeBaseSoftwareRecordDialog : Form
    {
        private readonly string _softwareId;
        private readonly DateTime _addedAt;

        private TextBox _txtTitle = null!;
        private TextBox _txtPath = null!;
        private TextBox _txtAddedAt = null!;

        public KnowledgeBaseSoftwareRecordDialog(string title, KbSoftwareRecord? existingRecord = null)
        {
            _softwareId = existingRecord?.SoftwareId?.Trim() ?? string.Empty;
            _addedAt = (existingRecord?.AddedAt ?? DateTime.Today).Date;

            Text = title;
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MinimizeBox = false;
            MaximizeBox = false;
            ShowInTaskbar = false;
            ClientSize = new Size(620, 210);
            AppIconProvider.Apply(this);

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(12),
                ColumnCount = 2,
                RowCount = 3
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 170F));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            _txtTitle = new TextBox
            {
                Dock = DockStyle.Fill,
                Text = existingRecord?.Title ?? string.Empty
            };
            layout.Controls.Add(CreateLabel("Наименование"), 0, 0);
            layout.Controls.Add(_txtTitle, 1, 0);

            _txtPath = new TextBox
            {
                Dock = DockStyle.Fill,
                Text = existingRecord?.Path ?? string.Empty
            };
            layout.Controls.Add(CreateLabel("Ссылка / путь к папке"), 0, 1);
            layout.Controls.Add(_txtPath, 1, 1);

            _txtAddedAt = new TextBox
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = Color.White,
                Text = _addedAt.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
            };
            layout.Controls.Add(CreateLabel("Дата добавления"), 0, 2);
            layout.Controls.Add(_txtAddedAt, 1, 2);

            var buttonsPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                FlowDirection = FlowDirection.RightToLeft,
                WrapContents = false,
                AutoSize = true,
                Padding = new Padding(12, 0, 12, 12)
            };

            var btnCancel = new Button
            {
                Text = "Отмена",
                DialogResult = DialogResult.Cancel,
                AutoSize = true
            };
            var btnOk = new Button
            {
                Text = "OK",
                AutoSize = true
            };
            btnOk.Click += BtnOk_Click;

            buttonsPanel.Controls.Add(btnCancel);
            buttonsPanel.Controls.Add(btnOk);

            Controls.Add(layout);
            Controls.Add(buttonsPanel);

            AcceptButton = btnOk;
            CancelButton = btnCancel;
        }

        public KbSoftwareRecord Result { get; private set; } = new();

        private void BtnOk_Click(object? sender, EventArgs e)
        {
            string title = _txtTitle.Text.Trim();
            string path = _txtPath.Text.Trim();
            if (string.IsNullOrWhiteSpace(title))
            {
                MessageBox.Show(
                    this,
                    "Укажите наименование ссылки на ПО.",
                    "Документация и ПО",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            if (string.IsNullOrWhiteSpace(path))
            {
                MessageBox.Show(
                    this,
                    "Укажите путь или ссылку на папку с ПО.",
                    "Документация и ПО",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            Result = new KbSoftwareRecord
            {
                SoftwareId = _softwareId,
                Title = title,
                Path = path,
                AddedAt = _addedAt
            };

            DialogResult = DialogResult.OK;
            Close();
        }

        private static Label CreateLabel(string text) =>
            new()
            {
                Text = text,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                Margin = new Padding(0, 0, 8, 8)
            };
    }
}
