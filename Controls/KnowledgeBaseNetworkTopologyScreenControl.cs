using AsutpKnowledgeBase.Models;
using AsutpKnowledgeBase.Services;

namespace AsutpKnowledgeBase
{
    public sealed class KnowledgeBaseNetworkTopologyScreenControl : UserControl
    {
        private readonly TopologyCanvas _canvas;
        private readonly Label _lblSummary;
        private readonly Button _btnLink;
        private readonly Button _btnEdit;
        private readonly Button _btnDelete;

        private KbNetworkTopology _topology = new();
        private string _selectedElementId = string.Empty;
        private string _pendingLinkSourceElementId = string.Empty;

        public KnowledgeBaseNetworkTopologyScreenControl()
        {
            Dock = DockStyle.Fill;
            BackColor = KnowledgeBaseWorkspaceVisuals.SurfaceColor;

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                BackColor = KnowledgeBaseWorkspaceVisuals.SurfaceColor,
                Padding = new Padding(12),
                ColumnCount = 1,
                RowCount = 3
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            _lblSummary = new Label
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                Font = new Font("Segoe UI", 9F),
                ForeColor = KnowledgeBaseWorkspaceVisuals.MutedTextColor,
                Margin = new Padding(0, 0, 0, 8)
            };

            var toolbar = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                BackColor = KnowledgeBaseWorkspaceVisuals.SurfaceColor,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = true,
                Margin = new Padding(0, 0, 0, 10)
            };

            toolbar.Controls.Add(CreateAddButton("PLC", KbNetworkElementKind.Plc));
            toolbar.Controls.Add(CreateAddButton("Панель", KbNetworkElementKind.Panel));
            toolbar.Controls.Add(CreateAddButton("SCALANCE", KbNetworkElementKind.Scalance));
            toolbar.Controls.Add(CreateAddButton("АРМ", KbNetworkElementKind.Arm));
            toolbar.Controls.Add(CreateAddButton("HMI", KbNetworkElementKind.Hmi));
            toolbar.Controls.Add(CreateAddButton("Сервер", KbNetworkElementKind.Server));
            toolbar.Controls.Add(CreateAddButton("I/O", KbNetworkElementKind.Io));
            toolbar.Controls.Add(CreateAddButton("Камера", KbNetworkElementKind.Camera));

            _btnLink = CreateActionButton("Связь");
            _btnLink.Click += (_, _) => BeginOrCancelLinkMode();
            _btnEdit = CreateActionButton("Изменить");
            _btnEdit.Click += (_, _) => EditSelectedElement();
            _btnDelete = CreateActionButton("Удалить");
            _btnDelete.Click += (_, _) => DeleteSelectedElement();
            toolbar.Controls.Add(_btnLink);
            toolbar.Controls.Add(_btnEdit);
            toolbar.Controls.Add(_btnDelete);

            _canvas = new TopologyCanvas
            {
                Dock = DockStyle.Fill,
                Topology = _topology
            };
            _canvas.SelectionChanged += (_, e) =>
            {
                _selectedElementId = e.ElementId;
                UpdateCommandState();
            };
            _canvas.ElementMoved += (_, _) => CommitTopologyChange();
            _canvas.ElementEditRequested += (_, e) => EditElement(e.ElementId);
            _canvas.LinkTargetSelected += (_, e) => CompleteLink(e.ElementId);

            layout.Controls.Add(_lblSummary, 0, 0);
            layout.Controls.Add(toolbar, 0, 1);
            layout.Controls.Add(_canvas, 0, 2);
            Controls.Add(layout);

