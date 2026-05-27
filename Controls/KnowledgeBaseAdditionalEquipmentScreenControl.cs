using System.Globalization;
using AsutpKnowledgeBase.Services;

namespace AsutpKnowledgeBase
{
    public sealed class KnowledgeBaseAdditionalEquipmentScreenControl : UserControl
    {
        private readonly KnowledgeBaseCompositionState _emptyState = new();

        private Button _btnAdd = null!;
        private Button _btnEditSelected = null!;
        private Button _btnDeleteSelected = null!;
        private DataGridView _gridEntries = null!;
        private Label _lblEmptyState = null!;
        private FlowLayoutPanel _entriesPanel = null!;
        private KnowledgeBaseWorkspaceVisuals.SectionPanel _entriesGroup = null!;

        private KnowledgeBaseCompositionState _currentState = new();
        private bool _isSynchronizingSelection;

        public KnowledgeBaseAdditionalEquipmentScreenControl()
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

            _btnAdd = CreateActionButton("Добавить доп. оборудование");
            _btnAdd.Click += (_, _) => AddRequested?.Invoke(this, EventArgs.Empty);
            _btnEditSelected = CreateActionButton("Изменить");
            _btnEditSelected.Click += (_, _) => EditSelectedRequested?.Invoke(this, EventArgs.Empty);
            _btnDeleteSelected = CreateActionButton("Удалить");
            _btnDeleteSelected.Click += (_, _) => DeleteSelectedRequested?.Invoke(this, EventArgs.Empty);

            actionsPanel.Controls.Add(_btnAdd);
            actionsPanel.Controls.Add(_btnEditSelected);
            actionsPanel.Controls.Add(_btnDeleteSelected);

            _gridEntries = CreateEntriesGrid();
            _gridEntries.ContextMenuStrip = CreateEntriesContextMenu();
            _gridEntries.SelectionChanged += (_, _) => HandleSelectionChanged();
            _gridEntries.MouseDown += HandleGridMouseDown;
            _gridEntries.CellDoubleClick += (_, _) =>
            {
                if (!string.IsNullOrWhiteSpace(SelectedEntryId))
                    EditSelectedRequested?.Invoke(this, EventArgs.Empty);
            };

            _lblEmptyState = CreateEmptyStateLabel("Доп. оборудование пока не добавлено.");

            _entriesPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                Margin = new Padding(0),
                Padding = new Padding(0, 0, 2, 0),
                BackColor = KnowledgeBaseWorkspaceVisuals.SurfaceColor
            };
            _entriesPanel.SizeChanged += (_, _) => UpdateEntriesCardSize();
            _entriesPanel.Controls.Add(CreateEntriesGroup());

            layout.Controls.Add(actionsPanel, 0, 0);
            layout.Controls.Add(_entriesPanel, 0, 1);
            Controls.Add(layout);

