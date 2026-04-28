using AsutpKnowledgeBase.Services;

namespace AsutpKnowledgeBase
{
    public sealed class KnowledgeBaseCompositionScreenControl : UserControl
    {
        private readonly KnowledgeBaseCompositionState _emptyState = new();

        private Label _lblSource = null!;
        private Label _lblSummary = null!;
        private Button _btnAddSlotted = null!;
        private Button _btnAddAuxiliary = null!;
        private Button _btnApplyTemplate = null!;
        private Button _btnCopyFromExisting = null!;
        private Button _btnEditSelected = null!;
        private Button _btnDeleteSelected = null!;
        private ListView _lvSlottedEntries = null!;
        private ListView _lvAuxiliaryEntries = null!;
        private Label _lblSlottedEmptyState = null!;
        private Label _lblAuxiliaryEmptyState = null!;

        private KnowledgeBaseCompositionState _currentState = new();
        private bool _isSynchronizingSelection;

        public KnowledgeBaseCompositionScreenControl()
        {
            Dock = DockStyle.Fill;

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
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
                ForeColor = Color.DimGray,
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
                Margin = new Padding(0, 0, 0, 12)
            };

            _btnAddSlotted = CreateActionButton("Добавить слот...");
            _btnAddSlotted.Click += (_, _) => AddSlottedRequested?.Invoke(this, EventArgs.Empty);
            _btnAddAuxiliary = CreateActionButton("Добавить оборудование...");
            _btnAddAuxiliary.Click += (_, _) => AddAuxiliaryRequested?.Invoke(this, EventArgs.Empty);
            _btnApplyTemplate = CreateActionButton("Применить шаблон...");
            _btnApplyTemplate.Click += (_, _) => ApplyTemplateRequested?.Invoke(this, EventArgs.Empty);
            _btnCopyFromExisting = CreateActionButton("Копировать из объекта...");
            _btnCopyFromExisting.Click += (_, _) => CopyFromExistingRequested?.Invoke(this, EventArgs.Empty);
            _btnEditSelected = CreateActionButton("Изменить...");
            _btnEditSelected.Click += (_, _) => EditSelectedRequested?.Invoke(this, EventArgs.Empty);
            _btnDeleteSelected = CreateActionButton("Удалить");
            _btnDeleteSelected.Click += (_, _) => DeleteSelectedRequested?.Invoke(this, EventArgs.Empty);

            actionsPanel.Controls.Add(_btnAddSlotted);
            actionsPanel.Controls.Add(_btnAddAuxiliary);
            actionsPanel.Controls.Add(_btnApplyTemplate);
            actionsPanel.Controls.Add(_btnCopyFromExisting);
            actionsPanel.Controls.Add(_btnEditSelected);
            actionsPanel.Controls.Add(_btnDeleteSelected);

