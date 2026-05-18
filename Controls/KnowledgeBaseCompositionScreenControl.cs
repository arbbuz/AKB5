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
        private FlowLayoutPanel _rackSelectorPanel = null!;
        private Panel _rackPanel = null!;
        private SplitContainer _splitRackDetails = null!;
        private DataGridView _dgvRackDetails = null!;
        private ToolTip _toolTip = null!;

        private KnowledgeBaseCompositionState _currentState = new();
        private bool _isSynchronizingSelection;
        private bool _isApplyingDetailsPanelHeight;
        private bool _isApplyingColumnWidths;
        private bool _isSplitterMoving;
        private int? _desiredDetailsPanelHeight;
        private Dictionary<string, int> _rackColumnWidths = new(StringComparer.Ordinal);
        private Dictionary<string, int> _rackDetailsColumnWidths = new(StringComparer.Ordinal);

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

            _toolTip = new ToolTip
            {
                ShowAlways = true
            };

            _rackSelectorPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = true,
                Margin = new Padding(0),
                Padding = new Padding(0, 0, 0, 4)
            };

            _rackPanel = new Panel
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(0),
                Padding = new Padding(0)
            };
            _rackPanel.SizeChanged += (_, _) => UpdateRackCardWidths();

            _dgvRackDetails = CreateRackDetailsGrid();
            _dgvRackDetails.ColumnWidthChanged += HandleRackDetailsColumnWidthChanged;
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
                SplitterWidth = 3,
                BackColor = SystemColors.Control,
                Margin = new Padding(0)
            };
            _splitRackDetails.Panel1.BackColor = SystemColors.Control;
            _splitRackDetails.Panel2.BackColor = SystemColors.Control;
            _splitRackDetails.Panel1.Controls.Add(CreateRacksGroup());
            _splitRackDetails.Panel2.Controls.Add(CreateDetailsGroup("Детали выбранного Rack", _dgvRackDetails));
            _splitRackDetails.SplitterMoving += (_, _) =>
            {
                _isSplitterMoving = true;
            };
            _splitRackDetails.SplitterMoved += (_, _) =>
            {
                bool wasUserMove = _isSplitterMoving;
                _isSplitterMoving = false;
                if (wasUserMove && !_isApplyingDetailsPanelHeight)
                {
                    _desiredDetailsPanelHeight = DetailsPanelHeight;
                    _splitRackDetails.Invalidate();
                    DetailsPanelHeightChanged?.Invoke(this, EventArgs.Empty);
                }
            };
            _splitRackDetails.SizeChanged += (_, _) =>
            {
                ApplyDesiredDetailsPanelHeight();
                _splitRackDetails.Invalidate();
            };
            _splitRackDetails.Paint += (_, e) => PaintRackDetailsSplitter(e.Graphics);
            _splitRackDetails.VisibleChanged += (_, _) => ApplyDesiredDetailsPanelHeight();
            _splitRackDetails.Layout += (_, _) => ApplyDesiredDetailsPanelHeight();

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

        public event EventHandler? ColumnWidthsChanged;

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
            ApplyDesiredDetailsPanelHeight();
            UpdateButtonStates();
        }

        public void ApplyDetailsPanelHeight(int? detailsPanelHeight)
        {
            _desiredDetailsPanelHeight = detailsPanelHeight;
            ApplyDesiredDetailsPanelHeight();
        }

        public void ApplyColumnWidths(
            IReadOnlyDictionary<string, int>? rackColumnWidths,
            IReadOnlyDictionary<string, int>? rackDetailsColumnWidths)
        {
            _rackColumnWidths = NormalizeColumnWidths(rackColumnWidths);
            _rackDetailsColumnWidths = NormalizeColumnWidths(rackDetailsColumnWidths);

            _isApplyingColumnWidths = true;
            try
            {
                foreach (var listView in _rackListViews.Values)
                    ApplyRackListViewColumnLayout(listView);

                ApplyGridColumnWidths(_dgvRackDetails, _rackDetailsColumnWidths);
            }
            finally
            {
                _isApplyingColumnWidths = false;
            }
        }

        public Dictionary<string, int> GetRackColumnWidths() =>
            new(_rackColumnWidths, StringComparer.Ordinal);

        public Dictionary<string, int> GetRackDetailsColumnWidths() =>
            new(_rackDetailsColumnWidths, StringComparer.Ordinal);

        private void ApplyDesiredDetailsPanelHeight()
        {
            if (!_desiredDetailsPanelHeight.HasValue ||
                _isSplitterMoving ||
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
                _desiredDetailsPanelHeight.Value,
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
                Padding = new Padding(8, 8, 8, 4)
            };
            var content = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
                Margin = new Padding(0),
                Padding = new Padding(0)
            };
            content.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            content.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            content.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            content.Controls.Add(_rackSelectorPanel, 0, 0);
            content.Controls.Add(_rackPanel, 0, 1);

            groupBox.Controls.Add(content);
            return groupBox;
        }

        private void PopulateRacks(
            string preferredSelectedEntryId,
            int preferredRackNumber,
            int? preferredSlotNumber)
        {
            _isSynchronizingSelection = true;
            _rackSelectorPanel.SuspendLayout();
            _rackPanel.SuspendLayout();
            try
            {
                SelectedRackNumber = ResolvePreferredRackNumber(preferredSelectedEntryId, preferredRackNumber);
                PopulateRackSelectors();

                _rackPanel.Controls.Clear();
                _rackListViews.Clear();

                var rack = _currentState.RackStates.FirstOrDefault(state => state.RackNumber == SelectedRackNumber);
                if (rack != null)
                    PopulateSelectedRack(rack, preferredSelectedEntryId, preferredSlotNumber);

                UpdateRackCardWidths();
            }
            finally
            {
                _rackPanel.ResumeLayout();
                _rackSelectorPanel.ResumeLayout();
                _isSynchronizingSelection = false;
            }
        }

        private void PopulateRackSelectors()
        {
            _rackSelectorPanel.Controls.Clear();

            foreach (var rack in _currentState.RackStates)
            {
                var selector = CreateRackSelector(rack);
                _rackSelectorPanel.Controls.Add(selector);
            }

            if (_currentState.SupportsEditing)
                _rackSelectorPanel.Controls.Add(CreateAddRackSelectorButton());
        }

        private void PopulateSelectedRack(
            KnowledgeBaseCompositionRackState rack,
            string preferredSelectedEntryId,
            int? preferredSlotNumber)
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
                    entry.NotesText,
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
                if (ShouldSelectEntry(entry, preferredSelectedEntryId, rack.RackNumber, preferredSlotNumber))
                    item.Selected = true;
            }

            _rackListViews[rack.RackNumber] = listView;
            _rackPanel.Controls.Add(CreateRackGroup(rack, listView));
        }

        private RadioButton CreateRackSelector(KnowledgeBaseCompositionRackState rack)
        {
            var selector = new RadioButton
            {
                Appearance = Appearance.Button,
                AutoSize = true,
                Checked = rack.RackNumber == SelectedRackNumber,
                Margin = new Padding(0, 0, 4, 4),
                MinimumSize = new Size(80, 28),
                Padding = new Padding(8, 2, 8, 2),
                Tag = rack,
                Text = BuildRackSelectorText(rack),
                TextAlign = ContentAlignment.MiddleCenter,
                UseVisualStyleBackColor = true
            };
            _toolTip.SetToolTip(selector, rack.Title);
            selector.CheckedChanged += HandleRackSelectorCheckedChanged;
            return selector;
        }

        private Button CreateAddRackSelectorButton()
        {
            var button = new Button
            {
                Text = "+",
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Margin = new Padding(4, 0, 0, 4),
                MinimumSize = new Size(32, 28),
                Padding = new Padding(8, 2, 8, 2)
            };
            _toolTip.SetToolTip(button, "Добавить Rack");
            button.Click += (_, _) => AddRackRequested?.Invoke(this, EventArgs.Empty);
            return button;
        }

        private void HandleRackSelectorCheckedChanged(object? sender, EventArgs e)
        {
            if (_isSynchronizingSelection ||
                sender is not RadioButton { Checked: true, Tag: KnowledgeBaseCompositionRackState rack })
            {
                return;
            }

            SelectRack(rack.RackNumber);
        }

        private void SelectRack(int rackNumber)
        {
            string preferredSelectedEntryId = SelectedRackNumber == rackNumber
                ? SelectedEntryId
                : string.Empty;
            int? preferredSlotNumber = SelectedRackNumber == rackNumber
                ? SelectedSlotNumber
                : null;

            PopulateRacks(preferredSelectedEntryId, rackNumber, preferredSlotNumber);
            ResolveSelectionAfterPopulate(preferredSelectedEntryId, rackNumber, preferredSlotNumber);
            UpdateButtonStates();
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

        private static string BuildRackSelectorText(KnowledgeBaseCompositionRackState rack)
        {
            string warningText = rack.WarningCount > 0
                ? $" !{rack.WarningCount}"
                : string.Empty;
            return $"{KnowledgeBaseCompositionRackSlotRulesService.FormatRackText(rack.RackNumber)} ({rack.FilledSlots}/{rack.TotalSlots}){warningText}";
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
                : _currentState.RackStates.Count > 0
                    ? _currentState.RackStates[0].RackNumber
                    : 0;
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

            _isSynchronizingSelection = true;
            try
            {
                ApplySelectedEntryState(state);
                SelectRackListEntry(state);
            }
            finally
            {
                _isSynchronizingSelection = false;
            }

            UpdateButtonStates();
        }

        private void HandleRackColumnWidthChanged(object? sender, ColumnWidthChangedEventArgs e)
        {
            if (_isApplyingColumnWidths || sender is not ListView source)
                return;

            _rackColumnWidths = GetListViewColumnWidths(source);
            _isApplyingColumnWidths = true;
            try
            {
                foreach (var listView in _rackListViews.Values)
                {
                    if (!ReferenceEquals(listView, source))
                        ApplyListViewColumnWidths(listView, _rackColumnWidths);
                }
            }
            finally
            {
                _isApplyingColumnWidths = false;
            }

            ColumnWidthsChanged?.Invoke(this, EventArgs.Empty);
        }

        private void HandleRackDetailsColumnWidthChanged(object? sender, DataGridViewColumnEventArgs e)
        {
            if (_isApplyingColumnWidths)
                return;

            _rackDetailsColumnWidths = GetGridColumnWidths(_dgvRackDetails);
            ColumnWidthsChanged?.Invoke(this, EventArgs.Empty);
        }

        private void PopulateRackDetails(int rackNumber, string preferredSelectedEntryId, int? preferredSlotNumber)
        {
            var rack = _currentState.RackStates.FirstOrDefault(state => state.RackNumber == rackNumber);
            if (rack == null && _currentState.RackStates.Count > 0)
                rack = _currentState.RackStates[0];

            _dgvRackDetails.Rows.Clear();
            if (rack == null)
                return;

            foreach (var entry in rack.SlotRows)
            {
                int rowIndex = _dgvRackDetails.Rows.Add(
                    entry.SlotText,
                    entry.SlotRoleText,
                    entry.ComponentTypeText,
                    entry.ComponentText,
                    entry.OrderNumberText,
                    entry.FirmwareText,
                    entry.MpiDpPnAddressText,
                    entry.InputAddressText,
                    entry.OutputAddressText,
                    entry.IpAddressText);
                var row = _dgvRackDetails.Rows[rowIndex];
                row.Tag = entry;
                ApplyGridRowStyle(row, entry);

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

        private void SelectRackListEntry(KnowledgeBaseCompositionEntryState state)
        {
            if (!_rackListViews.TryGetValue(state.RackNumber, out var listView))
                return;

            foreach (ListViewItem item in listView.Items)
            {
                if (ReferenceEquals(item.Tag, state))
                {
                    item.Selected = true;
                    item.Focused = true;
                    item.EnsureVisible();
                    return;
                }
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
                Padding = new Padding(6, 6, 6, 4),
                Margin = new Padding(0, 0, 0, 4),
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
                Padding = new Padding(8, 4, 8, 8),
                Margin = new Padding(0)
            };
            groupBox.Controls.Add(content);
            return groupBox;
        }

        private void PaintRackDetailsSplitter(Graphics graphics)
        {
            if (_splitRackDetails == null || _splitRackDetails.IsDisposed)
                return;

            var splitterBounds = new Rectangle(
                0,
                _splitRackDetails.SplitterDistance,
                _splitRackDetails.Width,
                _splitRackDetails.SplitterWidth);

            using var backgroundBrush = new SolidBrush(SystemColors.Control);
            graphics.FillRectangle(backgroundBrush, splitterBounds);

            int gripY = splitterBounds.Top + splitterBounds.Height / 2;
            int gripStart = Math.Min(24, Math.Max(0, splitterBounds.Width / 4));
            int gripEnd = Math.Max(gripStart, splitterBounds.Width - gripStart);
            using var gripPen = new Pen(SystemColors.ControlDark);
            graphics.DrawLine(gripPen, gripStart, gripY, gripEnd, gripY);
        }

        private ListView CreateRackListView()
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
            listView.Columns.Add("Примечание", 220);
            listView.Columns.Add("Проверка", 240);
            listView.ColumnWidthChanged += HandleRackColumnWidthChanged;
            listView.SizeChanged += (_, _) => ApplyRackListViewColumnLayout(listView);
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
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None,
                BackgroundColor = SystemColors.Window,
                BorderStyle = BorderStyle.Fixed3D,
                EditMode = DataGridViewEditMode.EditProgrammatically,
                MultiSelect = false,
                ReadOnly = true,
                RowHeadersVisible = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect
            };

            grid.Columns.Add(CreateGridColumn("Slot", "Slot", 55));
            grid.Columns.Add(CreateGridColumn("Role", "Роль", 80));
            grid.Columns.Add(CreateGridColumn("Type", "Тип", 120));
            grid.Columns.Add(CreateGridColumn("Module", "Модуль", 220));
            grid.Columns.Add(CreateGridColumn("OrderNumber", "Заказной номер", 150));
            grid.Columns.Add(CreateGridColumn("Firmware", "Firmware", 100));
            grid.Columns.Add(CreateGridColumn("MpiDpPnAddress", "MPI/DP/PN", 110));
            grid.Columns.Add(CreateGridColumn("InputAddress", "I address", 100));
            grid.Columns.Add(CreateGridColumn("OutputAddress", "Q address", 100));
            grid.Columns.Add(CreateGridColumn("IpAddress", "IP-адрес", 120));

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
                Width = fillWeight,
                MinimumWidth = Math.Min(80, fillWeight),
                FillWeight = fillWeight
            };

        private void UpdateRackCardWidths()
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
            int targetHeight = _rackPanel.Controls.Count == 1
                ? Math.Max(180, _rackPanel.ClientSize.Height - _rackPanel.Padding.Vertical - 4)
                : 270;
            foreach (Control control in _rackPanel.Controls)
            {
                control.Width = targetWidth;
                control.Height = targetHeight;
                if (control.Controls.Count > 0 && control.Controls[0] is ListView listView)
                    ApplyRackListViewColumnLayout(listView);
            }
        }

        private void ApplyRackListViewColumnLayout(ListView listView)
        {
            _isApplyingColumnWidths = true;
            try
            {
                if (_rackColumnWidths.Count > 0)
                    ApplyListViewColumnWidths(listView, _rackColumnWidths);
                else
                    ResizeRackListViewColumns(listView);
            }
            finally
            {
                _isApplyingColumnWidths = false;
            }
        }

        private static void ApplyListViewColumnWidths(ListView listView, IReadOnlyDictionary<string, int> columnWidths)
        {
            foreach (ColumnHeader column in listView.Columns)
            {
                if (columnWidths.TryGetValue(column.Text, out int width) && width > 0)
                    column.Width = width;
            }
        }

        private static void ApplyGridColumnWidths(DataGridView grid, IReadOnlyDictionary<string, int> columnWidths)
        {
            foreach (DataGridViewColumn column in grid.Columns)
            {
                if (columnWidths.TryGetValue(column.Name, out int width) && width > 0)
                    column.Width = width;
            }
        }

        private static Dictionary<string, int> GetListViewColumnWidths(ListView listView)
        {
            var widths = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (ColumnHeader column in listView.Columns)
            {
                if (!string.IsNullOrWhiteSpace(column.Text) && column.Width > 0)
                    widths[column.Text] = column.Width;
            }

            return widths;
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

        private static void ResizeRackListViewColumns(ListView listView)
        {
            if (listView.Columns.Count < 6)
                return;

            int availableWidth = Math.Max(520, listView.ClientSize.Width - SystemInformation.VerticalScrollBarWidth - 8);
            const int slotWidth = 56;
            const int roleWidth = 96;
            const int typeWidth = 150;
            const int notesWidth = 240;
            const int checkWidth = 300;
            int moduleWidth = Math.Max(220, availableWidth - slotWidth - roleWidth - typeWidth - notesWidth - checkWidth);

            listView.Columns[0].Width = slotWidth;
            listView.Columns[1].Width = roleWidth;
            listView.Columns[2].Width = moduleWidth;
            listView.Columns[3].Width = typeWidth;
            listView.Columns[4].Width = notesWidth;
            listView.Columns[5].Width = checkWidth;
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
