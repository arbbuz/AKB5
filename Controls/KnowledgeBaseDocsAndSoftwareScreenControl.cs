using AsutpKnowledgeBase.Services;

namespace AsutpKnowledgeBase
{
    public enum KnowledgeBaseDocsAndSoftwareSelectionKind
    {
        None = 0,
        DocumentLink = 1,
        SoftwareRecord = 2
    }

    public sealed class KnowledgeBaseDocsAndSoftwareScreenControl : UserControl
    {
        private sealed class ListItemTag
        {
            public KnowledgeBaseDocsAndSoftwareSelectionKind SelectionKind { get; init; }

            public string ItemId { get; init; } = string.Empty;
        }

        private readonly KnowledgeBaseDocsAndSoftwareState _emptyState = new();

        private Label _lblSource = null!;
        private Label _lblSummary = null!;
        private Button _btnAddScheme = null!;
        private Button _btnAddDocument = null!;
        private Button _btnAddSoftware = null!;
        private Button _btnOpenSelected = null!;
        private Button _btnEditSelected = null!;
        private Button _btnDeleteSelected = null!;
        private ListView _lvSchemes = null!;
        private ListView _lvDocuments = null!;
        private ListView _lvSoftware = null!;
        private Label _lblSchemesEmptyState = null!;
        private Label _lblDocumentsEmptyState = null!;
        private Label _lblSoftwareEmptyState = null!;

        private KnowledgeBaseDocsAndSoftwareState _currentState = new();
        private bool _isSynchronizingSelection;

        public KnowledgeBaseDocsAndSoftwareScreenControl()
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

            _btnAddScheme = CreateActionButton("Добавить схему");
            _btnAddScheme.Click += (_, _) => AddSchemeRequested?.Invoke(this, EventArgs.Empty);
            _btnAddDocument = CreateActionButton("Добавить инструкцию");
            _btnAddDocument.Click += (_, _) => AddDocumentRequested?.Invoke(this, EventArgs.Empty);
            _btnAddSoftware = CreateActionButton("Добавить ПО");
            _btnAddSoftware.Click += (_, _) => AddSoftwareRequested?.Invoke(this, EventArgs.Empty);
            _btnOpenSelected = CreateActionButton("Открыть");
            _btnOpenSelected.Click += (_, _) => OpenSelectedRequested?.Invoke(this, EventArgs.Empty);
            _btnEditSelected = CreateActionButton("Изменить");
            _btnEditSelected.Click += (_, _) => EditSelectedRequested?.Invoke(this, EventArgs.Empty);
            _btnDeleteSelected = CreateActionButton("Удалить");
            _btnDeleteSelected.Click += (_, _) => DeleteSelectedRequested?.Invoke(this, EventArgs.Empty);

            actionsPanel.Controls.Add(_btnAddScheme);
            actionsPanel.Controls.Add(_btnAddDocument);
            actionsPanel.Controls.Add(_btnAddSoftware);
            actionsPanel.Controls.Add(_btnOpenSelected);
            actionsPanel.Controls.Add(_btnEditSelected);
            actionsPanel.Controls.Add(_btnDeleteSelected);

