using AsutpKnowledgeBase.Services;

namespace AsutpKnowledgeBase
{
    public sealed class KnowledgeBaseNetworkScreenControl : UserControl
    {
        private sealed class ListItemTag
        {
            public string NetworkAssetId { get; init; } = string.Empty;
        }

        private readonly KnowledgeBaseNetworkState _emptyState = new();

        private Label _lblSource = null!;
        private Label _lblSummary = null!;
        private Button _btnAdd = null!;
        private Button _btnOpenSelected = null!;
        private Button _btnEditSelected = null!;
        private Button _btnDeleteSelected = null!;
        private TabControl _contentTabs = null!;
        private TabPage _filesPage = null!;
        private TabPage _previewPage = null!;
        private ListView _lvFiles = null!;
        private Label _lblFilesEmptyState = null!;
        private Label _lblPreviewTitleValue = null!;
        private TextBox _txtPreviewPath = null!;
        private Label _lblPreviewKindValue = null!;
        private Label _lblPreviewStatus = null!;
        private PictureBox _picPreview = null!;
        private Label _lblPreviewEmptyState = null!;

        private KnowledgeBaseNetworkState _currentState = new();
        private bool _isSynchronizingSelection;
        private Image? _previewImage;

        public KnowledgeBaseNetworkScreenControl()
        {
            Dock = DockStyle.Fill;

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(16),
                ColumnCount = 1,
                RowCount = 3
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
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

            layout.Controls.Add(_lblSource, 0, 0);
            layout.Controls.Add(_lblSummary, 0, 1);
            layout.Controls.Add(CreateContentTabs(), 0, 2);
            Controls.Add(layout);

            ApplyState(_emptyState);
        }

        public event EventHandler? AddRequested;

        public event EventHandler? OpenSelectedRequested;

        public event EventHandler? EditSelectedRequested;

        public event EventHandler? DeleteSelectedRequested;

        public string SelectedItemId { get; private set; } = string.Empty;

        public void ApplyState(KnowledgeBaseNetworkState state)
        {
            _currentState = state ?? _emptyState;
            string previousSelectionId = SelectedItemId;

            if (_contentTabs.SelectedTab != _filesPage)
                _contentTabs.SelectedTab = _filesPage;

            _lblSource.Text = _currentState.SourceText;
            _lblSummary.Text = _currentState.HasEntries
                ? $"Файлов сети: {_currentState.FileReferencesCount}"
                : _currentState.EmptyStateText;

            PopulateEntries(previousSelectionId);
            EnsureSelection();
            SelectedItemId = ResolveSelectedItemId();
            UpdateButtonStates();
            UpdatePreview();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                ClearPreviewImage();

            base.Dispose(disposing);
        }

        private Control CreateContentTabs()
        {
            _contentTabs = new TabControl
            {
                Dock = DockStyle.Fill
            };

            _filesPage = new TabPage("Файлы");
            _filesPage.Controls.Add(CreateFilesPageLayout());

            _previewPage = new TabPage("Предпросмотр");
            _previewPage.Controls.Add(CreatePreviewPageLayout());

            _contentTabs.TabPages.Add(_filesPage);
            _contentTabs.TabPages.Add(_previewPage);
            return _contentTabs;
        }

        private Control CreateFilesPageLayout()
        {
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
                Margin = new Padding(0, 0, 0, 12)
            };

            _btnAdd = CreateActionButton("Добавить файл...");
            _btnAdd.Click += (_, _) => AddRequested?.Invoke(this, EventArgs.Empty);
            _btnOpenSelected = CreateActionButton("Открыть оригинал");
            _btnOpenSelected.Click += (_, _) => OpenSelectedRequested?.Invoke(this, EventArgs.Empty);
            _btnEditSelected = CreateActionButton("Изменить...");
            _btnEditSelected.Click += (_, _) => EditSelectedRequested?.Invoke(this, EventArgs.Empty);
            _btnDeleteSelected = CreateActionButton("Удалить");
            _btnDeleteSelected.Click += (_, _) => DeleteSelectedRequested?.Invoke(this, EventArgs.Empty);

            actionsPanel.Controls.Add(_btnAdd);
            actionsPanel.Controls.Add(_btnOpenSelected);
            actionsPanel.Controls.Add(_btnEditSelected);
            actionsPanel.Controls.Add(_btnDeleteSelected);

            _lvFiles = CreateFilesListView();
            _lvFiles.SizeChanged += (_, _) => ResizeFilesColumns();
            _lvFiles.SelectedIndexChanged += (_, _) => HandleSelectionChanged();
            _lvFiles.ItemActivate += (_, _) => OpenSelectedRequested?.Invoke(this, EventArgs.Empty);

