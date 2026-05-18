using AsutpKnowledgeBase.Models;

namespace AsutpKnowledgeBase
{
    public sealed class KnowledgeBaseDocumentLinkDialog : Form
    {
        private readonly string _documentId;
        private readonly KbDocumentKind _kind;

        private TextBox _txtTitle = null!;
        private TextBox _txtPath = null!;
        private DateTimePicker _dtpUpdatedAt = null!;

        public KnowledgeBaseDocumentLinkDialog(
            string title,
            KbDocumentKind kind,
            KbDocumentLink? existingLink = null)
        {
            _documentId = existingLink?.DocumentId?.Trim() ?? string.Empty;
            _kind = existingLink?.Kind ?? kind;

            Text = title;
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MinimizeBox = false;
            MaximizeBox = false;
            ShowInTaskbar = false;
            ClientSize = new Size(660, 220);
            AppIconProvider.Apply(this);

            var rootLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2
            };
            rootLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            rootLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(12),
                ColumnCount = 2,
                RowCount = 3
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150F));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            _txtTitle = new TextBox
            {
                Dock = DockStyle.Fill,
                Text = existingLink?.Title ?? string.Empty
            };
            layout.Controls.Add(CreateLabel("Наименование"), 0, 0);
            layout.Controls.Add(_txtTitle, 1, 0);

            _txtPath = new TextBox
            {
                Dock = DockStyle.Fill,
                Text = existingLink?.Path ?? string.Empty
            };
            var pathPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                ColumnCount = 2,
                RowCount = 1,
                Margin = new Padding(0, 0, 0, 8)
            };
            pathPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            pathPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            pathPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            pathPanel.Controls.Add(_txtPath, 0, 0);

            var btnBrowse = new Button
            {
                Text = "Выбрать файл",
                AutoSize = true,
                Margin = new Padding(8, 0, 0, 0)
            };
            btnBrowse.Click += BtnBrowse_Click;
            pathPanel.Controls.Add(btnBrowse, 1, 0);

            layout.Controls.Add(CreateLabel("Ссылка / путь"), 0, 1);
            layout.Controls.Add(pathPanel, 1, 1);

            _dtpUpdatedAt = CreateDatePicker(existingLink?.UpdatedAt);
            layout.Controls.Add(CreateLabel("Дата обновления"), 0, 2);
            layout.Controls.Add(_dtpUpdatedAt, 1, 2);

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
                Text = "Сохранить",
                AutoSize = true
            };
            btnOk.Click += BtnOk_Click;

            buttonsPanel.Controls.Add(btnCancel);
            buttonsPanel.Controls.Add(btnOk);

            rootLayout.Controls.Add(layout, 0, 0);
            rootLayout.Controls.Add(buttonsPanel, 0, 1);
            Controls.Add(rootLayout);

            AcceptButton = btnOk;
            CancelButton = btnCancel;
        }

        public KbDocumentLink Result { get; private set; } = new();

        private void BtnBrowse_Click(object? sender, EventArgs e)
        {
            using var dialog = new OpenFileDialog
            {
                Title = _kind == KbDocumentKind.SchemeLink
                    ? "Выберите файл схемы"
                    : "Выберите файл инструкции",
                CheckFileExists = true,
                Filter = "Все файлы (*.*)|*.*"
            };

            if (dialog.ShowDialog(this) != DialogResult.OK)
                return;

            _txtPath.Text = dialog.FileName;
            if (string.IsNullOrWhiteSpace(_txtTitle.Text))
                _txtTitle.Text = System.IO.Path.GetFileNameWithoutExtension(dialog.FileName);
        }

        private void BtnOk_Click(object? sender, EventArgs e)
        {
            string title = _txtTitle.Text.Trim();
            string path = _txtPath.Text.Trim();
            if (string.IsNullOrWhiteSpace(title))
            {
                MessageBox.Show(
                    this,
                    "Укажите наименование ссылки.",
                    "Документация и ПО",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            if (string.IsNullOrWhiteSpace(path))
            {
                MessageBox.Show(
                    this,
                    "Укажите путь или ссылку.",
                    "Документация и ПО",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            Result = new KbDocumentLink
            {
                DocumentId = _documentId,
                Kind = _kind,
                Title = title,
                Path = path,
                UpdatedAt = _dtpUpdatedAt.Value.Date
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

        private static DateTimePicker CreateDatePicker(DateTime? value)
        {
            var picker = new DateTimePicker
            {
                Dock = DockStyle.Left,
                Width = 120,
                Format = DateTimePickerFormat.Custom,
                CustomFormat = "dd.MM.yyyy",
                Margin = new Padding(0, 0, 0, 8)
            };

            if (value.HasValue)
                picker.Value = value.Value.Date;
            else
                picker.Value = DateTime.Today;

            return picker;
        }
    }
}