            var contentLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                BackColor = KnowledgeBaseWorkspaceVisuals.SurfaceColor,
                ColumnCount = 1,
                RowCount = 3
            };
            contentLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            contentLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 33.34F));
            contentLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 33.33F));
            contentLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 33.33F));

            _lvSchemes = CreateDocumentListView();
            _lvDocuments = CreateDocumentListView();
            _lvSoftware = CreateSoftwareListView();
            _lvSchemes.ContextMenuStrip = CreateEntriesContextMenu(() => AddSchemeRequested?.Invoke(this, EventArgs.Empty));
            _lvDocuments.ContextMenuStrip = CreateEntriesContextMenu(() => AddDocumentRequested?.Invoke(this, EventArgs.Empty));
            _lvSoftware.ContextMenuStrip = CreateEntriesContextMenu(() => AddSoftwareRequested?.Invoke(this, EventArgs.Empty));
            _lblSchemesEmptyState = CreateEmptyStateLabel("Схемы для этого узла пока не добавлены.");
            _lblDocumentsEmptyState = CreateEmptyStateLabel("Инструкции для этого узла пока не добавлены.");
            _lblSoftwareEmptyState = CreateEmptyStateLabel("Ссылки на ПО для этого узла пока не добавлены.");

            WireSelectionEvents(_lvSchemes, _lvDocuments, _lvSoftware);
            WireSelectionEvents(_lvDocuments, _lvSchemes, _lvSoftware);
            WireSelectionEvents(_lvSoftware, _lvSchemes, _lvDocuments);

            contentLayout.Controls.Add(CreateEntriesGroup("Схемы", _lvSchemes, _lblSchemesEmptyState), 0, 0);
            contentLayout.Controls.Add(CreateEntriesGroup("Инструкции", _lvDocuments, _lblDocumentsEmptyState), 0, 1);
            contentLayout.Controls.Add(CreateEntriesGroup("ПО", _lvSoftware, _lblSoftwareEmptyState), 0, 2);

            layout.Controls.Add(_lblSource, 0, 0);
            layout.Controls.Add(_lblSummary, 0, 1);
            layout.Controls.Add(actionsPanel, 0, 2);
            layout.Controls.Add(contentLayout, 0, 3);
            Controls.Add(layout);

            ApplyState(_emptyState);
        }

        public event EventHandler? AddSchemeRequested;

        public event EventHandler? AddDocumentRequested;

        public event EventHandler? AddSoftwareRequested;

        public event EventHandler? OpenSelectedRequested;

        public event EventHandler? EditSelectedRequested;

        public event EventHandler? DeleteSelectedRequested;

        public KnowledgeBaseDocsAndSoftwareSelectionKind SelectedItemKind { get; private set; }

        public string SelectedItemId { get; private set; } = string.Empty;

        public void ApplyState(KnowledgeBaseDocsAndSoftwareState state)
        {
            _currentState = state ?? _emptyState;
            var previousSelectionKind = SelectedItemKind;
            string previousSelectionId = SelectedItemId;

            _lblSource.Text = _currentState.SourceText;
            _lblSummary.Text = _currentState.HasEntries
                ? $"Схем: {_currentState.SchemeLinksCount} | Инструкций: {_currentState.ManualsAndInstructionsCount} | Ссылок на ПО: {_currentState.SoftwareRecordsCount}"
                : _currentState.EmptyStateText;

            PopulateDocumentEntries(
                _lvSchemes,
                _lblSchemesEmptyState,
                _currentState.SchemeLinkStates,
                "Схемы для этого узла пока не добавлены.",
                previousSelectionKind,
                previousSelectionId);
            PopulateDocumentEntries(
                _lvDocuments,
                _lblDocumentsEmptyState,
                _currentState.ManualAndInstructionStates,
                "Инструкции для этого узла пока не добавлены.",
                previousSelectionKind,
                previousSelectionId);
            PopulateSoftwareEntries(
                _lvSoftware,
                _lblSoftwareEmptyState,
                _currentState.SoftwareRecordStates,
                "Ссылки на ПО для этого узла пока не добавлены.",
                previousSelectionKind,
                previousSelectionId);

            (SelectedItemKind, SelectedItemId) = ResolveSelectedItem();
            UpdateButtonStates();
        }

        private void PopulateDocumentEntries(
            ListView listView,
            Label emptyStateLabel,
            IReadOnlyList<KnowledgeBaseDocumentLinkState> entries,
            string emptyStateText,
            KnowledgeBaseDocsAndSoftwareSelectionKind preferredSelectionKind,
            string preferredSelectionId)
        {
            listView.BeginUpdate();
            try
            {
                listView.Items.Clear();
                foreach (var entry in entries)
                {
                    var item = new ListViewItem(
                    [
                        entry.TitleText,
                        entry.PathText,
                        entry.UpdatedAtText
                    ])
                    {
                        Tag = new ListItemTag
                        {
                            SelectionKind = KnowledgeBaseDocsAndSoftwareSelectionKind.DocumentLink,
                            ItemId = entry.DocumentId
                        }
                    };

                    listView.Items.Add(item);
                    if (preferredSelectionKind == KnowledgeBaseDocsAndSoftwareSelectionKind.DocumentLink &&
                        string.Equals(entry.DocumentId, preferredSelectionId, StringComparison.Ordinal))
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
            AdjustEntriesColumnWidths(listView);
        }

        private void PopulateSoftwareEntries(
            ListView listView,
            Label emptyStateLabel,
            IReadOnlyList<KnowledgeBaseSoftwareRecordState> entries,
            string emptyStateText,
            KnowledgeBaseDocsAndSoftwareSelectionKind preferredSelectionKind,
            string preferredSelectionId)
        {
            listView.BeginUpdate();
            try
            {
                listView.Items.Clear();
                foreach (var entry in entries)
                {
                    var item = new ListViewItem(
                    [
                        entry.TitleText,
                        entry.PathText,
                        entry.AddedAtText
                    ])
                    {
                        Tag = new ListItemTag
                        {
                            SelectionKind = KnowledgeBaseDocsAndSoftwareSelectionKind.SoftwareRecord,
                            ItemId = entry.SoftwareId
                        }
                    };

                    listView.Items.Add(item);
                    if (preferredSelectionKind == KnowledgeBaseDocsAndSoftwareSelectionKind.SoftwareRecord &&
                        string.Equals(entry.SoftwareId, preferredSelectionId, StringComparison.Ordinal))
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
            AdjustEntriesColumnWidths(listView);
        }

        private void WireSelectionEvents(ListView source, params ListView[] others)
        {
            source.SelectedIndexChanged += (_, _) => HandleSelectionChanged(source, others);
            source.ItemActivate += (_, _) => OpenSelectedRequested?.Invoke(this, EventArgs.Empty);
            source.MouseDown += (_, e) => HandleListViewMouseDown(source, others, e);
        }

        private void HandleListViewMouseDown(ListView source, IReadOnlyList<ListView> others, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Right)
                return;

            ListViewItem? item = source.GetItemAt(e.X, e.Y);
            _isSynchronizingSelection = true;
            try
            {
                foreach (var other in others)
                {
                    if (other.SelectedItems.Count > 0)
                        other.SelectedItems.Clear();
                }

                if (source.SelectedItems.Count > 0)
                    source.SelectedItems.Clear();

                if (item != null)
                {
                    item.Selected = true;
                    item.Focused = true;
                    source.Focus();
                }

                (SelectedItemKind, SelectedItemId) = ResolveSelectedItem();
                UpdateButtonStates();
            }
            finally
            {
                _isSynchronizingSelection = false;
            }
        }

        private void HandleSelectionChanged(ListView source, IReadOnlyList<ListView> others)
        {
            if (_isSynchronizingSelection)
                return;

            _isSynchronizingSelection = true;
            try
            {
                if (source.SelectedItems.Count > 0)
                {
                    foreach (var other in others)
                    {
                        if (other.SelectedItems.Count > 0)
                            other.SelectedItems.Clear();
                    }
                }

                (SelectedItemKind, SelectedItemId) = ResolveSelectedItem();
                UpdateButtonStates();
            }
            finally
            {
                _isSynchronizingSelection = false;
            }
        }

        private (KnowledgeBaseDocsAndSoftwareSelectionKind Kind, string ItemId) ResolveSelectedItem()
        {
            if (TryGetSelectedTag(_lvSchemes, out var schemeTag))
                return (schemeTag.SelectionKind, schemeTag.ItemId);

            if (TryGetSelectedTag(_lvDocuments, out var documentTag))
                return (documentTag.SelectionKind, documentTag.ItemId);

            if (TryGetSelectedTag(_lvSoftware, out var softwareTag))
                return (softwareTag.SelectionKind, softwareTag.ItemId);

            return (KnowledgeBaseDocsAndSoftwareSelectionKind.None, string.Empty);
        }

        private void UpdateButtonStates()
        {
            bool canAdd = _currentState.SupportsEditing;
            bool hasSelection =
                SelectedItemKind != KnowledgeBaseDocsAndSoftwareSelectionKind.None &&
                !string.IsNullOrWhiteSpace(SelectedItemId);

            _btnAddScheme.Enabled = canAdd;
            _btnAddDocument.Enabled = canAdd;
            _btnAddSoftware.Enabled = canAdd;
            _btnOpenSelected.Enabled = hasSelection;
            _btnEditSelected.Enabled = canAdd && hasSelection;
            _btnDeleteSelected.Enabled = canAdd && hasSelection;
        }

        private static bool TryGetSelectedTag(ListView listView, out ListItemTag tag)
        {
            if (listView.SelectedItems.Count > 0 && listView.SelectedItems[0].Tag is ListItemTag selectedTag)
            {
                tag = selectedTag;
                return true;
            }

            tag = new ListItemTag();
            return false;
        }

        private static Control CreateEntriesGroup(string title, ListView listView, Label emptyStateLabel)
        {
            var groupBox = new KnowledgeBaseWorkspaceVisuals.SectionPanel
            {
                Text = title,
                Dock = DockStyle.Fill,
                Padding = new Padding(10, 20, 10, 10),
                Margin = new Padding(0, 0, 0, 12)
            };

            var container = new KnowledgeBaseWorkspaceVisuals.BorderPanel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(1)
            };
            container.Controls.Add(listView);
            container.Controls.Add(emptyStateLabel);
            groupBox.Controls.Add(container);
            return groupBox;
        }

        private static ListView CreateDocumentListView()
        {
            var listView = CreateBaseListView();
            listView.Columns.Add("Наименование", 700);
            listView.Columns.Add("Путь", 330);
            listView.Columns.Add("Обновлено", 160);
            return listView;
        }

        private static ListView CreateSoftwareListView()
        {
            var listView = CreateBaseListView();
            listView.Columns.Add("Наименование", 700);
            listView.Columns.Add("Путь", 330);
            listView.Columns.Add("Добавлено", 160);
            return listView;
        }

        private static ListView CreateBaseListView() =>
            ConfigureBaseListView(
                new()
                {
                    Dock = DockStyle.Fill,
                    FullRowSelect = true,
                    GridLines = true,
                    HeaderStyle = ColumnHeaderStyle.Nonclickable,
                    MultiSelect = false,
                    View = View.Details
                });

        private static ListView ConfigureBaseListView(ListView listView)
        {
            KnowledgeBaseWorkspaceVisuals.ConfigureListView(listView);
            listView.Resize += (_, _) => AdjustEntriesColumnWidths(listView);
            return listView;
        }

        private static void AdjustEntriesColumnWidths(ListView listView)
        {
            if (listView.Columns.Count < 3)
                return;

            int availableWidth = Math.Max(0, listView.ClientSize.Width - 4);
            if (availableWidth <= 0)
                return;

            const int minimumTitleWidth = 280;
            const int minimumPathWidth = 180;
            const int minimumDateWidth = 110;
            int minimumTotal = minimumTitleWidth + minimumPathWidth + minimumDateWidth;

            if (availableWidth <= minimumTotal)
            {
                int compactTitleWidth = Math.Max(1, availableWidth * 58 / 100);
                int compactDateWidth = Math.Max(1, availableWidth * 14 / 100);

                listView.Columns[0].Width = compactTitleWidth;
                listView.Columns[1].Width = Math.Max(1, availableWidth - compactTitleWidth - compactDateWidth);
                listView.Columns[2].Width = compactDateWidth;
                return;
            }

            int titleWidth = Math.Max(minimumTitleWidth, availableWidth * 58 / 100);
            int dateWidth = Math.Max(minimumDateWidth, availableWidth * 14 / 100);
            int pathWidth = Math.Max(minimumPathWidth, availableWidth - titleWidth - dateWidth);

            listView.Columns[0].Width = titleWidth;
            listView.Columns[1].Width = pathWidth;
            listView.Columns[2].Width = Math.Max(minimumDateWidth, availableWidth - titleWidth - pathWidth);
        }

        private ContextMenuStrip CreateEntriesContextMenu(Action addAction)
        {
            var menu = new ContextMenuStrip();
            ToolStripMenuItem openItem = CreateContextMenuItem("Открыть", () => OpenSelectedRequested?.Invoke(this, EventArgs.Empty));
            ToolStripMenuItem editItem = CreateContextMenuItem("Изменить", () => EditSelectedRequested?.Invoke(this, EventArgs.Empty));
            ToolStripMenuItem addItem = CreateContextMenuItem("Добавить", addAction);
            ToolStripMenuItem deleteItem = CreateContextMenuItem("Удалить", () => DeleteSelectedRequested?.Invoke(this, EventArgs.Empty));

            menu.Items.Add(openItem);
            menu.Items.Add(editItem);
            menu.Items.Add(addItem);
            menu.Items.Add(deleteItem);
            menu.Opening += (_, _) =>
            {
                bool hasSelection =
                    SelectedItemKind != KnowledgeBaseDocsAndSoftwareSelectionKind.None &&
                    !string.IsNullOrWhiteSpace(SelectedItemId);
                openItem.Enabled = hasSelection;
                editItem.Enabled = _currentState.SupportsEditing && hasSelection;
                addItem.Enabled = _currentState.SupportsEditing;
                deleteItem.Enabled = _currentState.SupportsEditing && hasSelection;
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