            UpdateCommandState();
        }

        public event EventHandler? TopologyChangedByUser;

        public KbNetworkTopology CurrentTopology => CloneTopology(_topology);

        public void ApplyState(KbNetworkTopology? topology)
        {
            _topology = CloneTopology(topology);
            _selectedElementId = string.Empty;
            _pendingLinkSourceElementId = string.Empty;
            _canvas.Topology = _topology;
            _canvas.SelectedElementId = _selectedElementId;
            _canvas.PendingLinkSourceElementId = _pendingLinkSourceElementId;
            UpdateCommandState();
            _canvas.Invalidate();
        }

        private Button CreateAddButton(string text, KbNetworkElementKind kind)
        {
            var button = CreateActionButton(text);
            button.Click += (_, _) => AddElement(kind);
            return button;
        }

        private static Button CreateActionButton(string text) =>
            new()
            {
                Text = text,
                AutoSize = true,
                Height = 30,
                MinimumSize = new Size(68, 30),
                FlatStyle = FlatStyle.Standard,
                Margin = new Padding(0, 0, 8, 6)
            };

        private void AddElement(KbNetworkElementKind kind)
        {
            Point location = FindNextElementLocation();
            var element = new KbNetworkElement
            {
                ElementId = Guid.NewGuid().ToString("N"),
                Kind = kind,
                Name = BuildDefaultName(kind),
                X = location.X,
                Y = location.Y
            };

            _topology.Elements.Add(element);
            SelectElement(element.ElementId);
            CommitTopologyChange();
        }

        private Point FindNextElementLocation()
        {
            int index = _topology.Elements.Count;
            int x = 32 + (index % 5) * 132;
            int y = 32 + (index / 5) * 108;
            return new Point(x, y);
        }

        private string BuildDefaultName(KbNetworkElementKind kind)
        {
            string prefix = kind switch
            {
                KbNetworkElementKind.Plc => "PLC",
                KbNetworkElementKind.Panel => "PNL",
                KbNetworkElementKind.Scalance => "SCALANCE",
                KbNetworkElementKind.Arm => "ARM",
                KbNetworkElementKind.Hmi => "HMI",
                KbNetworkElementKind.Server => "SRV",
                KbNetworkElementKind.Io => "IO",
                KbNetworkElementKind.Camera => "CAM",
                _ => "DEV"
            };
            int number = _topology.Elements.Count(element => element.Kind == kind) + 1;
            return $"{prefix}-{number:00}";
        }

        private void BeginOrCancelLinkMode()
        {
            if (string.IsNullOrWhiteSpace(_selectedElementId))
                return;

            _pendingLinkSourceElementId = string.Equals(
                _pendingLinkSourceElementId,
                _selectedElementId,
                StringComparison.Ordinal)
                ? string.Empty
                : _selectedElementId;
            _canvas.PendingLinkSourceElementId = _pendingLinkSourceElementId;
            UpdateCommandState();
            _canvas.Invalidate();
        }

        private void CompleteLink(string targetElementId)
        {
            if (string.IsNullOrWhiteSpace(_pendingLinkSourceElementId) ||
                string.Equals(_pendingLinkSourceElementId, targetElementId, StringComparison.Ordinal))
            {
                return;
            }

            bool exists = _topology.Links.Any(link =>
                LinksConnectSameElements(link, _pendingLinkSourceElementId, targetElementId));
            if (!exists)
            {
                _topology.Links.Add(new KbNetworkLink
                {
                    LinkId = Guid.NewGuid().ToString("N"),
                    FromElementId = _pendingLinkSourceElementId,
                    ToElementId = targetElementId
                });
                CommitTopologyChange();
            }

            _pendingLinkSourceElementId = string.Empty;
            _canvas.PendingLinkSourceElementId = string.Empty;
            SelectElement(targetElementId);
        }

        private static bool LinksConnectSameElements(KbNetworkLink link, string firstElementId, string secondElementId) =>
            string.Equals(link.FromElementId, firstElementId, StringComparison.Ordinal) &&
            string.Equals(link.ToElementId, secondElementId, StringComparison.Ordinal) ||
            string.Equals(link.FromElementId, secondElementId, StringComparison.Ordinal) &&
            string.Equals(link.ToElementId, firstElementId, StringComparison.Ordinal);

        private void EditSelectedElement()
        {
            if (!string.IsNullOrWhiteSpace(_selectedElementId))
                EditElement(_selectedElementId);
        }

        private void EditElement(string elementId)
        {
            KbNetworkElement? element = FindElement(elementId);
            if (element == null)
                return;

            using var dialog = new NetworkElementDialog(element);
            if (dialog.ShowDialog(FindForm()) != DialogResult.OK)
                return;

            element.Kind = dialog.ElementKind;
            element.Name = dialog.ElementName;
            element.IpAddress = dialog.IpAddress;
            CommitTopologyChange();
        }

        private void DeleteSelectedElement()
        {
            if (string.IsNullOrWhiteSpace(_selectedElementId))
                return;

            _topology.Elements.RemoveAll(element => string.Equals(element.ElementId, _selectedElementId, StringComparison.Ordinal));
            _topology.Links.RemoveAll(link =>
                string.Equals(link.FromElementId, _selectedElementId, StringComparison.Ordinal) ||
                string.Equals(link.ToElementId, _selectedElementId, StringComparison.Ordinal));
            _pendingLinkSourceElementId = string.Empty;
            SelectElement(string.Empty);
            CommitTopologyChange();
        }

        private void SelectElement(string elementId)
        {
            _selectedElementId = elementId;
            _canvas.SelectedElementId = elementId;
            UpdateCommandState();
            _canvas.Invalidate();
        }

        private KbNetworkElement? FindElement(string elementId) =>
            _topology.Elements.FirstOrDefault(element => string.Equals(element.ElementId, elementId, StringComparison.Ordinal));

        private void CommitTopologyChange()
        {
            _topology = KnowledgeBaseDataService.NormalizeNetworkTopology(_topology);
            _canvas.Topology = _topology;
            if (FindElement(_selectedElementId) == null)
                _selectedElementId = string.Empty;
            _canvas.SelectedElementId = _selectedElementId;
            UpdateCommandState();
            _canvas.Invalidate();
            TopologyChangedByUser?.Invoke(this, EventArgs.Empty);
        }

        private void UpdateCommandState()
        {
            bool hasSelection = !string.IsNullOrWhiteSpace(_selectedElementId);
            _btnLink.Enabled = hasSelection;
            _btnEdit.Enabled = hasSelection;
            _btnDelete.Enabled = hasSelection;
            _btnLink.Text = !string.IsNullOrWhiteSpace(_pendingLinkSourceElementId)
                ? "Отмена связи"
                : "Связь";
            _lblSummary.Text = !string.IsNullOrWhiteSpace(_pendingLinkSourceElementId)
                ? "Выберите второй элемент для связи."
                : $"Элементов: {_topology.Elements.Count} | Связей: {_topology.Links.Count}";
        }

        private static KbNetworkTopology CloneTopology(KbNetworkTopology? topology) =>
            KnowledgeBaseDataService.NormalizeNetworkTopology(new KbNetworkTopology
            {
                Elements = topology?.Elements?
                    .Select(static element => new KbNetworkElement
                    {
                        ElementId = element.ElementId,
                        Kind = element.Kind,
                        Name = element.Name,
                        IpAddress = element.IpAddress,
                        X = element.X,
                        Y = element.Y
                    })
                    .ToList() ?? new List<KbNetworkElement>(),
                Links = topology?.Links?
                    .Select(static link => new KbNetworkLink
                    {
                        LinkId = link.LinkId,
                        FromElementId = link.FromElementId,
                        ToElementId = link.ToElementId,
                        Label = link.Label
                    })
                    .ToList() ?? new List<KbNetworkLink>()
            });

        private sealed class NetworkElementDialog : Form
        {
            private readonly ComboBox _cmbKind;
            private readonly TextBox _txtName;
            private readonly TextBox _txtIpAddress;

            public NetworkElementDialog(KbNetworkElement element)
            {
                Text = "Элемент сети";
                ClientSize = new Size(360, 178);
                StartPosition = FormStartPosition.CenterParent;
                FormBorderStyle = FormBorderStyle.FixedDialog;
                MaximizeBox = false;
                MinimizeBox = false;
                Padding = new Padding(16);
                AppIconProvider.Apply(this);

                var layout = new TableLayoutPanel
                {
                    Dock = DockStyle.Fill,
                    ColumnCount = 2,
                    RowCount = 4
                };
                layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 86F));
                layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
                layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
                layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
                layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
                layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

                _cmbKind = new ComboBox
                {
                    Dock = DockStyle.Top,
                    DropDownStyle = ComboBoxStyle.DropDownList,
                    DataSource = GetKindOptions(),
                    DisplayMember = nameof(KindOption.Label),
                    ValueMember = nameof(KindOption.Kind),
                    Margin = new Padding(0, 0, 0, 8)
                };
                _cmbKind.SelectedValue = element.Kind;

                _txtName = new TextBox
                {
                    Dock = DockStyle.Top,
                    Text = element.Name,
                    Margin = new Padding(0, 0, 0, 8)
                };
                _txtIpAddress = new TextBox
                {
                    Dock = DockStyle.Top,
                    Text = element.IpAddress,
                    Margin = new Padding(0, 0, 0, 12)
                };

                var buttons = new FlowLayoutPanel
                {
                    Dock = DockStyle.Top,
                    AutoSize = true,
                    FlowDirection = FlowDirection.RightToLeft,
                    Margin = new Padding(0)
                };
                var btnOk = new Button
                {
                    Text = "OK",
                    DialogResult = DialogResult.OK,
                    AutoSize = true,
                    MinimumSize = new Size(82, 28),
                    Margin = new Padding(8, 0, 0, 0)
                };
                var btnCancel = new Button
                {
                    Text = "Отмена",
                    DialogResult = DialogResult.Cancel,
                    AutoSize = true,
                    MinimumSize = new Size(86, 28),
                    Margin = new Padding(0)
                };
                buttons.Controls.Add(btnCancel);
                buttons.Controls.Add(btnOk);

                layout.Controls.Add(CreateLabel("Тип"), 0, 0);
                layout.Controls.Add(_cmbKind, 1, 0);
                layout.Controls.Add(CreateLabel("Название"), 0, 1);
                layout.Controls.Add(_txtName, 1, 1);
                layout.Controls.Add(CreateLabel("IP"), 0, 2);
                layout.Controls.Add(_txtIpAddress, 1, 2);
                layout.Controls.Add(buttons, 1, 3);
                Controls.Add(layout);

                AcceptButton = btnOk;
                CancelButton = btnCancel;
            }

            public KbNetworkElementKind ElementKind =>
                _cmbKind.SelectedValue is KbNetworkElementKind kind ? kind : KbNetworkElementKind.Other;

            public string ElementName => _txtName.Text.Trim();

            public string IpAddress => _txtIpAddress.Text.Trim();

            private static Label CreateLabel(string text) =>
                new()
                {
                    Text = text,
                    AutoSize = true,
                    Dock = DockStyle.Top,
                    Margin = new Padding(0, 3, 8, 8)
                };

            private static List<KindOption> GetKindOptions() =>
            [
                new(KbNetworkElementKind.Plc, "PLC"),
                new(KbNetworkElementKind.Panel, "Панель"),
                new(KbNetworkElementKind.Scalance, "SCALANCE"),
                new(KbNetworkElementKind.Arm, "АРМ"),
                new(KbNetworkElementKind.Hmi, "HMI"),
                new(KbNetworkElementKind.Server, "Сервер"),
                new(KbNetworkElementKind.Io, "I/O"),
                new(KbNetworkElementKind.Camera, "Камера"),
                new(KbNetworkElementKind.Other, "Другое")
            ];

            private sealed record KindOption(KbNetworkElementKind Kind, string Label);
        }

        private sealed class TopologyCanvas : Panel
        {
            private const int ElementWidth = 112;
            private const int ElementHeight = 78;
            private const int IconSize = 38;

            private string _dragElementId = string.Empty;
            private Point _dragOffset;
            private bool _dragMoved;

            public TopologyCanvas()
            {
                DoubleBuffered = true;
                BackColor = Color.White;
                BorderStyle = BorderStyle.FixedSingle;
                Cursor = Cursors.Default;
            }

            public KbNetworkTopology Topology { get; set; } = new();

            public string SelectedElementId { get; set; } = string.Empty;

            public string PendingLinkSourceElementId { get; set; } = string.Empty;

            public event EventHandler<NetworkElementEventArgs>? SelectionChanged;

            public event EventHandler? ElementMoved;

            public event EventHandler<NetworkElementEventArgs>? ElementEditRequested;

            public event EventHandler<NetworkElementEventArgs>? LinkTargetSelected;

            protected override void OnPaint(PaintEventArgs e)
            {
                base.OnPaint(e);
                e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                DrawGrid(e.Graphics);
                DrawLinks(e.Graphics);

                foreach (KbNetworkElement element in Topology.Elements)
                    DrawElement(e.Graphics, element);

                if (Topology.Elements.Count == 0)
                {
                    using var emptyStateFont = new Font("Segoe UI", 14F, FontStyle.Regular);
                    TextRenderer.DrawText(
                        e.Graphics,
                        "Сеть пока пуста",
                        emptyStateFont,
                        ClientRectangle,
                        Color.FromArgb(117, 129, 141),
                        TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                }
            }

            protected override void OnMouseDown(MouseEventArgs e)
            {
                base.OnMouseDown(e);
                if (e.Button != MouseButtons.Left)
                    return;

                KbNetworkElement? element = HitTestElement(e.Location);
                if (!string.IsNullOrWhiteSpace(PendingLinkSourceElementId))
                {
                    if (element != null)
                        LinkTargetSelected?.Invoke(this, new NetworkElementEventArgs(element.ElementId));
                    return;
                }

                SelectedElementId = element?.ElementId ?? string.Empty;
                SelectionChanged?.Invoke(this, new NetworkElementEventArgs(SelectedElementId));
                Invalidate();

                if (element == null)
                    return;

                Rectangle bounds = GetElementBounds(element);
                _dragElementId = element.ElementId;
                _dragOffset = new Point(e.X - bounds.X, e.Y - bounds.Y);
                _dragMoved = false;
                Cursor = Cursors.SizeAll;
            }

            protected override void OnMouseMove(MouseEventArgs e)
            {
                base.OnMouseMove(e);
                if (string.IsNullOrWhiteSpace(_dragElementId))
                {
                    Cursor = HitTestElement(e.Location) == null ? Cursors.Default : Cursors.Hand;
                    return;
                }

                KbNetworkElement? element = Topology.Elements.FirstOrDefault(candidate =>
                    string.Equals(candidate.ElementId, _dragElementId, StringComparison.Ordinal));
                if (element == null)
                    return;

                int maxX = Math.Max(0, ClientSize.Width - ElementWidth - 8);
                int maxY = Math.Max(0, ClientSize.Height - ElementHeight - 8);
                element.X = Math.Clamp(e.X - _dragOffset.X, 8, maxX);
                element.Y = Math.Clamp(e.Y - _dragOffset.Y, 8, maxY);
                _dragMoved = true;
                Invalidate();
            }

            protected override void OnMouseUp(MouseEventArgs e)
            {
                base.OnMouseUp(e);
                bool moved = _dragMoved;
                _dragElementId = string.Empty;
                _dragMoved = false;
                Cursor = Cursors.Default;
                if (moved)
                    ElementMoved?.Invoke(this, EventArgs.Empty);
            }

            protected override void OnDoubleClick(EventArgs e)
            {
                base.OnDoubleClick(e);
                if (!string.IsNullOrWhiteSpace(SelectedElementId))
                    ElementEditRequested?.Invoke(this, new NetworkElementEventArgs(SelectedElementId));
            }

            private void DrawGrid(Graphics graphics)
            {
                using var pen = new Pen(Color.FromArgb(238, 242, 246), 1F);
                for (int x = 0; x < Width; x += 24)
                    graphics.DrawLine(pen, x, 0, x, Height);
                for (int y = 0; y < Height; y += 24)
                    graphics.DrawLine(pen, 0, y, Width, y);
            }

            private void DrawLinks(Graphics graphics)
            {
                using var linkPen = new Pen(Color.FromArgb(27, 128, 48), 2F);
                using var pendingPen = new Pen(Color.FromArgb(27, 128, 48), 2F)
                {
                    DashStyle = System.Drawing.Drawing2D.DashStyle.Dash
                };

                foreach (KbNetworkLink link in Topology.Links)
                {
                    KbNetworkElement? from = FindElement(link.FromElementId);
                    KbNetworkElement? to = FindElement(link.ToElementId);
                    if (from == null || to == null)
                        continue;

                    Point fromPoint = GetElementConnectionPoint(from);
                    Point toPoint = GetElementConnectionPoint(to);
                    DrawOrthogonalLink(graphics, linkPen, fromPoint, toPoint);
                }

                KbNetworkElement? pendingSource = FindElement(PendingLinkSourceElementId);
                if (pendingSource != null)
                {
                    Rectangle bounds = GetElementBounds(pendingSource);
                    bounds.Inflate(5, 5);
                    graphics.DrawRectangle(pendingPen, bounds);
                }
            }

            private static void DrawOrthogonalLink(Graphics graphics, Pen pen, Point fromPoint, Point toPoint)
            {
                int middleY = fromPoint.Y + ((toPoint.Y - fromPoint.Y) / 2);
                var points = new[]
                {
                    fromPoint,
                    new Point(fromPoint.X, middleY),
                    new Point(toPoint.X, middleY),
                    toPoint
                };
                graphics.DrawLines(pen, points);
            }

            private void DrawElement(Graphics graphics, KbNetworkElement element)
            {
                Rectangle bounds = GetElementBounds(element);
                bool selected = string.Equals(element.ElementId, SelectedElementId, StringComparison.Ordinal);
                using var shadow = new SolidBrush(Color.FromArgb(18, 20, 34, 48));
                using var fill = new SolidBrush(Color.FromArgb(252, 254, 255));
                using var border = new Pen(selected ? Color.FromArgb(28, 118, 210) : Color.FromArgb(177, 190, 202), selected ? 2F : 1F);

                var shadowBounds = bounds;
                shadowBounds.Offset(2, 2);
                using (var shadowPath = CreateRoundedRectanglePath(shadowBounds, 7))
                using (var path = CreateRoundedRectanglePath(bounds, 7))
                {
                    graphics.FillPath(shadow, shadowPath);
                    graphics.FillPath(fill, path);
                    graphics.DrawPath(border, path);
                }

                Rectangle iconBounds = new(bounds.X + (ElementWidth - IconSize) / 2, bounds.Y + 6, IconSize, IconSize);
                DrawDeviceIcon(graphics, element.Kind, iconBounds);

                Rectangle nameBounds = new(bounds.X + 4, bounds.Y + 47, ElementWidth - 8, 16);
                Rectangle ipBounds = new(bounds.X + 4, bounds.Y + 62, ElementWidth - 8, 14);
                using var nameFont = new Font("Segoe UI Semibold", 8.2F, FontStyle.Bold);
                using var ipFont = new Font("Segoe UI", 7.5F);
                TextRenderer.DrawText(
                    graphics,
                    element.Name,
                    nameFont,
                    nameBounds,
                    Color.FromArgb(21, 33, 45),
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
                TextRenderer.DrawText(
                    graphics,
                    element.IpAddress,
                    ipFont,
                    ipBounds,
                    Color.FromArgb(58, 70, 82),
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
            }

            private static void DrawDeviceIcon(Graphics graphics, KbNetworkElementKind kind, Rectangle bounds)
            {
                switch (kind)
                {
                    case KbNetworkElementKind.Scalance:
                        DrawSwitchIcon(graphics, bounds);
                        break;
                    case KbNetworkElementKind.Arm:
                    case KbNetworkElementKind.Hmi:
                    case KbNetworkElementKind.Panel:
                        DrawScreenIcon(graphics, bounds, kind == KbNetworkElementKind.Panel);
                        break;
                    case KbNetworkElementKind.Server:
                        DrawServerIcon(graphics, bounds);
                        break;
                    case KbNetworkElementKind.Io:
                    case KbNetworkElementKind.Plc:
                        DrawPlcIcon(graphics, bounds, kind == KbNetworkElementKind.Io);
                        break;
                    case KbNetworkElementKind.Camera:
                        DrawCameraIcon(graphics, bounds);
                        break;
                    default:
                        DrawOtherDeviceIcon(graphics, bounds);
                        break;
                }
            }

            private static void DrawSwitchIcon(Graphics graphics, Rectangle bounds)
            {
                using var body = new SolidBrush(Color.FromArgb(123, 151, 164));
                using var border = new Pen(Color.FromArgb(45, 59, 70), 1F);
                graphics.FillRectangle(body, bounds);
                graphics.DrawRectangle(border, bounds);
                using var port = new SolidBrush(Color.FromArgb(20, 32, 42));
                for (int row = 0; row < 2; row++)
                {
                    for (int col = 0; col < 4; col++)
                    {
                        graphics.FillRectangle(port, bounds.X + 5 + (col * 8), bounds.Y + 8 + (row * 11), 5, 6);
                    }
                }
            }

            private static void DrawScreenIcon(Graphics graphics, Rectangle bounds, bool panel)
            {
                using var body = new SolidBrush(Color.FromArgb(67, 84, 96));
                using var screen = new SolidBrush(panel ? Color.FromArgb(150, 199, 198) : Color.FromArgb(155, 206, 224));
                using var border = new Pen(Color.FromArgb(38, 49, 59), 1F);
                Rectangle monitor = new(bounds.X + 4, bounds.Y + 3, bounds.Width - 8, bounds.Height - 14);
                graphics.FillRectangle(body, monitor);
                graphics.DrawRectangle(border, monitor);
                Rectangle display = new(monitor.X + 4, monitor.Y + 4, monitor.Width - 8, monitor.Height - 8);
                graphics.FillRectangle(screen, display);
                graphics.DrawLine(border, bounds.X + bounds.Width / 2, monitor.Bottom, bounds.X + bounds.Width / 2, bounds.Bottom - 4);
                graphics.DrawLine(border, bounds.X + 10, bounds.Bottom - 4, bounds.Right - 10, bounds.Bottom - 4);
            }

            private static void DrawServerIcon(Graphics graphics, Rectangle bounds)
            {
                Rectangle tower = new(bounds.X + 10, bounds.Y + 1, bounds.Width - 20, bounds.Height - 2);
                using var body = new SolidBrush(Color.FromArgb(105, 122, 137));
                using var border = new Pen(Color.FromArgb(42, 52, 64), 1F);
                graphics.FillRectangle(body, tower);
                graphics.DrawRectangle(border, tower);
                using var slot = new Pen(Color.FromArgb(215, 225, 232), 1F);
                graphics.DrawLine(slot, tower.X + 5, tower.Y + 9, tower.Right - 5, tower.Y + 9);
                graphics.DrawLine(slot, tower.X + 5, tower.Y + 17, tower.Right - 5, tower.Y + 17);
                using var led = new SolidBrush(Color.FromArgb(42, 184, 96));
                graphics.FillEllipse(led, tower.X + 6, tower.Bottom - 9, 4, 4);
            }

            private static void DrawPlcIcon(Graphics graphics, Rectangle bounds, bool io)
            {
                using var body = new SolidBrush(io ? Color.FromArgb(96, 126, 150) : Color.FromArgb(112, 142, 159));
                using var border = new Pen(Color.FromArgb(44, 58, 67), 1F);
                int moduleWidth = bounds.Width / 4;
                for (int index = 0; index < 4; index++)
                {
                    Rectangle module = new(bounds.X + index * moduleWidth, bounds.Y + 2, moduleWidth - 1, bounds.Height - 4);
                    graphics.FillRectangle(body, module);
                    graphics.DrawRectangle(border, module);
                    using var led = new SolidBrush(index == 0 ? Color.FromArgb(44, 183, 88) : Color.FromArgb(210, 224, 232));
                    graphics.FillRectangle(led, module.X + 4, module.Y + 5, 4, 4);
                    graphics.DrawLine(border, module.X + 4, module.Y + 14, module.Right - 4, module.Y + 14);
                }
            }

            private static void DrawCameraIcon(Graphics graphics, Rectangle bounds)
            {
                using var body = new SolidBrush(Color.FromArgb(214, 220, 224));
                using var border = new Pen(Color.FromArgb(56, 66, 74), 1F);
                Rectangle camera = new(bounds.X + 6, bounds.Y + 12, bounds.Width - 18, bounds.Height - 20);
                graphics.FillRectangle(body, camera);
                graphics.DrawRectangle(border, camera);
                using var lens = new SolidBrush(Color.FromArgb(62, 86, 108));
                graphics.FillEllipse(lens, camera.X + 12, camera.Y + 5, 12, 12);
                graphics.DrawLine(border, camera.Right, camera.Y + 8, bounds.Right - 3, camera.Y + 3);
                graphics.DrawLine(border, camera.Right, camera.Bottom - 5, bounds.Right - 3, camera.Bottom);
            }

            private static void DrawOtherDeviceIcon(Graphics graphics, Rectangle bounds)
            {
                using var body = new SolidBrush(Color.FromArgb(151, 166, 177));
                using var border = new Pen(Color.FromArgb(49, 63, 74), 1F);
                graphics.FillRectangle(body, bounds.X + 6, bounds.Y + 6, bounds.Width - 12, bounds.Height - 12);
                graphics.DrawRectangle(border, bounds.X + 6, bounds.Y + 6, bounds.Width - 12, bounds.Height - 12);
            }

            private KbNetworkElement? HitTestElement(Point point)
            {
                for (int index = Topology.Elements.Count - 1; index >= 0; index--)
                {
                    KbNetworkElement element = Topology.Elements[index];
                    if (GetElementBounds(element).Contains(point))
                        return element;
                }

                return null;
            }

            private KbNetworkElement? FindElement(string elementId) =>
                Topology.Elements.FirstOrDefault(element => string.Equals(element.ElementId, elementId, StringComparison.Ordinal));

            private static Rectangle GetElementBounds(KbNetworkElement element) =>
                new(element.X, element.Y, ElementWidth, ElementHeight);

            private static Point GetElementConnectionPoint(KbNetworkElement element)
            {
                Rectangle bounds = GetElementBounds(element);
                return new Point(bounds.X + bounds.Width / 2, bounds.Y + bounds.Height / 2);
            }

            private static System.Drawing.Drawing2D.GraphicsPath CreateRoundedRectanglePath(Rectangle rectangle, int radius)
            {
                var path = new System.Drawing.Drawing2D.GraphicsPath();
                int diameter = radius * 2;
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

        private sealed class NetworkElementEventArgs : EventArgs
        {
            public NetworkElementEventArgs(string elementId)
            {
                ElementId = elementId;
            }

            public string ElementId { get; }
        }
    }
}
