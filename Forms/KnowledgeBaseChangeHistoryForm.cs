using System.Globalization;
using AsutpKnowledgeBase.Services;

namespace AsutpKnowledgeBase
{
    public sealed class KnowledgeBaseChangeHistoryForm : Form
    {
        private readonly IReadOnlyList<KnowledgeBaseChangeLogEntry> _entries;
        private DataGridView _grid = null!;
        private TextBox _txtDetails = null!;
        private Label _lblSummary = null!;

        public KnowledgeBaseChangeHistoryForm(IReadOnlyList<KnowledgeBaseChangeLogEntry> entries)
        {
            _entries = entries ?? Array.Empty<KnowledgeBaseChangeLogEntry>();

            Text = "История изменений";
            StartPosition = FormStartPosition.CenterParent;
            MinimizeBox = false;
            ShowInTaskbar = false;
            Size = new Size(980, 620);
            MinimumSize = new Size(760, 460);
            AppIconProvider.Apply(this);

            Controls.Add(CreateLayout());
            BindEntries();
        }

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
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 140));
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
            buttonsPanel.Controls.Add(btnClose);
            layout.Controls.Add(buttonsPanel, 0, 3);
            AcceptButton = btnClose;
            CancelButton = btnClose;

            return layout;
        }

        private DataGridView CreateGrid()
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
                MultiSelect = false,
                ReadOnly = true,
                RowHeadersVisible = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect
            };

            grid.Columns.Add(CreateTextColumn("CreatedAt", "Время", 150));
            grid.Columns.Add(CreateTextColumn("ActionKind", "Действие", 150));
            grid.Columns.Add(CreateTextColumn("Summary", "Описание", 360));
            grid.Columns.Add(CreateTextColumn("Details", "Детали", 420));
            grid.SelectionChanged += (_, _) => UpdateDetails();
            grid.DataError += (_, e) => e.ThrowException = false;
            return grid;
        }

        private void BindEntries()
        {
            _grid.Rows.Clear();
            foreach (KnowledgeBaseChangeLogEntry entry in _entries)
            {
                int rowIndex = _grid.Rows.Add();
                DataGridViewRow row = _grid.Rows[rowIndex];
                row.Tag = entry;
                row.Cells["CreatedAt"].Value = FormatCreatedAt(entry.CreatedAt);
                row.Cells["ActionKind"].Value = FormatActionKind(entry.ActionKind);
                row.Cells["Summary"].Value = entry.Summary;
                row.Cells["Details"].Value = entry.Details;
            }

            if (_grid.Rows.Count > 0)
            {
                _grid.Rows[0].Selected = true;
                _grid.CurrentCell = _grid.Rows[0].Cells["CreatedAt"];
            }

            _lblSummary.Text = _entries.Count == 0
                ? "История изменений пуста."
                : $"Записей в истории: {_entries.Count}";
            UpdateDetails();
        }

        private void UpdateDetails()
        {
            KnowledgeBaseChangeLogEntry? entry = GetSelectedEntry();
            if (entry == null)
            {
                _txtDetails.Text = _entries.Count == 0 ? "История изменений пуста." : string.Empty;
                return;
            }

            _txtDetails.Text = string.Join(
                Environment.NewLine,
                $"Время: {FormatCreatedAt(entry.CreatedAt)}",
                $"Действие: {FormatActionKind(entry.ActionKind)}",
                $"Описание: {entry.Summary}",
                $"Детали: {entry.Details}",
                $"ID: {entry.ChangeId}");
        }

        private KnowledgeBaseChangeLogEntry? GetSelectedEntry()
        {
            if (_grid.SelectedRows.Count > 0 &&
                _grid.SelectedRows[0].Tag is KnowledgeBaseChangeLogEntry selectedEntry)
            {
                return selectedEntry;
            }

            return _grid.CurrentRow?.Tag as KnowledgeBaseChangeLogEntry;
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
    }
}
