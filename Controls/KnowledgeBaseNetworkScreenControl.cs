using AsutpKnowledgeBase.Services;

namespace AsutpKnowledgeBase
{
    public enum KnowledgeBaseNetworkSelectionKind
    {
        None = 0,
        Device = 1,
        Interface = 2,
        Connection = 3,
        FileReference = 4
    }

    public sealed class KnowledgeBaseNetworkScreenControl : UserControl
    {
        private sealed class ListItemTag
        {
            public KnowledgeBaseNetworkSelectionKind SelectionKind { get; init; }

            public string ItemId { get; init; } = string.Empty;
        }

        private readonly KnowledgeBaseNetworkState _emptyState = new();

        private Label _lblSource = null!;
        private Label _lblSummary = null!;
        private Button _btnAddDevice = null!;
        private Button _btnEditDevice = null!;
        private Button _btnDeleteDevice = null!;
        private Button _btnAddInterface = null!;
        private Button _btnEditInterface = null!;
        private Button _btnDeleteInterface = null!;
        private Button _btnAddConnection = null!;
        private Button _btnEditConnection = null!;
        private Button _btnDeleteConnection = null!;
        private TextBox _txtPassportFilter = null!;
        private Button _btnClearPassportFilter = null!;
        private Label _lblPassportFilterStatus = null!;
        private Button _btnAdd = null!;
        private Button _btnOpenSelected = null!;
        private Button _btnEditSelected = null!;
        private Button _btnDeleteSelected = null!;
        private TabControl _contentTabs = null!;
        private TabPage _passportPage = null!;
        private TabPage _filesPage = null!;
        private TabPage _previewPage = null!;
        private ListView _lvDevices = null!;
        private ListView _lvInterfaces = null!;
        private ListView _lvConnections = null!;
        private ListView _lvFiles = null!;
        private Label _lblDevicesEmptyState = null!;
        private Label _lblInterfacesEmptyState = null!;
        private Label _lblConnectionsEmptyState = null!;
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

        public event EventHandler? AddDeviceRequested;

        public event EventHandler? EditDeviceRequested;

        public event EventHandler? DeleteDeviceRequested;

        public event EventHandler? AddInterfaceRequested;

        public event EventHandler? EditInterfaceRequested;

        public event EventHandler? DeleteInterfaceRequested;

        public event EventHandler? AddConnectionRequested;

        public event EventHandler? EditConnectionRequested;

        public event EventHandler? DeleteConnectionRequested;

        public event EventHandler? AddRequested;

        public event EventHandler? OpenSelectedRequested;

        public event EventHandler? EditSelectedRequested;

        public event EventHandler? DeleteSelectedRequested;

        public string SelectedDeviceId { get; private set; } = string.Empty;

        public string SelectedInterfaceId { get; private set; } = string.Empty;

        public string SelectedConnectionId { get; private set; } = string.Empty;

        public string SelectedItemId { get; private set; } = string.Empty;

        public void ApplyState(KnowledgeBaseNetworkState state)
        {
            _currentState = state ?? _emptyState;
            string previousDeviceId = SelectedDeviceId;
            string previousInterfaceId = SelectedInterfaceId;
            string previousConnectionId = SelectedConnectionId;
            string previousFileReferenceId = SelectedItemId;

            if (_contentTabs.SelectedTab == _previewPage)
                _contentTabs.SelectedTab = _filesPage;

            _lblSource.Text = _currentState.SourceText;
            _lblSummary.Text = _currentState.HasPassportRows || _currentState.HasEntries
                ? $"Устройств: {_currentState.DeviceCount} | Интерфейсов: {_currentState.InterfaceCount} | Соединений: {_currentState.ConnectionCount} | Файлов: {_currentState.FileReferencesCount}"
                : _currentState.EmptyStateText;

            RefreshPassportEntries(previousDeviceId, previousInterfaceId, previousConnectionId);
            PopulateFileEntries(previousFileReferenceId);
            EnsureFileSelection();

            ResolvePassportSelection();
            SelectedItemId = ResolveSelectedFileReferenceId();
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

            _passportPage = new TabPage("Паспорт");
            _passportPage.Controls.Add(CreatePassportPageLayout());

            _filesPage = new TabPage("Файлы");
            _filesPage.Controls.Add(CreateFilesPageLayout());

            _previewPage = new TabPage("Предпросмотр");
            _previewPage.Controls.Add(CreatePreviewPageLayout());

            _contentTabs.TabPages.Add(_passportPage);
            _contentTabs.TabPages.Add(_filesPage);
            _contentTabs.TabPages.Add(_previewPage);
            return _contentTabs;
        }

