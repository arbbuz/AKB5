using System.Drawing.Drawing2D;
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
        private static readonly Color ReviewWarningBackColor = Color.FromArgb(255, 248, 225);
        private static readonly Color NetworkSurfaceColor = Color.White;
        private static readonly Color NetworkPanelColor = Color.FromArgb(251, 253, 254);
        private static readonly Color NetworkHairlineColor = Color.FromArgb(54, 119, 138, 156);
        private static readonly Color NetworkMutedTextColor = Color.FromArgb(99, 113, 129);

        private sealed class ListItemTag
        {
            public KnowledgeBaseNetworkSelectionKind SelectionKind { get; init; }

            public string ItemId { get; init; } = string.Empty;
        }

        private sealed class NetworkTopologyCanvas : Control
        {
            private readonly Pen _connectionPen = new(Color.FromArgb(28, 118, 70), 2F);
            private readonly Pen _objectPen = new(Color.FromArgb(125, 145, 165), 1F);
            private readonly SolidBrush _deviceBrush = new(Color.FromArgb(238, 247, 242));
            private readonly SolidBrush _objectBrush = new(Color.FromArgb(246, 248, 250));
            private readonly SolidBrush _switchBrush = new(Color.FromArgb(232, 241, 252));
            private readonly SolidBrush _warningBrush = new(Color.FromArgb(255, 248, 229));
            private readonly StringFormat _centerFormat = new()
            {
                Alignment = StringAlignment.Center,
                LineAlignment = StringAlignment.Center,
                Trimming = StringTrimming.EllipsisCharacter
            };

            private KnowledgeBaseNetworkState _state = new();

            public NetworkTopologyCanvas()
            {
                DoubleBuffered = true;
                BackColor = NetworkSurfaceColor;
                Dock = DockStyle.Fill;
                MinimumSize = new Size(320, 220);
            }

            protected override void Dispose(bool disposing)
            {
                if (disposing)
                {
                    _connectionPen.Dispose();
                    _objectPen.Dispose();
                    _deviceBrush.Dispose();
                    _objectBrush.Dispose();
                    _switchBrush.Dispose();
                    _warningBrush.Dispose();
                    _centerFormat.Dispose();
                }

                base.Dispose(disposing);
            }

            public void ApplyState(KnowledgeBaseNetworkState state)
            {
                _state = state ?? new KnowledgeBaseNetworkState();
                Invalidate();
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                base.OnPaint(e);
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                e.Graphics.Clear(NetworkSurfaceColor);

                var bounds = ClientRectangle;
                if (bounds.Width < 20 || bounds.Height < 20)
                    return;

                if (_state.DeviceStates.Count > 0)
                {
                    DrawDeviceTopology(e.Graphics, bounds);
                    return;
                }

                if (_state.ObjectStates.Count > 0)
                {
                    DrawObjectOverview(e.Graphics, bounds);
                    return;
                }

                DrawCenteredText(e.Graphics, "Для выбранного объекта пока нет данных для обзора сети.", bounds);
            }

            private void DrawDeviceTopology(Graphics graphics, Rectangle bounds)
            {
                var devices = _state.DeviceStates.Take(24).ToList();
                var rectangles = ArrangeItems(bounds, devices.Count);
                var rectanglesByDeviceId = devices
                    .Select((device, index) => new { device.NetworkDeviceId, Rectangle = rectangles[index] })
                    .Where(item => !string.IsNullOrWhiteSpace(item.NetworkDeviceId))
                    .ToDictionary(item => item.NetworkDeviceId, item => item.Rectangle, StringComparer.Ordinal);
                var interfacesById = _state.InterfaceStates
                    .Where(item => !string.IsNullOrWhiteSpace(item.NetworkInterfaceId))
                    .ToDictionary(item => item.NetworkInterfaceId, item => item, StringComparer.Ordinal);

                using var lineCap = new AdjustableArrowCap(4, 5);
                _connectionPen.CustomEndCap = lineCap;
                foreach (var connection in _state.ConnectionStates)
                {
                    if (!interfacesById.TryGetValue(connection.EndpointAInterfaceId, out var endpointA) ||
                        !interfacesById.TryGetValue(connection.EndpointBInterfaceId, out var endpointB) ||
                        !rectanglesByDeviceId.TryGetValue(endpointA.NetworkDeviceId, out var rectA) ||
                        !rectanglesByDeviceId.TryGetValue(endpointB.NetworkDeviceId, out var rectB))
                    {
                        continue;
                    }

                    graphics.DrawLine(_connectionPen, CenterOf(rectA), CenterOf(rectB));
                }

                foreach (var (device, index) in devices.Select((device, index) => (device, index)))
                {
                    var rectangle = rectangles[index];
                    bool hasWarning = !string.IsNullOrWhiteSpace(device.WarningText) &&
                        !string.Equals(device.WarningText, "-", StringComparison.Ordinal);
                    DrawNode(
                        graphics,
                        rectangle,
                        device.NameText,
                        BuildDeviceSubtitle(device),
                        IsSwitchRole(device.RoleText) ? _switchBrush : hasWarning ? _warningBrush : _deviceBrush);
                }

                if (_state.DeviceStates.Count > devices.Count)
                    DrawFooter(graphics, bounds, $"Показаны первые {devices.Count} устройств из {_state.DeviceStates.Count}.");
            }

            private void DrawObjectOverview(Graphics graphics, Rectangle bounds)
            {
                var objects = _state.ObjectStates.Take(24).ToList();
                var rectangles = ArrangeItems(bounds, objects.Count);
                var rectanglesByNodeId = objects
                    .Select((item, index) => new { item.NodeId, Rectangle = rectangles[index] })
                    .Where(item => !string.IsNullOrWhiteSpace(item.NodeId))
                    .ToDictionary(item => item.NodeId, item => item.Rectangle, StringComparer.Ordinal);

                using var objectLinePen = new Pen(Color.FromArgb(175, 185, 195), 1F);
                foreach (var item in objects)
                {
                    if (string.IsNullOrWhiteSpace(item.ParentNodeId) ||
                        !rectanglesByNodeId.TryGetValue(item.ParentNodeId, out var parentRect) ||
                        !rectanglesByNodeId.TryGetValue(item.NodeId, out var childRect))
                    {
                        continue;
                    }

                    graphics.DrawLine(objectLinePen, CenterOf(parentRect), CenterOf(childRect));
                }

                foreach (var (item, index) in objects.Select((item, index) => (item, index)))
                {
                    DrawNode(
                        graphics,
                        rectangles[index],
                        item.NameText,
                        item.ChildCount > 0 ? $"{item.TypeText} | объектов: {item.ChildCount}" : item.TypeText,
                        _objectBrush);
                }

                if (_state.ObjectStates.Count > objects.Count)
                    DrawFooter(graphics, bounds, $"Показаны первые {objects.Count} объектов из {_state.ObjectStates.Count}.");
            }

            private static List<Rectangle> ArrangeItems(Rectangle bounds, int count)
            {
                var rectangles = new List<Rectangle>(count);
                if (count == 0)
                    return rectangles;

                int columns = Math.Max(1, (int)Math.Ceiling(Math.Sqrt(count)));
                int rows = Math.Max(1, (int)Math.Ceiling(count / (double)columns));
                int horizontalGap = 24;
                int verticalGap = 26;
                int availableWidth = Math.Max(120, bounds.Width - 48);
                int availableHeight = Math.Max(100, bounds.Height - 58);
                int itemWidth = Math.Clamp((availableWidth - horizontalGap * (columns - 1)) / columns, 130, 210);
                int itemHeight = 64;
                int gridWidth = itemWidth * columns + horizontalGap * (columns - 1);
                int gridHeight = itemHeight * rows + verticalGap * (rows - 1);
                int startX = bounds.Left + Math.Max(16, (bounds.Width - gridWidth) / 2);
                int startY = bounds.Top + Math.Max(16, (availableHeight - gridHeight) / 2);

                for (int index = 0; index < count; index++)
                {
                    int row = index / columns;
                    int column = index % columns;
                    rectangles.Add(new Rectangle(
                        startX + column * (itemWidth + horizontalGap),
                        startY + row * (itemHeight + verticalGap),
                        itemWidth,
                        itemHeight));
                }

                return rectangles;
            }

            private static Point CenterOf(Rectangle rectangle) =>
                new(rectangle.Left + rectangle.Width / 2, rectangle.Top + rectangle.Height / 2);

            private static string BuildDeviceSubtitle(KnowledgeBaseNetworkDeviceState device)
            {
                var parts = new[]
                {
                    device.RoleText,
                    device.ProfinetNameText,
                    device.LinkedNodeText
                }.Where(CanDrawText).Take(2);
                return string.Join(" | ", parts);
            }

            private static bool CanDrawText(string? text) =>
                !string.IsNullOrWhiteSpace(text) && !string.Equals(text, "-", StringComparison.Ordinal);

            private static bool IsSwitchRole(string roleText) =>
                roleText.Contains("switch", StringComparison.OrdinalIgnoreCase) ||
                roleText.Contains("коммут", StringComparison.CurrentCultureIgnoreCase);

            private void DrawNode(
                Graphics graphics,
                Rectangle rectangle,
                string title,
                string subtitle,
                Brush fillBrush)
            {
                using var path = RoundedRectangle(rectangle, 8);
                graphics.FillPath(fillBrush, path);
                graphics.DrawPath(_objectPen, path);

                var titleBounds = new Rectangle(rectangle.Left + 8, rectangle.Top + 8, rectangle.Width - 16, 24);
                var subtitleBounds = new Rectangle(rectangle.Left + 8, rectangle.Top + 34, rectangle.Width - 16, 20);
                using var titleFont = new Font(Font, FontStyle.Bold);
                graphics.DrawString(title, titleFont, Brushes.Black, titleBounds, _centerFormat);
                graphics.DrawString(subtitle, Font, Brushes.DimGray, subtitleBounds, _centerFormat);
            }

            private void DrawCenteredText(Graphics graphics, string text, Rectangle bounds) =>
                graphics.DrawString(text, Font, Brushes.DimGray, bounds, _centerFormat);

            private static void DrawFooter(Graphics graphics, Rectangle bounds, string text)
            {
                var footerBounds = new Rectangle(bounds.Left + 8, bounds.Bottom - 26, bounds.Width - 16, 20);
                using var format = new StringFormat
                {
                    Alignment = StringAlignment.Center,
                    LineAlignment = StringAlignment.Center
                };
                graphics.DrawString(text, SystemFonts.MessageBoxFont, Brushes.DimGray, footerBounds, format);
            }

            private static GraphicsPath RoundedRectangle(Rectangle bounds, int radius)
            {
                int diameter = radius * 2;
                var path = new GraphicsPath();
                path.AddArc(bounds.Left, bounds.Top, diameter, diameter, 180, 90);
                path.AddArc(bounds.Right - diameter, bounds.Top, diameter, diameter, 270, 90);
                path.AddArc(bounds.Right - diameter, bounds.Bottom - diameter, diameter, diameter, 0, 90);
                path.AddArc(bounds.Left, bounds.Bottom - diameter, diameter, diameter, 90, 90);
                path.CloseFigure();
                return path;
            }
        }

        private readonly KnowledgeBaseNetworkState _emptyState = new();

        private Label _lblSource = null!;
        private Label _lblSummary = null!;
        private Button _btnAddDevice = null!;
        private Button _btnAddSimilarDevice = null!;
        private Button _btnEditDevice = null!;
        private Button _btnDeleteDevice = null!;
        private Button _btnCopyDevice = null!;
        private Button _btnAddInterface = null!;
        private Button _btnAddSimilarInterface = null!;
        private Button _btnEditInterface = null!;
        private Button _btnDeleteInterface = null!;
        private Button _btnCopyInterface = null!;
        private Button _btnAddConnection = null!;
        private Button _btnAddSimilarConnection = null!;
        private Button _btnEditConnection = null!;
        private Button _btnDeleteConnection = null!;
        private Button _btnCopyConnection = null!;
        private TextBox? _txtPassportFilter;
        private Button? _btnClearPassportFilter;
        private Button? _btnCopyVisiblePassport;
        private Label? _lblPassportFilterStatus;
        private Button _btnAdd = null!;
        private Button _btnOpenSelected = null!;
        private Button _btnEditSelected = null!;
        private Button _btnDeleteSelected = null!;
        private TabControl _contentTabs = null!;
        private TabPage _overviewPage = null!;
        private TabPage _passportPage = null!;
        private TabPage _filesPage = null!;
        private TabPage _previewPage = null!;
        private ListView _lvOverviewObjects = null!;
        private NetworkTopologyCanvas _topologyCanvas = null!;
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
        private TextBox _txtPreviewSourceNote = null!;
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
            BackColor = NetworkSurfaceColor;

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                BackColor = NetworkSurfaceColor,
                Padding = new Padding(12),
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
                ForeColor = NetworkMutedTextColor,
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

        public event EventHandler? AddSimilarDeviceRequested;

        public event EventHandler? EditDeviceRequested;

        public event EventHandler? DeleteDeviceRequested;

        public event EventHandler? AddInterfaceRequested;

        public event EventHandler? AddSimilarInterfaceRequested;

        public event EventHandler? EditInterfaceRequested;

        public event EventHandler? DeleteInterfaceRequested;

        public event EventHandler? AddConnectionRequested;

        public event EventHandler? AddSimilarConnectionRequested;

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

            RefreshOverviewEntries();
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
                Dock = DockStyle.Fill,
                HotTrack = true
            };

            _overviewPage = CreateContentTabPage("Обзор");
            _overviewPage.Controls.Add(CreateOverviewPageLayout());

            _passportPage = CreateContentTabPage("Паспорт");
            _passportPage.Controls.Add(CreatePassportPageLayout());

            _filesPage = CreateContentTabPage("Файлы");
            _filesPage.Controls.Add(CreateFilesPageLayout());

            _previewPage = CreateContentTabPage("Предпросмотр");
            _previewPage.Controls.Add(CreatePreviewPageLayout());

            _contentTabs.TabPages.Add(_overviewPage);
            _contentTabs.TabPages.Add(_passportPage);
            _contentTabs.TabPages.Add(_filesPage);
            _contentTabs.TabPages.Add(_previewPage);
            return _contentTabs;
        }

        private static TabPage CreateContentTabPage(string text) =>
            new(text)
            {
                BackColor = NetworkSurfaceColor,
                UseVisualStyleBackColor = false
            };

        private Control CreateOverviewPageLayout()
        {
            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                BackColor = NetworkSurfaceColor,
                Padding = new Padding(12),
                ColumnCount = 2,
                RowCount = 1
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 330F));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            _lvOverviewObjects = CreateOverviewObjectsListView();
            _topologyCanvas = new NetworkTopologyCanvas();

            layout.Controls.Add(CreateOverviewGroup("Объекты / устройства", _lvOverviewObjects), 0, 0);
            layout.Controls.Add(CreateOverviewGroup("Топология", _topologyCanvas), 1, 0);
            return layout;
        }

        private Control CreatePassportPageLayout()
        {
            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                BackColor = NetworkSurfaceColor,
                Padding = new Padding(12),
                ColumnCount = 1,
                RowCount = 3
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 34F));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 33F));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 33F));

            _lvDevices = CreateDevicesListView();
            _lvInterfaces = CreateInterfacesListView();
            _lvConnections = CreateConnectionsListView();
            ConfigureDeviceCopyActions();
            ConfigureInterfaceCopyActions();
            ConfigureConnectionCopyActions();

            _lblDevicesEmptyState = CreateEmptyStateLabel("Устройства сетевого паспорта пока не добавлены.");
            _lblInterfacesEmptyState = CreateEmptyStateLabel("Интерфейсы и IP-адреса пока не добавлены.");
            _lblConnectionsEmptyState = CreateEmptyStateLabel("Соединения между интерфейсами пока не добавлены.");

            WirePassportSelectionEvents(_lvDevices, _lvInterfaces, _lvConnections);
            WirePassportSelectionEvents(_lvInterfaces, _lvDevices, _lvConnections);
            WirePassportSelectionEvents(_lvConnections, _lvDevices, _lvInterfaces);

            layout.Controls.Add(
                CreatePassportGroup(
                    "Устройства",
                    CreateDeviceActionsPanel(),
                    _lvDevices,
                    _lblDevicesEmptyState),
                0,
                0);
            layout.Controls.Add(
                CreatePassportGroup(
                    "Интерфейсы / IP",
                    CreateInterfaceActionsPanel(),
                    _lvInterfaces,
                    _lblInterfacesEmptyState),
                0,
                1);
            layout.Controls.Add(
                CreatePassportGroup(
                    "Соединения",
                    CreateConnectionActionsPanel(),
                    _lvConnections,
                    _lblConnectionsEmptyState),
                0,
                2);

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

            _btnCopyVisiblePassport = CreateActionButton("Копировать видимое");
            _btnCopyVisiblePassport.Click += (_, _) => CopyVisiblePassportRows();

            var filterActions = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoSize = true,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                Margin = new Padding(0)
            };
            filterActions.Controls.Add(_btnClearPassportFilter);
            filterActions.Controls.Add(_btnCopyVisiblePassport);
            layout.Controls.Add(filterActions, 2, 0);

            _lblPassportFilterStatus = new Label
            {
                Dock = DockStyle.Fill,
                AutoEllipsis = true,
                ForeColor = NetworkMutedTextColor,
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
                BackColor = NetworkSurfaceColor,
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
                BackColor = NetworkSurfaceColor,
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

            var listHost = new ModernNetworkBorderPanel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(1)
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
            _btnAddSimilarDevice = CreateActionButton("Добавить похожее");
            _btnAddSimilarDevice.Click += (_, _) => AddSimilarDeviceRequested?.Invoke(this, EventArgs.Empty);
            _btnEditDevice = CreateActionButton("Изменить");
            _btnEditDevice.Click += (_, _) => EditDeviceRequested?.Invoke(this, EventArgs.Empty);
            _btnCopyDevice = CreateActionButton("Копировать строку");
            _btnCopyDevice.Click += (_, _) => CopySelectedDeviceRow();
            _btnDeleteDevice = CreateActionButton("Удалить");
            _btnDeleteDevice.Click += (_, _) => DeleteDeviceRequested?.Invoke(this, EventArgs.Empty);
            panel.Controls.Add(_btnAddDevice);
            panel.Controls.Add(_btnAddSimilarDevice);
            panel.Controls.Add(_btnEditDevice);
            panel.Controls.Add(_btnCopyDevice);
            panel.Controls.Add(_btnDeleteDevice);
            return panel;
        }

        private FlowLayoutPanel CreateInterfaceActionsPanel()
        {
            var panel = CreateActionsPanel();
            _btnAddInterface = CreateActionButton("Добавить интерфейс");
            _btnAddInterface.Click += (_, _) => AddInterfaceRequested?.Invoke(this, EventArgs.Empty);
            _btnAddSimilarInterface = CreateActionButton("Добавить похожий");
            _btnAddSimilarInterface.Click += (_, _) => AddSimilarInterfaceRequested?.Invoke(this, EventArgs.Empty);
            _btnEditInterface = CreateActionButton("Изменить");
            _btnEditInterface.Click += (_, _) => EditInterfaceRequested?.Invoke(this, EventArgs.Empty);
            _btnCopyInterface = CreateActionButton("Копировать строку");
            _btnCopyInterface.Click += (_, _) => CopySelectedInterfaceRow();
            _btnDeleteInterface = CreateActionButton("Удалить");
            _btnDeleteInterface.Click += (_, _) => DeleteInterfaceRequested?.Invoke(this, EventArgs.Empty);
            panel.Controls.Add(_btnAddInterface);
            panel.Controls.Add(_btnAddSimilarInterface);
            panel.Controls.Add(_btnEditInterface);
            panel.Controls.Add(_btnCopyInterface);
            panel.Controls.Add(_btnDeleteInterface);
            return panel;
        }

        private FlowLayoutPanel CreateConnectionActionsPanel()
        {
            var panel = CreateActionsPanel();
            _btnAddConnection = CreateActionButton("Добавить соединение");
            _btnAddConnection.Click += (_, _) => AddConnectionRequested?.Invoke(this, EventArgs.Empty);
            _btnAddSimilarConnection = CreateActionButton("Добавить похожее");
            _btnAddSimilarConnection.Click += (_, _) => AddSimilarConnectionRequested?.Invoke(this, EventArgs.Empty);
            _btnEditConnection = CreateActionButton("Изменить");
            _btnEditConnection.Click += (_, _) => EditConnectionRequested?.Invoke(this, EventArgs.Empty);
            _btnCopyConnection = CreateActionButton("Копировать строку");
            _btnCopyConnection.Click += (_, _) => CopySelectedConnectionRow();
            _btnDeleteConnection = CreateActionButton("Удалить");
            _btnDeleteConnection.Click += (_, _) => DeleteConnectionRequested?.Invoke(this, EventArgs.Empty);
            panel.Controls.Add(_btnAddConnection);
            panel.Controls.Add(_btnAddSimilarConnection);
            panel.Controls.Add(_btnEditConnection);
            panel.Controls.Add(_btnCopyConnection);
            panel.Controls.Add(_btnDeleteConnection);
            return panel;
        }

        private Control CreatePreviewPageLayout()
        {
            var host = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = NetworkSurfaceColor,
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
                RowCount = 6
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 135F));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
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
                BackColor = NetworkSurfaceColor,
                Multiline = true,
                Height = 44,
                ScrollBars = ScrollBars.Vertical,
                TabStop = false
            };
            layout.Controls.Add(CreateTextFieldFrame(_txtPreviewPath, multiline: true), 1, 1);

            layout.Controls.Add(CreateValueLabel("Тип предпросмотра"), 0, 2);
            _lblPreviewKindValue = CreateReadOnlyValueLabel();
            layout.Controls.Add(_lblPreviewKindValue, 1, 2);

            layout.Controls.Add(CreateValueLabel("Фрагмент / комментарий"), 0, 3);
            _txtPreviewSourceNote = new TextBox
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                BackColor = NetworkSurfaceColor,
                Multiline = true,
                Height = 44,
                ScrollBars = ScrollBars.Vertical,
                TabStop = false
            };
            layout.Controls.Add(CreateTextFieldFrame(_txtPreviewSourceNote, multiline: true), 1, 3);

            _lblPreviewStatus = new Label
            {
                Dock = DockStyle.Fill,
                AutoSize = true,
                ForeColor = NetworkMutedTextColor,
                Margin = new Padding(0, 6, 0, 10)
            };
            layout.Controls.Add(_lblPreviewStatus, 0, 4);
            layout.SetColumnSpan(_lblPreviewStatus, 2);

            var previewHost = new ModernNetworkBorderPanel
            {
                Dock = DockStyle.Fill,
                BackColor = NetworkSurfaceColor,
                Padding = new Padding(1)
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

            layout.Controls.Add(previewHost, 0, 5);
            layout.SetColumnSpan(previewHost, 2);

            return layout;
        }

        private void RefreshOverviewEntries()
        {
            _topologyCanvas.ApplyState(_currentState);
            PopulateOverviewObjects();
        }

        private void PopulateOverviewObjects()
        {
            _lvOverviewObjects.BeginUpdate();
            try
            {
                _lvOverviewObjects.Items.Clear();
                if (_currentState.DeviceStates.Count > 0)
                {
                    foreach (var device in _currentState.DeviceStates)
                    {
                        var firstInterface = _currentState.InterfaceStates.FirstOrDefault(item =>
                            string.Equals(item.NetworkDeviceId, device.NetworkDeviceId, StringComparison.Ordinal));
                        _lvOverviewObjects.Items.Add(new ListViewItem(
                        [
                            device.NameText,
                            device.RoleText,
                            firstInterface?.IpAddressText ?? "-",
                            device.ProfinetNameText,
                            device.LinkedNodeText
                        ]));
                    }

                    return;
                }

                foreach (var item in _currentState.ObjectStates)
                {
                    _lvOverviewObjects.Items.Add(new ListViewItem(
                    [
                        item.NameText,
                        item.TypeText,
                        "-",
                        "-",
                        item.ParentText
                    ]));
                }
            }
            finally
            {
                _lvOverviewObjects.EndUpdate();
            }
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
                        entry.VendorText,
                        entry.ModelText,
                        entry.ProfinetNameText,
                        entry.MacAddressText,
                        entry.LocationText,
                        entry.LinkedNodeText,
                        entry.WarningText,
                        entry.InterfacesCount.ToString(),
                        entry.ConnectionsCount.ToString()
                    ])
                    {
                        ToolTipText = BuildDeviceRowText(entry),
                        Tag = new ListItemTag
                        {
                            SelectionKind = KnowledgeBaseNetworkSelectionKind.Device,
                            ItemId = entry.NetworkDeviceId
                        }
                    };
                    ApplyReviewWarningStyle(item, entry.WarningText);

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
                        entry.MpiDpPnAddressText,
                        entry.MediumText,
                        entry.SpeedText,
                        entry.VlanText,
                        entry.MacAddressText,
                        entry.WarningText,
                        entry.NotesText
                    ])
                    {
                        ToolTipText = BuildInterfaceRowText(entry),
                        Tag = new ListItemTag
                        {
                            SelectionKind = KnowledgeBaseNetworkSelectionKind.Interface,
                            ItemId = entry.NetworkInterfaceId
                        }
                    };
                    ApplyReviewWarningStyle(item, entry.WarningText);

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
                        entry.LengthText,
                        entry.RouteText,
                        entry.StatusText,
                        entry.WarningText,
                        entry.NotesText
                    ])
                    {
                        ToolTipText = BuildConnectionRowText(entry),
                        Tag = new ListItemTag
                        {
                            SelectionKind = KnowledgeBaseNetworkSelectionKind.Connection,
                            ItemId = entry.NetworkConnectionId
                        }
                    };
                    ApplyReviewWarningStyle(item, entry.WarningText);

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
                        entry.SourceNoteText,
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

        private string GetPassportFilterText() => _txtPassportFilter?.Text.Trim() ?? string.Empty;

        private void UpdatePassportFilterStatus(
            int visibleDeviceCount,
            int visibleInterfaceCount,
            int visibleConnectionCount)
        {
            if (_btnClearPassportFilter != null)
                _btnClearPassportFilter.Enabled = HasPassportFilter;
            if (_btnCopyVisiblePassport != null)
                _btnCopyVisiblePassport.Enabled = visibleDeviceCount + visibleInterfaceCount + visibleConnectionCount > 0;
            if (_lblPassportFilterStatus != null)
            {
                _lblPassportFilterStatus.Text = HasPassportFilter
                    ? $"Найдено: {visibleDeviceCount}/{_currentState.DeviceStates.Count} | {visibleInterfaceCount}/{_currentState.InterfaceStates.Count} | {visibleConnectionCount}/{_currentState.ConnectionStates.Count}"
                    : string.Empty;
            }
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
                entry.WarningText,
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
                entry.WarningText,
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
                entry.WarningText,
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

        private static void ApplyReviewWarningStyle(ListViewItem item, string warningText)
        {
            if (CanCopyText(warningText))
                item.BackColor = ReviewWarningBackColor;
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

        private void ConfigureDeviceCopyActions()
        {
            _lvDevices.KeyDown += (_, e) =>
            {
                if (e.Control && e.KeyCode == Keys.C)
                {
                    e.SuppressKeyPress = true;
                    CopySelectedDeviceRow();
                }
            };

            _lvDevices.MouseDown += (_, e) => SelectListViewItemOnRightClick(_lvDevices, e);

            var copyRow = new ToolStripMenuItem("Копировать строку", null, (_, _) => CopySelectedDeviceRow());
            var copyVisibleRows = new ToolStripMenuItem("Копировать видимые строки", null, (_, _) => CopyVisibleDeviceRows());
            var copyDevice = new ToolStripMenuItem("Копировать устройство", null, (_, _) => CopySelectedDeviceSummary());
            var copyProfinetName = new ToolStripMenuItem("Копировать PROFINET-name", null, (_, _) => CopySelectedDeviceProfinetName());
            var copyMac = new ToolStripMenuItem("Копировать MAC", null, (_, _) => CopySelectedDeviceMac());
            var addSimilar = new ToolStripMenuItem("Добавить похожее", null, (_, _) => AddSimilarDeviceRequested?.Invoke(this, EventArgs.Empty));
            var menu = new ContextMenuStrip();
            menu.Opening += (_, _) =>
            {
                var selectedDevice = FindSelectedDeviceState();
                bool hasDevice = selectedDevice != null;
                addSimilar.Enabled = hasDevice && _currentState.SupportsEditing && _currentState.SupportsPassportEditing;
                copyRow.Enabled = hasDevice;
                copyVisibleRows.Enabled = _lvDevices.Items.Count > 0;
                copyDevice.Enabled = hasDevice && CanCopyText(BuildDeviceSummaryText(selectedDevice!));
                copyProfinetName.Enabled = hasDevice && CanCopyText(selectedDevice!.ProfinetNameText);
                copyMac.Enabled = hasDevice && CanCopyText(selectedDevice!.MacAddressText);
            };
            menu.Items.Add(addSimilar);
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add(copyRow);
            menu.Items.Add(copyVisibleRows);
            menu.Items.Add(copyDevice);
            menu.Items.Add(copyProfinetName);
            menu.Items.Add(copyMac);
            _lvDevices.ContextMenuStrip = menu;
        }

        private void ConfigureInterfaceCopyActions()
        {
            _lvInterfaces.KeyDown += (_, e) =>
            {
                if (e.Control && e.KeyCode == Keys.C)
                {
                    e.SuppressKeyPress = true;
                    CopySelectedInterfaceRow();
                }
            };

            _lvInterfaces.MouseDown += (_, e) => SelectListViewItemOnRightClick(_lvInterfaces, e);

            var copyRow = new ToolStripMenuItem("Копировать строку", null, (_, _) => CopySelectedInterfaceRow());
            var copyVisibleRows = new ToolStripMenuItem("Копировать видимые строки", null, (_, _) => CopyVisibleInterfaceRows());
            var copyEndpoint = new ToolStripMenuItem("Копировать интерфейс", null, (_, _) => CopySelectedInterfaceEndpoint());
            var copyIp = new ToolStripMenuItem("Копировать IP", null, (_, _) => CopySelectedInterfaceIp());
            var copyMpiDpPn = new ToolStripMenuItem("Копировать MPI/DP/PN", null, (_, _) => CopySelectedInterfaceMpiDpPn());
            var addSimilar = new ToolStripMenuItem("Добавить похожее", null, (_, _) => AddSimilarInterfaceRequested?.Invoke(this, EventArgs.Empty));
            var menu = new ContextMenuStrip();
            menu.Opening += (_, _) =>
            {
                var selectedInterface = FindSelectedInterfaceState();
                bool hasInterface = selectedInterface != null;
                addSimilar.Enabled = hasInterface && _currentState.SupportsEditing && _currentState.SupportsPassportEditing;
                copyRow.Enabled = hasInterface;
                copyVisibleRows.Enabled = _lvInterfaces.Items.Count > 0;
                copyEndpoint.Enabled = hasInterface && CanCopyText(selectedInterface!.EndpointText);
                copyIp.Enabled = hasInterface && CanCopyText(selectedInterface!.IpAddressText);
                copyMpiDpPn.Enabled = hasInterface && CanCopyText(selectedInterface!.MpiDpPnAddressText);
            };
            menu.Items.Add(addSimilar);
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add(copyRow);
            menu.Items.Add(copyVisibleRows);
            menu.Items.Add(copyEndpoint);
            menu.Items.Add(copyIp);
            menu.Items.Add(copyMpiDpPn);
            _lvInterfaces.ContextMenuStrip = menu;
        }

        private void ConfigureConnectionCopyActions()
        {
            _lvConnections.KeyDown += (_, e) =>
            {
                if (e.Control && e.KeyCode == Keys.C)
                {
                    e.SuppressKeyPress = true;
                    CopySelectedConnectionRow();
                }
            };

            _lvConnections.MouseDown += (_, e) => SelectListViewItemOnRightClick(_lvConnections, e);

            var copyRow = new ToolStripMenuItem("Копировать строку", null, (_, _) => CopySelectedConnectionRow());
            var copyVisibleRows = new ToolStripMenuItem("Копировать видимые строки", null, (_, _) => CopyVisibleConnectionRows());
            var copyEndpointA = new ToolStripMenuItem("Копировать интерфейс A", null, (_, _) => CopySelectedConnectionEndpointA());
            var copyEndpointB = new ToolStripMenuItem("Копировать интерфейс B", null, (_, _) => CopySelectedConnectionEndpointB());
            var addSimilar = new ToolStripMenuItem("Добавить похожее", null, (_, _) => AddSimilarConnectionRequested?.Invoke(this, EventArgs.Empty));
            var menu = new ContextMenuStrip();
            menu.Opening += (_, _) =>
            {
                bool hasConnection = FindSelectedConnectionState() != null;
                addSimilar.Enabled = hasConnection && _currentState.SupportsEditing && _currentState.SupportsPassportEditing;
                copyRow.Enabled = hasConnection;
                copyVisibleRows.Enabled = _lvConnections.Items.Count > 0;
                copyEndpointA.Enabled = hasConnection;
                copyEndpointB.Enabled = hasConnection;
            };
            menu.Items.Add(addSimilar);
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add(copyRow);
            menu.Items.Add(copyVisibleRows);
            menu.Items.Add(copyEndpointA);
            menu.Items.Add(copyEndpointB);
            _lvConnections.ContextMenuStrip = menu;
        }

        private static void SelectListViewItemOnRightClick(ListView listView, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Right)
                return;

            var item = listView.GetItemAt(e.X, e.Y);
            if (item == null)
                return;

            listView.SelectedItems.Clear();
            item.Selected = true;
            item.Focused = true;
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

        private KnowledgeBaseNetworkDeviceState? FindSelectedDeviceState()
        {
            if (_lvDevices.SelectedItems.Count == 0 ||
                _lvDevices.SelectedItems[0].Tag is not ListItemTag tag)
            {
                return null;
            }

            return _currentState.DeviceStates.FirstOrDefault(entry =>
                string.Equals(entry.NetworkDeviceId, tag.ItemId, StringComparison.Ordinal));
        }

        private KnowledgeBaseNetworkInterfaceState? FindSelectedInterfaceState()
        {
            if (_lvInterfaces.SelectedItems.Count == 0 ||
                _lvInterfaces.SelectedItems[0].Tag is not ListItemTag tag)
            {
                return null;
            }

            return _currentState.InterfaceStates.FirstOrDefault(entry =>
                string.Equals(entry.NetworkInterfaceId, tag.ItemId, StringComparison.Ordinal));
        }

        private KnowledgeBaseNetworkConnectionState? FindSelectedConnectionState()
        {
            if (_lvConnections.SelectedItems.Count == 0 ||
                _lvConnections.SelectedItems[0].Tag is not ListItemTag tag)
            {
                return null;
            }

            return _currentState.ConnectionStates.FirstOrDefault(entry =>
                string.Equals(entry.NetworkConnectionId, tag.ItemId, StringComparison.Ordinal));
        }

        private void CopySelectedDeviceRow()
        {
            var device = FindSelectedDeviceState();
            if (device == null)
                return;

            CopyPassportText(BuildDeviceRowText(device), "строка устройства");
        }

        private void CopyVisibleDeviceRows() =>
            CopyVisibleListRows(_lvDevices, "видимые устройства");

        private void CopySelectedDeviceSummary()
        {
            var device = FindSelectedDeviceState();
            if (device == null)
                return;

            CopyPassportText(BuildDeviceSummaryText(device), "устройство");
        }

        private void CopySelectedDeviceProfinetName()
        {
            var device = FindSelectedDeviceState();
            if (device == null)
                return;

            CopyPassportText(device.ProfinetNameText, "PROFINET-name устройства");
        }

        private void CopySelectedDeviceMac()
        {
            var device = FindSelectedDeviceState();
            if (device == null)
                return;

            CopyPassportText(device.MacAddressText, "MAC устройства");
        }

        private void CopySelectedInterfaceRow()
        {
            var networkInterface = FindSelectedInterfaceState();
            if (networkInterface == null)
                return;

            CopyPassportText(BuildInterfaceRowText(networkInterface), "строка интерфейса");
        }

        private void CopyVisibleInterfaceRows() =>
            CopyVisibleListRows(_lvInterfaces, "видимые интерфейсы");

        private void CopySelectedInterfaceEndpoint()
        {
            var networkInterface = FindSelectedInterfaceState();
            if (networkInterface == null)
                return;

            CopyPassportText(networkInterface.EndpointText, "интерфейс");
        }

        private void CopySelectedInterfaceIp()
        {
            var networkInterface = FindSelectedInterfaceState();
            if (networkInterface == null)
                return;

            CopyPassportText(networkInterface.IpAddressText, "IP интерфейса");
        }

        private void CopySelectedInterfaceMpiDpPn()
        {
            var networkInterface = FindSelectedInterfaceState();
            if (networkInterface == null)
                return;

            CopyPassportText(networkInterface.MpiDpPnAddressText, "MPI/DP/PN интерфейса");
        }

        private void CopySelectedConnectionRow()
        {
            var connection = FindSelectedConnectionState();
            if (connection == null)
                return;

            CopyPassportText(BuildConnectionRowText(connection), "строка соединения");
        }

        private void CopyVisibleConnectionRows() =>
            CopyVisibleListRows(_lvConnections, "видимые соединения");

        private void CopySelectedConnectionEndpointA()
        {
            var connection = FindSelectedConnectionState();
            if (connection == null)
                return;

            CopyPassportText(connection.EndpointAText, "интерфейс A");
        }

        private void CopySelectedConnectionEndpointB()
        {
            var connection = FindSelectedConnectionState();
            if (connection == null)
                return;

            CopyPassportText(connection.EndpointBText, "интерфейс B");
        }

        private void CopyVisiblePassportRows()
        {
            var sections = new List<string>();
            AppendVisibleListSection(sections, "Устройства", _lvDevices);
            AppendVisibleListSection(sections, "Интерфейсы", _lvInterfaces);
            AppendVisibleListSection(sections, "Соединения", _lvConnections);
            if (sections.Count == 0)
                return;

            CopyPassportText(string.Join(Environment.NewLine + Environment.NewLine, sections), "видимые строки паспорта");
        }

        private void CopyVisibleListRows(ListView listView, string copiedPart) =>
            CopyPassportText(BuildVisibleListText(listView), copiedPart);

        private void CopyPassportText(string text, string copiedPart)
        {
            if (!CanCopyText(text))
                return;

            Clipboard.SetText(text);
            if (_lblPassportFilterStatus != null)
                _lblPassportFilterStatus.Text = $"Скопировано: {copiedPart}.";
        }

        private static bool CanCopyText(string? text) =>
            !string.IsNullOrWhiteSpace(text) &&
            !string.Equals(text, "-", StringComparison.Ordinal);

        private static void AppendVisibleListSection(ICollection<string> sections, string title, ListView listView)
        {
            string text = BuildVisibleListText(listView);
            if (CanCopyText(text))
                sections.Add(title + Environment.NewLine + text);
        }

        private static string BuildVisibleListText(ListView listView)
        {
            if (listView.Items.Count == 0)
                return string.Empty;

            var lines = new List<string>
            {
                BuildListHeaderText(listView)
            };

            foreach (ListViewItem item in listView.Items)
                lines.Add(BuildListItemText(item));

            return string.Join(Environment.NewLine, lines);
        }

        private static string BuildListHeaderText(ListView listView)
        {
            var values = new List<string>(listView.Columns.Count);
            foreach (ColumnHeader column in listView.Columns)
                values.Add(column.Text);

            return string.Join("\t", values);
        }

        private static string BuildListItemText(ListViewItem item)
        {
            var values = new List<string>(item.SubItems.Count);
            foreach (ListViewItem.ListViewSubItem subItem in item.SubItems)
                values.Add(subItem.Text);

            return string.Join("\t", values);
        }

        private static string BuildDeviceSummaryText(KnowledgeBaseNetworkDeviceState device) =>
            string.Join(
                " / ",
                new[]
                {
                    device.NameText,
                    device.RoleText,
                    device.VendorText,
                    device.ModelText
                }.Where(CanCopyText));

        private static string BuildDeviceRowText(KnowledgeBaseNetworkDeviceState device) =>
            string.Join(
                "\t",
                new[]
                {
                    device.NameText,
                    device.RoleText,
                    device.VendorText,
                    device.ModelText,
                    device.ProfinetNameText,
                    device.MacAddressText,
                    device.LocationText,
                    device.LinkedNodeText,
                    device.WarningText,
                    device.InterfacesCount.ToString(),
                    device.ConnectionsCount.ToString()
                });

        private static string BuildInterfaceRowText(KnowledgeBaseNetworkInterfaceState networkInterface) =>
            string.Join(
                "\t",
                new[]
                {
                    networkInterface.DeviceNameText,
                    networkInterface.InterfaceNameText,
                    networkInterface.IpAddressText,
                    networkInterface.SubnetMaskText,
                    networkInterface.GatewayText,
                    networkInterface.ProtocolText,
                    networkInterface.MpiDpPnAddressText,
                    networkInterface.MediumText,
                    networkInterface.SpeedText,
                    networkInterface.VlanText,
                    networkInterface.MacAddressText,
                    networkInterface.WarningText,
                    networkInterface.NotesText
                });

        private static string BuildConnectionRowText(KnowledgeBaseNetworkConnectionState connection) =>
            string.Join(
                "\t",
                new[]
                {
                    connection.EndpointAText,
                    connection.EndpointBText,
                    connection.CableLabelText,
                    connection.CableTypeText,
                    connection.ProtocolText,
                    connection.MediumText,
                    connection.LengthText,
                    connection.RouteText,
                    connection.StatusText,
                    connection.WarningText,
                    connection.NotesText
                });

        private void UpdateButtonStates()
        {
            bool canFileEdit = _currentState.SupportsEditing;
            bool canPassportEdit = _currentState.SupportsEditing && _currentState.SupportsPassportEditing;
            var selectedFileState = FindSelectedFileReferenceState();
            bool hasFileSelection = selectedFileState != null;
            bool hasFilePath = hasFileSelection && !string.Equals(selectedFileState!.PathText, "-", StringComparison.Ordinal);

            _btnAddDevice.Enabled = canPassportEdit;
            _btnAddSimilarDevice.Enabled = canPassportEdit && !string.IsNullOrWhiteSpace(SelectedDeviceId);
            _btnEditDevice.Enabled = canPassportEdit && !string.IsNullOrWhiteSpace(SelectedDeviceId);
            _btnCopyDevice.Enabled = !string.IsNullOrWhiteSpace(SelectedDeviceId);
            _btnDeleteDevice.Enabled = canPassportEdit && !string.IsNullOrWhiteSpace(SelectedDeviceId);
            _btnAddInterface.Enabled = canPassportEdit && _currentState.DeviceCount > 0;
            _btnAddSimilarInterface.Enabled = canPassportEdit && !string.IsNullOrWhiteSpace(SelectedInterfaceId);
            _btnEditInterface.Enabled = canPassportEdit && !string.IsNullOrWhiteSpace(SelectedInterfaceId);
            _btnCopyInterface.Enabled = !string.IsNullOrWhiteSpace(SelectedInterfaceId);
            _btnDeleteInterface.Enabled = canPassportEdit && !string.IsNullOrWhiteSpace(SelectedInterfaceId);
            _btnAddConnection.Enabled = canPassportEdit && _currentState.InterfaceCount >= 2;
            _btnAddSimilarConnection.Enabled = canPassportEdit && !string.IsNullOrWhiteSpace(SelectedConnectionId);
            _btnEditConnection.Enabled = canPassportEdit && !string.IsNullOrWhiteSpace(SelectedConnectionId);
            _btnCopyConnection.Enabled = !string.IsNullOrWhiteSpace(SelectedConnectionId);
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
                _txtPreviewSourceNote.Text = string.Empty;
                ShowPreviewMessage("Выберите файл сети для предпросмотра.");
                return;
            }

            string path = string.Equals(selectedState.PathText, "-", StringComparison.Ordinal)
                ? string.Empty
                : selectedState.PathText;

            _lblPreviewTitleValue.Text = selectedState.TitleText;
            _txtPreviewPath.Text = path;
            _lblPreviewKindValue.Text = selectedState.PreviewKindText;
            _txtPreviewSourceNote.Text = string.Equals(selectedState.SourceNoteText, "-", StringComparison.Ordinal)
                ? string.Empty
                : selectedState.SourceNoteText;

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
            if (_lvFiles.Columns.Count != 4)
                return;

            int clientWidth = _lvFiles.ClientSize.Width;
            if (clientWidth <= 0)
                return;

            int previewWidth = 170;
            int sourceWidth = Math.Max(180, (int)(clientWidth * 0.24f));
            int titleWidth = Math.Max(200, (int)(clientWidth * 0.24f));
            int pathWidth = Math.Max(240, clientWidth - titleWidth - previewWidth - sourceWidth - 8);

            _lvFiles.Columns[0].Width = titleWidth;
            _lvFiles.Columns[1].Width = previewWidth;
            _lvFiles.Columns[2].Width = sourceWidth;
            _lvFiles.Columns[3].Width = pathWidth;
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

        private static ModernNetworkSectionPanel CreateOverviewGroup(string title, Control content)
        {
            var groupBox = new ModernNetworkSectionPanel
            {
                Text = title,
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 0, 10, 10)
            };

            var container = new ModernNetworkBorderPanel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(1)
            };
            container.Controls.Add(content);

            groupBox.Controls.Add(container);
            return groupBox;
        }

        private static ModernNetworkSectionPanel CreatePassportGroup(
            string title,
            Control actionsPanel,
            ListView listView,
            Label emptyStateLabel)
        {
            var groupBox = new ModernNetworkSectionPanel
            {
                Text = title,
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 0, 0, 10)
            };

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Transparent,
                ColumnCount = 1,
                RowCount = 2
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            var container = new ModernNetworkBorderPanel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(1)
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
                BackColor = Color.Transparent,
                Margin = new Padding(0, 0, 0, 8)
            };

        private static ListView CreateDevicesListView()
        {
            var listView = CreateBaseListView();
            listView.Columns.Add("Устройство", 180);
            listView.Columns.Add("Роль", 120);
            listView.Columns.Add("Производитель", 130);
            listView.Columns.Add("Модель", 170);
            listView.Columns.Add("PROFINET-name", 150);
            listView.Columns.Add("MAC", 145);
            listView.Columns.Add("Место", 150);
            listView.Columns.Add("Карточка", 150);
            listView.Columns.Add("Проверка", 210);
            listView.Columns.Add("Инт.", 55);
            listView.Columns.Add("Связи", 55);
            return listView;
        }

        private static ListView CreateOverviewObjectsListView()
        {
            var listView = CreateBaseListView();
            listView.Columns.Add("Объект / устройство", 150);
            listView.Columns.Add("Тип / роль", 95);
            listView.Columns.Add("IP", 95);
            listView.Columns.Add("PROFINET-name", 120);
            listView.Columns.Add("Шкаф / узел", 130);
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
            listView.Columns.Add("MPI/DP/PN", 100);
            listView.Columns.Add("Среда", 95);
            listView.Columns.Add("Скорость", 90);
            listView.Columns.Add("VLAN", 70);
            listView.Columns.Add("MAC", 145);
            listView.Columns.Add("Проверка", 210);
            listView.Columns.Add("Примечание", 220);
            return listView;
        }

        private static ListView CreateConnectionsListView()
        {
            var listView = CreateBaseListView();
            listView.Columns.Add("Интерфейс A", 320);
            listView.Columns.Add("Интерфейс B", 320);
            listView.Columns.Add("Кабель", 120);
            listView.Columns.Add("Тип", 110);
            listView.Columns.Add("Протокол", 110);
            listView.Columns.Add("Среда", 100);
            listView.Columns.Add("Длина", 80);
            listView.Columns.Add("Трасса / место", 160);
            listView.Columns.Add("Статус", 100);
            listView.Columns.Add("Проверка", 210);
            listView.Columns.Add("Примечание", 220);
            return listView;
        }

        private static ListView CreateFilesListView()
        {
            var listView = CreateBaseListView();
            listView.Columns.Add("Наименование", 220);
            listView.Columns.Add("Предпросмотр", 170);
            listView.Columns.Add("Фрагмент / комментарий", 220);
            listView.Columns.Add("Путь", 360);
            return listView;
        }

        private static ListView CreateBaseListView() =>
            new()
            {
                Dock = DockStyle.Fill,
                BackColor = NetworkSurfaceColor,
                BorderStyle = BorderStyle.None,
                FullRowSelect = true,
                GridLines = false,
                HeaderStyle = ColumnHeaderStyle.Nonclickable,
                HideSelection = false,
                MultiSelect = false,
                ShowItemToolTips = true,
                View = View.Details
            };

        private static Label CreateValueLabel(string text) =>
            new()
            {
                Text = text,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                ForeColor = Color.FromArgb(28, 38, 49),
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
                ForeColor = NetworkMutedTextColor,
                Padding = new Padding(24),
                Visible = false
            };

        private static ModernNetworkBorderPanel CreateTextFieldFrame(TextBox textBox, bool multiline = false)
        {
            var margin = textBox.Margin;
            textBox.BorderStyle = BorderStyle.None;
            textBox.Margin = new Padding(0);
            textBox.Dock = DockStyle.Fill;
            textBox.BackColor = NetworkSurfaceColor;

            var frame = new ModernNetworkBorderPanel
            {
                Dock = DockStyle.Fill,
                Margin = margin,
                Padding = multiline ? new Padding(6, 4, 4, 4) : new Padding(6, 5, 6, 3)
            };
            frame.Controls.Add(textBox);
            return frame;
        }

        private static Button CreateActionButton(string text)
        {
            var button = new Button
            {
                Text = text,
                AutoSize = true,
                BackColor = NetworkSurfaceColor,
                FlatStyle = FlatStyle.Flat,
                UseVisualStyleBackColor = false,
                Margin = new Padding(0, 0, 8, 8)
            };

            button.FlatAppearance.BorderColor = Color.FromArgb(150, 190, 202, 214);
            button.FlatAppearance.BorderSize = 1;
            button.FlatAppearance.MouseDownBackColor = Color.FromArgb(231, 241, 248);
            button.FlatAppearance.MouseOverBackColor = Color.FromArgb(239, 246, 250);
            button.EnabledChanged += (_, _) => ApplyActionButtonVisualState(button);
            ApplyActionButtonVisualState(button);
            return button;
        }

        private static void ApplyActionButtonVisualState(Button button)
        {
            button.BackColor = button.Enabled ? NetworkSurfaceColor : Color.FromArgb(245, 247, 249);
            button.ForeColor = button.Enabled ? Color.FromArgb(17, 24, 32) : Color.FromArgb(154, 165, 175);
        }

        private sealed class ModernNetworkSectionPanel : Panel
        {
            public ModernNetworkSectionPanel()
            {
                DoubleBuffered = true;
                BackColor = NetworkPanelColor;
                Padding = new Padding(10, 18, 10, 10);
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                base.OnPaint(e);

                var bounds = ClientRectangle;
                bounds.X += 1;
                bounds.Y += 8;
                bounds.Width -= 2;
                bounds.Height -= 9;
                if (bounds.Width <= 0 || bounds.Height <= 0)
                    return;

                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using var path = CreateRoundedRectanglePath(bounds, 6);
                using var pen = new Pen(NetworkHairlineColor, 0.25F);
                e.Graphics.DrawPath(pen, path);

                if (string.IsNullOrWhiteSpace(Text))
                    return;

                var titleSize = TextRenderer.MeasureText(Text, Font);
                var titleBounds = new Rectangle(14, 0, titleSize.Width + 8, 18);
                using var titleBrush = new SolidBrush(NetworkPanelColor);
                e.Graphics.FillRectangle(titleBrush, titleBounds);
                TextRenderer.DrawText(
                    e.Graphics,
                    Text,
                    Font,
                    new Point(18, 1),
                    Color.FromArgb(28, 38, 49),
                    TextFormatFlags.NoPrefix);
            }

            private static GraphicsPath CreateRoundedRectanglePath(Rectangle rectangle, int radius)
            {
                var path = new GraphicsPath();
                var diameter = radius * 2;
                var arc = new Rectangle(rectangle.Location, new Size(diameter, diameter));

                path.AddArc(arc, 180, 90);
                arc.X = rectangle.Right - diameter;
                path.AddArc(arc, 270, 90);
                arc.Y = rectangle.Bottom - diameter;
                path.AddArc(arc, 0, 90);
                arc.X = rectangle.Left;
                path.AddArc(arc, 90, 90);
                path.CloseFigure();
                return path;
            }
        }

        private sealed class ModernNetworkBorderPanel : Panel
        {
            public ModernNetworkBorderPanel()
            {
                DoubleBuffered = true;
                BackColor = NetworkSurfaceColor;
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                base.OnPaint(e);

                var bounds = ClientRectangle;
                bounds.Width -= 1;
                bounds.Height -= 1;
                if (bounds.Width <= 0 || bounds.Height <= 0)
                    return;

                using var pen = new Pen(NetworkHairlineColor, 0.25F);
                e.Graphics.DrawRectangle(pen, bounds);
            }
        }
    }
}
