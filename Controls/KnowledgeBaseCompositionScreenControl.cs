using AsutpKnowledgeBase.Services;

namespace AsutpKnowledgeBase
{
    public sealed class KnowledgeBaseCompositionScreenControl : UserControl
    {
        private readonly KnowledgeBaseCompositionState _emptyState = new();
        private readonly Dictionary<int, ListView> _rackListViews = new();

        private Button _btnAddRack = null!;
        private Button _btnEditRack = null!;
        private Button _btnDeleteRack = null!;
        private Button _btnAddSlotted = null!;
        private Button _btnCopyFromExisting = null!;
        private FlowLayoutPanel _rackPanel = null!;
        private SplitContainer _splitRackDetails = null!;
        private DataGridView _dgvRackDetails = null!;

        private KnowledgeBaseCompositionState _currentState = new();
        private bool _isSynchronizingSelection;
        private bool _isApplyingDetailsPanelHeight;
        private int? _pendingDetailsPanelHeight;

        public KnowledgeBaseCompositionScreenControl()
        {
            Dock = DockStyle.Fill;

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
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
                Margin = new Padding(0, 0, 0, 8)
            };

            _btnAddRack = CreateActionButton("Добавить Rack");
            _btnAddRack.Click += (_, _) => AddRackRequested?.Invoke(this, EventArgs.Empty);
            _btnEditRack = CreateActionButton("Изменить Rack...");
            _btnEditRack.Click += (_, _) => EditRackRequested?.Invoke(this, EventArgs.Empty);
            _btnDeleteRack = CreateActionButton("Удалить Rack");
            _btnDeleteRack.Click += (_, _) => DeleteRackRequested?.Invoke(this, EventArgs.Empty);
            _btnAddSlotted = CreateActionButton("Добавить слот...");
            _btnAddSlotted.Click += (_, _) => AddSlottedRequested?.Invoke(this, EventArgs.Empty);
            _btnCopyFromExisting = CreateActionButton("Копировать из объекта...");
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
                Padding = new Padding(4)
            };
            _rackPanel.SizeChanged += (_, _) => UpdateRackCardWidths();

            _dgvRackDetails = CreateRackDetailsGrid();
            _dgvRackDetails.SelectionChanged += (_, _) => HandleRackDetailsSelectionChanged();
            _dgvRackDetails.CellDoubleClick += (_, _) =>
            {
                if (!string.IsNullOrWhiteSpace(SelectedEntryId))
                    EditSelectedRequested?.Invoke(this, EventArgs.Empty);
            };

