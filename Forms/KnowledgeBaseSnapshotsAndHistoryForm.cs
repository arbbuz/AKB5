using System.Globalization;
using AsutpKnowledgeBase.Services;

namespace AsutpKnowledgeBase
{
    public enum KnowledgeBaseSnapshotsAndHistoryAction
    {
        None,
        CreateSnapshot,
        Restore,
        Compare
    }

    public sealed class KnowledgeBaseSnapshotsAndHistoryForm : Form
    {
        private readonly IReadOnlyList<KnowledgeBaseSnapshotEntry> _snapshots;
        private readonly IReadOnlyList<KnowledgeBaseChangeLogEntry> _historyEntries;
        private readonly string _snapshotDirectoryPath;
        private readonly bool _isHistorySupported;
        private readonly string _historyErrorMessage;

        private DataGridView _snapshotGrid = null!;
        private DataGridView _historyGrid = null!;
        private TextBox _txtSnapshotDetails = null!;
        private TextBox _txtHistoryDetails = null!;
        private Label _lblSnapshotSummary = null!;
        private Label _lblHistorySummary = null!;
        private Button _btnRestore = null!;
        private Button _btnCompare = null!;

        public KnowledgeBaseSnapshotsAndHistoryForm(
            IReadOnlyList<KnowledgeBaseSnapshotEntry> snapshots,
            string snapshotDirectoryPath,
            IReadOnlyList<KnowledgeBaseChangeLogEntry> historyEntries,
            bool isHistorySupported,
            string? historyErrorMessage)
        {
            _snapshots = snapshots ?? Array.Empty<KnowledgeBaseSnapshotEntry>();
            _snapshotDirectoryPath = snapshotDirectoryPath?.Trim() ?? string.Empty;
            _historyEntries = historyEntries ?? Array.Empty<KnowledgeBaseChangeLogEntry>();
            _isHistorySupported = isHistorySupported;
            _historyErrorMessage = historyErrorMessage?.Trim() ?? string.Empty;

            Text = "Снимки и история базы";
            StartPosition = FormStartPosition.CenterParent;
            MinimizeBox = false;
            ShowInTaskbar = false;
            Size = new Size(1120, 680);
            MinimumSize = new Size(900, 540);
            AppIconProvider.Apply(this);

            Controls.Add(CreateLayout());
            BindSnapshots();
            BindHistory();
        }

        public KnowledgeBaseSnapshotsAndHistoryAction SelectedAction { get; private set; }

        public IReadOnlyList<KnowledgeBaseSnapshotEntry> SelectedSnapshots => GetSelectedSnapshots();

        private TableLayoutPanel CreateLayout()
        {
            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(12),
                ColumnCount = 1,
                RowCount = 2
            };
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            var tabs = new TabControl
            {
                Dock = DockStyle.Fill
            };
            tabs.TabPages.Add(CreateSnapshotsTab());
            tabs.TabPages.Add(CreateHistoryTab());

            layout.Controls.Add(tabs, 0, 0);
            layout.Controls.Add(CreateBottomPanel(), 0, 1);
            return layout;
        }

        private TabPage CreateSnapshotsTab()
        {
            var tab = new TabPage("Снимки");
            var layout = CreateTabLayout();

            _lblSnapshotSummary = new Label
            {
                AutoSize = true,
                Margin = new Padding(0, 0, 0, 8)
            };
            layout.Controls.Add(_lblSnapshotSummary, 0, 0);

            _snapshotGrid = CreateSnapshotGrid();
            layout.Controls.Add(_snapshotGrid, 0, 1);

            _txtSnapshotDetails = CreateDetailsTextBox();
            layout.Controls.Add(_txtSnapshotDetails, 0, 2);

            tab.Controls.Add(layout);
            return tab;
        }

        private TabPage CreateHistoryTab()
        {
            var tab = new TabPage("История");
            var layout = CreateTabLayout();

            _lblHistorySummary = new Label
            {
                AutoSize = true,
                Margin = new Padding(0, 0, 0, 8)
            };
            layout.Controls.Add(_lblHistorySummary, 0, 0);

            _historyGrid = CreateHistoryGrid();
            layout.Controls.Add(_historyGrid, 0, 1);

            _txtHistoryDetails = CreateDetailsTextBox();
            layout.Controls.Add(_txtHistoryDetails, 0, 2);

            tab.Controls.Add(layout);
            return tab;
        }

