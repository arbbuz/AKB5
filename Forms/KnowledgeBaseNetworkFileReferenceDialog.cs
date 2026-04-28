using AsutpKnowledgeBase.Models;
using AsutpKnowledgeBase.Services;

namespace AsutpKnowledgeBase
{
    public sealed class KnowledgeBaseNetworkFileReferenceDialog : Form
    {
        private readonly string _networkAssetId;

        private TextBox _txtTitle = null!;
        private TextBox _txtPath = null!;
        private TextBox _txtPreviewKind = null!;

        public KnowledgeBaseNetworkFileReferenceDialog(
            string title,
            KbNetworkFileReference? existingReference = null)
        {
            _networkAssetId = existingReference?.NetworkAssetId?.Trim() ?? string.Empty;

            Text = title;
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MinimizeBox = false;
            MaximizeBox = false;
            ShowInTaskbar = false;
            ClientSize = new Size(720, 220);
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
                Text = existingReference?.Title ?? string.Empty
            };
            layout.Controls.Add(CreateLabel("Наименование"), 0, 0);
            layout.Controls.Add(_txtTitle, 1, 0);

            var pathPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                Margin = new Padding(0)
            };
            pathPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            pathPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

            _txtPath = new TextBox
            {
                Dock = DockStyle.Fill,
                Text = existingReference?.Path ?? string.Empty
            };
            _txtPath.TextChanged += (_, _) => UpdatePreviewKind();
            pathPanel.Controls.Add(_txtPath, 0, 0);

            var btnBrowse = new Button
            {
                Text = "Выбрать файл...",
                AutoSize = true,
                Margin = new Padding(8, 0, 0, 0)
            };
            btnBrowse.Click += BtnBrowse_Click;
            pathPanel.Controls.Add(btnBrowse, 1, 0);

            layout.Controls.Add(CreateLabel("Путь / ссылка"), 0, 1);
            layout.Controls.Add(pathPanel, 1, 1);

            _txtPreviewKind = new TextBox
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = Color.White
            };
            layout.Controls.Add(CreateLabel("Тип предпросмотра"), 0, 2);
            layout.Controls.Add(_txtPreviewKind, 1, 2);

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

            Controls.Add(layout);
            Controls.Add(buttonsPanel);

            AcceptButton = btnOk;
            CancelButton = btnCancel;

            UpdatePreviewKind();
        }

        public KbNetworkFileReference Result { get; private set; } = new();

        private void BtnBrowse_Click(object? sender, EventArgs e)
        {
            using var dialog = new OpenFileDialog
            {
                Title = "Выберите файл сети",
                CheckFileExists = true,
                Filter =
                    "Поддерживаемые изображения (*.jpg;*.jpeg;*.png;*.bmp;*.gif)|*.jpg;*.jpeg;*.png;*.bmp;*.gif|Все файлы (*.*)|*.*"
            };

            if (dialog.ShowDialog(this) == DialogResult.OK)
                _txtPath.Text = dialog.FileName;
        }

        private void BtnOk_Click(object? sender, EventArgs e)
        {
            string title = _txtTitle.Text.Trim();
            string path = _txtPath.Text.Trim();
            if (string.IsNullOrWhiteSpace(title))
            {
                MessageBox.Show(
                    this,
                    "Укажите наименование файла сети.",
                    "Сеть",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            if (string.IsNullOrWhiteSpace(path))
            {
                MessageBox.Show(
                    this,
                    "Укажите путь или ссылку на файл сети.",
                    "Сеть",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            Result = new KbNetworkFileReference
            {
                NetworkAssetId = _networkAssetId,
                Title = title,
                Path = path,
                PreviewKind = KnowledgeBaseNetworkPreviewService.ResolvePreviewKind(path)
            };

            DialogResult = DialogResult.OK;
            Close();
        }

        private void UpdatePreviewKind()
        {
            string path = _txtPath.Text.Trim();
            _txtPreviewKind.Text = KnowledgeBaseNetworkPreviewService.GetPreviewKindText(
                KnowledgeBaseNetworkPreviewService.ResolvePreviewKind(path));
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