            _splitRackDetails = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Horizontal,
                FixedPanel = FixedPanel.Panel2,
                Panel1MinSize = 240,
                Panel2MinSize = 130,
                SplitterWidth = 6,
                Margin = new Padding(0)
            };
            _splitRackDetails.Panel1.Controls.Add(CreateRacksGroup());
            _splitRackDetails.Panel2.Controls.Add(CreateDetailsGroup("Детали выбранного Rack", _dgvRackDetails));
            _splitRackDetails.SplitterMoved += (_, _) =>
            {
                if (!_isApplyingDetailsPanelHeight)
                    DetailsPanelHeightChanged?.Invoke(this, EventArgs.Empty);
            };
            _splitRackDetails.SizeChanged += (_, _) => ApplyPendingDetailsPanelHeight();

            layout.Controls.Add(actionsPanel, 0, 0);
            layout.Controls.Add(_splitRackDetails, 0, 1);
            Controls.Add(layout);

            ApplyState(_emptyState);
        }

        public event EventHandler? AddSlottedRequested;

        public event EventHandler? AddRackRequested;

        public event EventHandler? EditRackRequested;

        public event EventHandler? DeleteRackRequested;

        public event EventHandler? CopyFromExistingRequested;

        public event EventHandler? EditSelectedRequested;

        public event EventHandler? DetailsPanelHeightChanged;

        public string SelectedEntryId { get; private set; } = string.Empty;

        public int SelectedRackNumber { get; private set; }

        public int? SelectedSlotNumber { get; private set; }

        public bool SelectedRackCanDelete =>
            _currentState.RackStates.FirstOrDefault(rack => rack.RackNumber == SelectedRackNumber)?.CanDelete == true;

        public KnowledgeBaseCompositionRackState? SelectedRackState =>
            _currentState.RackStates.FirstOrDefault(rack => rack.RackNumber == SelectedRackNumber);

        public int DetailsPanelHeight =>
            _splitRackDetails?.Panel2.Height ?? 0;

        public void ApplyState(KnowledgeBaseCompositionState state)
        {
            _currentState = state ?? _emptyState;
            string previouslySelectedEntryId = SelectedEntryId;
            int previouslySelectedRack = SelectedRackNumber;
            int? previouslySelectedSlot = SelectedSlotNumber;

            PopulateRacks(previouslySelectedEntryId, previouslySelectedRack, previouslySelectedSlot);
            ResolveSelectionAfterPopulate(previouslySelectedEntryId, previouslySelectedRack, previouslySelectedSlot);
            UpdateButtonStates();
        }

        public void ApplyDetailsPanelHeight(int? detailsPanelHeight)
        {
            _pendingDetailsPanelHeight = detailsPanelHeight;
            ApplyPendingDetailsPanelHeight();
        }

        private void ApplyPendingDetailsPanelHeight()
        {
            if (!_pendingDetailsPanelHeight.HasValue ||
                _splitRackDetails == null ||
                _splitRackDetails.IsDisposed ||
                _splitRackDetails.Height <= 0)
            {
                return;
            }

            int availableHeight = _splitRackDetails.Height - _splitRackDetails.SplitterWidth;
            if (availableHeight <= _splitRackDetails.Panel1MinSize + _splitRackDetails.Panel2MinSize)
                return;

            int maximumDetailsHeight = availableHeight - _splitRackDetails.Panel1MinSize;
            int detailsHeight = Math.Clamp(
                _pendingDetailsPanelHeight.Value,
                _splitRackDetails.Panel2MinSize,
                maximumDetailsHeight);
            int splitterDistance = availableHeight - detailsHeight;
            splitterDistance = Math.Clamp(
                splitterDistance,
                _splitRackDetails.Panel1MinSize,
                availableHeight - _splitRackDetails.Panel2MinSize);

            if (_splitRackDetails.SplitterDistance == splitterDistance)
                return;

            _isApplyingDetailsPanelHeight = true;
            try
            {
                _splitRackDetails.SplitterDistance = splitterDistance;
            }
            finally
            {
                _isApplyingDetailsPanelHeight = false;
            }
        }

        private Control CreateRacksGroup()
        {
            var groupBox = new GroupBox
            {
                Text = "Rack-компоновка",
                Dock = DockStyle.Fill,
                Padding = new Padding(10)
            };
            groupBox.Controls.Add(_rackPanel);
            return groupBox;
        }

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
                _rackListViews.Clear();

                foreach (var rack in _currentState.RackStates)
                {
                    var listView = CreateRackListView();
                    listView.Tag = rack;
                    listView.SelectedIndexChanged += (_, _) => HandleRackSelectionChanged(listView);
                    listView.DoubleClick += (_, _) =>
                    {
                        if (!string.IsNullOrWhiteSpace(SelectedEntryId))
                            EditSelectedRequested?.Invoke(this, EventArgs.Empty);
                    };

                    foreach (var entry in rack.SlotRows)
                    {
                        var item = new ListViewItem(
                        [
                            entry.SlotText,
                            entry.SlotRoleText,
                            entry.ComponentText,
                            entry.ComponentTypeText,
                            entry.SlotAdvisoryText
                        ])
                        {
                            Tag = entry,
                            ToolTipText = entry.SlotAdvisoryText == "-"
                                ? entry.SlotRoleText
                                : entry.SlotAdvisoryText
                        };
                        ApplyListViewItemStyle(item, entry);

                        listView.Items.Add(item);
                        if (ShouldSelectEntry(entry, preferredSelectedEntryId, preferredRackNumber, preferredSlotNumber))
                            item.Selected = true;
                    }

                    _rackListViews[rack.RackNumber] = listView;
                    _rackPanel.Controls.Add(CreateRackGroup(rack, listView));
                }

                UpdateRackCardWidths();
            }
            finally
            {
                _rackPanel.ResumeLayout();
                _isSynchronizingSelection = false;
            }
        }

        private void ResolveSelectionAfterPopulate(
            string preferredSelectedEntryId,
            int preferredRackNumber,
            int? preferredSlotNumber)
        {
            if (!string.IsNullOrWhiteSpace(SelectedEntryId) &&
                _currentState.SlottedEntryStates.Any(entry => string.Equals(entry.EntryId, SelectedEntryId, StringComparison.Ordinal)))
            {
                PopulateRackDetails(SelectedRackNumber, SelectedEntryId, SelectedSlotNumber);
                return;
            }

            SelectedEntryId = string.Empty;
            SelectedRackNumber = _currentState.RackStates.Any(rack => rack.RackNumber == preferredRackNumber)
                ? preferredRackNumber
                : _currentState.RackStates.FirstOrDefault()?.RackNumber ?? 0;
            SelectedSlotNumber = preferredSlotNumber;
            PopulateRackDetails(SelectedRackNumber, preferredSelectedEntryId, preferredSlotNumber);
        }

        private void HandleRackSelectionChanged(ListView source)
        {
            if (_isSynchronizingSelection)
                return;

            if (source.SelectedItems.Count == 0)
                return;

            var state = source.SelectedItems[0].Tag as KnowledgeBaseCompositionEntryState;
            if (state == null)
                return;

            _isSynchronizingSelection = true;
            try
            {
                ClearOtherRackSelections(source);
                ApplySelectedEntryState(state);
                PopulateRackDetails(state.RackNumber, state.EntryId, state.SlotNumberValue);
            }
            finally
            {
                _isSynchronizingSelection = false;
            }

            UpdateButtonStates();
        }

        private void HandleRackDetailsSelectionChanged()
        {
            if (_isSynchronizingSelection || _dgvRackDetails.SelectedRows.Count == 0)
                return;

            var state = _dgvRackDetails.SelectedRows[0].Tag as KnowledgeBaseCompositionEntryState;
            if (state == null)
                return;

            ApplySelectedEntryState(state);
            ClearOtherRackSelections(null);
            UpdateButtonStates();
        }

        private void PopulateRackDetails(int rackNumber, string preferredSelectedEntryId, int? preferredSlotNumber)
        {
            var rack = _currentState.RackStates.FirstOrDefault(state => state.RackNumber == rackNumber) ??
                _currentState.RackStates.FirstOrDefault();

            _dgvRackDetails.Rows.Clear();
            if (rack == null)
                return;

            foreach (var entry in rack.SlotRows)
            {
                int rowIndex = _dgvRackDetails.Rows.Add(
                    entry.RackText,
                    entry.SlotText,
                    entry.SlotRoleText,
                    entry.ComponentTypeText,
                    entry.ComponentText,
                    entry.IpAddressText,
                    entry.LastCalibrationText,
                    entry.NextCalibrationText,
                    entry.NotesText,
                    entry.SlotAdvisoryText);
                var row = _dgvRackDetails.Rows[rowIndex];
                row.Tag = entry;
                ApplyGridRowStyle(row, entry);
                if (entry.SlotAdvisoryText != "-")
                    row.Cells["Check"].ToolTipText = entry.SlotAdvisoryText;

                if (ShouldSelectEntry(entry, preferredSelectedEntryId, rack.RackNumber, preferredSlotNumber))
                    row.Selected = true;
            }

            if (_dgvRackDetails.SelectedRows.Count == 0 && _dgvRackDetails.Rows.Count > 0)
                _dgvRackDetails.Rows[0].Selected = true;
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

        private void ClearOtherRackSelections(ListView? except)
        {
            foreach (var listView in _rackListViews.Values)
            {
                if (ReferenceEquals(listView, except))
                    continue;

                listView.SelectedItems.Clear();
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

        private static Control CreateRackGroup(KnowledgeBaseCompositionRackState rack, ListView listView)
        {
            string warningText = rack.WarningCount > 0
                ? $"   !{rack.WarningCount}"
                : string.Empty;
            var groupBox = new GroupBox
            {
                Text = $"{rack.Title}   ({rack.FilledSlots}/{rack.TotalSlots}){warningText}",
                Width = 720,
                Height = 270,
                Padding = new Padding(8),
                Margin = new Padding(0, 0, 0, 12),
                MinimumSize = new Size(520, 220)
            };

            groupBox.Controls.Add(listView);
            return groupBox;
        }

        private static Control CreateDetailsGroup(string title, Control content)
        {
            var groupBox = new GroupBox
            {
                Text = title,
                Dock = DockStyle.Fill,
                Padding = new Padding(10),
                Margin = new Padding(0, 0, 0, 12)
            };
            groupBox.Controls.Add(content);
            return groupBox;
        }

        private static ListView CreateRackListView()
        {
            var listView = new ListView
            {
                Dock = DockStyle.Fill,
                FullRowSelect = true,
                GridLines = true,
                HeaderStyle = ColumnHeaderStyle.Nonclickable,
                HideSelection = false,
                MultiSelect = false,
                ShowItemToolTips = true,
                View = View.Details
            };
            listView.Columns.Add("Slot", 48);
            listView.Columns.Add("Роль", 76);
            listView.Columns.Add("Модуль", 170);
            listView.Columns.Add("Тип", 105);
            listView.Columns.Add("Проверка", 240);
            listView.SizeChanged += (_, _) => ResizeRackListViewColumns(listView);
            return listView;
        }

        private static DataGridView CreateRackDetailsGrid()
        {
            var grid = new DataGridView
            {
                Dock = DockStyle.Fill,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToResizeRows = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                BackgroundColor = SystemColors.Window,
                BorderStyle = BorderStyle.Fixed3D,
                EditMode = DataGridViewEditMode.EditProgrammatically,
                MultiSelect = false,
                ReadOnly = true,
                RowHeadersVisible = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect
            };

            grid.Columns.Add(CreateGridColumn("Rack", "Rack", 70));
            grid.Columns.Add(CreateGridColumn("Slot", "Slot", 55));
            grid.Columns.Add(CreateGridColumn("Role", "Роль", 80));
            grid.Columns.Add(CreateGridColumn("Type", "Тип", 120));
            grid.Columns.Add(CreateGridColumn("Module", "Модуль", 220));
            grid.Columns.Add(CreateGridColumn("IpAddress", "IP-адрес", 120));
            grid.Columns.Add(CreateGridColumn("LastCalibration", "Последняя калибровка", 140));
            grid.Columns.Add(CreateGridColumn("NextCalibration", "Следующая калибровка", 140));
            grid.Columns.Add(CreateGridColumn("Notes", "Примечание", 260));
            grid.Columns.Add(CreateGridColumn("Check", "Проверка", 220));

            return grid;
        }

        private static void ApplyListViewItemStyle(ListViewItem item, KnowledgeBaseCompositionEntryState entry)
        {
            if (entry.HasSlotWarning)
            {
                item.ForeColor = Color.DarkOrange;
                return;
            }

            if (entry.HasSlotHint)
            {
                item.ForeColor = Color.SteelBlue;
                return;
            }

            if (entry.IsPlaceholder)
                item.ForeColor = Color.DimGray;
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
                FillWeight = fillWeight
            };

        private void UpdateRackCardWidths()
        {
            if (_rackPanel == null || _rackPanel.IsDisposed)
                return;

            int availableWidth = _rackPanel.ClientSize.Width -
                _rackPanel.Padding.Horizontal -
                SystemInformation.VerticalScrollBarWidth -
                8;
            int targetWidth = Math.Max(520, availableWidth);
            foreach (Control control in _rackPanel.Controls)
            {
                control.Width = targetWidth;
                if (control.Controls.Count > 0 && control.Controls[0] is ListView listView)
                    ResizeRackListViewColumns(listView);
            }
        }

        private static void ResizeRackListViewColumns(ListView listView)
        {
            if (listView.Columns.Count < 5)
                return;

            int availableWidth = Math.Max(520, listView.ClientSize.Width - SystemInformation.VerticalScrollBarWidth - 8);
            const int slotWidth = 56;
            const int roleWidth = 96;
            const int typeWidth = 150;
            const int checkWidth = 300;
            int moduleWidth = Math.Max(220, availableWidth - slotWidth - roleWidth - typeWidth - checkWidth);

            listView.Columns[0].Width = slotWidth;
            listView.Columns[1].Width = roleWidth;
            listView.Columns[2].Width = moduleWidth;
            listView.Columns[3].Width = typeWidth;
            listView.Columns[4].Width = checkWidth;
        }

        private static Label CreateEmptyStateLabel(string text) =>
            new()
            {
                Dock = DockStyle.Fill,
                Text = text,
                TextAlign = ContentAlignment.MiddleCenter,
                ForeColor = Color.DimGray,
                Padding = new Padding(24),
                Visible = false
            };

        private static Button CreateActionButton(string text) =>
            new()
            {
                Text = text,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Margin = new Padding(0, 0, 8, 8),
                Padding = new Padding(8, 2, 8, 2),
                MinimumSize = new Size(0, 28)
            };
    }
}
