using AsutpKnowledgeBase.Services;

namespace AsutpKnowledgeBase
{
    public sealed class KnowledgeBaseCompositionScreenControl : UserControl
    {
        private readonly KnowledgeBaseCompositionState _emptyState = new();
        private readonly Dictionary<int, DataGridView> _rackDetailGrids = new();

        private Button _btnAddRack = null!;
        private Button _btnEditRack = null!;
        private Button _btnDeleteRack = null!;
        private Button _btnAddSlotted = null!;
        private Button _btnCopyFromExisting = null!;
        private FlowLayoutPanel _rackPanel = null!;

        private KnowledgeBaseCompositionState _currentState = new();
        private bool _isSynchronizingSelection;
        private bool _isApplyingColumnWidths;
        private Dictionary<string, int> _rackDetailsColumnWidths = new(StringComparer.Ordinal);
        private string _rackDetailsContextCellText = string.Empty;

        public KnowledgeBaseCompositionScreenControl()
        {
            Dock = DockStyle.Fill;

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                BackColor = KnowledgeBaseWorkspaceVisuals.SurfaceColor,
                Padding = new Padding(12),
                ColumnCount = 1,
                RowCount = 2
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            var actionsPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = true,
                AutoSize = true,
                BackColor = KnowledgeBaseWorkspaceVisuals.SurfaceColor,
                Margin = new Padding(0, 0, 0, 8)
            };

            _btnAddRack = CreateSquareActionButton("+");
            _btnAddRack.Click += (_, _) => AddRackRequested?.Invoke(this, EventArgs.Empty);
            _btnEditRack = CreateActionButton("Изменить Rack");
            _btnEditRack.Click += (_, _) => EditRackRequested?.Invoke(this, EventArgs.Empty);
            _btnDeleteRack = CreateActionButton("Удалить Rack");
            _btnDeleteRack.Click += (_, _) => DeleteRackRequested?.Invoke(this, EventArgs.Empty);
            _btnAddSlotted = CreateActionButton("Добавить слот");
            _btnAddSlotted.Click += (_, _) => AddSlottedRequested?.Invoke(this, EventArgs.Empty);
            _btnCopyFromExisting = CreateActionButton("Копировать из объекта");
            _btnCopyFromExisting.Click += (_, _) => CopyFromExistingRequested?.Invoke(this, EventArgs.Empty);

            actionsPanel.Controls.Add(_btnAddRack);
            actionsPanel.Controls.Add(_btnEditRack);
            actionsPanel.Controls.Add(_btnDeleteRack);
            actionsPanel.Controls.Add(_btnAddSlotted);
            actionsPanel.Controls.Add(_btnCopyFromExisting);

            _rackPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                Margin = new Padding(0),
                Padding = new Padding(0, 0, 2, 0),
                BackColor = KnowledgeBaseWorkspaceVisuals.SurfaceColor
            };
            _rackPanel.SizeChanged += (_, _) => UpdateRackCardSizes();

            layout.Controls.Add(actionsPanel, 0, 0);
            layout.Controls.Add(_rackPanel, 0, 1);
            Controls.Add(layout);

