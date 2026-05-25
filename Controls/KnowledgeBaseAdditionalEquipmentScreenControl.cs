using AsutpKnowledgeBase.Services;

namespace AsutpKnowledgeBase
{
    public sealed class KnowledgeBaseAdditionalEquipmentScreenControl : UserControl
    {
        private readonly KnowledgeBaseCompositionState _emptyState = new();

        private Label _lblSource = null!;
        private Label _lblSummary = null!;
        private Button _btnAdd = null!;
        private Button _btnEditSelected = null!;
        private Button _btnDeleteSelected = null!;
        private ListView _lvEntries = null!;
        private Label _lblEmptyState = null!;

        private KnowledgeBaseCompositionState _currentState = new();
        private bool _isSynchronizingSelection;

        public KnowledgeBaseAdditionalEquipmentScreenControl()
        {
            Dock = DockStyle.Fill;

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                BackColor = KnowledgeBaseWorkspaceVisuals.SurfaceColor,
                Padding = new Padding(16),
                ColumnCount = 1,
                RowCount = 4
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            _lblSource = new Label
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                Font = new Font("Segoe UI", 9F, FontStyle.Regular),
                ForeColor = KnowledgeBaseWorkspaceVisuals.MutedTextColor,
                Margin = new Padding(0, 0, 0, 8)
            };

            _lblSummary = new Label
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold),
                Margin = new Padding(0, 0, 0, 12)
            };

            var actionsPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = true,
                AutoSize = true,
                BackColor = KnowledgeBaseWorkspaceVisuals.SurfaceColor,
                Margin = new Padding(0, 0, 0, 12)
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

            _lvEntries = CreateEntriesListView();
            _lvEntries.SelectedIndexChanged += (_, _) => HandleSelectionChanged();
            _lvEntries.DoubleClick += (_, _) =>
            {
                if (!string.IsNullOrWhiteSpace(SelectedEntryId))
                    EditSelectedRequested?.Invoke(this, EventArgs.Empty);
            };

            _lblEmptyState = CreateEmptyStateLabel("Доп. оборудование пока не добавлено.");

            layout.Controls.Add(_lblSource, 0, 0);
            layout.Controls.Add(_lblSummary, 0, 1);
            layout.Controls.Add(actionsPanel, 0, 2);
            layout.Controls.Add(CreateEntriesGroup(), 0, 3);
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

            _lblSource.Text = BuildSourceText(_currentState);
            _lblSummary.Text = BuildSummaryText(_currentState);

            PopulateEntries(previouslySelectedEntryId);
            UpdateButtonStates();
        }

        private void PopulateEntries(string preferredSelectedEntryId)
        {
            _isSynchronizingSelection = true;
            _lvEntries.BeginUpdate();
            try
            {
                _lvEntries.Items.Clear();
                foreach (var entry in _currentState.AuxiliaryEntryStates)
                {
                    var item = new ListViewItem(
                    [
                        entry.PositionText,
                        entry.ComponentTypeText,
                        entry.ComponentText,
                        entry.IpAddressText,
                        entry.NotesText
                    ])
                    {
                        Tag = entry
                    };

                    _lvEntries.Items.Add(item);
                    if (!string.IsNullOrWhiteSpace(preferredSelectedEntryId) &&
                        string.Equals(entry.EntryId, preferredSelectedEntryId, StringComparison.Ordinal))
                    {
                        item.Selected = true;
                    }
                }
            }
            finally
            {
                _lvEntries.EndUpdate();
                _isSynchronizingSelection = false;
            }

            SelectedEntryId = _lvEntries.SelectedItems.Count > 0 &&
                _lvEntries.SelectedItems[0].Tag is KnowledgeBaseCompositionEntryState selectedState &&
                !selectedState.IsPlaceholder
                    ? selectedState.EntryId
                    : string.Empty;

            bool hasEntries = _currentState.AuxiliaryEntryStates.Count > 0;
            _lvEntries.Visible = hasEntries;
            _lblEmptyState.Visible = !hasEntries;
        }

        private void HandleSelectionChanged()
        {
            if (_isSynchronizingSelection || _lvEntries.SelectedItems.Count == 0)
                return;

            var state = _lvEntries.SelectedItems[0].Tag as KnowledgeBaseCompositionEntryState;
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
            var groupBox = new KnowledgeBaseWorkspaceVisuals.SectionPanel
            {
                Text = "Доп. оборудование",
                Dock = DockStyle.Fill,
                Padding = new Padding(10, 20, 10, 10),
                Margin = new Padding(0)
            };

            var container = new KnowledgeBaseWorkspaceVisuals.BorderPanel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(1)
            };
            container.Controls.Add(_lvEntries);
            container.Controls.Add(_lblEmptyState);
            groupBox.Controls.Add(container);
            return groupBox;
        }

        private static ListView CreateEntriesListView()
        {
            var listView = new ListView
            {
                Dock = DockStyle.Fill,
                FullRowSelect = true,
                GridLines = true,
                HeaderStyle = ColumnHeaderStyle.Nonclickable,
                HideSelection = false,
                MultiSelect = false,
                ShowItemToolTips = false,
                View = View.Details
            };
            KnowledgeBaseWorkspaceVisuals.ConfigureListView(listView);
            listView.Columns.Add("Позиция", 110);
            listView.Columns.Add("Тип", 120);
            listView.Columns.Add("Компонент", 200);
            listView.Columns.Add("IP-адрес", 120);
            listView.Columns.Add("Примечание", 260);
            return listView;
        }

        private static string BuildSourceText(KnowledgeBaseCompositionState state)
        {
            if (!state.SupportsEditing)
                return state.EmptyStateText;

            return state.AuxiliaryEntries > 0
                ? "Показано доп. оборудование выбранного шкафа или щита."
                : "Доп. оборудование выбранного шкафа или щита пока не заполнено.";
        }

        private static string BuildSummaryText(KnowledgeBaseCompositionState state)
        {
            if (!state.SupportsEditing)
                return state.EmptyStateText;

            return $"Всего: {state.AuxiliaryEntries}";
        }

        private static Label CreateEmptyStateLabel(string text) =>
            KnowledgeBaseWorkspaceVisuals.CreateEmptyStateLabel(text);

        private static Button CreateActionButton(string text) =>
            KnowledgeBaseWorkspaceVisuals.CreateActionButton(text);
    }
}
