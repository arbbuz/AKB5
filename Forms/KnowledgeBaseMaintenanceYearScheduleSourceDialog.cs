using System.Globalization;
using AsutpKnowledgeBase.Models;
using AsutpKnowledgeBase.Services;

namespace AsutpKnowledgeBase
{
    public sealed class KnowledgeBaseMaintenanceYearScheduleSourceDialog : Form
    {
        private const string OwnerNodeIdColumnName = "OwnerNodeId";
        private const string SequenceNumberColumnName = "SequenceNumber";
        private const string PathColumnName = "Path";
        private const string NodeNameColumnName = "NodeName";
        private const string InventoryNumberColumnName = "InventoryNumber";
        private const string IncludedColumnName = "Included";
        private const string SourceColumnName = "Source";
        private const int MonthColumnWidth = 88;

        private static readonly string[] WorkKindValues = { string.Empty, "ТО1", "ТО2", "ТО3" };

        private readonly List<KnowledgeBaseMaintenanceYearScheduleSourceRow> _rows;
        private readonly DataGridView _grid = new BufferedDataGridView();

        public KnowledgeBaseMaintenanceYearScheduleSourceDialog(
            string workshopName,
            IReadOnlyList<KnowledgeBaseMaintenanceYearScheduleSourceRow> rows)
        {
            _rows = (rows ?? Array.Empty<KnowledgeBaseMaintenanceYearScheduleSourceRow>())
                .Select(CloneRow)
                .ToList();

            Text = string.IsNullOrWhiteSpace(workshopName)
                ? "Источник годового графика ТО"
                : $"Источник годового графика ТО - {workshopName}";
            StartPosition = FormStartPosition.CenterParent;
            MinimizeBox = false;
            ShowInTaskbar = false;
            Size = new Size(1180, 650);
            MinimumSize = new Size(920, 520);
            WindowState = FormWindowState.Maximized;

            Controls.Add(CreateLayout());

            AcceptButton = Controls
                .Find("btnOk", searchAllChildren: true)
                .OfType<Button>()
                .FirstOrDefault();
            CancelButton = Controls
                .Find("btnCancel", searchAllChildren: true)
                .OfType<Button>()
                .FirstOrDefault();

            Shown += (_, _) => FocusFirstEditableCell();
        }

        public List<KnowledgeBaseMaintenanceYearScheduleSourceRow> ResultRows { get; private set; } = new();

        private Control CreateLayout()
        {
            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(12),
                ColumnCount = 1,
                RowCount = 3
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            var summary = new Label
            {
                AutoSize = true,
                Text = $"Профилей ТО: {_rows.Count}",
                Margin = new Padding(0, 0, 0, 8)
            };
            layout.Controls.Add(summary, 0, 0);

            ConfigureGrid();
            BindRows();
            layout.Controls.Add(_grid, 0, 1);

            var buttonsPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoSize = true,
                FlowDirection = FlowDirection.RightToLeft,
                Margin = new Padding(0, 12, 0, 0)
            };

            var btnOk = new Button
            {
                Name = "btnOk",
                Text = "Сохранить",
                AutoSize = true
            };
            btnOk.Click += (_, _) => Submit();

            var btnCancel = new Button
            {
                Name = "btnCancel",
                Text = "Отмена",
                AutoSize = true,
                DialogResult = DialogResult.Cancel
            };

            buttonsPanel.Controls.Add(btnOk);
            buttonsPanel.Controls.Add(btnCancel);
            layout.Controls.Add(buttonsPanel, 0, 2);

            return layout;
        }

        private void ConfigureGrid()
        {
            _grid.Dock = DockStyle.Fill;
            _grid.AllowUserToAddRows = false;
            _grid.AllowUserToDeleteRows = false;
            _grid.AllowUserToResizeRows = false;
            _grid.AutoGenerateColumns = false;
            _grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;
            _grid.BackgroundColor = SystemColors.Window;
            _grid.BorderStyle = BorderStyle.Fixed3D;
            _grid.ClipboardCopyMode = DataGridViewClipboardCopyMode.EnableWithoutHeaderText;
            _grid.EditMode = DataGridViewEditMode.EditOnEnter;
            _grid.MultiSelect = true;
            _grid.RowHeadersVisible = false;
            _grid.SelectionMode = DataGridViewSelectionMode.CellSelect;
            _grid.ShowCellToolTips = false;
            _grid.RowTemplate.Height = 24;

            _grid.Columns.Add(CreateTextColumn(OwnerNodeIdColumnName, "OwnerNodeId", 120, readOnly: true, visible: false));
            _grid.Columns.Add(CreateTextColumn(SequenceNumberColumnName, "№ п.п.", 58, readOnly: true));
            _grid.Columns.Add(CreateTextColumn(PathColumnName, "Путь", 330, readOnly: true));
            _grid.Columns.Add(CreateTextColumn(NodeNameColumnName, "Узел", 170, readOnly: true));
            _grid.Columns.Add(CreateTextColumn(InventoryNumberColumnName, "Инв. №", 95, readOnly: true));
            _grid.Columns.Add(CreateTextColumn(IncludedColumnName, "В графике", 80, readOnly: true));
            _grid.Columns.Add(CreateTextColumn(SourceColumnName, "Источник", 82, readOnly: true));

            for (int month = 1; month <= 12; month++)
                _grid.Columns.Add(CreateMonthColumn(month));

            _grid.CurrentCellDirtyStateChanged += (_, _) =>
            {
                if (_grid.IsCurrentCellDirty)
                    _grid.CommitEdit(DataGridViewDataErrorContexts.Commit);
            };
            _grid.CellValueChanged += (_, e) =>
            {
                if (e.RowIndex >= 0 && IsMonthColumn(_grid.Columns[e.ColumnIndex].Name))
                    UpdateSourceCell(_grid.Rows[e.RowIndex]);
            };
            _grid.DataError += (_, e) => e.ThrowException = false;
        }

