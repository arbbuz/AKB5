using System.ComponentModel;
using AsutpKnowledgeBase.Services;

namespace AsutpKnowledgeBase
{
    public enum KnowledgeBaseActsJournalAction
    {
        None = 0,
        Open = 1,
        GenerateDocument = 2,
        DeleteDraft = 3,
        SignAct = 4,
        CancelAct = 5,
        OpenDocument = 6
    }

    public sealed class KnowledgeBaseActsJournalActionRequestedEventArgs : EventArgs
    {
        public KnowledgeBaseActsJournalActionRequestedEventArgs(
            string actId,
            KnowledgeBaseActsJournalAction action)
        {
            ActId = actId;
            Action = action;
        }

        public string ActId { get; }

        public KnowledgeBaseActsJournalAction Action { get; }
    }

    public sealed class KnowledgeBaseActsJournalForm : Form
    {
        private const int FilterIconHeaderPadding = 18;
        private const int FilterIconSize = 10;
        private const int FilterIconRightPadding = 6;

        private readonly DataGridView _grid;
        private readonly ComboBox _yearFilter;
        private readonly Button _btnClearFilters;
        private readonly Button _btnOpen;
        private readonly Button _btnOpenDocument;
        private readonly Button _btnGenerateDocument;
        private readonly Button _btnDeleteDraft;
        private readonly Button _btnCancelAct;
        private readonly Button _btnSignAct;
        private readonly ContextMenuStrip _rowContextMenu;
        private readonly KnowledgeBaseActJournalFilterService _filterService = new();
        private readonly KnowledgeBaseActJournalFilterState _columnFilterState = new();
        private readonly Dictionary<string, string> _columnHeaderTexts = new(StringComparer.Ordinal);
        private readonly List<KnowledgeBaseActJournalRow> _allRows = new();
        private Dictionary<string, int> _columnWidths = new(StringComparer.Ordinal);
        private FormWindowState _lastNonMinimizedWindowState = FormWindowState.Maximized;
        private bool _isActionInProgress;
        private bool _isUpdatingYearFilter;
        private bool _isApplyingColumnWidths;

        public KnowledgeBaseActsJournalForm(
            IEnumerable<KnowledgeBaseActJournalRow> rows,
            string? preferredActId = null,
            IReadOnlyDictionary<string, int>? columnWidths = null)
        {
            _columnWidths = NormalizeColumnWidths(columnWidths);
            Text = "Журнал актов";
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.Sizable;
            MinimizeBox = true;
            MaximizeBox = true;
            ShowInTaskbar = true;
            MinimumSize = new Size(940, 520);
            ClientSize = new Size(1120, 620);
            WindowState = FormWindowState.Maximized;
            AppIconProvider.Apply(this);

            var rootLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3
            };
            rootLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            rootLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            rootLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            _yearFilter = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Width = 120
            };
            _yearFilter.SelectedIndexChanged += (_, _) =>
            {
                if (!_isUpdatingYearFilter)
                    ApplyCurrentFilters(preferredActId: null);
            };