            ApplyState(_emptyState);
        }

        public event EventHandler? AddSlottedRequested;

        public event EventHandler? AddRackRequested;

        public event EventHandler? EditRackRequested;

        public event EventHandler? DeleteRackRequested;

        public event EventHandler? CopyFromExistingRequested;

        public event EventHandler? EditSelectedRequested;

        public event EventHandler? DeleteSelectedRequested;

        public event EventHandler? ColumnWidthsChanged;

        public string SelectedEntryId { get; private set; } = string.Empty;

        public int SelectedRackNumber { get; private set; }

        public int? SelectedSlotNumber { get; private set; }

        public bool SelectedRackCanDelete =>
            _currentState.RackStates.FirstOrDefault(rack => rack.RackNumber == SelectedRackNumber)?.CanDelete == true;

        public KnowledgeBaseCompositionRackState? SelectedRackState =>
            _currentState.RackStates.FirstOrDefault(rack => rack.RackNumber == SelectedRackNumber);

        public void ApplyState(KnowledgeBaseCompositionState state)
        {
            _currentState = state ?? _emptyState;
            string previouslySelectedEntryId = SelectedEntryId;
            int previouslySelectedRack = SelectedRackNumber;
            int? previouslySelectedSlot = SelectedSlotNumber;

            PopulateRacks(previouslySelectedEntryId, previouslySelectedRack, previouslySelectedSlot);
            UpdateButtonStates();
        }

        public void ApplyColumnWidths(IReadOnlyDictionary<string, int>? rackDetailsColumnWidths)
        {
            _rackDetailsColumnWidths = NormalizeColumnWidths(rackDetailsColumnWidths);

            _isApplyingColumnWidths = true;
            try
            {
                foreach (var grid in _rackDetailGrids.Values)
                    ApplyGridColumnWidths(grid, _rackDetailsColumnWidths);
            }
            finally
            {
                _isApplyingColumnWidths = false;
            }
        }

        public Dictionary<string, int> GetRackDetailsColumnWidths() =>
            new(_rackDetailsColumnWidths, StringComparer.Ordinal);

        private void PopulateRacks(
            string preferredSelectedEntryId,
            int preferredRackNumber,
            int? preferredSlotNumber)
        {
            _isSynchronizingSelection = true;
            _rackPanel.SuspendLayout();
            try
            {
                _rackPanel.Controls.Clear();
                _rackDetailGrids.Clear();

                SelectedEntryId = string.Empty;
                SelectedRackNumber = ResolvePreferredRackNumber(preferredSelectedEntryId, preferredRackNumber);
                SelectedSlotNumber = preferredSlotNumber;

                KnowledgeBaseCompositionEntryState? selectedEntry = null;
                foreach (var rack in _currentState.RackStates)
                {
                    var grid = CreateRackDetailsGrid();
                    grid.Tag = rack;
                    grid.ContextMenuStrip = CreateRackDetailsContextMenu();
                    grid.ColumnWidthChanged += HandleRackDetailsColumnWidthChanged;
                    grid.SelectionChanged += HandleRackDetailsSelectionChanged;
                    grid.MouseDown += HandleRackDetailsMouseDown;
                    grid.CellDoubleClick += (_, _) =>
                    {
                        if (!string.IsNullOrWhiteSpace(SelectedEntryId))
                            EditSelectedRequested?.Invoke(this, EventArgs.Empty);
                    };

                    _rackDetailGrids[rack.RackNumber] = grid;
                    _rackPanel.Controls.Add(CreateRackDetailsGroup(rack, grid));

                    var gridSelection = PopulateRackDetailsGrid(
                        grid,
                        rack,
                        preferredSelectedEntryId,
                        preferredSlotNumber,
                        rack.RackNumber == SelectedRackNumber);
                    selectedEntry ??= gridSelection;
                }

                if (selectedEntry != null)
                    ApplySelectedEntryState(selectedEntry);
                else if (_currentState.RackStates.Count > 0)
                    SelectedRackNumber = _currentState.RackStates[0].RackNumber;

                UpdateRackCardSizes();
            }
            finally
            {
                _rackPanel.ResumeLayout();
                _isSynchronizingSelection = false;
            }
        }

        private static KnowledgeBaseCompositionEntryState? PopulateRackDetailsGrid(
            DataGridView grid,
            KnowledgeBaseCompositionRackState rack,
            string preferredSelectedEntryId,
            int? preferredSlotNumber,
            bool shouldSelectRack)
        {
            grid.Rows.Clear();

            DataGridViewRow? preferredRow = null;
            foreach (var entry in rack.SlotRows)
            {
                int rowIndex = grid.Rows.Add(
                    entry.SlotText,
                    entry.SlotRoleText,
                    entry.ComponentTypeText,
                    entry.OrderNumberText);
                var row = grid.Rows[rowIndex];
                row.Tag = entry;
                ApplyGridRowStyle(row, entry);

                if (shouldSelectRack &&
                    preferredRow == null &&
                    ShouldSelectEntry(entry, preferredSelectedEntryId, rack.RackNumber, preferredSlotNumber))
                {
                    preferredRow = row;
                }
            }

            grid.ClearSelection();
            grid.CurrentCell = null;
            if (!shouldSelectRack)
                return null;

            preferredRow ??= grid.Rows.Count > 0 ? grid.Rows[0] : null;
            if (preferredRow == null)
                return null;

            preferredRow.Selected = true;
            grid.CurrentCell = preferredRow.Cells[0];
            return preferredRow.Tag as KnowledgeBaseCompositionEntryState;
        }

        private int ResolvePreferredRackNumber(string preferredSelectedEntryId, int preferredRackNumber)
        {
            if (!string.IsNullOrWhiteSpace(preferredSelectedEntryId))
            {
                var preferredEntry = _currentState.SlottedEntryStates.FirstOrDefault(entry =>
                    string.Equals(entry.EntryId, preferredSelectedEntryId, StringComparison.Ordinal));
                if (preferredEntry != null)
                    return preferredEntry.RackNumber;
            }

            if (_currentState.RackStates.Any(rack => rack.RackNumber == preferredRackNumber))
                return preferredRackNumber;

            return _currentState.RackStates.Count > 0
                ? _currentState.RackStates[0].RackNumber
                : 0;
        }

        private void HandleRackDetailsMouseDown(object? sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Right || sender is not DataGridView source)
                return;

            var hit = source.HitTest(e.X, e.Y);
            var rack = source.Tag as KnowledgeBaseCompositionRackState;
            _rackDetailsContextCellText = GetRackDetailsCellText(source, hit);

            _isSynchronizingSelection = true;
            try
            {
                ClearOtherRackSelections(source);
                source.ClearSelection();

                if (hit.RowIndex >= 0 && hit.RowIndex < source.Rows.Count)
                {
                    DataGridViewRow row = source.Rows[hit.RowIndex];
                    row.Selected = true;
                    int currentColumnIndex = hit.ColumnIndex >= 0 && hit.ColumnIndex < source.Columns.Count
                        ? hit.ColumnIndex
                        : 0;
                    source.CurrentCell = row.Cells[currentColumnIndex];

                    if (row.Tag is KnowledgeBaseCompositionEntryState state)
                    {
                        ApplySelectedEntryState(state);
                    }
                    else
                    {
                        SelectedEntryId = string.Empty;
                        SelectedRackNumber = rack?.RackNumber ?? 0;
                        SelectedSlotNumber = null;
                    }
                }
                else
                {
                    source.CurrentCell = null;
                    SelectedEntryId = string.Empty;
                    if (rack != null)
                    {
                        SelectedRackNumber = rack.RackNumber;
                        SelectedSlotNumber = null;
                    }
                }

                source.Focus();
            }
            finally
            {
                _isSynchronizingSelection = false;
            }

            UpdateButtonStates();
        }

        private void HandleRackDetailsSelectionChanged(object? sender, EventArgs e)
        {
            if (_isSynchronizingSelection ||
                sender is not DataGridView source ||
                source.SelectedRows.Count == 0)
            {
                return;
            }

            if (!source.ContainsFocus)
            {
                ClearAutomaticRackSelection(source);
                return;
            }

            var state = source.SelectedRows[0].Tag as KnowledgeBaseCompositionEntryState;
            if (state == null)
                return;

            _isSynchronizingSelection = true;
            try
            {
                ClearOtherRackSelections(source);
                ApplySelectedEntryState(state);
            }
            finally
            {
                _isSynchronizingSelection = false;
            }

            UpdateButtonStates();
        }

        private void ClearAutomaticRackSelection(DataGridView source)
        {
            if (source.Tag is not KnowledgeBaseCompositionRackState rack ||
                rack.RackNumber == SelectedRackNumber)
            {
                return;
            }

            _isSynchronizingSelection = true;
            try
            {
                source.ClearSelection();
                source.CurrentCell = null;
            }
            finally
            {
                _isSynchronizingSelection = false;
            }
        }

        private void HandleRackDetailsColumnWidthChanged(object? sender, DataGridViewColumnEventArgs e)
        {
            if (_isApplyingColumnWidths || sender is not DataGridView source)
                return;

            _rackDetailsColumnWidths = GetGridColumnWidths(source);
            _isApplyingColumnWidths = true;
            try
            {
                foreach (var grid in _rackDetailGrids.Values)
                {
                    if (!ReferenceEquals(grid, source))
                        ApplyGridColumnWidths(grid, _rackDetailsColumnWidths);
                }
            }
            finally
            {
                _isApplyingColumnWidths = false;
            }

            ColumnWidthsChanged?.Invoke(this, EventArgs.Empty);
        }

        private static bool ShouldSelectEntry(
            KnowledgeBaseCompositionEntryState entry,
            string preferredSelectedEntryId,
            int preferredRackNumber,
            int? preferredSlotNumber)
        {
            if (!string.IsNullOrWhiteSpace(preferredSelectedEntryId) &&
                string.Equals(entry.EntryId, preferredSelectedEntryId, StringComparison.Ordinal))
            {
                return true;
            }

            return string.IsNullOrWhiteSpace(preferredSelectedEntryId) &&
                entry.RackNumber == preferredRackNumber &&
                entry.SlotNumberValue == preferredSlotNumber;
        }

        private void ApplySelectedEntryState(KnowledgeBaseCompositionEntryState state)
        {
            SelectedEntryId = state.IsPlaceholder ? string.Empty : state.EntryId;
            SelectedRackNumber = state.RackNumber;
            SelectedSlotNumber = state.SlotNumberValue;
        }

        private void ClearOtherRackSelections(DataGridView? except)
        {
            foreach (var grid in _rackDetailGrids.Values)
            {
                if (ReferenceEquals(grid, except))
                    continue;

                grid.ClearSelection();
                grid.CurrentCell = null;
            }
        }

        private void UpdateButtonStates()
        {
            bool canAdd = _currentState.SupportsEditing;

            _btnAddRack.Enabled = canAdd;
            _btnEditRack.Enabled = canAdd && SelectedRackState != null;
            _btnDeleteRack.Enabled = canAdd && SelectedRackCanDelete;
            _btnAddSlotted.Enabled = canAdd && SelectedRackState != null;
            _btnCopyFromExisting.Enabled = canAdd;
        }

        private static Control CreateRackDetailsGroup(KnowledgeBaseCompositionRackState rack, DataGridView grid)
        {
            string warningText = rack.WarningCount > 0
                ? $"   !{rack.WarningCount}"
                : string.Empty;
            var groupBox = new KnowledgeBaseWorkspaceVisuals.SectionPanel
            {
                Text = $"{rack.Title}   ({rack.FilledSlots}/{rack.TotalSlots}){warningText}",
                Width = 720,
                Height = 300,
                Padding = new Padding(10, 20, 10, 10),
                Margin = new Padding(0, 0, 0, 6),
                MinimumSize = new Size(520, 220)
            };

            var container = new KnowledgeBaseWorkspaceVisuals.BorderPanel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(1)
            };
            container.Controls.Add(grid);
            groupBox.Controls.Add(container);
            return groupBox;
        }

        private DataGridView CreateRackDetailsGrid()
        {
            var grid = new DataGridView
            {
                Dock = DockStyle.Fill,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToResizeRows = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                EditMode = DataGridViewEditMode.EditProgrammatically,
                MultiSelect = false,
                ReadOnly = true,
                RowHeadersVisible = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect
            };
            KnowledgeBaseWorkspaceVisuals.ConfigureGrid(grid);
            grid.DefaultCellStyle.SelectionBackColor = SystemColors.Highlight;
            grid.DefaultCellStyle.SelectionForeColor = SystemColors.HighlightText;
            grid.RowsDefaultCellStyle.SelectionBackColor = SystemColors.Highlight;
            grid.RowsDefaultCellStyle.SelectionForeColor = SystemColors.HighlightText;
            grid.AlternatingRowsDefaultCellStyle.SelectionBackColor = SystemColors.Highlight;
            grid.AlternatingRowsDefaultCellStyle.SelectionForeColor = SystemColors.HighlightText;

            grid.Columns.Add(CreateGridColumn("Slot", "Slot", 55));
            grid.Columns.Add(CreateGridColumn("Role", "Роль", 80));
            grid.Columns.Add(CreateGridColumn("Type", "Тип", 120));
            grid.Columns.Add(CreateGridColumn("OrderNumber", "Заказной номер", 220));
            ApplyGridColumnWidths(grid, _rackDetailsColumnWidths);

            return grid;
        }

        private static void ApplyGridRowStyle(DataGridViewRow row, KnowledgeBaseCompositionEntryState entry)
        {
            if (entry.HasSlotWarning)
            {
                row.DefaultCellStyle.ForeColor = Color.DarkOrange;
                return;
            }

            if (entry.HasSlotHint)
            {
                row.DefaultCellStyle.ForeColor = Color.SteelBlue;
                return;
            }

            if (entry.IsPlaceholder)
                row.DefaultCellStyle.ForeColor = Color.DimGray;
        }

        private static DataGridViewTextBoxColumn CreateGridColumn(string name, string headerText, int fillWeight) =>
            new()
            {
                Name = name,
                HeaderText = headerText,
                Width = fillWeight,
                MinimumWidth = Math.Min(80, fillWeight),
                FillWeight = fillWeight,
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
                SortMode = DataGridViewColumnSortMode.NotSortable
            };

        private ContextMenuStrip CreateRackDetailsContextMenu()
        {
            var menu = new ContextMenuStrip();
            ToolStripMenuItem copyCellItem = CreateContextMenuItem("Копировать ячейку", CopyRackDetailsContextCell);
            ToolStripMenuItem editItem = CreateContextMenuItem("Изменить", () => EditSelectedRequested?.Invoke(this, EventArgs.Empty));
            ToolStripMenuItem addItem = CreateContextMenuItem("Добавить слот", () => AddSlottedRequested?.Invoke(this, EventArgs.Empty));
            ToolStripMenuItem deleteItem = CreateContextMenuItem("Удалить", () => DeleteSelectedRequested?.Invoke(this, EventArgs.Empty));

            menu.Items.Add(copyCellItem);
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add(editItem);
            menu.Items.Add(addItem);
            menu.Items.Add(deleteItem);
            menu.Opening += (_, _) =>
            {
                bool canEdit = _currentState.SupportsEditing;
                bool hasSelection = !string.IsNullOrWhiteSpace(SelectedEntryId);
                copyCellItem.Enabled = !string.IsNullOrWhiteSpace(_rackDetailsContextCellText);
                editItem.Enabled = canEdit && hasSelection;
                addItem.Enabled = canEdit && SelectedRackState != null;
                deleteItem.Enabled = canEdit && hasSelection;
            };

            return menu;
        }

        private void CopyRackDetailsContextCell()
        {
            if (!string.IsNullOrWhiteSpace(_rackDetailsContextCellText))
                Clipboard.SetText(_rackDetailsContextCellText);
        }

        private static string GetRackDetailsCellText(DataGridView grid, DataGridView.HitTestInfo hit)
        {
            if (hit.RowIndex < 0 ||
                hit.RowIndex >= grid.Rows.Count ||
                hit.ColumnIndex < 0 ||
                hit.ColumnIndex >= grid.Columns.Count)
            {
                return string.Empty;
            }

            object? formattedValue = grid.Rows[hit.RowIndex].Cells[hit.ColumnIndex].FormattedValue;
            return formattedValue?.ToString()?.Trim() ?? string.Empty;
        }

        private static ToolStripMenuItem CreateContextMenuItem(string text, Action action)
        {
            var item = new ToolStripMenuItem(text);
            item.Click += (_, _) => action();
            return item;
        }

        private void UpdateRackCardSizes()
        {
            if (_rackPanel == null || _rackPanel.IsDisposed)
                return;

            int verticalScrollbarWidth = _rackPanel.VerticalScroll.Visible
                ? SystemInformation.VerticalScrollBarWidth
                : 0;
            int availableWidth = _rackPanel.ClientSize.Width -
                _rackPanel.Padding.Horizontal -
                verticalScrollbarWidth -
                2;
            int targetWidth = Math.Max(520, availableWidth);

            int rackCount = Math.Max(1, _rackPanel.Controls.Count);
            int visibleRackCount = Math.Min(2, rackCount);
            int availableHeight = _rackPanel.ClientSize.Height -
                _rackPanel.Padding.Vertical -
                (visibleRackCount * 6) -
                2;
            int targetHeight = Math.Max(220, availableHeight / visibleRackCount);

            foreach (Control control in _rackPanel.Controls)
            {
                control.Width = targetWidth;
                control.Height = targetHeight;
            }
        }

        private static void ApplyGridColumnWidths(DataGridView grid, IReadOnlyDictionary<string, int> columnWidths)
        {
            foreach (DataGridViewColumn column in grid.Columns)
            {
                if (columnWidths.TryGetValue(column.Name, out int width) && width > 0)
                    ApplyGridColumnWidth(column, width);
            }
        }

        private static void ApplyGridColumnWidth(DataGridViewColumn column, int width)
        {
            if (column.DataGridView?.AutoSizeColumnsMode == DataGridViewAutoSizeColumnsMode.Fill ||
                column.AutoSizeMode == DataGridViewAutoSizeColumnMode.Fill)
            {
                column.FillWeight = Math.Max(1F, width);
                return;
            }

            column.Width = width;
        }

        private static Dictionary<string, int> GetGridColumnWidths(DataGridView grid)
        {
            var widths = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (DataGridViewColumn column in grid.Columns)
            {
                if (column.Visible && !string.IsNullOrWhiteSpace(column.Name) && column.Width > 0)
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

        private static Button CreateActionButton(string text) =>
            KnowledgeBaseWorkspaceVisuals.CreateActionButton(text);

        private static Button CreateSquareActionButton(string text) =>
            KnowledgeBaseWorkspaceVisuals.CreateSquareActionButton(text);
    }
}