        private void BindRows()
        {
            _grid.SuspendLayout();
            _grid.Rows.Clear();
            var gridRows = new List<DataGridViewRow>(_rows.Count);

            foreach (KnowledgeBaseMaintenanceYearScheduleSourceRow sourceRow in _rows)
            {
                var gridRow = new DataGridViewRow();
                gridRow.CreateCells(_grid);
                gridRow.Tag = sourceRow;
                SetBufferedCellValue(gridRow, OwnerNodeIdColumnName, sourceRow.OwnerNodeId);
                SetBufferedCellValue(gridRow, SequenceNumberColumnName, sourceRow.SequenceNumber > 0 ? sourceRow.SequenceNumber.ToString(CultureInfo.InvariantCulture) : string.Empty);
                SetBufferedCellValue(gridRow, PathColumnName, sourceRow.Path);
                SetBufferedCellValue(gridRow, NodeNameColumnName, sourceRow.NodeName);
                SetBufferedCellValue(gridRow, InventoryNumberColumnName, sourceRow.InventoryNumber);
                SetBufferedCellValue(gridRow, IncludedColumnName, sourceRow.IsIncludedInSchedule ? "Да" : "Нет");
                SetBufferedCellValue(gridRow, SourceColumnName, sourceRow.HasManualSchedule ? "Ручной" : "Авто");

                Dictionary<int, KbMaintenanceWorkKind> entriesByMonth = sourceRow.YearScheduleEntries
                    .Where(static entry => entry.Month is >= 1 and <= 12)
                    .GroupBy(static entry => entry.Month)
                    .ToDictionary(
                        static group => group.Key,
                        static group => group.OrderByDescending(static entry => entry.WorkKind).First().WorkKind);

                for (int month = 1; month <= 12; month++)
                {
                    SetBufferedCellValue(
                        gridRow,
                        GetMonthColumnName(month),
                        entriesByMonth.TryGetValue(month, out KbMaintenanceWorkKind workKind)
                            ? FormatWorkKind(workKind)
                            : string.Empty);
                }

                gridRows.Add(gridRow);
            }

            if (gridRows.Count > 0)
                _grid.Rows.AddRange(gridRows.ToArray());

            _grid.ResumeLayout();
        }

        private void SetBufferedCellValue(DataGridViewRow gridRow, string columnName, object? value)
        {
            int columnIndex = _grid.Columns[columnName].Index;
            gridRow.Cells[columnIndex].Value = value;
        }

        private void Submit()
        {
            if (_grid.IsCurrentCellDirty)
                _grid.CommitEdit(DataGridViewDataErrorContexts.Commit);
            _grid.EndEdit();

            var resultRows = new List<KnowledgeBaseMaintenanceYearScheduleSourceRow>();
            foreach (DataGridViewRow gridRow in _grid.Rows)
            {
                if (gridRow.IsNewRow || gridRow.Tag is not KnowledgeBaseMaintenanceYearScheduleSourceRow sourceRow)
                    continue;

                var entries = new List<KbMaintenanceYearScheduleEntry>();
                for (int month = 1; month <= 12; month++)
                {
                    DataGridViewCell cell = gridRow.Cells[GetMonthColumnName(month)];
                    string value = Convert.ToString(cell.Value)?.Trim() ?? string.Empty;
                    if (string.IsNullOrWhiteSpace(value))
                        continue;

                    if (!TryParseWorkKind(value, out KbMaintenanceWorkKind workKind))
                    {
                        _grid.CurrentCell = cell;
                        MessageBox.Show(
                            this,
                            $"Месяц {GetMonthHeader(month)}: выберите ТО1, ТО2, ТО3 или пустое значение.",
                            "Источник годового графика ТО",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Warning);
                        return;
                    }

                    entries.Add(new KbMaintenanceYearScheduleEntry
                    {
                        Month = month,
                        WorkKind = workKind
                    });
                }

                resultRows.Add(new KnowledgeBaseMaintenanceYearScheduleSourceRow
                {
                    OwnerNodeId = sourceRow.OwnerNodeId,
                    Path = sourceRow.Path,
                    NodeName = sourceRow.NodeName,
                    InventoryNumber = sourceRow.InventoryNumber,
                    IsIncludedInSchedule = sourceRow.IsIncludedInSchedule,
                    TreeOrder = sourceRow.TreeOrder,
                    YearScheduleEntries = entries
                });
            }

            ResultRows = resultRows;
            DialogResult = DialogResult.OK;
            Close();
        }