            var filterPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoSize = true,
                WrapContents = false,
                Padding = new Padding(12, 10, 12, 4)
            };
            filterPanel.Controls.Add(new Label
            {
                Text = "Год:",
                AutoSize = true,
                Margin = new Padding(0, 6, 8, 0)
            });
            filterPanel.Controls.Add(_yearFilter);

            _btnClearFilters = new Button
            {
                Text = "Сбросить все фильтры",
                AutoSize = true,
                Margin = new Padding(12, 2, 0, 0)
            };
            _btnClearFilters.Click += (_, _) => ClearAllFilters();
            filterPanel.Controls.Add(_btnClearFilters);

            _grid = CreateGrid();
            CaptureColumnHeaderTexts(_grid);
            ApplyGridColumnWidths(_grid, _columnWidths);
            _grid.SelectionChanged += (_, _) => UpdateButtonStates();
            _grid.ColumnWidthChanged += Grid_ColumnWidthChanged;
            _grid.ColumnHeaderMouseClick += Grid_ColumnHeaderMouseClick;
            _grid.CellPainting += Grid_CellPainting;
            _grid.CellMouseDown += Grid_CellMouseDown;
            _grid.CellDoubleClick += (_, e) =>
            {
                if (e.RowIndex >= 0)
                    RequestAction(KnowledgeBaseActsJournalAction.Open);
            };
            _rowContextMenu = new ContextMenuStrip();
            _rowContextMenu.Opening += RowContextMenu_Opening;
            _grid.ContextMenuStrip = _rowContextMenu;
            FormClosed += (_, _) => _rowContextMenu.Dispose();

            var buttonsPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.RightToLeft,
                WrapContents = true,
                AutoSize = true,
                Padding = new Padding(12, 8, 12, 12)
            };

            var btnClose = new Button
            {
                Text = "Закрыть",
                AutoSize = true
            };
            btnClose.Click += (_, _) => Close();
            _btnSignAct = CreateButton("Подписать", KnowledgeBaseActsJournalAction.SignAct);
            _btnCancelAct = CreateButton("Отменить", KnowledgeBaseActsJournalAction.CancelAct);
            _btnDeleteDraft = CreateButton("Удалить черновик", KnowledgeBaseActsJournalAction.DeleteDraft);
            _btnGenerateDocument = CreateButton("Сформировать DOCX", KnowledgeBaseActsJournalAction.GenerateDocument);
            _btnOpenDocument = CreateButton("Открыть DOCX", KnowledgeBaseActsJournalAction.OpenDocument);
            _btnOpen = CreateButton("Открыть", KnowledgeBaseActsJournalAction.Open);

            buttonsPanel.Controls.Add(btnClose);
            buttonsPanel.Controls.Add(_btnSignAct);
            buttonsPanel.Controls.Add(_btnCancelAct);
            buttonsPanel.Controls.Add(_btnDeleteDraft);
            buttonsPanel.Controls.Add(_btnGenerateDocument);
            buttonsPanel.Controls.Add(_btnOpenDocument);
            buttonsPanel.Controls.Add(_btnOpen);

            rootLayout.Controls.Add(filterPanel, 0, 0);
            rootLayout.Controls.Add(_grid, 0, 1);
            rootLayout.Controls.Add(buttonsPanel, 0, 2);
            Controls.Add(rootLayout);

            AcceptButton = _btnOpen;
            CancelButton = btnClose;
            ApplyRows(rows, preferredActId);
        }

        public event EventHandler<KnowledgeBaseActsJournalActionRequestedEventArgs>? ActionRequested;

        public event EventHandler? ColumnWidthsChanged;

        public Dictionary<string, int> GetColumnWidths() =>
            new(_columnWidths, StringComparer.Ordinal);

        public void RefreshRows(
            IEnumerable<KnowledgeBaseActJournalRow> rows,
            string? preferredActId)
        {
            ApplyRows(rows, preferredActId);
        }

        public void SetActionInProgress(bool inProgress)
        {
            _isActionInProgress = inProgress;
            _grid.Enabled = !inProgress;
            _yearFilter.Enabled = !inProgress;
            _btnClearFilters.Enabled = !inProgress && HasActiveFilters();
            UpdateButtonStates();
        }

        public void RestoreForActivation()
        {
            if (WindowState == FormWindowState.Minimized)
                WindowState = _lastNonMinimizedWindowState;

            Show();
            BringToFront();
            Activate();
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);

            if (WindowState != FormWindowState.Minimized)
                _lastNonMinimizedWindowState = WindowState;
        }

        private static DataGridView CreateGrid()
        {
            var grid = new BufferedDataGridView
            {
                Dock = DockStyle.Fill,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToResizeColumns = true,
                AllowUserToResizeRows = false,
                AutoGenerateColumns = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None,
                BackgroundColor = SystemColors.Window,
                BorderStyle = BorderStyle.Fixed3D,
                ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize,
                MultiSelect = false,
                ReadOnly = true,
                RowHeadersVisible = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                ShowCellToolTips = false
            };
            KnowledgeBaseWorkspaceVisuals.ConfigureGrid(grid);
            grid.DefaultCellStyle.SelectionBackColor = SystemColors.Highlight;
            grid.DefaultCellStyle.SelectionForeColor = SystemColors.HighlightText;
            grid.RowsDefaultCellStyle.SelectionBackColor = SystemColors.Highlight;
            grid.RowsDefaultCellStyle.SelectionForeColor = SystemColors.HighlightText;
            grid.AlternatingRowsDefaultCellStyle.SelectionBackColor = SystemColors.Highlight;
            grid.AlternatingRowsDefaultCellStyle.SelectionForeColor = SystemColors.HighlightText;

            AddColumn(grid, "ActDate", "Дата", 95);
            AddColumn(grid, "ActNumber", "Номер", 110);
            AddColumn(grid, "Status", "Статус", 120);
            AddColumn(grid, "ActType", "Тип", 170);
            AddColumn(grid, "Workshop", "Цех", 180);
            AddColumn(grid, "Object", "Объект", 300);
            AddColumn(grid, "Equipment", "Оборудование", 380);
            AddColumn(grid, "OrderNumber", "Заказной номер", 170);
            AddColumn(grid, "DocumentState", "Документ", 115);
            return grid;
        }

        private void CaptureColumnHeaderTexts(DataGridView grid)
        {
            _columnHeaderTexts.Clear();
            foreach (DataGridViewColumn column in grid.Columns)
            {
                if (!string.IsNullOrWhiteSpace(column.Name))
                    _columnHeaderTexts[column.Name] = column.HeaderText;
            }
        }

        private static void AddColumn(
            DataGridView grid,
            string name,
            string headerText,
            int width)
        {
            var column = new DataGridViewTextBoxColumn
            {
                Name = name,
                HeaderText = headerText,
                Width = width,
                MinimumWidth = Math.Min(80, width),
                AutoSizeMode = DataGridViewAutoSizeColumnMode.None,
                SortMode = DataGridViewColumnSortMode.NotSortable
            };
            column.HeaderCell.Style.Padding = new Padding(0, 0, FilterIconHeaderPadding, 0);
            grid.Columns.Add(column);
        }

        private void Grid_ColumnWidthChanged(object? sender, DataGridViewColumnEventArgs e)
        {
            if (_isApplyingColumnWidths || sender is not DataGridView source)
                return;

            _columnWidths = GetGridColumnWidths(source);
            ColumnWidthsChanged?.Invoke(this, EventArgs.Empty);
        }

        private void Grid_ColumnHeaderMouseClick(object? sender, DataGridViewCellMouseEventArgs e)
        {
            if (_isActionInProgress ||
                e.Button != MouseButtons.Left ||
                e.ColumnIndex < 0 ||
                e.ColumnIndex >= _grid.Columns.Count)
            {
                return;
            }

            DataGridViewColumn column = _grid.Columns[e.ColumnIndex];
            if (!KnowledgeBaseActJournalFilterService.IsSupportedColumn(column.Name))
                return;

            OpenColumnFilter(column);
        }

        private void Grid_CellPainting(object? sender, DataGridViewCellPaintingEventArgs e)
        {
            if (e.RowIndex != -1 ||
                e.ColumnIndex < 0 ||
                e.ColumnIndex >= _grid.Columns.Count)
            {
                return;
            }

            DataGridViewColumn column = _grid.Columns[e.ColumnIndex];
            if (!KnowledgeBaseActJournalFilterService.IsSupportedColumn(column.Name))
                return;

            e.Paint(e.CellBounds, e.PaintParts);
            Color iconColor = _columnFilterState.HasFilter(column.Name)
                ? Color.FromArgb(0, 120, 215)
                : SystemColors.GrayText;
            if (e.Graphics != null)
                DrawFilterIcon(e.Graphics, e.CellBounds, iconColor);
            e.Handled = true;
        }

        private Button CreateButton(string text, KnowledgeBaseActsJournalAction action)
        {
            var button = new Button
            {
                Text = text,
                AutoSize = true
            };
            button.Click += (_, _) => RequestAction(action);
            return button;
        }

        private void Grid_CellMouseDown(object? sender, DataGridViewCellMouseEventArgs e)
        {
            if (e.Button != MouseButtons.Right || e.RowIndex < 0)
                return;

            _grid.ClearSelection();
            _grid.Rows[e.RowIndex].Selected = true;
            if (e.ColumnIndex >= 0)
                _grid.CurrentCell = _grid[e.ColumnIndex, e.RowIndex];
        }

        private void RowContextMenu_Opening(object? sender, CancelEventArgs e)
        {
            _rowContextMenu.Items.Clear();
            KnowledgeBaseActJournalRow? row = GetSelectedRow();
            if (_isActionInProgress || row == null)
            {
                e.Cancel = true;
                return;
            }

            bool hasDocumentActions = false;
            hasDocumentActions |= AddContextAction(
                "Открыть",
                KnowledgeBaseActsJournalAction.Open,
                row.CanEdit);
            hasDocumentActions |= AddContextAction(
                "Открыть DOCX",
                KnowledgeBaseActsJournalAction.OpenDocument,
                row.CanOpenDocument);
            hasDocumentActions |= AddContextAction(
                "Сформировать DOCX",
                KnowledgeBaseActsJournalAction.GenerateDocument,
                row.CanGenerateDocument);

            bool hasStatusActions = row.CanSign || row.CanCancel || row.CanDeletePhysically;
            if (hasDocumentActions && hasStatusActions)
                _rowContextMenu.Items.Add(new ToolStripSeparator());

            AddContextAction("Подписать", KnowledgeBaseActsJournalAction.SignAct, row.CanSign);
            AddContextAction("Отменить", KnowledgeBaseActsJournalAction.CancelAct, row.CanCancel);
            AddContextAction(
                "Удалить черновик",
                KnowledgeBaseActsJournalAction.DeleteDraft,
                row.CanDeletePhysically);

            e.Cancel = _rowContextMenu.Items.Count == 0;
        }

        private bool AddContextAction(
            string text,
            KnowledgeBaseActsJournalAction action,
            bool isAvailable)
        {
            if (!isAvailable)
                return false;

            var item = new ToolStripMenuItem(text);
            item.Click += (_, _) => RequestAction(action);
            _rowContextMenu.Items.Add(item);
            return true;
        }

        private void ApplyRows(
            IEnumerable<KnowledgeBaseActJournalRow> rows,
            string? preferredActId)
        {
            int? selectedYear = GetSelectedYear();
            _allRows.Clear();
            _allRows.AddRange(rows);
            RefreshYearFilter(selectedYear);
            ApplyCurrentFilters(preferredActId);
        }

        private void RefreshYearFilter(int? preferredYear)
        {
            _isUpdatingYearFilter = true;
            try
            {
                _yearFilter.Items.Clear();
                _yearFilter.Items.Add(new YearFilterOption(null, "Все годы"));

                foreach (int year in _allRows
                    .Select(static row => row.ActYear)
                    .Where(static year => year > 0)
                    .Distinct()
                    .OrderByDescending(static year => year))
                {
                    _yearFilter.Items.Add(new YearFilterOption(year, year.ToString()));
                }

                int selectedIndex = 0;
                if (preferredYear.HasValue)
                {
                    for (int i = 1; i < _yearFilter.Items.Count; i++)
                    {
                        if (_yearFilter.Items[i] is YearFilterOption option &&
                            option.Year == preferredYear.Value)
                        {
                            selectedIndex = i;
                            break;
                        }
                    }
                }

                _yearFilter.SelectedIndex = selectedIndex;
            }
            finally
            {
                _isUpdatingYearFilter = false;
            }
        }

        private int? GetSelectedYear() =>
            (_yearFilter.SelectedItem as YearFilterOption)?.Year;

        private IEnumerable<KnowledgeBaseActJournalRow> GetFilteredRows()
        {
            int? selectedYear = GetSelectedYear();
            IEnumerable<KnowledgeBaseActJournalRow> yearFilteredRows = selectedYear.HasValue
                ? _allRows.Where(row => row.ActYear == selectedYear.Value)
                : _allRows;
            return _filterService.Apply(yearFilteredRows, _columnFilterState);
        }

        private IEnumerable<KnowledgeBaseActJournalRow> GetRowsForFilterOptions(string columnName)
        {
            int? selectedYear = GetSelectedYear();
            IEnumerable<KnowledgeBaseActJournalRow> yearFilteredRows = selectedYear.HasValue
                ? _allRows.Where(row => row.ActYear == selectedYear.Value)
                : _allRows;
            return _filterService.Apply(yearFilteredRows, _columnFilterState, columnName);
        }

        private void ApplyCurrentFilters(string? preferredActId)
        {
            UpdateColumnHeaders();
            ApplyGridRows(GetFilteredRows(), preferredActId);
            UpdateFilterControls();
        }

        private void ApplyGridRows(
            IEnumerable<KnowledgeBaseActJournalRow> rows,
            string? preferredActId)
        {
            string selectedActId = preferredActId?.Trim() ?? string.Empty;
            _grid.Rows.Clear();

            int selectedIndex = -1;
            foreach (KnowledgeBaseActJournalRow row in rows)
            {
                int index = _grid.Rows.Add(
                    row.ActDateText,
                    row.ActNumberText,
                    row.StatusText,
                    row.ActTypeText,
                    row.WorkshopName,
                    row.ObjectName,
                    row.EquipmentName,
                    row.OrderNumber,
                    row.DocumentStateText);
                _grid.Rows[index].Tag = row;
                if (!string.IsNullOrWhiteSpace(selectedActId) &&
                    string.Equals(row.ActId, selectedActId, StringComparison.Ordinal))
                {
                    selectedIndex = index;
                }
            }

            if (_grid.Rows.Count > 0)
            {
                int index = selectedIndex >= 0 ? selectedIndex : 0;
                _grid.ClearSelection();
                _grid.Rows[index].Selected = true;
                _grid.CurrentCell = _grid.Rows[index].Cells[0];
            }

            UpdateButtonStates();
        }

        private void OpenColumnFilter(DataGridViewColumn column)
        {
            string columnName = column.Name;
            string selectedActId = GetSelectedActId();
            IReadOnlyList<string> availableValues = _filterService.GetDistinctValues(
                GetRowsForFilterOptions(columnName),
                columnName);
            IReadOnlyCollection<string> selectedValues = _columnFilterState.HasFilter(columnName)
                ? _columnFilterState.GetSelectedValues(columnName)
                : availableValues;

            using var dialog = new KnowledgeBaseActsJournalColumnFilterDialog(
                GetColumnHeaderText(column),
                availableValues,
                selectedValues);
            if (dialog.ShowDialog(this) != DialogResult.OK)
                return;

            IReadOnlyList<string> dialogSelectedValues = dialog.SelectedValues;
            if (dialog.ClearFilterRequested || dialogSelectedValues.Count == availableValues.Count)
                _columnFilterState.ClearColumn(columnName);
            else
                _columnFilterState.SetSelectedValues(columnName, dialogSelectedValues);

            ApplyCurrentFilters(selectedActId);
        }

        private void ClearAllFilters()
        {
            if (!HasActiveFilters())
                return;

            string selectedActId = GetSelectedActId();
            _columnFilterState.Clear();
            _isUpdatingYearFilter = true;
            try
            {
                if (_yearFilter.Items.Count > 0)
                    _yearFilter.SelectedIndex = 0;
            }
            finally
            {
                _isUpdatingYearFilter = false;
            }

            ApplyCurrentFilters(selectedActId);
        }

        private KnowledgeBaseActJournalRow? GetSelectedRow()
        {
            if (_grid.SelectedRows.Count == 0)
                return null;

            return _grid.SelectedRows[0].Tag as KnowledgeBaseActJournalRow;
        }

        private string GetSelectedActId() =>
            GetSelectedRow()?.ActId ?? string.Empty;

        private bool HasActiveFilters() =>
            _columnFilterState.HasFilters || GetSelectedYear().HasValue;

        private void UpdateFilterControls()
        {
            _btnClearFilters.Enabled = !_isActionInProgress && HasActiveFilters();
        }

        private void UpdateColumnHeaders()
        {
            foreach (DataGridViewColumn column in _grid.Columns)
            {
                if (string.IsNullOrWhiteSpace(column.Name))
                    continue;

                string headerText = GetColumnHeaderText(column);
                column.HeaderText = _columnFilterState.HasFilter(column.Name)
                    ? $"{headerText} *"
                    : headerText;
            }

            _grid.Invalidate();
        }

        private string GetColumnHeaderText(DataGridViewColumn column) =>
            _columnHeaderTexts.TryGetValue(column.Name, out string? headerText)
                ? headerText
                : column.HeaderText;

        private static void DrawFilterIcon(Graphics graphics, Rectangle cellBounds, Color color)
        {
            int x = cellBounds.Right - FilterIconRightPadding - FilterIconSize;
            int y = cellBounds.Top + Math.Max(0, (cellBounds.Height - FilterIconSize) / 2);
            if (x <= cellBounds.Left || y <= cellBounds.Top)
                return;

            Point[] points =
            {
                new(x, y),
                new(x + FilterIconSize, y),
                new(x + 6, y + 5),
                new(x + 6, y + FilterIconSize),
                new(x + 4, y + FilterIconSize),
                new(x + 4, y + 5)
            };

            using var brush = new SolidBrush(color);
            graphics.FillPolygon(brush, points);
        }

        private void UpdateButtonStates()
        {
            if (_isActionInProgress)
            {
                _btnOpen.Enabled = false;
                _btnOpenDocument.Enabled = false;
                _btnGenerateDocument.Enabled = false;
                _btnDeleteDraft.Enabled = false;
                _btnCancelAct.Enabled = false;
                _btnSignAct.Enabled = false;
                return;
            }

            KnowledgeBaseActJournalRow? row = GetSelectedRow();
            bool hasSelection = row != null;
            _btnOpen.Enabled = row?.CanEdit == true;
            _btnOpenDocument.Enabled = row?.CanOpenDocument == true;
            _btnGenerateDocument.Enabled = row?.CanGenerateDocument == true;
            _btnDeleteDraft.Enabled = row?.CanDeletePhysically == true;
            _btnSignAct.Enabled = row?.CanSign == true;
            _btnCancelAct.Enabled = row?.CanCancel == true;
        }

        private void RequestAction(KnowledgeBaseActsJournalAction action)
        {
            if (_isActionInProgress)
                return;

            KnowledgeBaseActJournalRow? row = GetSelectedRow();
            if (row == null)
                return;

            ActionRequested?.Invoke(
                this,
                new KnowledgeBaseActsJournalActionRequestedEventArgs(row.ActId, action));
        }

        private void ApplyGridColumnWidths(DataGridView grid, IReadOnlyDictionary<string, int> columnWidths)
        {
            _isApplyingColumnWidths = true;
            try
            {
                foreach (DataGridViewColumn column in grid.Columns)
                {
                    if (columnWidths.TryGetValue(column.Name, out int width) && width > 0)
                        column.Width = width;
                }
            }
            finally
            {
                _isApplyingColumnWidths = false;
            }
        }

        private static Dictionary<string, int> GetGridColumnWidths(DataGridView grid)
        {
            var widths = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (DataGridViewColumn column in grid.Columns)
            {
                if (!column.Visible ||
                    string.IsNullOrWhiteSpace(column.Name))
                {
                    continue;
                }

                if (column.Width > 0)
                    widths[column.Name] = column.Width;
            }

            return widths;
        }

        private static Dictionary<string, int> NormalizeColumnWidths(IReadOnlyDictionary<string, int>? columnWidths)
        {
            var normalized = new Dictionary<string, int>(StringComparer.Ordinal);
            if (columnWidths == null)
                return normalized;

            foreach (var pair in columnWidths)
            {
                string columnName = pair.Key?.Trim() ?? string.Empty;
                if (!string.IsNullOrWhiteSpace(columnName) && pair.Value > 0)
                    normalized[columnName] = pair.Value;
            }

            return normalized;
        }

        private sealed class YearFilterOption
        {
            public YearFilterOption(int? year, string text)
            {
                Year = year;
                Text = text;
            }

            public int? Year { get; }

            private string Text { get; }

            public override string ToString() => Text;
        }
    }
}