            ApplyState(_emptyState);
        }

        public event EventHandler? AddRequested;

        public event EventHandler? EditSelectedRequested;

        public event EventHandler? DeleteSelectedRequested;

        public string SelectedEntryId { get; private set; } = string.Empty;

        public void ApplyState(KnowledgeBaseCompositionState state)
        {
            _currentState = state ?? _emptyState;
            string previouslySelectedEntryId = SelectedEntryId;

            PopulateEntries(previouslySelectedEntryId);
            UpdateButtonStates();
            UpdateEntriesGroupTitle();
            UpdateEntriesCardSize();
        }

        private void PopulateEntries(string preferredSelectedEntryId)
        {
            _isSynchronizingSelection = true;
            try
            {
                _gridEntries.Rows.Clear();
                DataGridViewRow? preferredRow = null;
                for (int entryIndex = 0; entryIndex < _currentState.AuxiliaryEntryStates.Count; entryIndex++)
                {
                    var entry = _currentState.AuxiliaryEntryStates[entryIndex];
                    int rowIndex = _gridEntries.Rows.Add(
                        (entryIndex + 1).ToString(CultureInfo.InvariantCulture),
                        entry.ComponentTypeText,
                        entry.ComponentText,
                        entry.NotesText);
                    DataGridViewRow row = _gridEntries.Rows[rowIndex];
                    row.Tag = entry;

                    if (!string.IsNullOrWhiteSpace(preferredSelectedEntryId) &&
                        string.Equals(entry.EntryId, preferredSelectedEntryId, StringComparison.Ordinal))
                    {
                        preferredRow = row;
                    }
                }

                _gridEntries.ClearSelection();
                _gridEntries.CurrentCell = null;
                if (preferredRow != null)
                {
                    preferredRow.Selected = true;
                    _gridEntries.CurrentCell = preferredRow.Cells[0];
                }
            }
            finally
            {
                _isSynchronizingSelection = false;
            }

            SelectedEntryId = _gridEntries.SelectedRows.Count > 0 &&
                _gridEntries.SelectedRows[0].Tag is KnowledgeBaseCompositionEntryState selectedState &&
                !selectedState.IsPlaceholder
                    ? selectedState.EntryId
                    : string.Empty;

            bool hasEntries = _currentState.AuxiliaryEntryStates.Count > 0;
            _gridEntries.Visible = hasEntries;
            _lblEmptyState.Visible = !hasEntries;
        }

        private void HandleGridMouseDown(object? sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Right)
                return;

            var hit = _gridEntries.HitTest(e.X, e.Y);
            _isSynchronizingSelection = true;
            try
            {
                _gridEntries.ClearSelection();

                if (hit.RowIndex >= 0 && hit.RowIndex < _gridEntries.Rows.Count)
                {
                    DataGridViewRow row = _gridEntries.Rows[hit.RowIndex];
                    row.Selected = true;
                    _gridEntries.CurrentCell = row.Cells[0];

                    SelectedEntryId = row.Tag is KnowledgeBaseCompositionEntryState state && !state.IsPlaceholder
                        ? state.EntryId
                        : string.Empty;
                }
                else
                {
                    _gridEntries.CurrentCell = null;
                    SelectedEntryId = string.Empty;
                }

                _gridEntries.Focus();
                UpdateButtonStates();
            }
            finally
            {
                _isSynchronizingSelection = false;
            }
        }

        private void HandleSelectionChanged()
        {
            if (_isSynchronizingSelection)
                return;

            if (_gridEntries.SelectedRows.Count == 0)
            {
                SelectedEntryId = string.Empty;
                UpdateButtonStates();
                return;
            }

            var state = _gridEntries.SelectedRows[0].Tag as KnowledgeBaseCompositionEntryState;
            SelectedEntryId = state == null || state.IsPlaceholder
                ? string.Empty
                : state.EntryId;

            UpdateButtonStates();
        }

        private void UpdateButtonStates()
        {
            bool canEdit = _currentState.SupportsEditing;
            bool hasEditableSelection = canEdit && !string.IsNullOrWhiteSpace(SelectedEntryId);

            _btnAdd.Enabled = canEdit;
            _btnEditSelected.Enabled = hasEditableSelection;
            _btnDeleteSelected.Enabled = hasEditableSelection;
        }

        private Control CreateEntriesGroup()
        {
            _entriesGroup = new KnowledgeBaseWorkspaceVisuals.SectionPanel
            {
                Text = "Доп. оборудование",
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
            container.Controls.Add(_gridEntries);
            container.Controls.Add(_lblEmptyState);
            _entriesGroup.Controls.Add(container);
            return _entriesGroup;
        }

        private static DataGridView CreateEntriesGrid()
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
            grid.Columns.Add(CreateGridColumn("Number", "№", 44));
            grid.Columns.Add(CreateGridColumn("Type", "Тип", 700));
            grid.Columns.Add(CreateGridColumn("Component", "Компонент", 240));
            grid.Columns.Add(CreateGridColumn("Notes", "Примечание", 230));
            return grid;
        }

        private static DataGridViewTextBoxColumn CreateGridColumn(string name, string headerText, int fillWeight) =>
            new()
            {
                Name = name,
                HeaderText = headerText,
                Width = fillWeight,
                MinimumWidth = Math.Min(80, fillWeight),
                FillWeight = fillWeight,
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill
            };

        private void UpdateEntriesGroupTitle()
        {
            if (_entriesGroup != null)
                _entriesGroup.Text = $"Доп. оборудование   ({_currentState.AuxiliaryEntries})";
        }

        private void UpdateEntriesCardSize()
        {
            if (_entriesPanel == null || _entriesPanel.IsDisposed || _entriesGroup == null)
                return;

            int verticalScrollbarWidth = _entriesPanel.VerticalScroll.Visible
                ? SystemInformation.VerticalScrollBarWidth
                : 0;
            int availableWidth = _entriesPanel.ClientSize.Width -
                _entriesPanel.Padding.Horizontal -
                verticalScrollbarWidth -
                2;
            int targetWidth = Math.Max(520, availableWidth);

            int targetHeight = Math.Max(
                220,
                _entriesPanel.ClientSize.Height - _entriesPanel.Padding.Vertical - 2);

            _entriesGroup.Width = targetWidth;
            _entriesGroup.Height = targetHeight;
        }

        private ContextMenuStrip CreateEntriesContextMenu()
        {
            var menu = new ContextMenuStrip();
            ToolStripMenuItem editItem = CreateContextMenuItem("Изменить", () => EditSelectedRequested?.Invoke(this, EventArgs.Empty));
            ToolStripMenuItem addItem = CreateContextMenuItem("Добавить", () => AddRequested?.Invoke(this, EventArgs.Empty));
            ToolStripMenuItem deleteItem = CreateContextMenuItem("Удалить", () => DeleteSelectedRequested?.Invoke(this, EventArgs.Empty));

            menu.Items.Add(editItem);
            menu.Items.Add(addItem);
            menu.Items.Add(deleteItem);
            menu.Opening += (_, _) =>
            {
                bool canEdit = _currentState.SupportsEditing;
                bool hasSelection = !string.IsNullOrWhiteSpace(SelectedEntryId);
                editItem.Enabled = canEdit && hasSelection;
                addItem.Enabled = canEdit;
                deleteItem.Enabled = canEdit && hasSelection;
            };

            return menu;
        }

        private static ToolStripMenuItem CreateContextMenuItem(string text, Action action)
        {
            var item = new ToolStripMenuItem(text);
            item.Click += (_, _) => action();
            return item;
        }

        private static Label CreateEmptyStateLabel(string text) =>
            KnowledgeBaseWorkspaceVisuals.CreateEmptyStateLabel(text);

        private static Button CreateActionButton(string text) =>
            KnowledgeBaseWorkspaceVisuals.CreateActionButton(text);
    }
}
