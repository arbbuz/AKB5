using System.Globalization;
using AsutpKnowledgeBase.Services;

namespace AsutpKnowledgeBase
{
    public enum KnowledgeBaseSnapshotBrowserAction
    {
        None,
        Restore,
        Compare
    }

    public sealed class KnowledgeBaseSnapshotBrowserForm : Form
    {
        private readonly IReadOnlyList<KnowledgeBaseSnapshotEntry> _snapshots;
        private readonly string _snapshotDirectoryPath;
        private DataGridView _grid = null!;
        private TextBox _txtDetails = null!;
        private Label _lblSummary = null!;
        private Button _btnRestore = null!;
        private Button _btnCompare = null!;

        public KnowledgeBaseSnapshotBrowserForm(
            IReadOnlyList<KnowledgeBaseSnapshotEntry> snapshots,
            string snapshotDirectoryPath)
        {
            _snapshots = snapshots ?? Array.Empty<KnowledgeBaseSnapshotEntry>();
            _snapshotDirectoryPath = snapshotDirectoryPath?.Trim() ?? string.Empty;

            Text = "Снимки базы";
            StartPosition = FormStartPosition.CenterParent;
            MinimizeBox = false;
            ShowInTaskbar = false;
            Size = new Size(1080, 640);
            MinimumSize = new Size(860, 500);
            AppIconProvider.Apply(this);

            Controls.Add(CreateLayout());
            BindSnapshots();
        }

        public KnowledgeBaseSnapshotBrowserAction SelectedAction { get; private set; }

        public IReadOnlyList<KnowledgeBaseSnapshotEntry> SelectedSnapshots => GetSelectedSnapshots();