        private void FocusFirstEditableCell()
        {
            if (_grid.Rows.Count == 0)
                return;

            _grid.CurrentCell = _grid.Rows[0].Cells[GetMonthColumnName(1)];
            _grid.Focus();
        }

        private void UpdateSourceCell(DataGridViewRow gridRow)
        {
            bool hasManualSource = Enumerable
                .Range(1, 12)
                .Any(month => !string.IsNullOrWhiteSpace(Convert.ToString(gridRow.Cells[GetMonthColumnName(month)].Value)));
            gridRow.Cells[SourceColumnName].Value = hasManualSource ? "Ручной" : "Авто";
        }

        private static DataGridViewTextBoxColumn CreateTextColumn(
            string name,
            string headerText,
            int width,
            bool readOnly,
            bool visible = true) =>
            new()
            {
                Name = name,
                HeaderText = headerText,
                Width = width,
                ReadOnly = readOnly,
                Visible = visible,
                SortMode = DataGridViewColumnSortMode.NotSortable
            };

        private static DataGridViewComboBoxColumn CreateMonthColumn(int month)
        {
            var column = new DataGridViewComboBoxColumn
            {
                Name = GetMonthColumnName(month),
                HeaderText = GetMonthHeader(month),
                Width = MonthColumnWidth,
                MinimumWidth = MonthColumnWidth,
                DisplayStyle = DataGridViewComboBoxDisplayStyle.DropDownButton,
                DisplayStyleForCurrentCellOnly = true,
                DropDownWidth = MonthColumnWidth,
                FlatStyle = FlatStyle.Flat,
                SortMode = DataGridViewColumnSortMode.NotSortable
            };
            column.Items.AddRange(WorkKindValues);
            column.DefaultCellStyle.NullValue = string.Empty;
            return column;
        }

        private static bool IsMonthColumn(string columnName) =>
            columnName.StartsWith("M", StringComparison.Ordinal) &&
            columnName.Length == 3;

        private static string GetMonthColumnName(int month) => $"M{month:00}";

        private static string GetMonthHeader(int month) =>
            month switch
            {
                1 => "Янв",
                2 => "Фев",
                3 => "Мар",
                4 => "Апр",
                5 => "Май",
                6 => "Июн",
                7 => "Июл",
                8 => "Авг",
                9 => "Сен",
                10 => "Окт",
                11 => "Ноя",
                12 => "Дек",
                _ => month.ToString()
            };

        private static KnowledgeBaseMaintenanceYearScheduleSourceRow CloneRow(
            KnowledgeBaseMaintenanceYearScheduleSourceRow row) =>
            new()
            {
                OwnerNodeId = row.OwnerNodeId,
                Path = row.Path,
                NodeName = row.NodeName,
                InventoryNumber = row.InventoryNumber,
                SequenceNumber = row.SequenceNumber,
                SystemNodeId = row.SystemNodeId,
                SystemName = row.SystemName,
                SystemInventoryNumber = row.SystemInventoryNumber,
                SystemTreeOrder = row.SystemTreeOrder,
                IsIncludedInSchedule = row.IsIncludedInSchedule,
                TreeOrder = row.TreeOrder,
                SourceRowNumber = row.SourceRowNumber,
                YearScheduleEntries = KnowledgeBaseMaintenanceYearScheduleSourceService.CloneYearScheduleEntries(row.YearScheduleEntries)
            };

        private static string FormatWorkKind(KbMaintenanceWorkKind workKind) => workKind switch
        {
            KbMaintenanceWorkKind.To1 => "ТО1",
            KbMaintenanceWorkKind.To2 => "ТО2",
            KbMaintenanceWorkKind.To3 => "ТО3",
            _ => string.Empty
        };

        private static bool TryParseWorkKind(string value, out KbMaintenanceWorkKind workKind)
        {
            workKind = value.Trim().ToUpperInvariant() switch
            {
                "ТО1" => KbMaintenanceWorkKind.To1,
                "ТО2" => KbMaintenanceWorkKind.To2,
                "ТО3" => KbMaintenanceWorkKind.To3,
                _ => KbMaintenanceWorkKind.To1
            };

            return value.Trim().ToUpperInvariant() is "ТО1" or "ТО2" or "ТО3";
        }

        private sealed class BufferedDataGridView : DataGridView
        {
            public BufferedDataGridView()
            {
                DoubleBuffered = true;
                SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint, true);
            }
        }
    }
}