        private Control CreatePassportPageLayout()
        {
            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(12),
                ColumnCount = 1,
                RowCount = 4
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 34F));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 33F));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 33F));

            _lvDevices = CreateDevicesListView();
            _lvInterfaces = CreateInterfacesListView();
            _lvConnections = CreateConnectionsListView();

            _lblDevicesEmptyState = CreateEmptyStateLabel("Устройства сетевого паспорта пока не добавлены.");
            _lblInterfacesEmptyState = CreateEmptyStateLabel("Интерфейсы и IP-адреса пока не добавлены.");
            _lblConnectionsEmptyState = CreateEmptyStateLabel("Соединения между интерфейсами пока не добавлены.");

            WirePassportSelectionEvents(_lvDevices, _lvInterfaces, _lvConnections);
            WirePassportSelectionEvents(_lvInterfaces, _lvDevices, _lvConnections);
            WirePassportSelectionEvents(_lvConnections, _lvDevices, _lvInterfaces);

            layout.Controls.Add(CreatePassportFilterPanel(), 0, 0);
            layout.Controls.Add(
                CreatePassportGroup(
                    "Устройства",
                    CreateDeviceActionsPanel(),
                    _lvDevices,
                    _lblDevicesEmptyState),
                0,
                1);
            layout.Controls.Add(
                CreatePassportGroup(
                    "Интерфейсы / IP",
                    CreateInterfaceActionsPanel(),
                    _lvInterfaces,
                    _lblInterfacesEmptyState),
                0,
                2);
            layout.Controls.Add(
                CreatePassportGroup(
                    "Соединения",
                    CreateConnectionActionsPanel(),
                    _lvConnections,
                    _lblConnectionsEmptyState),
                0,
                3);

            return layout;
        }

        private Control CreatePassportFilterPanel()
        {
            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                ColumnCount = 4,
                RowCount = 1,
                Margin = new Padding(0, 0, 0, 10)
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 210F));

            layout.Controls.Add(CreateValueLabel("Фильтр"), 0, 0);

            _txtPassportFilter = new TextBox
            {
                Dock = DockStyle.Fill,
                AccessibleName = "Фильтр паспорта",
                PlaceholderText = "IP / протокол / среда / кабель",
                Margin = new Padding(0, 0, 8, 8)
            };
            _txtPassportFilter.TextChanged += (_, _) => HandlePassportFilterChanged();
            layout.Controls.Add(_txtPassportFilter, 1, 0);

            _btnClearPassportFilter = CreateActionButton("Сбросить");
            _btnClearPassportFilter.Click += (_, _) => _txtPassportFilter.Clear();
            layout.Controls.Add(_btnClearPassportFilter, 2, 0);

            _lblPassportFilterStatus = new Label
            {
                Dock = DockStyle.Fill,
                AutoEllipsis = true,
                ForeColor = Color.DimGray,
                TextAlign = ContentAlignment.MiddleLeft,
                Margin = new Padding(0, 3, 0, 8)
            };
            layout.Controls.Add(_lblPassportFilterStatus, 3, 0);

            return layout;
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

            _btnAdd = CreateActionButton("Добавить файл");
            _btnAdd.Click += (_, _) => AddRequested?.Invoke(this, EventArgs.Empty);
            _btnOpenSelected = CreateActionButton("Открыть оригинал");
            _btnOpenSelected.Click += (_, _) => OpenSelectedRequested?.Invoke(this, EventArgs.Empty);
            _btnEditSelected = CreateActionButton("Изменить");
            _btnEditSelected.Click += (_, _) => EditSelectedRequested?.Invoke(this, EventArgs.Empty);
            _btnDeleteSelected = CreateActionButton("Удалить");
            _btnDeleteSelected.Click += (_, _) => DeleteSelectedRequested?.Invoke(this, EventArgs.Empty);

            actionsPanel.Controls.Add(_btnAdd);
            actionsPanel.Controls.Add(_btnOpenSelected);
            actionsPanel.Controls.Add(_btnEditSelected);
            actionsPanel.Controls.Add(_btnDeleteSelected);

            _lvFiles = CreateFilesListView();
            _lvFiles.SizeChanged += (_, _) => ResizeFilesColumns();
            _lvFiles.SelectedIndexChanged += (_, _) => HandleFileSelectionChanged();
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

        private FlowLayoutPanel CreateDeviceActionsPanel()
        {
            var panel = CreateActionsPanel();
            _btnAddDevice = CreateActionButton("Добавить устройство");
            _btnAddDevice.Click += (_, _) => AddDeviceRequested?.Invoke(this, EventArgs.Empty);
            _btnEditDevice = CreateActionButton("Изменить");
            _btnEditDevice.Click += (_, _) => EditDeviceRequested?.Invoke(this, EventArgs.Empty);
            _btnDeleteDevice = CreateActionButton("Удалить");
            _btnDeleteDevice.Click += (_, _) => DeleteDeviceRequested?.Invoke(this, EventArgs.Empty);
            panel.Controls.Add(_btnAddDevice);
            panel.Controls.Add(_btnEditDevice);
            panel.Controls.Add(_btnDeleteDevice);
            return panel;
        }

        private FlowLayoutPanel CreateInterfaceActionsPanel()
        {
            var panel = CreateActionsPanel();
            _btnAddInterface = CreateActionButton("Добавить интерфейс");
            _btnAddInterface.Click += (_, _) => AddInterfaceRequested?.Invoke(this, EventArgs.Empty);
            _btnEditInterface = CreateActionButton("Изменить");
            _btnEditInterface.Click += (_, _) => EditInterfaceRequested?.Invoke(this, EventArgs.Empty);
            _btnDeleteInterface = CreateActionButton("Удалить");
            _btnDeleteInterface.Click += (_, _) => DeleteInterfaceRequested?.Invoke(this, EventArgs.Empty);
            panel.Controls.Add(_btnAddInterface);
            panel.Controls.Add(_btnEditInterface);
            panel.Controls.Add(_btnDeleteInterface);
            return panel;
        }

        private FlowLayoutPanel CreateConnectionActionsPanel()
        {
            var panel = CreateActionsPanel();
            _btnAddConnection = CreateActionButton("Добавить соединение");
            _btnAddConnection.Click += (_, _) => AddConnectionRequested?.Invoke(this, EventArgs.Empty);
            _btnEditConnection = CreateActionButton("Изменить");
            _btnEditConnection.Click += (_, _) => EditConnectionRequested?.Invoke(this, EventArgs.Empty);
            _btnDeleteConnection = CreateActionButton("Удалить");
            _btnDeleteConnection.Click += (_, _) => DeleteConnectionRequested?.Invoke(this, EventArgs.Empty);
            panel.Controls.Add(_btnAddConnection);
            panel.Controls.Add(_btnEditConnection);
            panel.Controls.Add(_btnDeleteConnection);
            return panel;
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

        private void RefreshPassportEntries(
            string preferredDeviceId,
            string preferredInterfaceId,
            string preferredConnectionId)
        {
            int visibleDeviceCount = PopulateDeviceEntries(preferredDeviceId);
            int visibleInterfaceCount = PopulateInterfaceEntries(preferredInterfaceId);
            int visibleConnectionCount = PopulateConnectionEntries(preferredConnectionId);
            UpdatePassportFilterStatus(visibleDeviceCount, visibleInterfaceCount, visibleConnectionCount);
        }

        private int PopulateDeviceEntries(string preferredSelectionId)
        {
            string filterText = GetPassportFilterText();
            int visibleCount = 0;

            _lvDevices.BeginUpdate();
            try
            {
                _lvDevices.Items.Clear();
                foreach (var entry in _currentState.DeviceStates)
                {
                    if (!DeviceMatchesFilter(entry, filterText))
                        continue;

                    var item = new ListViewItem(
                    [
                        entry.NameText,
                        entry.RoleText,
                        entry.ModelText,
                        entry.ProfinetNameText,
                        entry.MacAddressText,
                        entry.LinkedNodeText,
                        entry.InterfacesCount.ToString(),
                        entry.ConnectionsCount.ToString()
                    ])
                    {
                        Tag = new ListItemTag
                        {
                            SelectionKind = KnowledgeBaseNetworkSelectionKind.Device,
                            ItemId = entry.NetworkDeviceId
                        }
                    };

                    _lvDevices.Items.Add(item);
                    visibleCount++;
                    if (string.Equals(entry.NetworkDeviceId, preferredSelectionId, StringComparison.Ordinal))
                        item.Selected = true;
                }
            }
            finally
            {
                _lvDevices.EndUpdate();
            }

            bool hasEntries = visibleCount > 0;
            _lvDevices.Visible = hasEntries;
            _lblDevicesEmptyState.Visible = !hasEntries;
            _lblDevicesEmptyState.Text = HasPassportFilter && _currentState.DeviceStates.Count > 0
                ? "По фильтру ничего не найдено."
                : _currentState.SupportsPassportEditing
                ? "Устройства сетевого паспорта пока не добавлены."
                : "Сетевой паспорт доступен только для системы уровня 2.";
            return visibleCount;
        }

        private int PopulateInterfaceEntries(string preferredSelectionId)
        {
            string filterText = GetPassportFilterText();
            int visibleCount = 0;

            _lvInterfaces.BeginUpdate();
            try
            {
                _lvInterfaces.Items.Clear();
                foreach (var entry in _currentState.InterfaceStates)
                {
                    if (!InterfaceMatchesFilter(entry, filterText))
                        continue;

                    var item = new ListViewItem(
                    [
                        entry.DeviceNameText,
                        entry.InterfaceNameText,
                        entry.IpAddressText,
                        entry.SubnetMaskText,
                        entry.GatewayText,
                        entry.ProtocolText,
                        entry.VlanText,
                        entry.MacAddressText
                    ])
                    {
                        Tag = new ListItemTag
                        {
                            SelectionKind = KnowledgeBaseNetworkSelectionKind.Interface,
                            ItemId = entry.NetworkInterfaceId
                        }
                    };

                    _lvInterfaces.Items.Add(item);
                    visibleCount++;
                    if (string.Equals(entry.NetworkInterfaceId, preferredSelectionId, StringComparison.Ordinal))
                        item.Selected = true;
                }
            }
            finally
            {
                _lvInterfaces.EndUpdate();
            }

            bool hasEntries = visibleCount > 0;
            _lvInterfaces.Visible = hasEntries;
            _lblInterfacesEmptyState.Visible = !hasEntries;
            _lblInterfacesEmptyState.Text = HasPassportFilter && _currentState.InterfaceStates.Count > 0
                ? "По фильтру ничего не найдено."
                : _currentState.DeviceCount > 0
                ? "Интерфейсы и IP-адреса пока не добавлены."
                : "Сначала добавьте устройство сетевого паспорта.";
            return visibleCount;
        }

        private int PopulateConnectionEntries(string preferredSelectionId)
        {
            string filterText = GetPassportFilterText();
            int visibleCount = 0;

            _lvConnections.BeginUpdate();
            try
            {
                _lvConnections.Items.Clear();
                foreach (var entry in _currentState.ConnectionStates)
                {
                    if (!ConnectionMatchesFilter(entry, filterText))
                        continue;

                    var item = new ListViewItem(
                    [
                        entry.EndpointAText,
                        entry.EndpointBText,
                        entry.CableLabelText,
                        entry.CableTypeText,
                        entry.ProtocolText,
                        entry.MediumText,
                        entry.RouteText,
                        entry.StatusText,
                        entry.NotesText
                    ])
                    {
                        Tag = new ListItemTag
                        {
                            SelectionKind = KnowledgeBaseNetworkSelectionKind.Connection,
                            ItemId = entry.NetworkConnectionId
                        }
                    };

                    _lvConnections.Items.Add(item);
                    visibleCount++;
                    if (string.Equals(entry.NetworkConnectionId, preferredSelectionId, StringComparison.Ordinal))
                        item.Selected = true;
                }
            }
            finally
            {
                _lvConnections.EndUpdate();
            }

            bool hasEntries = visibleCount > 0;
            _lvConnections.Visible = hasEntries;
            _lblConnectionsEmptyState.Visible = !hasEntries;
            _lblConnectionsEmptyState.Text = HasPassportFilter && _currentState.ConnectionStates.Count > 0
                ? "По фильтру ничего не найдено."
                : _currentState.InterfaceCount >= 2
                ? "Соединения между интерфейсами пока не добавлены."
                : "Для соединения нужны минимум два интерфейса.";
            return visibleCount;
        }

        private void PopulateFileEntries(string preferredSelectionId)
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
                            SelectionKind = KnowledgeBaseNetworkSelectionKind.FileReference,
                            ItemId = entry.NetworkAssetId
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
            _lblFilesEmptyState.Text = _currentState.SupportsEditing
                ? "Для этого узла пока нет файлов сети."
                : _currentState.EmptyStateText;
            ResizeFilesColumns();
        }

        private void HandlePassportFilterChanged()
        {
            RefreshPassportEntries(SelectedDeviceId, SelectedInterfaceId, SelectedConnectionId);
            ResolvePassportSelection();
            UpdateButtonStates();
        }

        private bool HasPassportFilter => !string.IsNullOrWhiteSpace(GetPassportFilterText());

        private string GetPassportFilterText() => _txtPassportFilter.Text.Trim();

        private void UpdatePassportFilterStatus(
            int visibleDeviceCount,
            int visibleInterfaceCount,
            int visibleConnectionCount)
        {
            _btnClearPassportFilter.Enabled = HasPassportFilter;
            _lblPassportFilterStatus.Text = HasPassportFilter
                ? $"Найдено: {visibleDeviceCount}/{_currentState.DeviceStates.Count} | {visibleInterfaceCount}/{_currentState.InterfaceStates.Count} | {visibleConnectionCount}/{_currentState.ConnectionStates.Count}"
                : string.Empty;
        }

        private static bool DeviceMatchesFilter(KnowledgeBaseNetworkDeviceState entry, string filterText) =>
            MatchesFilter(
                filterText,
                entry.NameText,
                entry.RoleText,
                entry.VendorText,
                entry.ModelText,
                entry.OrderNumberText,
                entry.SerialNumberText,
                entry.FirmwareText,
                entry.ProfinetNameText,
                entry.MacAddressText,
                entry.LocationText,
                entry.CabinetText,
                entry.LinkedNodeText,
                entry.NotesText);

        private static bool InterfaceMatchesFilter(KnowledgeBaseNetworkInterfaceState entry, string filterText) =>
            MatchesFilter(
                filterText,
                entry.DeviceNameText,
                entry.DeviceRoleText,
                entry.InterfaceNameText,
                entry.PortNumberText,
                entry.MacAddressText,
                entry.IpAddressText,
                entry.SubnetMaskText,
                entry.GatewayText,
                entry.VlanText,
                entry.ProtocolText,
                entry.MpiDpPnAddressText,
                entry.SpeedText,
                entry.MediumText,
                entry.NotesText);

        private static bool ConnectionMatchesFilter(KnowledgeBaseNetworkConnectionState entry, string filterText) =>
            MatchesFilter(
                filterText,
                entry.EndpointAText,
                entry.EndpointBText,
                entry.CableLabelText,
                entry.CableTypeText,
                entry.ProtocolText,
                entry.MediumText,
                entry.LengthText,
                entry.RouteText,
                entry.StatusText,
                entry.NotesText);

        private static bool MatchesFilter(string filterText, params string[] values)
        {
            if (string.IsNullOrWhiteSpace(filterText))
                return true;

            foreach (string value in values)
            {
                if (string.IsNullOrWhiteSpace(value) ||
                    string.Equals(value, "-", StringComparison.Ordinal))
                {
                    continue;
                }

                if (value.Contains(filterText, StringComparison.CurrentCultureIgnoreCase))
                    return true;
            }

            return false;
        }

        private void EnsureFileSelection()
        {
            if (_lvFiles.SelectedItems.Count > 0 || _lvFiles.Items.Count == 0)
                return;

            _lvFiles.Items[0].Selected = true;
        }

        private void WirePassportSelectionEvents(ListView source, params ListView[] others)
        {
            source.SelectedIndexChanged += (_, _) => HandlePassportSelectionChanged(source, others);
            source.ItemActivate += (_, _) =>
            {
                if (ReferenceEquals(source, _lvDevices))
                    EditDeviceRequested?.Invoke(this, EventArgs.Empty);
                else if (ReferenceEquals(source, _lvInterfaces))
                    EditInterfaceRequested?.Invoke(this, EventArgs.Empty);
                else if (ReferenceEquals(source, _lvConnections))
                    EditConnectionRequested?.Invoke(this, EventArgs.Empty);
            };
        }

        private void HandlePassportSelectionChanged(ListView source, IReadOnlyList<ListView> others)
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

                ResolvePassportSelection();
                UpdateButtonStates();
            }
            finally
            {
                _isSynchronizingSelection = false;
            }
        }

        private void HandleFileSelectionChanged()
        {
            if (_isSynchronizingSelection)
                return;

            _isSynchronizingSelection = true;
            try
            {
                SelectedItemId = ResolveSelectedFileReferenceId();
                UpdateButtonStates();
                UpdatePreview();
            }
            finally
            {
                _isSynchronizingSelection = false;
            }
        }

        private void ResolvePassportSelection()
        {
            SelectedDeviceId = TryGetSelectedTag(_lvDevices, out var deviceTag)
                ? deviceTag.ItemId
                : string.Empty;
            SelectedInterfaceId = TryGetSelectedTag(_lvInterfaces, out var interfaceTag)
                ? interfaceTag.ItemId
                : string.Empty;
            SelectedConnectionId = TryGetSelectedTag(_lvConnections, out var connectionTag)
                ? connectionTag.ItemId
                : string.Empty;
        }

        private string ResolveSelectedFileReferenceId()
        {
            if (_lvFiles.SelectedItems.Count > 0 && _lvFiles.SelectedItems[0].Tag is ListItemTag tag)
                return tag.ItemId;

            return string.Empty;
        }

        private KnowledgeBaseNetworkFileReferenceState? FindSelectedFileReferenceState()
        {
            if (string.IsNullOrWhiteSpace(SelectedItemId))
                return null;

            return _currentState.FileReferenceStates.FirstOrDefault(entry =>
                string.Equals(entry.NetworkAssetId, SelectedItemId, StringComparison.Ordinal));
        }

        private void UpdateButtonStates()
        {
            bool canFileEdit = _currentState.SupportsEditing;
            bool canPassportEdit = _currentState.SupportsEditing && _currentState.SupportsPassportEditing;
            var selectedFileState = FindSelectedFileReferenceState();
            bool hasFileSelection = selectedFileState != null;
            bool hasFilePath = hasFileSelection && !string.Equals(selectedFileState!.PathText, "-", StringComparison.Ordinal);

            _btnAddDevice.Enabled = canPassportEdit;
            _btnEditDevice.Enabled = canPassportEdit && !string.IsNullOrWhiteSpace(SelectedDeviceId);
            _btnDeleteDevice.Enabled = canPassportEdit && !string.IsNullOrWhiteSpace(SelectedDeviceId);
            _btnAddInterface.Enabled = canPassportEdit && _currentState.DeviceCount > 0;
            _btnEditInterface.Enabled = canPassportEdit && !string.IsNullOrWhiteSpace(SelectedInterfaceId);
            _btnDeleteInterface.Enabled = canPassportEdit && !string.IsNullOrWhiteSpace(SelectedInterfaceId);
            _btnAddConnection.Enabled = canPassportEdit && _currentState.InterfaceCount >= 2;
            _btnEditConnection.Enabled = canPassportEdit && !string.IsNullOrWhiteSpace(SelectedConnectionId);
            _btnDeleteConnection.Enabled = canPassportEdit && !string.IsNullOrWhiteSpace(SelectedConnectionId);

            _btnAdd.Enabled = canFileEdit;
            _btnOpenSelected.Enabled = hasFilePath;
            _btnEditSelected.Enabled = canFileEdit && hasFileSelection;
            _btnDeleteSelected.Enabled = canFileEdit && hasFileSelection;
        }

        private void UpdatePreview()
        {
            var selectedState = FindSelectedFileReferenceState();
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

        private static GroupBox CreatePassportGroup(
            string title,
            Control actionsPanel,
            ListView listView,
            Label emptyStateLabel)
        {
            var groupBox = new GroupBox
            {
                Text = title,
                Dock = DockStyle.Fill,
                Padding = new Padding(10),
                Margin = new Padding(0, 0, 0, 10)
            };

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            var container = new Panel
            {
                Dock = DockStyle.Fill
            };
            container.Controls.Add(listView);
            container.Controls.Add(emptyStateLabel);

            layout.Controls.Add(actionsPanel, 0, 0);
            layout.Controls.Add(container, 0, 1);
            groupBox.Controls.Add(layout);
            return groupBox;
        }

        private static FlowLayoutPanel CreateActionsPanel() =>
            new()
            {
                Dock = DockStyle.Top,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = true,
                AutoSize = true,
                Margin = new Padding(0, 0, 0, 8)
            };

        private static ListView CreateDevicesListView()
        {
            var listView = CreateBaseListView();
            listView.Columns.Add("Устройство", 180);
            listView.Columns.Add("Роль", 120);
            listView.Columns.Add("Модель", 170);
            listView.Columns.Add("PROFINET-name", 150);
            listView.Columns.Add("MAC", 145);
            listView.Columns.Add("Карточка", 150);
            listView.Columns.Add("Инт.", 55);
            listView.Columns.Add("Связи", 55);
            return listView;
        }

        private static ListView CreateInterfacesListView()
        {
            var listView = CreateBaseListView();
            listView.Columns.Add("Устройство", 160);
            listView.Columns.Add("Интерфейс", 130);
            listView.Columns.Add("IP", 120);
            listView.Columns.Add("Маска", 120);
            listView.Columns.Add("Шлюз", 120);
            listView.Columns.Add("Протокол", 110);
            listView.Columns.Add("VLAN", 70);
            listView.Columns.Add("MAC", 145);
            return listView;
        }

        private static ListView CreateConnectionsListView()
        {
            var listView = CreateBaseListView();
            listView.Columns.Add("Интерфейс A", 230);
            listView.Columns.Add("Интерфейс B", 230);
            listView.Columns.Add("Кабель", 120);
            listView.Columns.Add("Тип", 110);
            listView.Columns.Add("Протокол", 110);
            listView.Columns.Add("Среда", 100);
            listView.Columns.Add("Трасса / место", 160);
            listView.Columns.Add("Статус", 100);
            listView.Columns.Add("Примечание", 220);
            return listView;
        }

        private static ListView CreateFilesListView()
        {
            var listView = CreateBaseListView();
            listView.Columns.Add("Наименование", 220);
            listView.Columns.Add("Предпросмотр", 170);
            listView.Columns.Add("Путь", 360);
            return listView;
        }

        private static ListView CreateBaseListView() =>
            new()
            {
                Dock = DockStyle.Fill,
                FullRowSelect = true,
                GridLines = true,
                HeaderStyle = ColumnHeaderStyle.Nonclickable,
                MultiSelect = false,
                View = View.Details
            };

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