            _lblFilesEmptyState = CreateEmptyStateLabel("Для этого узла пока нет файлов сети.");

            var listHost = new Panel
            {
                Dock = DockStyle.Fill,
                BorderStyle = BorderStyle.FixedSingle
            };
            listHost.Controls.Add(_lvFiles);
            listHost.Controls.Add(_lblFilesEmptyState);

            layout.Controls.Add(actionsPanel, 0, 0);
            layout.Controls.Add(listHost, 0, 1);
            return layout;
        }

        private Control CreatePreviewPageLayout()
        {
            var host = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(12)
            };
            host.Controls.Add(CreatePreviewLayout());
            return host;
        }

        private TableLayoutPanel CreatePreviewLayout()
        {
            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 5
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 135F));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            layout.Controls.Add(CreateValueLabel("Наименование"), 0, 0);
            _lblPreviewTitleValue = CreateReadOnlyValueLabel();
            layout.Controls.Add(_lblPreviewTitleValue, 1, 0);

            layout.Controls.Add(CreateValueLabel("Путь"), 0, 1);
            _txtPreviewPath = new TextBox
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = Color.White,
                Multiline = true,
                Height = 44,
                ScrollBars = ScrollBars.Vertical,
                TabStop = false
            };
            layout.Controls.Add(_txtPreviewPath, 1, 1);

            layout.Controls.Add(CreateValueLabel("Тип предпросмотра"), 0, 2);
            _lblPreviewKindValue = CreateReadOnlyValueLabel();
            layout.Controls.Add(_lblPreviewKindValue, 1, 2);

            _lblPreviewStatus = new Label
            {
                Dock = DockStyle.Fill,
                AutoSize = true,
                ForeColor = Color.DimGray,
                Margin = new Padding(0, 6, 0, 10)
            };
            layout.Controls.Add(_lblPreviewStatus, 0, 3);
            layout.SetColumnSpan(_lblPreviewStatus, 2);

            var previewHost = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.WhiteSmoke,
                BorderStyle = BorderStyle.FixedSingle
            };
            _picPreview = new PictureBox
            {
                Dock = DockStyle.Fill,
                SizeMode = PictureBoxSizeMode.Zoom,
                Visible = false
            };
            _lblPreviewEmptyState = CreateEmptyStateLabel("Выберите файл сети для предпросмотра.");
            previewHost.Controls.Add(_picPreview);
            previewHost.Controls.Add(_lblPreviewEmptyState);

            layout.Controls.Add(previewHost, 0, 4);
            layout.SetColumnSpan(previewHost, 2);

            return layout;
        }

        private void PopulateEntries(string preferredSelectionId)
        {
            _lvFiles.BeginUpdate();
            try
            {
                _lvFiles.Items.Clear();
                foreach (var entry in _currentState.FileReferenceStates)
                {
                    var item = new ListViewItem(
                    [
                        entry.TitleText,
                        entry.PreviewKindText,
                        entry.PathText
                    ])
                    {
                        Tag = new ListItemTag
                        {
                            NetworkAssetId = entry.NetworkAssetId
                        }
                    };

                    _lvFiles.Items.Add(item);
                    if (string.Equals(entry.NetworkAssetId, preferredSelectionId, StringComparison.Ordinal))
                        item.Selected = true;
                }
            }
            finally
            {
                _lvFiles.EndUpdate();
            }

            bool hasEntries = _currentState.FileReferenceStates.Count > 0;
            _lvFiles.Visible = hasEntries;
            _lblFilesEmptyState.Visible = !hasEntries;
            _lblFilesEmptyState.Text = _currentState.EmptyStateText;
            ResizeFilesColumns();
        }

        private void EnsureSelection()
        {
            if (_lvFiles.SelectedItems.Count > 0 || _lvFiles.Items.Count == 0)
                return;

            _lvFiles.Items[0].Selected = true;
        }

        private void HandleSelectionChanged()
        {
            if (_isSynchronizingSelection)
                return;

            _isSynchronizingSelection = true;
            try
            {
                SelectedItemId = ResolveSelectedItemId();
                UpdateButtonStates();
                UpdatePreview();
            }
            finally
            {
                _isSynchronizingSelection = false;
            }
        }

        private string ResolveSelectedItemId()
        {
            if (_lvFiles.SelectedItems.Count > 0 && _lvFiles.SelectedItems[0].Tag is ListItemTag tag)
                return tag.NetworkAssetId;

            return string.Empty;
        }

        private KnowledgeBaseNetworkFileReferenceState? FindSelectedState()
        {
            if (string.IsNullOrWhiteSpace(SelectedItemId))
                return null;

            return _currentState.FileReferenceStates.FirstOrDefault(entry =>
                string.Equals(entry.NetworkAssetId, SelectedItemId, StringComparison.Ordinal));
        }

        private void UpdateButtonStates()
        {
            bool canEdit = _currentState.SupportsEditing;
            var selectedState = FindSelectedState();
            bool hasSelection = selectedState != null;
            bool hasPath = hasSelection && !string.Equals(selectedState!.PathText, "-", StringComparison.Ordinal);

            _btnAdd.Enabled = canEdit;
            _btnOpenSelected.Enabled = hasPath;
            _btnEditSelected.Enabled = canEdit && hasSelection;
            _btnDeleteSelected.Enabled = canEdit && hasSelection;
        }

        private void UpdatePreview()
        {
            var selectedState = FindSelectedState();
            if (selectedState == null)
            {
                ClearPreviewImage();
                _lblPreviewTitleValue.Text = "-";
                _txtPreviewPath.Text = string.Empty;
                _lblPreviewKindValue.Text = "-";
                ShowPreviewMessage("Выберите файл сети для предпросмотра.");
                return;
            }

            string path = string.Equals(selectedState.PathText, "-", StringComparison.Ordinal)
                ? string.Empty
                : selectedState.PathText;

            _lblPreviewTitleValue.Text = selectedState.TitleText;
            _txtPreviewPath.Text = path;
            _lblPreviewKindValue.Text = selectedState.PreviewKindText;

            if (string.IsNullOrWhiteSpace(path))
            {
                ShowPreviewMessage("У выбранного файла сети не заполнен путь.");
                return;
            }

            if (!selectedState.CanPreviewInForm)
            {
                ShowPreviewMessage("Для этого типа файла встроенный предпросмотр пока не поддерживается. Используйте \"Открыть оригинал\".");
                return;
            }

            if (!File.Exists(path))
            {
                ShowPreviewMessage("Файл недоступен по указанному пути. Проверьте доступ к серверу или откройте оригинал напрямую.");
                return;
            }

            if (!TryLoadPreviewImage(path, out var previewImage, out var errorMessage))
            {
                ShowPreviewMessage($"Не удалось загрузить предпросмотр: {errorMessage}");
                return;
            }

            SetPreviewImage(previewImage);
        }

        private void ShowPreviewMessage(string message)
        {
            ClearPreviewImage();
            _lblPreviewStatus.Text = message;
            _picPreview.Visible = false;
            _lblPreviewEmptyState.Visible = true;
            _lblPreviewEmptyState.Text = message;
        }

        private void SetPreviewImage(Image image)
        {
            ClearPreviewImage();
            _previewImage = image;
            _picPreview.Image = _previewImage;
            _picPreview.Visible = true;
            _lblPreviewEmptyState.Visible = false;
            _lblPreviewStatus.Text = "Предпросмотр изображения.";
        }

        private void ClearPreviewImage()
        {
            _picPreview.Image = null;
            _previewImage?.Dispose();
            _previewImage = null;
        }

        private static bool TryLoadPreviewImage(string path, out Image image, out string errorMessage)
        {
            try
            {
                using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                using var sourceImage = Image.FromStream(stream);
                image = new Bitmap(sourceImage);
                errorMessage = string.Empty;
                return true;
            }
            catch (Exception ex)
            {
                image = null!;
                errorMessage = ex.Message;
                return false;
            }
        }

        private void ResizeFilesColumns()
        {
            if (_lvFiles.Columns.Count != 3)
                return;

            int clientWidth = _lvFiles.ClientSize.Width;
            if (clientWidth <= 0)
                return;

            int previewWidth = 170;
            int titleWidth = Math.Max(220, (int)(clientWidth * 0.28f));
            int pathWidth = Math.Max(280, clientWidth - titleWidth - previewWidth - 8);

            _lvFiles.Columns[0].Width = titleWidth;
            _lvFiles.Columns[1].Width = previewWidth;
            _lvFiles.Columns[2].Width = pathWidth;
        }

        private static ListView CreateFilesListView()
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
            listView.Columns.Add("Наименование", 220);
            listView.Columns.Add("Предпросмотр", 170);
            listView.Columns.Add("Путь", 360);
            return listView;
        }

        private static Label CreateValueLabel(string text) =>
            new()
            {
                Text = text,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                Margin = new Padding(0, 0, 8, 8)
            };

        private static Label CreateReadOnlyValueLabel() =>
            new()
            {
                Dock = DockStyle.Fill,
                AutoSize = true,
                TextAlign = ContentAlignment.MiddleLeft,
                AutoEllipsis = true,
                Margin = new Padding(0, 0, 0, 8)
            };

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