        private static TableLayoutPanel CreateTabLayout()
        {
            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(8),
                ColumnCount = 1,
                RowCount = 3
            };
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 150));
            return layout;
        }

        private static TextBox CreateDetailsTextBox() =>
            new()
            {
                Dock = DockStyle.Fill,
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Vertical,
                Font = new Font("Consolas", 9F),
                Margin = new Padding(0, 10, 0, 0)
            };

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
            _btnCompare.Click += (_, _) => CloseWithAction(KnowledgeBaseSnapshotsAndHistoryAction.Compare);

            _btnRestore = new Button
            {
                Text = "Восстановить",
                AutoSize = true,
                Enabled = false
            };
            _btnRestore.Click += (_, _) => CloseWithAction(KnowledgeBaseSnapshotsAndHistoryAction.Restore);

            var btnCreateSnapshot = new Button
            {
                Text = "Создать снимок",
                AutoSize = true
            };
            btnCreateSnapshot.Click += (_, _) => CloseWithAction(KnowledgeBaseSnapshotsAndHistoryAction.CreateSnapshot);

            buttonsPanel.Controls.Add(btnClose);
            buttonsPanel.Controls.Add(_btnCompare);
            buttonsPanel.Controls.Add(_btnRestore);
            buttonsPanel.Controls.Add(btnCreateSnapshot);
            AcceptButton = btnClose;
            CancelButton = btnClose;
            return buttonsPanel;
        }

        private void CloseWithAction(KnowledgeBaseSnapshotsAndHistoryAction action)
        {
            SelectedAction = action;
            DialogResult = DialogResult.OK;
            Close();
        }

        private DataGridView CreateSnapshotGrid()
        {
            var grid = CreateBaseGrid(multiSelect: true);
            grid.Columns.Add(CreateTextColumn("CreatedAt", "Создан", 150));
            grid.Columns.Add(CreateTextColumn("Kind", "Тип", 120));
            grid.Columns.Add(CreateTextColumn("SnapshotFileName", "Файл снимка", 260));
            grid.Columns.Add(CreateTextColumn("SourcePath", "Исходный файл", 220));
            grid.Columns.Add(CreateTextColumn("Size", "Размер", 90));
            grid.Columns.Add(CreateTextColumn("Note", "Примечание", 320));
            grid.SelectionChanged += (_, _) =>
            {
                UpdateSnapshotDetails();
                UpdateActionButtons();
            };
            return grid;
        }

        private DataGridView CreateHistoryGrid()
        {
            var grid = CreateBaseGrid(multiSelect: false);
            grid.Columns.Add(CreateTextColumn("CreatedAt", "Время", 150));
            grid.Columns.Add(CreateTextColumn("ActionKind", "Действие", 150));
            grid.Columns.Add(CreateTextColumn("Summary", "Описание", 360));
            grid.Columns.Add(CreateTextColumn("Details", "Детали", 420));
            grid.SelectionChanged += (_, _) => UpdateHistoryDetails();
            return grid;
        }

        private static DataGridView CreateBaseGrid(bool multiSelect)
        {
            var grid = new DataGridView
            {
                Dock = DockStyle.Fill,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToResizeRows = false,
                AutoGenerateColumns = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None,
                BackgroundColor = SystemColors.Window,
                BorderStyle = BorderStyle.Fixed3D,
                MultiSelect = multiSelect,
                ReadOnly = true,
                RowHeadersVisible = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                ShowCellToolTips = false
            };
            grid.DataError += (_, e) => e.ThrowException = false;
            return grid;
        }

        private void BindSnapshots()
        {
            _snapshotGrid.Rows.Clear();
            foreach (KnowledgeBaseSnapshotEntry snapshot in _snapshots)
            {
                int rowIndex = _snapshotGrid.Rows.Add();
                DataGridViewRow row = _snapshotGrid.Rows[rowIndex];
                row.Tag = snapshot;
                row.Cells["CreatedAt"].Value = FormatCreatedAt(snapshot.CreatedAt);
                row.Cells["Kind"].Value = FormatSnapshotKind(snapshot.Kind);
                row.Cells["SnapshotFileName"].Value = snapshot.SnapshotFileName;
                row.Cells["SourcePath"].Value = Path.GetFileName(snapshot.SourcePath);
                row.Cells["Size"].Value = FormatSize(snapshot.SizeBytes);
                row.Cells["Note"].Value = FormatOptional(snapshot.Note);
            }

            if (_snapshotGrid.Rows.Count > 0)
            {
                _snapshotGrid.Rows[0].Selected = true;
                _snapshotGrid.CurrentCell = _snapshotGrid.Rows[0].Cells["CreatedAt"];
            }

            _lblSnapshotSummary.Text = _snapshots.Count == 0
                ? $"Снимков для текущей базы нет. Каталог: {_snapshotDirectoryPath}"
                : $"Снимков: {_snapshots.Count}. Каталог: {_snapshotDirectoryPath}";
            UpdateSnapshotDetails();
            UpdateActionButtons();
        }

        private void BindHistory()
        {
            _historyGrid.Rows.Clear();

            if (!_isHistorySupported)
            {
                _lblHistorySummary.Text = string.IsNullOrWhiteSpace(_historyErrorMessage)
                    ? "История изменений доступна только для базы .akb."
                    : $"История изменений недоступна: {_historyErrorMessage}";
                _txtHistoryDetails.Text = _lblHistorySummary.Text;
                return;
            }

            foreach (KnowledgeBaseChangeLogEntry entry in _historyEntries)
            {
                int rowIndex = _historyGrid.Rows.Add();
                DataGridViewRow row = _historyGrid.Rows[rowIndex];
                row.Tag = entry;
                row.Cells["CreatedAt"].Value = FormatCreatedAt(entry.CreatedAt);
                row.Cells["ActionKind"].Value = FormatActionKind(entry.ActionKind);
                row.Cells["Summary"].Value = entry.Summary;
                row.Cells["Details"].Value = entry.Details;
            }

            if (_historyGrid.Rows.Count > 0)
            {
                _historyGrid.Rows[0].Selected = true;
                _historyGrid.CurrentCell = _historyGrid.Rows[0].Cells["CreatedAt"];
            }

            _lblHistorySummary.Text = _historyEntries.Count == 0
                ? "История изменений пуста."
                : $"Записей в истории: {_historyEntries.Count}";
            UpdateHistoryDetails();
        }

        private void UpdateSnapshotDetails()
        {
            KnowledgeBaseSnapshotEntry? snapshot = GetSelectedSnapshot();
            if (snapshot == null)
            {
                _txtSnapshotDetails.Text = _snapshots.Count == 0
                    ? "Снимков для текущей базы нет."
                    : string.Empty;
                return;
            }

            string metadataPath = string.IsNullOrWhiteSpace(snapshot.MetadataPath)
                ? "нет"
                : snapshot.MetadataPath;

            _txtSnapshotDetails.Text = string.Join(
                Environment.NewLine,
                $"Создан: {FormatCreatedAt(snapshot.CreatedAt)}",
                $"Тип: {FormatSnapshotKind(snapshot.Kind)}",
                $"Файл снимка: {snapshot.SnapshotPath}",
                $"Метаданные: {metadataPath}",
                $"Исходный файл: {snapshot.SourcePath}",
                $"Размер: {FormatSize(snapshot.SizeBytes)}",
                $"Примечание: {FormatOptional(snapshot.Note)}");
        }

        private void UpdateHistoryDetails()
        {
            if (!_isHistorySupported)
                return;

            KnowledgeBaseChangeLogEntry? entry = GetSelectedHistoryEntry();
            if (entry == null)
            {
                _txtHistoryDetails.Text = _historyEntries.Count == 0 ? "История изменений пуста." : string.Empty;
                return;
            }

            _txtHistoryDetails.Text = string.Join(
                Environment.NewLine,
                $"Время: {FormatCreatedAt(entry.CreatedAt)}",
                $"Действие: {FormatActionKind(entry.ActionKind)}",
                $"Описание: {entry.Summary}",
                $"Детали: {entry.Details}",
                $"ID: {entry.ChangeId}");
        }

        private KnowledgeBaseSnapshotEntry? GetSelectedSnapshot()
        {
            if (_snapshotGrid.SelectedRows.Count > 0 &&
                _snapshotGrid.SelectedRows[0].Tag is KnowledgeBaseSnapshotEntry selectedSnapshot)
            {
                return selectedSnapshot;
            }

            return _snapshotGrid.CurrentRow?.Tag as KnowledgeBaseSnapshotEntry;
        }

        private IReadOnlyList<KnowledgeBaseSnapshotEntry> GetSelectedSnapshots()
        {
            var selected = _snapshotGrid.SelectedRows
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

        private KnowledgeBaseChangeLogEntry? GetSelectedHistoryEntry()
        {
            if (_historyGrid.SelectedRows.Count > 0 &&
                _historyGrid.SelectedRows[0].Tag is KnowledgeBaseChangeLogEntry selectedEntry)
            {
                return selectedEntry;
            }

            return _historyGrid.CurrentRow?.Tag as KnowledgeBaseChangeLogEntry;
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

        private static string FormatSnapshotKind(string kind) =>
            kind?.Trim() switch
            {
                "manual" => "Ручной",
                "before-save" => "Перед сохранением",
                "before-restore" => "Перед восстановлением",
                "" or null => "Снимок",
                var value => value
            };

        private static string FormatActionKind(string actionKind) =>
            actionKind?.Trim() switch
            {
                "save" => "Сохранение",
                "import" => "Импорт",
                "migration" => "Миграция",
                "manual-snapshot" => "Ручной снимок",
                "restore" => "Восстановление",
                "catalog-template-import" => "Импорт каталога/шаблонов",
                "" or null => "Действие",
                var value => value
            };

        private static string FormatOptional(string value) =>
            string.IsNullOrWhiteSpace(value) ? "-" : value.Trim();

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