        private TableLayoutPanel CreateLayout()
        {
            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(12),
                ColumnCount = 1,
                RowCount = 4
            };
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 150));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            _lblSummary = new Label
            {
                AutoSize = true,
                Margin = new Padding(0, 0, 0, 8)
            };
            layout.Controls.Add(_lblSummary, 0, 0);

            _grid = CreateGrid();
            layout.Controls.Add(_grid, 0, 1);

            _txtDetails = new TextBox
            {
                Dock = DockStyle.Fill,
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Vertical,
                Font = new Font("Consolas", 9F),
                Margin = new Padding(0, 10, 0, 0)
            };
            layout.Controls.Add(_txtDetails, 0, 2);

            layout.Controls.Add(CreateBottomPanel(), 0, 3);
            return layout;
        }

        private FlowLayoutPanel CreateBottomPanel()
        {
            var buttonsPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.RightToLeft,
                AutoSize = true,
                Margin = new Padding(0, 10, 0, 0)
            };

            var btnClose = new Button
            {
                Text = "Закрыть",
                DialogResult = DialogResult.OK,
                AutoSize = true
            };

            _btnCompare = new Button
            {
                Text = "Сравнить два снимка",
                AutoSize = true,
                Enabled = false
            };
            _btnCompare.Click += (_, _) =>
            {
                SelectedAction = KnowledgeBaseSnapshotBrowserAction.Compare;
                DialogResult = DialogResult.OK;
                Close();
            };

            _btnRestore = new Button
            {
                Text = "Восстановить",
                AutoSize = true,
                Enabled = false
            };
            _btnRestore.Click += (_, _) =>
            {
                SelectedAction = KnowledgeBaseSnapshotBrowserAction.Restore;
                DialogResult = DialogResult.OK;
                Close();
            };

            buttonsPanel.Controls.Add(btnClose);
            buttonsPanel.Controls.Add(_btnCompare);
            buttonsPanel.Controls.Add(_btnRestore);
            AcceptButton = btnClose;
            CancelButton = btnClose;
            return buttonsPanel;
        }

        private BufferedDataGridView CreateGrid()
        {
            var grid = new BufferedDataGridView
            {
                Dock = DockStyle.Fill,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToResizeRows = false,
                AutoGenerateColumns = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None,
                BackgroundColor = SystemColors.Window,
                BorderStyle = BorderStyle.Fixed3D,
                MultiSelect = true,
                ReadOnly = true,
                RowHeadersVisible = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                ShowCellToolTips = false
            };

            grid.Columns.Add(CreateTextColumn("CreatedAt", "Создан", 150));
            grid.Columns.Add(CreateTextColumn("Kind", "Тип", 120));
            grid.Columns.Add(CreateTextColumn("SnapshotFileName", "Файл снимка", 260));
            grid.Columns.Add(CreateTextColumn("SourcePath", "Исходный файл", 220));
            grid.Columns.Add(CreateTextColumn("Size", "Размер", 90));
            grid.Columns.Add(CreateTextColumn("Note", "Примечание", 320));

            grid.SelectionChanged += (_, _) =>
            {
                UpdateDetails();
                UpdateActionButtons();
            };
            grid.DataError += (_, e) => e.ThrowException = false;
            return grid;
        }

        private void BindSnapshots()
        {
            _grid.Rows.Clear();
            foreach (KnowledgeBaseSnapshotEntry snapshot in _snapshots)
            {
                int rowIndex = _grid.Rows.Add();
                DataGridViewRow row = _grid.Rows[rowIndex];
                row.Tag = snapshot;
                row.Cells["CreatedAt"].Value = FormatCreatedAt(snapshot.CreatedAt);
                row.Cells["Kind"].Value = FormatKind(snapshot.Kind);
                row.Cells["SnapshotFileName"].Value = snapshot.SnapshotFileName;
                row.Cells["SourcePath"].Value = Path.GetFileName(snapshot.SourcePath);
                row.Cells["Size"].Value = FormatSize(snapshot.SizeBytes);
                row.Cells["Note"].Value = FormatNote(snapshot.Note);
            }

            if (_grid.Rows.Count > 0)
            {
                _grid.Rows[0].Selected = true;
                _grid.CurrentCell = _grid.Rows[0].Cells["CreatedAt"];
            }

            _lblSummary.Text = _snapshots.Count == 0
                ? $"Снимков для текущей базы нет. Каталог: {_snapshotDirectoryPath}"
                : $"Снимков: {_snapshots.Count}. Каталог: {_snapshotDirectoryPath}";
            UpdateDetails();
            UpdateActionButtons();
        }

        private void UpdateDetails()
        {
            KnowledgeBaseSnapshotEntry? snapshot = GetSelectedSnapshot();
            if (snapshot == null)
            {
                _txtDetails.Text = _snapshots.Count == 0
                    ? "Снимков для текущей базы нет."
                    : string.Empty;
                return;
            }

            string metadataPath = string.IsNullOrWhiteSpace(snapshot.MetadataPath)
                ? "нет"
                : snapshot.MetadataPath;

            _txtDetails.Text = string.Join(
                Environment.NewLine,
                $"Создан: {FormatCreatedAt(snapshot.CreatedAt)}",
                $"Тип: {FormatKind(snapshot.Kind)}",
                $"Файл снимка: {snapshot.SnapshotPath}",
                $"Метаданные: {metadataPath}",
                $"Исходный файл: {snapshot.SourcePath}",
                $"Размер: {FormatSize(snapshot.SizeBytes)}",
                $"Примечание: {FormatNote(snapshot.Note)}");
        }

        private KnowledgeBaseSnapshotEntry? GetSelectedSnapshot()
        {
            if (_grid.SelectedRows.Count > 0 &&
                _grid.SelectedRows[0].Tag is KnowledgeBaseSnapshotEntry selectedSnapshot)
            {
                return selectedSnapshot;
            }

            return _grid.CurrentRow?.Tag as KnowledgeBaseSnapshotEntry;
        }

        private IReadOnlyList<KnowledgeBaseSnapshotEntry> GetSelectedSnapshots()
        {
            var selected = _grid.SelectedRows
                .Cast<DataGridViewRow>()
                .OrderBy(static row => row.Index)
                .Select(static row => row.Tag)
                .OfType<KnowledgeBaseSnapshotEntry>()
                .ToList();

            if (selected.Count > 0)
                return selected;

            KnowledgeBaseSnapshotEntry? current = GetSelectedSnapshot();
            return current == null
                ? Array.Empty<KnowledgeBaseSnapshotEntry>()
                : new[] { current };
        }

        private void UpdateActionButtons()
        {
            int selectedCount = SelectedSnapshots.Count;
            _btnRestore.Enabled = selectedCount == 1;
            _btnCompare.Enabled = selectedCount == 2;
        }

        private static DataGridViewTextBoxColumn CreateTextColumn(
            string name,
            string headerText,
            int width) =>
            new()
            {
                Name = name,
                HeaderText = headerText,
                Width = width,
                SortMode = DataGridViewColumnSortMode.NotSortable
            };

        private static string FormatCreatedAt(DateTimeOffset createdAt) =>
            createdAt == DateTimeOffset.MinValue
                ? string.Empty
                : createdAt.ToLocalTime().ToString("dd.MM.yyyy HH:mm:ss", CultureInfo.InvariantCulture);

        private static string FormatKind(string kind)
        {
            return kind?.Trim() switch
            {
                "manual" => "Ручной",
                "before-save" => "Перед сохранением",
                "before-restore" => "Перед восстановлением",
                "" or null => "Снимок",
                var value => value
            };
        }

        private static string FormatNote(string note) =>
            string.IsNullOrWhiteSpace(note) ? "-" : note.Trim();

        private static string FormatSize(long sizeBytes)
        {
            if (sizeBytes < 1024)
                return $"{sizeBytes} Б";

            double size = sizeBytes / 1024D;
            if (size < 1024D)
                return $"{size:0.0} КБ";

            size /= 1024D;
            return $"{size:0.0} МБ";
        }
    }
}