            var contentLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2
            };
            contentLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            contentLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 56F));
            contentLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 44F));

            _lvSlottedEntries = CreateEntriesListView();
            _lvAuxiliaryEntries = CreateEntriesListView();
            _lvSlottedEntries.SelectedIndexChanged += (_, _) => HandleSelectionChanged(_lvSlottedEntries, _lvAuxiliaryEntries);
            _lvAuxiliaryEntries.SelectedIndexChanged += (_, _) => HandleSelectionChanged(_lvAuxiliaryEntries, _lvSlottedEntries);

            _lblSlottedEmptyState = CreateEmptyStateLabel("Слоты пока не заполнены.");
            _lblAuxiliaryEmptyState = CreateEmptyStateLabel("Оборудование пока не добавлено.");

            contentLayout.Controls.Add(CreateEntriesGroup("Слоты", _lvSlottedEntries, _lblSlottedEmptyState), 0, 0);
            contentLayout.Controls.Add(CreateEntriesGroup("Оборудование", _lvAuxiliaryEntries, _lblAuxiliaryEmptyState), 0, 1);

            layout.Controls.Add(_lblSource, 0, 0);
            layout.Controls.Add(_lblSummary, 0, 1);
            layout.Controls.Add(actionsPanel, 0, 2);
            layout.Controls.Add(contentLayout, 0, 3);
            Controls.Add(layout);

            ApplyState(_emptyState);
        }

        public event EventHandler? AddSlottedRequested;

        public event EventHandler? AddAuxiliaryRequested;

        public event EventHandler? ApplyTemplateRequested;

        public event EventHandler? CopyFromExistingRequested;

        public event EventHandler? EditSelectedRequested;

        public event EventHandler? DeleteSelectedRequested;

        public string SelectedEntryId { get; private set; } = string.Empty;

        public void ApplyState(KnowledgeBaseCompositionState state)
        {
            _currentState = state ?? _emptyState;
            string previouslySelectedEntryId = SelectedEntryId;

            _lblSource.Text = _currentState.SourceText;
            _lblSummary.Text = _currentState.HasEntries
                ? $"Всего: {_currentState.TotalEntries} | Слотов: {_currentState.SlottedEntries} | Оборудования: {_currentState.AuxiliaryEntries}"
                : _currentState.EmptyStateText;

            PopulateEntries(
                _lvSlottedEntries,
                _lblSlottedEmptyState,
                _currentState.SlottedEntryStates,
                "Слоты пока не заполнены.",
                previouslySelectedEntryId);
            PopulateEntries(
                _lvAuxiliaryEntries,
                _lblAuxiliaryEmptyState,
                _currentState.AuxiliaryEntryStates,
                "Оборудование пока не добавлено.",
                previouslySelectedEntryId);

            SelectedEntryId = ResolveSelectedEntryId();
            UpdateButtonStates();
        }

        private void PopulateEntries(
            ListView listView,
            Label emptyStateLabel,
            IReadOnlyList<KnowledgeBaseCompositionEntryState> entries,
            string emptyStateText,
            string preferredSelectedEntryId)
        {
            listView.BeginUpdate();
            try
            {
                listView.Items.Clear();
                foreach (var entry in entries)
                {
                    var item = new ListViewItem(
                    [
                        entry.PositionText,
                        entry.SlotText,
                        entry.ComponentTypeText,
                        entry.ComponentText,
                        entry.IpAddressText,
                        entry.LastCalibrationText,
                        entry.NextCalibrationText,
                        entry.NotesText
                    ])
                    {
                        Tag = entry.EntryId
                    };

                    listView.Items.Add(item);
                    if (!string.IsNullOrWhiteSpace(preferredSelectedEntryId) &&
                        string.Equals(entry.EntryId, preferredSelectedEntryId, StringComparison.Ordinal))
                    {
                        item.Selected = true;
                    }
                }
            }
            finally
            {
                listView.EndUpdate();
            }

            bool hasEntries = entries.Count > 0;
            listView.Visible = hasEntries;
            emptyStateLabel.Visible = !hasEntries;
            emptyStateLabel.Text = emptyStateText;
        }

        private void HandleSelectionChanged(ListView source, ListView other)
        {
            if (_isSynchronizingSelection)
                return;

            _isSynchronizingSelection = true;
            try
            {
                if (source.SelectedItems.Count > 0 && other.SelectedItems.Count > 0)
                    other.SelectedItems.Clear();

                SelectedEntryId = ResolveSelectedEntryId();
                UpdateButtonStates();
            }
            finally
            {
                _isSynchronizingSelection = false;
            }
        }

        private string ResolveSelectedEntryId()
        {
            if (_lvSlottedEntries.SelectedItems.Count > 0)
                return _lvSlottedEntries.SelectedItems[0].Tag as string ?? string.Empty;

            if (_lvAuxiliaryEntries.SelectedItems.Count > 0)
                return _lvAuxiliaryEntries.SelectedItems[0].Tag as string ?? string.Empty;

            return string.Empty;
        }

        private void UpdateButtonStates()
        {
            bool canAdd = _currentState.SupportsEditing;
            bool hasEditableSelection = canAdd && !string.IsNullOrWhiteSpace(SelectedEntryId);

            _btnAddSlotted.Enabled = canAdd;
            _btnAddAuxiliary.Enabled = canAdd;
            _btnApplyTemplate.Enabled = canAdd && _currentState.CanApplyTemplates;
            _btnCopyFromExisting.Enabled = canAdd;
            _btnEditSelected.Enabled = hasEditableSelection;
            _btnDeleteSelected.Enabled = hasEditableSelection;
        }

        private static Control CreateEntriesGroup(string title, ListView listView, Label emptyStateLabel)
        {
            var groupBox = new GroupBox
            {
                Text = title,
                Dock = DockStyle.Fill,
                Padding = new Padding(10),
                Margin = new Padding(0, 0, 0, 12)
            };

            var container = new Panel
            {
                Dock = DockStyle.Fill
            };
            container.Controls.Add(listView);
            container.Controls.Add(emptyStateLabel);
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
                MultiSelect = false,
                View = View.Details
            };
            listView.Columns.Add("Позиция", 110);
            listView.Columns.Add("Слот", 70);
            listView.Columns.Add("Тип", 120);
            listView.Columns.Add("Компонент", 200);
            listView.Columns.Add("IP", 120);
            listView.Columns.Add("Последняя калибровка", 140);
            listView.Columns.Add("Следующая калибровка", 140);
            listView.Columns.Add("Примечание", 260);
            return listView;
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
                Margin = new Padding(0, 0, 8, 8)
            };
    }
}
