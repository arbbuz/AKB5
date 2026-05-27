using System.Drawing.Drawing2D;
using System.Globalization;
using AsutpKnowledgeBase.Models;
using AsutpKnowledgeBase.Services;

namespace AsutpKnowledgeBase
{
    public sealed class KnowledgeBaseNetworkTopologyScreenControl : UserControl
    {
        private readonly TopologyCanvas _canvas;
        private readonly Label _lblSummary;
        private readonly ContextMenuStrip _canvasContextMenu;
        private readonly Panel _linkKindPalette;
        private readonly List<LinkKindPaletteButton> _linkKindButtons = [];
        private readonly ComboBox _cmbZoom = new();
        private readonly Button _btnLink;
        private readonly Button _btnEdit;
        private readonly Button _btnDelete;

        private KbNetworkTopology _topology = new();
        private string _selectedElementId = string.Empty;
        private string _selectedLinkId = string.Empty;
        private string _pendingLinkSourceElementId = string.Empty;
        private KbNetworkLinkKind _selectedLinkKind = KbNetworkLinkKind.CopperProfinet;
        private bool _updatingZoomControls;

        public KnowledgeBaseNetworkTopologyScreenControl()
        {
            Dock = DockStyle.Fill;
            BackColor = KnowledgeBaseWorkspaceVisuals.SurfaceColor;

            _canvasContextMenu = new ContextMenuStrip();

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
            toolbar.Controls.Add(CreateAddButton("ПЧ", KbNetworkElementKind.FrequencyConverter));
            toolbar.Controls.Add(CreateAddButton("SCALANCE", KbNetworkElementKind.Scalance));
            toolbar.Controls.Add(CreateAddButton("АРМ", KbNetworkElementKind.Arm));
            toolbar.Controls.Add(CreateAddButton("HMI", KbNetworkElementKind.Hmi));
            toolbar.Controls.Add(CreateAddButton("Сервер", KbNetworkElementKind.Server));
            toolbar.Controls.Add(CreateAddButton("ET", KbNetworkElementKind.Et200));
            toolbar.Controls.Add(CreateAddButton("OLM", KbNetworkElementKind.Olm));
            toolbar.Controls.Add(CreateAddButton("Внешняя связь", KbNetworkElementKind.ExternalConnection));

            _btnLink = CreateActionButton("Связь", NetworkIconPainter.CreateCommandIcon(NetworkCommandIconKind.Link));
            _btnLink.Click += (_, _) => BeginOrCancelLinkMode();
            _btnEdit = CreateActionButton("Изменить", NetworkIconPainter.CreateCommandIcon(NetworkCommandIconKind.Edit));
            _btnEdit.Click += (_, _) => EditSelectedElement();
            _btnDelete = CreateActionButton("Удалить", NetworkIconPainter.CreateCommandIcon(NetworkCommandIconKind.Delete));
            _btnDelete.Click += (_, _) => DeleteSelection();
            toolbar.Controls.Add(_btnLink);
            toolbar.Controls.Add(_btnEdit);
            toolbar.Controls.Add(_btnDelete);

            _canvas = new TopologyCanvas
            {
                Location = Point.Empty,
                Topology = _topology
            };
            var canvasShell = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                BackColor = KnowledgeBaseWorkspaceVisuals.SurfaceColor,
                Margin = new Padding(3),
                ColumnCount = 1,
                RowCount = 2
            };
            canvasShell.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            canvasShell.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            canvasShell.RowStyles.Add(new RowStyle(SizeType.Absolute, 42F));
            var canvasViewport = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                Margin = Padding.Empty
            };
            canvasViewport.Controls.Add(_canvas);
            _linkKindPalette = CreateLinkKindPalette();
            canvasShell.Controls.Add(canvasViewport, 0, 0);
            canvasShell.Controls.Add(_linkKindPalette, 0, 1);
            canvasViewport.Resize += (_, _) => _canvas.UpdateViewportSize(canvasViewport.ClientSize);
            _canvas.UpdateViewportSize(canvasViewport.ClientSize);

            _canvas.SelectionChanged += (_, e) =>
            {
                _selectedElementId = e.ElementId;
                if (!string.IsNullOrWhiteSpace(e.ElementId))
                    _selectedLinkId = string.Empty;
                UpdateCommandState();
            };
            _canvas.LinkSelectionChanged += (_, e) =>
            {
                _selectedLinkId = e.LinkId;
                if (!string.IsNullOrWhiteSpace(e.LinkId))
                    _selectedElementId = string.Empty;

                KbNetworkLink? selectedLink = FindLink(e.LinkId);
                if (selectedLink != null)
                {
                    _selectedLinkKind = selectedLink.Kind;
                    _canvas.PendingLinkKind = selectedLink.Kind;
                    UpdateLinkKindButtons();
                }

                UpdateCommandState();
            };
            _canvas.ElementMoved += (_, _) => CommitTopologyChange();
            _canvas.ElementEditRequested += (_, e) => EditElement(e.ElementId);
            _canvas.LinkTargetSelected += (_, e) => CompleteLink(e.ElementId);
            _canvas.LinkEndpointMoved += (_, _) => CommitTopologyChange();
            _canvas.CanvasContextMenuRequested += (_, e) => ShowCanvasContextMenu(e.Location, e.CanvasLocation, e.LinkId);
            _canvas.ZoomFactorChanged += (_, _) => UpdateZoomControls();

            layout.Controls.Add(_lblSummary, 0, 0);
            layout.Controls.Add(toolbar, 0, 1);
            layout.Controls.Add(canvasShell, 0, 2);
            Controls.Add(layout);

            UpdateLinkKindButtons();
            UpdateZoomControls();
            UpdateCommandState();
        }

        public event EventHandler? TopologyChangedByUser;

        public KbNetworkTopology CurrentTopology => CloneTopology(_topology);

        public void ApplyState(KbNetworkTopology? topology)
        {
            _topology = CloneTopology(topology);
            _selectedElementId = string.Empty;
            _selectedLinkId = string.Empty;
            _pendingLinkSourceElementId = string.Empty;
            _canvas.Topology = _topology;
            _canvas.SelectedElementId = _selectedElementId;
            _canvas.SelectedLinkId = _selectedLinkId;
            _canvas.PendingLinkSourceElementId = _pendingLinkSourceElementId;
            _canvas.PendingLinkKind = SelectedLinkKind;
            UpdateCommandState();
            _canvas.Invalidate();
        }

        private Button CreateAddButton(string text, KbNetworkElementKind kind)
        {
            var button = CreateActionButton(text, NetworkIconPainter.CreateDeviceIcon(kind, 22));
            button.Click += (_, _) => AddElement(kind);
            return button;
        }

        private Panel CreateLinkKindPalette()
        {
            var strip = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(250, 252, 255),
                BorderStyle = BorderStyle.FixedSingle,
                ColumnCount = 2,
                RowCount = 1,
                Margin = Padding.Empty
            };
            strip.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            strip.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 132F));
            strip.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            var palette = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(250, 252, 255),
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                AutoScroll = false,
                Padding = new Padding(10, 5, 10, 3),
                Margin = Padding.Empty
            };

            var stripLabelFont = new Font("Segoe UI", 9F, FontStyle.Bold);
            palette.Controls.Add(new Label
            {
                Text = "Тип связи:",
                AutoSize = false,
                Size = new Size(GetStripLabelWidth("Тип связи:", stripLabelFont), 28),
                Font = stripLabelFont,
                ForeColor = KnowledgeBaseWorkspaceVisuals.TextColor,
                TextAlign = ContentAlignment.MiddleLeft,
                Margin = new Padding(0, 0, 8, 0)
            });

            AddLinkKindButton(palette, KbNetworkLinkKind.FiberProfibus, "Profibus, оптоволокно");
            AddLinkKindButton(palette, KbNetworkLinkKind.CopperProfibus, "Profibus, медь");
            AddLinkKindButton(palette, KbNetworkLinkKind.CopperMpi, "MPI, медь");
            AddLinkKindButton(palette, KbNetworkLinkKind.FiberProfinet, "Profinet, оптоволокно");
            AddLinkKindButton(palette, KbNetworkLinkKind.CopperProfinet, "Profinet, медь");

            strip.Controls.Add(palette, 0, 0);
            strip.Controls.Add(CreateZoomControls(), 1, 0);
            return strip;
        }

        private static int GetStripLabelWidth(string text, Font font)
        {
            Size textSize = TextRenderer.MeasureText(text, font, Size.Empty, TextFormatFlags.NoPadding);
            return textSize.Width + 14;
        }

        private void AddLinkKindButton(Control palette, KbNetworkLinkKind kind, string text)
        {
            var button = new LinkKindPaletteButton(kind, text)
            {
                Size = new Size(LinkKindPaletteButton.GetPreferredButtonWidth(text), 28),
                Margin = new Padding(4, 0, 0, 0)
            };
            button.Click += (_, _) => SelectLinkKind(kind);
            _linkKindButtons.Add(button);
            palette.Controls.Add(button);
        }

        private Control CreateZoomControls()
        {
            var panel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(250, 252, 255),
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                Padding = new Padding(4, 5, 4, 3),
                Margin = Padding.Empty
            };

            Button zoomOutButton = CreateZoomButton("-", (_, _) => _canvas.ZoomOutFromCenter());
            Button zoomInButton = CreateZoomButton("+", (_, _) => _canvas.ZoomInFromCenter());

            _cmbZoom.DropDownStyle = ComboBoxStyle.DropDown;
            _cmbZoom.FlatStyle = FlatStyle.Standard;
            _cmbZoom.Font = new Font("Segoe UI", 8.5F, FontStyle.Regular);
            _cmbZoom.Size = new Size(62, 28);
            _cmbZoom.Margin = new Padding(2, 0, 2, 0);
            _cmbZoom.Items.AddRange(["35%", "50%", "75%", "100%", "125%", "150%", "200%", "250%"]);
            _cmbZoom.SelectedIndexChanged += (_, _) => ApplyZoomFromCombo();
            _cmbZoom.Leave += (_, _) => ApplyZoomFromCombo();
            _cmbZoom.KeyDown += (_, e) =>
            {
                if (e.KeyCode != Keys.Enter)
                    return;

                ApplyZoomFromCombo();
                e.SuppressKeyPress = true;
            };

            panel.Controls.Add(zoomOutButton);
            panel.Controls.Add(_cmbZoom);
            panel.Controls.Add(zoomInButton);
            return panel;
        }

        private static Button CreateZoomButton(string text, EventHandler clickHandler)
        {
            var button = new Button
            {
                Text = text,
                Size = new Size(28, 28),
                FlatStyle = FlatStyle.Standard,
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                Margin = new Padding(2, 0, 2, 0),
                TabStop = false,
                UseVisualStyleBackColor = true
            };
            button.Click += clickHandler;
            return button;
        }

        private void UpdateZoomControls()
        {
            _updatingZoomControls = true;
            _cmbZoom.Text = FormatZoomPercent(_canvas.ZoomFactor);
            _updatingZoomControls = false;
        }

        private void ApplyZoomFromCombo()
        {
            if (_updatingZoomControls)
                return;

            if (TryParseZoomPercent(_cmbZoom.Text, out int zoomPercent))
                _canvas.ZoomToPercent(zoomPercent);

            UpdateZoomControls();
        }

        private static string FormatZoomPercent(float zoomFactor) =>
            FormattableString.Invariant($"{(int)Math.Round(zoomFactor * 100F)}%");

        private static bool TryParseZoomPercent(string text, out int zoomPercent)
        {
            string normalized = text.Trim().TrimEnd('%').Trim();
            if (!float.TryParse(normalized, NumberStyles.Float, CultureInfo.CurrentCulture, out float value) &&
                !float.TryParse(normalized, NumberStyles.Float, CultureInfo.InvariantCulture, out value))
            {
                zoomPercent = 100;
                return false;
            }

            zoomPercent = Math.Clamp((int)Math.Round(value), 35, 250);
            return true;
        }

        private void SelectLinkKind(KbNetworkLinkKind kind)
        {
            _selectedLinkKind = kind;
            _canvas.PendingLinkKind = kind;
            UpdateLinkKindButtons();

            KbNetworkLink? selectedLink = FindLink(_selectedLinkId);
            if (selectedLink != null && selectedLink.Kind != kind)
            {
                selectedLink.Kind = kind;
                CommitTopologyChange();
                return;
            }

            UpdateCommandState();
            _canvas.Invalidate();
        }

        private void UpdateLinkKindButtons()
        {
            foreach (LinkKindPaletteButton button in _linkKindButtons)
                button.Selected = button.Kind == _selectedLinkKind;
        }

        private static Button CreateActionButton(string text, Image? image = null)
        {
            var button = new Button
            {
                Text = text,
                AutoSize = true,
                Height = 32,
                MinimumSize = new Size(image == null ? 68 : 82, 32),
                FlatStyle = FlatStyle.Standard,
                Margin = new Padding(0, 0, 8, 6),
                Padding = image == null ? new Padding(6, 0, 6, 0) : new Padding(5, 0, 8, 0),
                TextImageRelation = TextImageRelation.ImageBeforeText,
                ImageAlign = ContentAlignment.MiddleLeft,
                TextAlign = ContentAlignment.MiddleCenter
            };
            button.Image = image;
            return button;
        }

        private KbNetworkLinkKind SelectedLinkKind =>
            _selectedLinkKind;

        private void AddElement(KbNetworkElementKind kind, Point? preferredCanvasLocation = null)
        {
            Point location = preferredCanvasLocation.HasValue
                ? _canvas.GetElementDropLocation(preferredCanvasLocation.Value)
                : FindNextElementLocation();
            var element = new KbNetworkElement
            {
                ElementId = Guid.NewGuid().ToString("N"),
                Kind = kind,
                Name = kind == KbNetworkElementKind.ExternalConnection ? string.Empty : BuildDefaultName(kind),
                X = location.X,
                Y = location.Y
            };

            using var dialog = new NetworkElementDialog(element, _topology.Elements, focusIpAddress: true);
            if (dialog.ShowDialog(FindForm()) != DialogResult.OK)
                return;

            element.Kind = dialog.ElementKind;
            element.Name = dialog.ElementName;
            element.IpAddress = dialog.IpAddress;
            element.AdditionalIpAddresses = dialog.AdditionalIpAddresses;

            _topology.Elements.Add(element);
            SelectElement(element.ElementId);
            CommitTopologyChange();
        }

        private void ShowCanvasContextMenu(Point location, Point canvasLocation, string linkId)
        {
            _canvasContextMenu.Items.Clear();

            ToolStripMenuItem addMenu = CreateAddContextMenu(canvasLocation);
            _canvasContextMenu.Items.Add(addMenu);
            _canvasContextMenu.Items.Add(new ToolStripSeparator());

            bool hasSelection = !string.IsNullOrWhiteSpace(_selectedElementId);
            bool hasPendingLink = !string.IsNullOrWhiteSpace(_pendingLinkSourceElementId);
            bool hasContextLink = !string.IsNullOrWhiteSpace(linkId);
            bool useElementSelection = hasSelection && !hasContextLink;
            bool canCompleteLink = hasPendingLink &&
                useElementSelection &&
                !string.Equals(_pendingLinkSourceElementId, _selectedElementId, StringComparison.Ordinal);

            ToolStripMenuItem editItem = CreateContextMenuItem(
                "Изменить",
                () => EditSelectedElement(),
                useElementSelection);
            ToolStripMenuItem linkItem = canCompleteLink
                ? CreateContextMenuItem("Завершить связь", () => CompleteLink(_selectedElementId), true)
                : CreateContextMenuItem("Связать", () => BeginOrCancelLinkMode(), useElementSelection);
            ToolStripMenuItem deleteItem = CreateContextMenuItem(
                "Удалить",
                () => DeleteSelectedElement(),
                useElementSelection);
            ToolStripMenuItem deleteLinkItem = CreateContextMenuItem(
                "Удалить связь",
                () => DeleteLink(linkId),
                hasContextLink);
            ToolStripMenuItem linkKindMenu = CreateLinkKindContextMenu(linkId);

            _canvasContextMenu.Items.Add(editItem);
            _canvasContextMenu.Items.Add(linkItem);
            if (hasContextLink)
            {
                _canvasContextMenu.Items.Add(linkKindMenu);
                _canvasContextMenu.Items.Add(deleteLinkItem);
            }
            _canvasContextMenu.Items.Add(deleteItem);

            if (hasPendingLink)
            {
                _canvasContextMenu.Items.Add(new ToolStripSeparator());
                _canvasContextMenu.Items.Add(CreateContextMenuItem("Отмена связи", CancelLinkMode, true));
            }

            _canvasContextMenu.Show(_canvas, location);
        }

        private static ToolStripMenuItem CreateContextMenuItem(
            string text,
            Action action,
            bool enabled = true)
        {
            var item = new ToolStripMenuItem(text)
            {
                Enabled = enabled
            };
            item.Click += (_, _) => action();
            return item;
        }

        private ToolStripMenuItem CreateLinkKindContextMenu(string linkId)
        {
            KbNetworkLink? link = _topology.Links.FirstOrDefault(candidate =>
                string.Equals(candidate.LinkId, linkId, StringComparison.Ordinal));
            var menu = new ToolStripMenuItem("Тип связи")
            {
                Enabled = link != null
            };

            foreach (LinkKindOption option in GetLinkKindOptions())
            {
                ToolStripMenuItem item = CreateContextMenuItem(
                    option.Label,
                    () => ChangeLinkKind(linkId, option.Kind),
                    link != null);
                item.Checked = link?.Kind == option.Kind;
                menu.DropDownItems.Add(item);
            }

            return menu;
        }

        private void ChangeLinkKind(string linkId, KbNetworkLinkKind kind)
        {
            KbNetworkLink? link = FindLink(linkId);
            if (link == null || link.Kind == kind)
                return;

            link.Kind = kind;
            _selectedLinkKind = kind;
            _canvas.PendingLinkKind = kind;
            UpdateLinkKindButtons();
            CommitTopologyChange();
        }

        private ToolStripMenuItem CreateAddContextMenu(Point location)
        {
            var addMenu = new ToolStripMenuItem("Добавить");
            foreach ((string text, KbNetworkElementKind kind) in GetAddElementOptions())
            {
                ToolStripMenuItem item = CreateContextMenuItem(text, () => AddElement(kind, location));
                addMenu.DropDownItems.Add(item);
            }

            return addMenu;
        }

        private static IReadOnlyList<(string Text, KbNetworkElementKind Kind)> GetAddElementOptions() =>
        [
            ("PLC", KbNetworkElementKind.Plc),
            ("ПЧ", KbNetworkElementKind.FrequencyConverter),
            ("SCALANCE", KbNetworkElementKind.Scalance),
            ("АРМ", KbNetworkElementKind.Arm),
            ("HMI", KbNetworkElementKind.Hmi),
            ("Сервер", KbNetworkElementKind.Server),
            ("ET", KbNetworkElementKind.Et200),
            ("OLM", KbNetworkElementKind.Olm),
            ("Внешняя связь", KbNetworkElementKind.ExternalConnection)
        ];

        private Point FindNextElementLocation()
        {
            int index = _topology.Elements.Count;
            int x = 32 + (index % 5) * 158;
            int y = 32 + (index / 5) * 130;
            return _canvas.GetSnappedElementLocation(new Point(x, y));
        }

        private string BuildDefaultName(KbNetworkElementKind kind)
        {
            string prefix = kind switch
            {
                KbNetworkElementKind.Plc => "PLC",
                KbNetworkElementKind.FrequencyConverter => "ПЧ",
                KbNetworkElementKind.Scalance => "SCALANCE",
                KbNetworkElementKind.Arm => "ARM",
                KbNetworkElementKind.Hmi => "HMI",
                KbNetworkElementKind.Server => "SRV",
                KbNetworkElementKind.Et200 => "ET",
                KbNetworkElementKind.Olm => "OLM",
                KbNetworkElementKind.ExternalConnection => "Внешняя связь",
                _ => "DEV"
            };
            int number = _topology.Elements.Count(element => element.Kind == kind) + 1;
            return $"{prefix}-{number:00}";
        }

        private void BeginOrCancelLinkMode()
        {
            if (string.IsNullOrWhiteSpace(_selectedElementId))
            {
                if (!string.IsNullOrWhiteSpace(_pendingLinkSourceElementId))
                    CancelLinkMode();

                return;
            }

            if (!string.IsNullOrWhiteSpace(_pendingLinkSourceElementId) &&
                !string.Equals(_pendingLinkSourceElementId, _selectedElementId, StringComparison.Ordinal))
            {
                CompleteLink(_selectedElementId);
                return;
            }

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

        private void CancelLinkMode()
        {
            _pendingLinkSourceElementId = string.Empty;
            _canvas.PendingLinkSourceElementId = string.Empty;
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
                string linkId = Guid.NewGuid().ToString("N");
                _topology.Links.Add(new KbNetworkLink
                {
                    LinkId = linkId,
                    FromElementId = _pendingLinkSourceElementId,
                    ToElementId = targetElementId,
                    Kind = SelectedLinkKind
                });
                _selectedLinkId = linkId;
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

            using var dialog = new NetworkElementDialog(
                element,
                _topology.Elements.Where(candidate => !string.Equals(candidate.ElementId, element.ElementId, StringComparison.Ordinal)));
            if (dialog.ShowDialog(FindForm()) != DialogResult.OK)
                return;

            element.Kind = dialog.ElementKind;
            element.Name = dialog.ElementName;
            element.IpAddress = dialog.IpAddress;
            element.AdditionalIpAddresses = dialog.AdditionalIpAddresses;
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
            _selectedLinkId = string.Empty;
            SelectElement(string.Empty);
            CommitTopologyChange();
        }

        private void DeleteSelection()
        {
            if (!string.IsNullOrWhiteSpace(_selectedElementId))
            {
                DeleteSelectedElement();
                return;
            }

            if (!string.IsNullOrWhiteSpace(_selectedLinkId))
                DeleteLink(_selectedLinkId);
        }

        private void DeleteLink(string linkId)
        {
            if (string.IsNullOrWhiteSpace(linkId))
                return;

            int removed = _topology.Links.RemoveAll(link =>
                string.Equals(link.LinkId, linkId, StringComparison.Ordinal));
            if (removed == 0)
                return;

            if (string.Equals(_selectedLinkId, linkId, StringComparison.Ordinal))
            {
                _selectedLinkId = string.Empty;
                _canvas.SelectedLinkId = string.Empty;
            }

            CommitTopologyChange();
        }

        private void SelectElement(string elementId)
        {
            _selectedElementId = elementId;
            if (!string.IsNullOrWhiteSpace(elementId))
            {
                _selectedLinkId = string.Empty;
                _canvas.SelectedLinkId = string.Empty;
            }

            _canvas.SelectedElementId = elementId;
            UpdateCommandState();
            _canvas.Invalidate();
        }

        private KbNetworkElement? FindElement(string elementId) =>
            _topology.Elements.FirstOrDefault(element => string.Equals(element.ElementId, elementId, StringComparison.Ordinal));

        private KbNetworkLink? FindLink(string linkId) =>
            _topology.Links.FirstOrDefault(link => string.Equals(link.LinkId, linkId, StringComparison.Ordinal));

        private void CommitTopologyChange()
        {
            _topology = KnowledgeBaseDataService.NormalizeNetworkTopology(_topology);
            _canvas.Topology = _topology;
            if (FindElement(_selectedElementId) == null)
                _selectedElementId = string.Empty;
            if (FindLink(_selectedLinkId) == null)
                _selectedLinkId = string.Empty;
            _canvas.SelectedElementId = _selectedElementId;
            _canvas.SelectedLinkId = _selectedLinkId;
            UpdateCommandState();
            _canvas.Invalidate();
            TopologyChangedByUser?.Invoke(this, EventArgs.Empty);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _canvasContextMenu.Dispose();
            }

            base.Dispose(disposing);
        }

        private void UpdateCommandState()
        {
            bool hasSelection = !string.IsNullOrWhiteSpace(_selectedElementId);
            bool hasSelectedLink = !string.IsNullOrWhiteSpace(_selectedLinkId);
            bool hasPendingLink = !string.IsNullOrWhiteSpace(_pendingLinkSourceElementId);
            _btnLink.Enabled = hasSelection || hasPendingLink;
            _btnEdit.Enabled = hasSelection;
            _btnDelete.Enabled = hasSelection || hasSelectedLink;
            _btnLink.Text = hasPendingLink
                ? hasSelection && !string.Equals(_pendingLinkSourceElementId, _selectedElementId, StringComparison.Ordinal)
                    ? "Завершить связь"
                    : "Отмена связи"
                : "Связь";
            _btnDelete.Text = hasSelectedLink && !hasSelection
                ? "Удалить связь"
                : "Удалить";
            _lblSummary.Text = hasPendingLink
                ? "Выберите второй элемент для связи."
                : hasSelectedLink
                    ? $"Выбрана связь: {GetLinkKindDisplayName(SelectedLinkKind)}."
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
                        AdditionalIpAddresses = element.AdditionalIpAddresses?.ToList() ?? [],
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
                        Kind = link.Kind,
                        Label = link.Label
                    })
                    .ToList() ?? new List<KbNetworkLink>()
            });

        private static List<string> NormalizeAdditionalIpAddresses(IEnumerable<string>? ipAddresses)
        {
            var result = new List<string>();
            var used = new HashSet<string>(StringComparer.Ordinal);
            foreach (string? ipAddress in ipAddresses ?? Enumerable.Empty<string>())
            {
                if (!TryNormalizeIpAddress(ipAddress ?? string.Empty, out string normalizedIpAddress) ||
                    !used.Add(normalizedIpAddress))
                {
                    continue;
                }

                result.Add(normalizedIpAddress);
            }

            return result;
        }

        private static Dictionary<string, string> CreateReservedIpAddressMap(
            IEnumerable<KbNetworkElement> elements,
            string excludedElementId = "",
            string excludedIpAddress = "")
        {
            var result = new Dictionary<string, string>(StringComparer.Ordinal);
            TryNormalizeIpAddress(excludedIpAddress, out string normalizedExcludedIpAddress);
            foreach (KbNetworkElement element in elements)
            {
                string elementName = string.IsNullOrWhiteSpace(element.Name) ? "без названия" : element.Name.Trim();
                AddReservedIpAddress(result, element, element.IpAddress, elementName, excludedElementId, normalizedExcludedIpAddress);
                foreach (string additionalIpAddress in element.AdditionalIpAddresses ?? [])
                    AddReservedIpAddress(result, element, additionalIpAddress, elementName, excludedElementId, normalizedExcludedIpAddress);
            }

            return result;
        }

        private static void AddReservedIpAddress(
            Dictionary<string, string> reservedIpAddresses,
            KbNetworkElement element,
            string ipAddress,
            string elementName,
            string excludedElementId,
            string normalizedExcludedIpAddress)
        {
            if (!TryNormalizeIpAddress(ipAddress, out string normalizedIpAddress) ||
                string.IsNullOrWhiteSpace(normalizedIpAddress) ||
                reservedIpAddresses.ContainsKey(normalizedIpAddress))
            {
                return;
            }

            if (!string.IsNullOrWhiteSpace(normalizedExcludedIpAddress) &&
                string.Equals(element.ElementId, excludedElementId, StringComparison.Ordinal) &&
                string.Equals(normalizedIpAddress, normalizedExcludedIpAddress, StringComparison.Ordinal))
            {
                return;
            }

            reservedIpAddresses.Add(normalizedIpAddress, elementName);
        }

        private static bool TryNormalizeIpAddress(string ipAddress, out string normalizedIpAddress)
        {
            normalizedIpAddress = string.Empty;
            string[] parts = ipAddress.Trim().Split('.');
            if (parts.Length != 4)
                return false;

            var normalizedParts = new string[4];
            for (int index = 0; index < parts.Length; index++)
            {
                string part = parts[index].Trim();
                if (part.Length == 0 || !int.TryParse(part, out int value) || value < 0 || value > 255)
                    return false;

                normalizedParts[index] = value.ToString(CultureInfo.InvariantCulture);
            }

            normalizedIpAddress = string.Join(".", normalizedParts);
            return true;
        }

        private static string[] SplitIpAddress(string ipAddress)
        {
            string[] result = ["", "", "", ""];
            string[] parts = ipAddress.Trim().Split('.');
            if (parts.Length != result.Length)
                return result;

            for (int index = 0; index < parts.Length; index++)
            {
                string part = parts[index].Trim();
                if (part.Length is 0 or > 3 || part.Any(static value => !char.IsDigit(value)))
                    return ["", "", "", ""];

                result[index] = part;
            }

            return result;
        }

        private static List<LinkKindOption> GetLinkKindOptions() =>
        [
            new(KbNetworkLinkKind.FiberProfibus, "Оптоволоконная линия Profibus"),
            new(KbNetworkLinkKind.CopperProfibus, "Медная линия Profibus"),
            new(KbNetworkLinkKind.CopperMpi, "Медная линия MPI"),
            new(KbNetworkLinkKind.FiberProfinet, "Оптоволоконная линия Profinet"),
            new(KbNetworkLinkKind.CopperProfinet, "Медная линия Profinet")
        ];

        private static string GetLinkKindDisplayName(KbNetworkLinkKind kind) => kind switch
        {
            KbNetworkLinkKind.FiberProfibus => "Profibus, оптоволокно",
            KbNetworkLinkKind.CopperProfibus => "Profibus, медь",
            KbNetworkLinkKind.CopperMpi => "MPI, медь",
            KbNetworkLinkKind.FiberProfinet => "Profinet, оптоволокно",
            _ => "Profinet, медь"
        };

        private sealed record LinkKindOption(KbNetworkLinkKind Kind, string Label)
        {
            public override string ToString() => Label;
        }

        private static (Color Color, DashStyle DashStyle) GetLinkStyle(KbNetworkLinkKind kind) => kind switch
        {
            KbNetworkLinkKind.FiberProfibus => (Color.FromArgb(255, 0, 255), DashStyle.Dash),
            KbNetworkLinkKind.CopperProfibus => (Color.FromArgb(255, 0, 255), DashStyle.Solid),
            KbNetworkLinkKind.CopperMpi => (Color.FromArgb(255, 0, 0), DashStyle.Solid),
            KbNetworkLinkKind.FiberProfinet => (Color.FromArgb(0, 217, 36), DashStyle.Dash),
            _ => (Color.FromArgb(0, 217, 36), DashStyle.Solid)
        };

        private sealed class LinkKindPaletteButton : Button
        {
            private const int LineSampleLeft = 10;
            private const int LineSampleRight = 42;
            private const int TextLeft = 50;
            private const int TextRightPadding = 16;
            private const int MeasuredTextSafetyPadding = 14;

            private bool _selected;

            public LinkKindPaletteButton(KbNetworkLinkKind kind, string text)
            {
                Kind = kind;
                Text = text;
                Cursor = Cursors.Hand;
                FlatStyle = FlatStyle.Flat;
                Font = new Font("Segoe UI", 8F, FontStyle.Regular);
                TabStop = false;
                UseVisualStyleBackColor = false;
            }

            public KbNetworkLinkKind Kind { get; }

            public static int GetPreferredButtonWidth(string text)
            {
                using var regularFont = new Font("Segoe UI", 8F, FontStyle.Regular);
                using var selectedFont = new Font("Segoe UI", 8F, FontStyle.Bold);
                int regularWidth = TextRenderer.MeasureText(text, regularFont, Size.Empty, TextFormatFlags.NoPadding).Width;
                int selectedWidth = TextRenderer.MeasureText(text, selectedFont, Size.Empty, TextFormatFlags.NoPadding).Width;
                return TextLeft + Math.Max(regularWidth, selectedWidth) + TextRightPadding + MeasuredTextSafetyPadding;
            }

            public bool Selected
            {
                get => _selected;
                set
                {
                    if (_selected == value)
                        return;

                    _selected = value;
                    Invalidate();
                }
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                Rectangle bounds = ClientRectangle;
                bounds.Width--;
                bounds.Height--;

                using var fill = new SolidBrush(Selected ? Color.FromArgb(235, 244, 255) : Color.FromArgb(252, 253, 254));
                using var border = new Pen(Selected ? Color.FromArgb(85, 135, 205) : Color.FromArgb(178, 184, 190), 1F);
                e.Graphics.FillRectangle(fill, bounds);
                e.Graphics.DrawRectangle(border, bounds);

                (Color color, DashStyle dashStyle) = GetLinkStyle(Kind);
                using var linePen = new Pen(color, 3F)
                {
                    DashStyle = dashStyle
                };
                if (linePen.DashStyle == DashStyle.Dash)
                    linePen.DashPattern = [4F, 3F];

                int lineY = Height / 2;
                e.Graphics.DrawLine(linePen, LineSampleLeft, lineY, LineSampleRight, lineY);

                using var textFont = new Font(Font.FontFamily, Font.Size, Selected ? FontStyle.Bold : FontStyle.Regular);
                TextRenderer.DrawText(
                    e.Graphics,
                    Text,
                    textFont,
                    new Rectangle(TextLeft, 0, Width - TextLeft - TextRightPadding, Height),
                    KnowledgeBaseWorkspaceVisuals.TextColor,
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
            }
        }

        private sealed class NetworkIpAddressDialog : Form
        {
            private readonly TextBox[] _txtIpOctets;
            private readonly Dictionary<string, string> _reservedIpAddresses;
            private bool _updatingIpOctets;

            public NetworkIpAddressDialog(
                Dictionary<string, string> reservedIpAddresses,
                string initialIpAddress = "",
                string title = "Добавить IP-адрес")
            {
                _reservedIpAddresses = reservedIpAddresses;
                Text = title;
                ClientSize = new Size(360, 118);
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
                    RowCount = 2
                };
                layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 86F));
                layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
                layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
                layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

                _txtIpOctets = CreateIpOctetBoxes(initialIpAddress);
                FlowLayoutPanel ipAddressPanel = CreateIpAddressPanel(_txtIpOctets);

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
                    AutoSize = true,
                    MinimumSize = new Size(82, 28),
                    Margin = new Padding(8, 0, 0, 0)
                };
                btnOk.Click += (_, _) => TryAccept();
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

                layout.Controls.Add(CreateLabel("IP"), 0, 0);
                layout.Controls.Add(ipAddressPanel, 1, 0);
                layout.Controls.Add(buttons, 1, 1);
                Controls.Add(layout);

                AcceptButton = btnOk;
                CancelButton = btnCancel;
                Shown += (_, _) => FocusFirstIpOctet(_txtIpOctets);
            }

            public string IpAddress { get; private set; } = string.Empty;

            private void TryAccept()
            {
                if (!TryBuildIpAddress(out string ipAddress, out string validationError))
                {
                    ShowValidationWarning(validationError);
                    return;
                }

                if (string.IsNullOrWhiteSpace(ipAddress))
                {
                    ShowValidationWarning("Укажите IP-адрес.");
                    return;
                }

                if (_reservedIpAddresses.TryGetValue(ipAddress, out string existingElementName))
                {
                    ShowValidationWarning(
                        $"IP-адрес {ipAddress} уже используется элементом \"{existingElementName}\". Укажите другой IP-адрес.");
                    return;
                }

                IpAddress = ipAddress;
                DialogResult = DialogResult.OK;
                Close();
            }

            private void ShowValidationWarning(string message)
            {
                MessageBox.Show(
                    this,
                    message,
                    "Проверка IP-адреса",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                FocusFirstIpOctet(_txtIpOctets);
            }

            private bool TryBuildIpAddress(out string ipAddress, out string validationError)
            {
                string[] octets = _txtIpOctets
                    .Select(static textBox => textBox.Text.Trim())
                    .ToArray();

                ipAddress = string.Empty;
                validationError = string.Empty;

                if (octets.All(static octet => octet.Length == 0))
                    return true;

                if (octets.Any(static octet => octet.Length == 0))
                {
                    validationError = "Заполните все 4 октета IP-адреса.";
                    return false;
                }

                var normalizedOctets = new string[4];
                for (int index = 0; index < octets.Length; index++)
                {
                    if (!int.TryParse(octets[index], out int value) || value < 0 || value > 255)
                    {
                        validationError = "Каждый октет IP-адреса должен быть числом от 0 до 255.";
                        return false;
                    }

                    normalizedOctets[index] = value.ToString(CultureInfo.InvariantCulture);
                }

                ipAddress = string.Join(".", normalizedOctets);
                return true;
            }

            private TextBox[] CreateIpOctetBoxes(string ipAddress)
            {
                string[] values = SplitIpAddress(ipAddress);
                var textBoxes = new TextBox[4];
                for (int index = 0; index < textBoxes.Length; index++)
                {
                    int octetIndex = index;
                    var textBox = new TextBox
                    {
                        Dock = DockStyle.None,
                        Width = 42,
                        MaxLength = 3,
                        Text = values[index],
                        TextAlign = HorizontalAlignment.Center,
                        Margin = new Padding(0, 0, 4, 0)
                    };

                    textBox.KeyPress += (_, e) =>
                    {
                        if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar))
                            e.Handled = true;
                    };
                    textBox.KeyDown += (_, e) => HandleIpOctetKeyDown(textBoxes, octetIndex, e);
                    textBox.TextChanged += (_, _) => HandleIpOctetTextChanged(textBoxes, octetIndex);

                    textBoxes[index] = textBox;
                }

                return textBoxes;
            }

            private static FlowLayoutPanel CreateIpAddressPanel(TextBox[] octets)
            {
                var panel = new FlowLayoutPanel
                {
                    Dock = DockStyle.Top,
                    AutoSize = true,
                    WrapContents = false,
                    FlowDirection = FlowDirection.LeftToRight,
                    Margin = new Padding(0, 0, 0, 12)
                };

                for (int index = 0; index < octets.Length; index++)
                {
                    panel.Controls.Add(octets[index]);
                    if (index < octets.Length - 1)
                        panel.Controls.Add(CreateIpSeparator());
                }

                return panel;
            }

            private static Label CreateIpSeparator() =>
                new()
                {
                    Text = ".",
                    AutoSize = true,
                    Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold),
                    Margin = new Padding(0, 4, 4, 0)
                };

            private void HandleIpOctetTextChanged(TextBox[] ipOctets, int octetIndex)
            {
                if (_updatingIpOctets)
                    return;

                TextBox textBox = ipOctets[octetIndex];
                string digits = new(textBox.Text.Where(char.IsDigit).Take(3).ToArray());
                if (!string.Equals(textBox.Text, digits, StringComparison.Ordinal))
                {
                    _updatingIpOctets = true;
                    int selectionStart = Math.Min(textBox.SelectionStart, digits.Length);
                    textBox.Text = digits;
                    textBox.SelectionStart = selectionStart;
                    _updatingIpOctets = false;
                }

                if (digits.Length == 3 && octetIndex < ipOctets.Length - 1)
                {
                    ipOctets[octetIndex + 1].Focus();
                    ipOctets[octetIndex + 1].SelectAll();
                }
            }

            private void HandleIpOctetKeyDown(TextBox[] ipOctets, int octetIndex, KeyEventArgs e)
            {
                if (e.KeyCode == Keys.OemPeriod || e.KeyCode == Keys.Decimal)
                {
                    FocusIpOctet(ipOctets, octetIndex + 1);
                    e.SuppressKeyPress = true;
                    return;
                }

                if (e.KeyCode == Keys.Back &&
                    octetIndex > 0 &&
                    ipOctets[octetIndex].SelectionStart == 0 &&
                    ipOctets[octetIndex].SelectionLength == 0)
                {
                    FocusIpOctet(ipOctets, octetIndex - 1, selectEnd: true);
                    e.SuppressKeyPress = true;
                }
            }

            private static void FocusIpOctet(TextBox[] ipOctets, int octetIndex, bool selectEnd = false)
            {
                if (octetIndex < 0 || octetIndex >= ipOctets.Length)
                    return;

                TextBox textBox = ipOctets[octetIndex];
                textBox.Focus();

                if (selectEnd)
                    textBox.SelectionStart = textBox.Text.Length;
                else
                    textBox.SelectAll();
            }

            private static void FocusFirstIpOctet(TextBox[] ipOctets)
            {
                ipOctets[0].Focus();
                ipOctets[0].SelectAll();
            }

            private static Label CreateLabel(string text) =>
                new()
                {
                    Text = text,
                    AutoSize = true,
                    Dock = DockStyle.Top,
                    Margin = new Padding(0, 3, 8, 8)
                };
        }

        private sealed class NetworkElementDialog : Form
        {
            private readonly ComboBox _cmbKind;
            private readonly Label _lblName;
            private readonly Label _lblIp;
            private readonly Label _lblAdditionalIp;
            private readonly TextBox _txtName;
            private readonly TextBox[] _txtIpOctets;
            private readonly TextBox[] _txtAdditionalIpOctets;
            private readonly FlowLayoutPanel _ipAddressPanel;
            private readonly FlowLayoutPanel _additionalIpAddressPanel;
            private readonly Dictionary<string, string> _reservedIpAddresses;
            private bool _updatingIpOctets;

            public NetworkElementDialog(
                KbNetworkElement element,
                IEnumerable<KbNetworkElement> reservedElements,
                bool focusIpAddress = false)
            {
                _reservedIpAddresses = CreateReservedIpAddressMap(reservedElements);
                Text = "Элемент сети";
                ClientSize = new Size(360, 212);
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
                    RowCount = 5
                };
                layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 86F));
                layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
                layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
                layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
                layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
                layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
                layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

                List<KindOption> kindOptions = GetKindOptions();
                _cmbKind = new ComboBox
                {
                    Dock = DockStyle.Top,
                    DropDownStyle = ComboBoxStyle.DropDownList,
                    Margin = new Padding(0, 0, 0, 8)
                };
                _cmbKind.Items.AddRange(kindOptions.Cast<object>().ToArray());
                _cmbKind.SelectedItem = kindOptions.FirstOrDefault(option => option.Kind == element.Kind) ??
                    kindOptions.First(option => option.Kind == KbNetworkElementKind.Other);

                _txtName = new TextBox
                {
                    Dock = DockStyle.Top,
                    Text = element.Name,
                    Margin = new Padding(0, 0, 0, 8)
                };
                _txtIpOctets = CreateIpOctetBoxes(element.IpAddress);
                _ipAddressPanel = CreateIpAddressPanel(_txtIpOctets);
                _txtAdditionalIpOctets = CreateIpOctetBoxes(element.AdditionalIpAddresses?.FirstOrDefault() ?? string.Empty);
                _additionalIpAddressPanel = CreateIpAddressPanel(_txtAdditionalIpOctets);

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
                    AutoSize = true,
                    MinimumSize = new Size(82, 28),
                    Margin = new Padding(8, 0, 0, 0)
                };
                btnOk.Click += (_, _) => TryAccept();
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

                _lblName = CreateLabel("Название");
                _lblIp = CreateLabel("IP");
                _lblAdditionalIp = CreateLabel("Доп. IP");

                layout.Controls.Add(CreateLabel("Тип"), 0, 0);
                layout.Controls.Add(_cmbKind, 1, 0);
                layout.Controls.Add(_lblName, 0, 1);
                layout.Controls.Add(_txtName, 1, 1);
                layout.Controls.Add(_lblIp, 0, 2);
                layout.Controls.Add(_ipAddressPanel, 1, 2);
                layout.Controls.Add(_lblAdditionalIp, 0, 3);
                layout.Controls.Add(_additionalIpAddressPanel, 1, 3);
                layout.Controls.Add(buttons, 1, 4);
                Controls.Add(layout);

                AcceptButton = btnOk;
                CancelButton = btnCancel;
                _cmbKind.SelectedIndexChanged += (_, _) => UpdateElementFieldMode();
                UpdateElementFieldMode();

                if (focusIpAddress)
                {
                    Shown += (_, _) =>
                    {
                        if (IsExternalConnectionSelected)
                            FocusNameText();
                        else
                            FocusFirstIpOctet(_txtIpOctets);
                    };
                }
            }

            public KbNetworkElementKind ElementKind =>
                _cmbKind.SelectedItem is KindOption option ? option.Kind : KbNetworkElementKind.Other;

            public string ElementName => _txtName.Text.Trim();

            public string IpAddress
            {
                get
                {
                    if (IsExternalConnectionSelected)
                        return string.Empty;

                    return TryBuildIpAddress(_txtIpOctets, allowEmpty: true, out string ipAddress, out _)
                        ? ipAddress
                        : BuildRawIpAddress(_txtIpOctets);
                }
            }

            public List<string> AdditionalIpAddresses
            {
                get
                {
                    if (IsExternalConnectionSelected)
                        return [];

                    return TryBuildIpAddress(_txtAdditionalIpOctets, allowEmpty: true, out string ipAddress, out _) &&
                        !string.IsNullOrWhiteSpace(ipAddress)
                        ? [ipAddress]
                        : [];
                }
            }

            private void TryAccept()
            {
                if (IsExternalConnectionSelected)
                {
                    DialogResult = DialogResult.OK;
                    Close();
                    return;
                }

                if (!TryBuildIpAddress(_txtIpOctets, allowEmpty: true, out string ipAddress, out string validationError))
                {
                    ShowValidationWarning(validationError, _txtIpOctets);
                    return;
                }

                if (!TryBuildIpAddress(_txtAdditionalIpOctets, allowEmpty: true, out string additionalIpAddress, out validationError))
                {
                    ShowValidationWarning(validationError, _txtAdditionalIpOctets);
                    return;
                }

                if (!string.IsNullOrWhiteSpace(ipAddress) &&
                    string.Equals(ipAddress, additionalIpAddress, StringComparison.Ordinal))
                {
                    ShowValidationWarning("Дополнительный IP-адрес совпадает с основным IP-адресом.", _txtAdditionalIpOctets);
                    return;
                }

                if (!string.IsNullOrWhiteSpace(ipAddress) &&
                    _reservedIpAddresses.TryGetValue(ipAddress, out string existingElementName))
                {
                    ShowValidationWarning(
                        $"IP-адрес {ipAddress} уже используется элементом \"{existingElementName}\". Укажите другой IP-адрес.",
                        _txtIpOctets);
                    return;
                }

                if (!string.IsNullOrWhiteSpace(additionalIpAddress) &&
                    _reservedIpAddresses.TryGetValue(additionalIpAddress, out existingElementName))
                {
                    ShowValidationWarning(
                        $"IP-адрес {additionalIpAddress} уже используется элементом \"{existingElementName}\". Укажите другой IP-адрес.",
                        _txtAdditionalIpOctets);
                    return;
                }

                DialogResult = DialogResult.OK;
                Close();
            }

            private bool IsExternalConnectionSelected =>
                ElementKind == KbNetworkElementKind.ExternalConnection;

            private void UpdateElementFieldMode()
            {
                bool isExternalConnection = IsExternalConnectionSelected;
                _lblName.Text = isExternalConnection ? "Текст" : "Название";
                _txtName.Multiline = isExternalConnection;
                _txtName.AcceptsReturn = isExternalConnection;
                _txtName.WordWrap = isExternalConnection;
                _txtName.ScrollBars = isExternalConnection ? ScrollBars.Vertical : ScrollBars.None;
                _txtName.Height = isExternalConnection ? 72 : 23;

                _lblIp.Visible = !isExternalConnection;
                _ipAddressPanel.Visible = !isExternalConnection;
                _lblAdditionalIp.Visible = !isExternalConnection;
                _additionalIpAddressPanel.Visible = !isExternalConnection;
                ClientSize = new Size(360, isExternalConnection ? 184 : 212);
            }

            private void FocusNameText()
            {
                _txtName.Focus();
                _txtName.SelectAll();
            }

            private void ShowValidationWarning(string message, TextBox[]? focusOctets = null)
            {
                MessageBox.Show(
                    this,
                    message,
                    "Проверка IP-адреса",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                FocusFirstIpOctet(focusOctets ?? _txtIpOctets);
            }

            private static bool TryBuildIpAddress(
                TextBox[] ipOctets,
                bool allowEmpty,
                out string ipAddress,
                out string validationError)
            {
                string[] octets = ipOctets
                    .Select(static textBox => textBox.Text.Trim())
                    .ToArray();

                ipAddress = string.Empty;
                validationError = string.Empty;

                if (octets.All(static octet => octet.Length == 0) && allowEmpty)
                    return true;

                if (octets.Any(static octet => octet.Length == 0))
                {
                    validationError = "Заполните все 4 октета IP-адреса или оставьте IP пустым.";
                    return false;
                }

                var normalizedOctets = new string[4];
                for (int index = 0; index < octets.Length; index++)
                {
                    if (!int.TryParse(octets[index], out int value) || value < 0 || value > 255)
                    {
                        validationError = "Каждый октет IP-адреса должен быть числом от 0 до 255.";
                        return false;
                    }

                    normalizedOctets[index] = value.ToString(CultureInfo.InvariantCulture);
                }

                ipAddress = string.Join(".", normalizedOctets);
                return true;
            }

            private static string BuildRawIpAddress(TextBox[] ipOctets)
            {
                string[] octets = ipOctets
                    .Select(static textBox => textBox.Text.Trim())
                    .ToArray();

                return octets.All(static octet => octet.Length == 0)
                    ? string.Empty
                    : string.Join(".", octets);
            }

            private TextBox[] CreateIpOctetBoxes(string ipAddress)
            {
                string[] values = SplitIpAddress(ipAddress);
                var textBoxes = new TextBox[4];

                for (int index = 0; index < textBoxes.Length; index++)
                {
                    int octetIndex = index;
                    var textBox = new TextBox
                    {
                        Dock = DockStyle.None,
                        Width = 42,
                        MaxLength = 3,
                        Text = values[index],
                        TextAlign = HorizontalAlignment.Center,
                        Margin = new Padding(0, 0, 4, 0)
                    };

                    textBox.KeyPress += (_, e) =>
                    {
                        if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar))
                            e.Handled = true;
                    };
                    textBox.KeyDown += (_, e) => HandleIpOctetKeyDown(textBoxes, octetIndex, e);
                    textBox.TextChanged += (_, _) => HandleIpOctetTextChanged(textBoxes, octetIndex);

                    textBoxes[index] = textBox;
                }

                return textBoxes;
            }

            private static FlowLayoutPanel CreateIpAddressPanel(TextBox[] octets)
            {
                var panel = new FlowLayoutPanel
                {
                    Dock = DockStyle.Top,
                    AutoSize = true,
                    WrapContents = false,
                    FlowDirection = FlowDirection.LeftToRight,
                    Margin = new Padding(0, 0, 0, 12)
                };

                for (int index = 0; index < octets.Length; index++)
                {
                    panel.Controls.Add(octets[index]);
                    if (index < octets.Length - 1)
                        panel.Controls.Add(CreateIpSeparator());
                }

                return panel;
            }

            private static Label CreateIpSeparator() =>
                new()
                {
                    Text = ".",
                    AutoSize = true,
                    Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold),
                    Margin = new Padding(0, 4, 4, 0)
                };

            private void HandleIpOctetTextChanged(TextBox[] ipOctets, int octetIndex)
            {
                if (_updatingIpOctets)
                    return;

                TextBox textBox = ipOctets[octetIndex];
                string digits = new(textBox.Text.Where(char.IsDigit).Take(3).ToArray());
                if (!string.Equals(textBox.Text, digits, StringComparison.Ordinal))
                {
                    _updatingIpOctets = true;
                    int selectionStart = Math.Min(textBox.SelectionStart, digits.Length);
                    textBox.Text = digits;
                    textBox.SelectionStart = selectionStart;
                    _updatingIpOctets = false;
                }

                if (digits.Length == 3 && octetIndex < ipOctets.Length - 1)
                {
                    ipOctets[octetIndex + 1].Focus();
                    ipOctets[octetIndex + 1].SelectAll();
                }
            }

            private void HandleIpOctetKeyDown(TextBox[] ipOctets, int octetIndex, KeyEventArgs e)
            {
                if (e.KeyCode == Keys.OemPeriod || e.KeyCode == Keys.Decimal)
                {
                    FocusIpOctet(ipOctets, octetIndex + 1);
                    e.SuppressKeyPress = true;
                    return;
                }

                if (e.KeyCode == Keys.Back &&
                    octetIndex > 0 &&
                    ipOctets[octetIndex].SelectionStart == 0 &&
                    ipOctets[octetIndex].SelectionLength == 0)
                {
                    FocusIpOctet(ipOctets, octetIndex - 1, selectEnd: true);
                    e.SuppressKeyPress = true;
                }
            }

            private static void FocusIpOctet(TextBox[] ipOctets, int octetIndex, bool selectEnd = false)
            {
                if (octetIndex < 0 || octetIndex >= ipOctets.Length)
                    return;

                TextBox textBox = ipOctets[octetIndex];
                textBox.Focus();

                if (selectEnd)
                    textBox.SelectionStart = textBox.Text.Length;
                else
                    textBox.SelectAll();
            }

            private static void FocusFirstIpOctet(TextBox[] ipOctets)
            {
                ipOctets[0].Focus();
                ipOctets[0].SelectAll();
            }

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
                new(KbNetworkElementKind.FrequencyConverter, "ПЧ / преобразователь частоты"),
                new(KbNetworkElementKind.Scalance, "SCALANCE"),
                new(KbNetworkElementKind.Arm, "АРМ"),
                new(KbNetworkElementKind.Hmi, "HMI"),
                new(KbNetworkElementKind.Server, "Сервер"),
                new(KbNetworkElementKind.Et200, "ET"),
                new(KbNetworkElementKind.Olm, "OLM"),
                new(KbNetworkElementKind.ExternalConnection, "Внешняя связь"),
                new(KbNetworkElementKind.Other, "Другое")
            ];

            private sealed record KindOption(KbNetworkElementKind Kind, string Label)
            {
                public override string ToString() => Label;
            }
        }

        private enum NetworkCommandIconKind
        {
            Link,
            Edit,
            Delete
        }

        private static class NetworkIconPainter
        {
            private static readonly Color ApprovedIconColor = Color.FromArgb(17, 24, 39);

            private const string PlcIconPath = "M64 64V448H448V64H64ZM106.667 405.333V106.667H234.667V405.333H106.667ZM277.333 405.333V106.667H320V405.333H277.333ZM362.667 405.333V106.667H405.333V405.333H362.667ZM213.333 128H128V234.667H213.333V128ZM128 320H149.333V341.333H128V320ZM213.333 320H192V341.333H213.333V320ZM160 277.333H181.333V298.667H160V277.333ZM181.333 362.667H160V384H181.333V362.667Z";
            private const string FrequencyConverterIconPath = "M19 6C20.6569 6 22 7.34315 22 9V15C22 16.6569 20.6569 18 19 18H7V6H19ZM9 16H18V14H9V16ZM6 15H5V13H2V11H5V9H6V15ZM9 13H18V11H9V13ZM9 10H18V8H9V10Z";
            private const string SwitchIconPath = "M170.667 42.668h170.666v128h-64v64h192v42.667h-64v64h64v128H298.667v-128h64v-64H149.333v64h64v128H42.667v-128h64v-64h-64v-42.667h192v-64h-64zm42.666 42.667h85.334V128h-85.334zM85.333 384h85.334v42.667H85.333zm341.334 0h-85.334v42.667h85.334z";
            private const string ArmIconPath = "M6,2C4.89,2 4,2.89 4,4V12C4,13.11 4.89,14 6,14H18C19.11,14 20,13.11 20,12V4C20,2.89 19.11,2 18,2H6M6,4H18V12H6V4M4,15C2.89,15 2,15.89 2,17V20C2,21.11 2.89,22 4,22H20C21.11,22 22,21.11 22,20V17C22,15.89 21.11,15 20,15H4M8,17H20V20H8V17M9,17.75V19.25H13V17.75H9M15,17.75V19.25H19V17.75H15Z";
            private const string HmiIconPath = "M469.333 106.667v298.666H42.666V106.667zm-42.666 64H85.333v192h341.334zM149.333 128h-64v21.333h64z";
            private const string ServerIconPath = "M85.3333 64H320H362.667V106.667V149.333H320V106.667H85.3333V298.667H277.333V341.333H224V384H256V426.667H149.333V384H181.333V341.333H85.3333H42.6666V298.667V106.667V64H85.3333ZM277.333 159.549L202.667 116.43L128 159.549V245.785L202.667 288.905L277.333 245.785V159.549ZM149.333 233.47V183.242L192 207.867V258.11L149.333 233.47ZM213.333 258.11V207.867L256 183.242V233.47L213.333 258.11ZM202.667 141.065L244.521 165.236L202.667 189.392L160.812 165.236L202.667 141.065ZM469.333 170.667V448H298.667V170.667H469.333ZM426.667 213.333H341.333V256H426.667V213.333ZM341.333 405.333V277.333H426.667V405.333H341.333ZM405.333 320V362.667H362.667V320H405.333Z";

            public static Bitmap CreateDeviceIcon(KbNetworkElementKind kind, int size)
            {
                var bitmap = new Bitmap(size, size);
                using Graphics graphics = Graphics.FromImage(bitmap);
                graphics.Clear(Color.Transparent);
                DrawDeviceIcon(graphics, kind, new Rectangle(0, 0, size, size));
                return bitmap;
            }

            public static Bitmap CreateCommandIcon(NetworkCommandIconKind kind)
            {
                const int size = 18;
                var bitmap = new Bitmap(size, size);
                using Graphics graphics = Graphics.FromImage(bitmap);
                graphics.SmoothingMode = SmoothingMode.AntiAlias;
                graphics.Clear(Color.Transparent);

                using var pen = new Pen(Color.FromArgb(42, 61, 79), 1.8F)
                {
                    StartCap = LineCap.Round,
                    EndCap = LineCap.Round,
                    LineJoin = LineJoin.Round
                };
                switch (kind)
                {
                    case NetworkCommandIconKind.Link:
                        graphics.DrawEllipse(pen, 2.2F, 3.5F, 5.5F, 5.5F);
                        graphics.DrawEllipse(pen, 10.3F, 9F, 5.5F, 5.5F);
                        graphics.DrawLine(pen, 7.1F, 7.4F, 10.9F, 10.2F);
                        break;
                    case NetworkCommandIconKind.Edit:
                        graphics.DrawLine(pen, 4F, 13.8F, 12.8F, 5F);
                        graphics.DrawLine(pen, 10.7F, 3.1F, 14.9F, 7.3F);
                        graphics.DrawLine(pen, 3.2F, 14.7F, 7.5F, 13.6F);
                        break;
                    case NetworkCommandIconKind.Delete:
                        graphics.DrawLine(pen, 5.2F, 6.2F, 12.8F, 6.2F);
                        graphics.DrawLine(pen, 7F, 4F, 11F, 4F);
                        graphics.DrawRectangle(pen, 5.7F, 7.5F, 6.6F, 7F);
                        graphics.DrawLine(pen, 7.9F, 9F, 7.9F, 12.7F);
                        graphics.DrawLine(pen, 10.1F, 9F, 10.1F, 12.7F);
                        break;
                }

                return bitmap;
            }

            public static void DrawDeviceIcon(Graphics graphics, KbNetworkElementKind kind, Rectangle bounds)
            {
                if (bounds.Width <= 0 || bounds.Height <= 0)
                    return;

                GraphicsState state = graphics.Save();
                graphics.SmoothingMode = SmoothingMode.AntiAlias;

                if (kind == KbNetworkElementKind.Et200)
                {
                    DrawEt200Icon(graphics, bounds);
                    graphics.Restore(state);
                    return;
                }

                if (kind == KbNetworkElementKind.Olm)
                {
                    DrawOlmIcon(graphics, bounds);
                    graphics.Restore(state);
                    return;
                }

                if (kind == KbNetworkElementKind.ExternalConnection)
                {
                    DrawExternalConnectionIcon(graphics, bounds);
                    graphics.Restore(state);
                    return;
                }

                if (TryGetApprovedIcon(kind, out IconDefinition icon))
                {
                    DrawSvgPathIcon(graphics, icon, bounds);
                    graphics.Restore(state);
                    return;
                }

                DrawOtherGlyph(graphics, bounds);
                graphics.Restore(state);
            }

            private static bool TryGetApprovedIcon(KbNetworkElementKind kind, out IconDefinition icon)
            {
                icon = kind switch
                {
                    KbNetworkElementKind.Plc => new IconDefinition(PlcIconPath, 512F, 512F, FillMode.Alternate),
                    KbNetworkElementKind.FrequencyConverter => new IconDefinition(FrequencyConverterIconPath, 24F, 24F, FillMode.Winding),
                    KbNetworkElementKind.Scalance => new IconDefinition(SwitchIconPath, 512F, 512F, FillMode.Alternate),
                    KbNetworkElementKind.Arm => new IconDefinition(ArmIconPath, 24F, 24F, FillMode.Winding),
                    KbNetworkElementKind.Hmi => new IconDefinition(HmiIconPath, 512F, 512F, FillMode.Winding),
                    KbNetworkElementKind.Server => new IconDefinition(ServerIconPath, 512F, 512F, FillMode.Alternate),
                    _ => default
                };
                return kind is
                    KbNetworkElementKind.Plc or
                    KbNetworkElementKind.FrequencyConverter or
                    KbNetworkElementKind.Scalance or
                    KbNetworkElementKind.Arm or
                    KbNetworkElementKind.Hmi or
                    KbNetworkElementKind.Server;
            }

            private static void DrawSvgPathIcon(Graphics graphics, IconDefinition icon, Rectangle bounds)
            {
                using GraphicsPath path = SvgPathParser.Parse(icon.PathData, icon.FillMode);
                if (Math.Abs(icon.TranslateX) > float.Epsilon || Math.Abs(icon.TranslateY) > float.Epsilon)
                {
                    using Matrix translate = new(1F, 0F, 0F, 1F, icon.TranslateX, icon.TranslateY);
                    path.Transform(translate);
                }

                float scale = Math.Min(bounds.Width / icon.ViewBoxWidth, bounds.Height / icon.ViewBoxHeight);
                float left = bounds.X + (bounds.Width - icon.ViewBoxWidth * scale) / 2F;
                float top = bounds.Y + (bounds.Height - icon.ViewBoxHeight * scale) / 2F;
                using Matrix matrix = new(scale, 0F, 0F, scale, left, top);
                path.Transform(matrix);

                using var brush = new SolidBrush(ApprovedIconColor);
                graphics.FillPath(brush, path);
            }

            private static void DrawEt200Icon(Graphics graphics, Rectangle bounds)
            {
                using var fill = new SolidBrush(ApprovedIconColor);
                using var cut = new SolidBrush(Color.White);
                using (GraphicsPath body = CreateRoundedRectanglePath(ScaleRectangle(bounds, 8F, 19F, 48F, 26F), ScaleLength(bounds, 2F)))
                    graphics.FillPath(fill, body);

                graphics.FillRectangle(cut, ScaleRectangle(bounds, 12F, 23F, 10F, 18F));
                graphics.FillRectangle(cut, ScaleRectangle(bounds, 25F, 23F, 5F, 18F));
                graphics.FillRectangle(cut, ScaleRectangle(bounds, 33F, 23F, 5F, 18F));
                graphics.FillRectangle(cut, ScaleRectangle(bounds, 41F, 23F, 5F, 18F));
                graphics.FillRectangle(cut, ScaleRectangle(bounds, 49F, 23F, 3F, 18F));
                graphics.FillEllipse(fill, ScaleRectangle(bounds, 13.4F, 26.4F, 3.2F, 3.2F));
                graphics.FillEllipse(fill, ScaleRectangle(bounds, 13.4F, 34.4F, 3.2F, 3.2F));
            }

            private static void DrawOlmIcon(Graphics graphics, Rectangle bounds)
            {
                using var fill = new SolidBrush(ApprovedIconColor);
                using var cut = new SolidBrush(Color.White);
                using (GraphicsPath body = CreateRoundedRectanglePath(ScaleRectangle(bounds, 18F, 9F, 28F, 46F), ScaleLength(bounds, 4F)))
                    graphics.FillPath(fill, body);

                graphics.FillRectangle(cut, ScaleRectangle(bounds, 23F, 15F, 18F, 6F));
                using (GraphicsPath topPort = CreateRoundedRectanglePath(ScaleRectangle(bounds, 24F, 27F, 16F, 5F), ScaleLength(bounds, 2F)))
                using (GraphicsPath bottomPort = CreateRoundedRectanglePath(ScaleRectangle(bounds, 24F, 37F, 16F, 5F), ScaleLength(bounds, 2F)))
                {
                    graphics.FillPath(cut, topPort);
                    graphics.FillPath(cut, bottomPort);
                }

                graphics.FillEllipse(cut, ScaleRectangle(bounds, 23.5F, 47.5F, 3F, 3F));
                graphics.FillEllipse(cut, ScaleRectangle(bounds, 30.5F, 47.5F, 3F, 3F));
                graphics.FillEllipse(cut, ScaleRectangle(bounds, 37.5F, 47.5F, 3F, 3F));
            }

            private static void DrawExternalConnectionIcon(Graphics graphics, Rectangle bounds)
            {
                using var pen = new Pen(ApprovedIconColor, Math.Max(1.6F, bounds.Width / 18F))
                {
                    StartCap = LineCap.Round,
                    EndCap = LineCap.Round,
                    LineJoin = LineJoin.Round
                };
                using var fill = new SolidBrush(ApprovedIconColor);

                RectangleF sourceBounds = ScaleRectangle(bounds, 9F, 18F, 23F, 28F);
                RectangleF targetBounds = ScaleRectangle(bounds, 35F, 14F, 20F, 18F);
                RectangleF lowerTargetBounds = ScaleRectangle(bounds, 35F, 36F, 20F, 14F);
                graphics.DrawRectangle(pen, sourceBounds.X, sourceBounds.Y, sourceBounds.Width, sourceBounds.Height);
                graphics.DrawRectangle(pen, targetBounds.X, targetBounds.Y, targetBounds.Width, targetBounds.Height);
                graphics.DrawRectangle(pen, lowerTargetBounds.X, lowerTargetBounds.Y, lowerTargetBounds.Width, lowerTargetBounds.Height);

                PointF from = new(
                    sourceBounds.Right,
                    sourceBounds.Top + sourceBounds.Height * 0.5F);
                PointF to = new(
                    targetBounds.Left,
                    targetBounds.Top + targetBounds.Height * 0.5F);
                graphics.DrawLine(pen, from, to);

                float arrowSize = Math.Max(4F, bounds.Width / 9F);
                PointF[] arrow =
                [
                    to,
                    new PointF(to.X - arrowSize, to.Y - arrowSize * 0.55F),
                    new PointF(to.X - arrowSize, to.Y + arrowSize * 0.55F)
                ];
                graphics.FillPolygon(fill, arrow);

                PointF branchStart = new(
                    sourceBounds.Right,
                    sourceBounds.Top + sourceBounds.Height * 0.72F);
                PointF branchEnd = new(
                    lowerTargetBounds.Left,
                    lowerTargetBounds.Top + lowerTargetBounds.Height * 0.5F);
                graphics.DrawLine(pen, branchStart, branchEnd);
            }

            private static void DrawOtherGlyph(Graphics graphics, Rectangle bounds)
            {
                Rectangle glyph = bounds;
                glyph.Inflate(-Math.Max(3, bounds.Width / 7), -Math.Max(3, bounds.Height / 7));
                PointF[] points =
                [
                    new(glyph.X + glyph.Width / 2F, glyph.Y),
                    new(glyph.Right, glyph.Y + glyph.Height / 2F),
                    new(glyph.X + glyph.Width / 2F, glyph.Bottom),
                    new(glyph.X, glyph.Y + glyph.Height / 2F)
                ];
                using var brush = new SolidBrush(ApprovedIconColor);
                graphics.FillPolygon(brush, points);
            }

            private static RectangleF ScaleRectangle(Rectangle bounds, float x, float y, float width, float height)
            {
                float scale = Math.Min(bounds.Width, bounds.Height) / 64F;
                float left = bounds.X + ((bounds.Width - (64F * scale)) / 2F);
                float top = bounds.Y + ((bounds.Height - (64F * scale)) / 2F);
                return new RectangleF(left + (x * scale), top + (y * scale), width * scale, height * scale);
            }

            private static float ScaleLength(Rectangle bounds, float value) =>
                value * (Math.Min(bounds.Width, bounds.Height) / 64F);

            private static GraphicsPath CreateRoundedRectanglePath(RectangleF rectangle, float radius)
            {
                var path = new GraphicsPath();
                float diameter = Math.Min(radius * 2F, Math.Min(rectangle.Width, rectangle.Height));
                if (diameter <= 0F)
                {
                    path.AddRectangle(rectangle);
                    path.CloseFigure();
                    return path;
                }

                path.AddArc(rectangle.Left, rectangle.Top, diameter, diameter, 180F, 90F);
                path.AddArc(rectangle.Right - diameter, rectangle.Top, diameter, diameter, 270F, 90F);
                path.AddArc(rectangle.Right - diameter, rectangle.Bottom - diameter, diameter, diameter, 0F, 90F);
                path.AddArc(rectangle.Left, rectangle.Bottom - diameter, diameter, diameter, 90F, 90F);
                path.CloseFigure();
                return path;
            }

            private readonly record struct IconDefinition(
                string PathData,
                float ViewBoxWidth,
                float ViewBoxHeight,
                FillMode FillMode,
                float TranslateX = 0F,
                float TranslateY = 0F);

            private sealed class SvgPathParser
            {
                private readonly string _data;
                private readonly FillMode _fillMode;
                private int _index;
                private char _command;
                private PointF _currentPoint;
                private PointF _figureStartPoint;

                private SvgPathParser(string data, FillMode fillMode)
                {
                    _data = data;
                    _fillMode = fillMode;
                }

                public static GraphicsPath Parse(string data, FillMode fillMode)
                {
                    var parser = new SvgPathParser(data, fillMode);
                    return parser.ParsePath();
                }

                private GraphicsPath ParsePath()
                {
                    var path = new GraphicsPath(_fillMode);
                    while (MoveNextCommandIfPresent())
                    {
                        switch (_command)
                        {
                            case 'M':
                            case 'm':
                                ParseMove(path, char.IsLower(_command));
                                break;
                            case 'L':
                            case 'l':
                                ParseLines(path, char.IsLower(_command));
                                break;
                            case 'H':
                            case 'h':
                                ParseHorizontalLines(path, char.IsLower(_command));
                                break;
                            case 'V':
                            case 'v':
                                ParseVerticalLines(path, char.IsLower(_command));
                                break;
                            case 'C':
                            case 'c':
                                ParseCubicCurves(path, char.IsLower(_command));
                                break;
                            case 'A':
                            case 'a':
                                ParseArcs(path, char.IsLower(_command));
                                break;
                            case 'Z':
                            case 'z':
                                path.CloseFigure();
                                _currentPoint = _figureStartPoint;
                                break;
                            default:
                                throw new NotSupportedException($"SVG path command '{_command}' is not supported.");
                        }
                    }

                    return path;
                }

                private bool MoveNextCommandIfPresent()
                {
                    SkipSeparators();
                    if (_index >= _data.Length)
                        return false;

                    if (IsCommand(_data[_index]))
                    {
                        _command = _data[_index++];
                        return true;
                    }

                    if (_command == default)
                        throw new FormatException("SVG path data started without a command.");

                    return true;
                }

                private void ParseMove(GraphicsPath path, bool isRelative)
                {
                    bool firstPoint = true;
                    while (HasNumber())
                    {
                        PointF point = ReadPoint(isRelative);
                        if (firstPoint)
                        {
                            path.StartFigure();
                            _currentPoint = point;
                            _figureStartPoint = point;
                            firstPoint = false;
                        }
                        else
                        {
                            AddLine(path, point);
                        }
                    }

                    _command = isRelative ? 'l' : 'L';
                }

                private void ParseLines(GraphicsPath path, bool isRelative)
                {
                    while (HasNumber())
                        AddLine(path, ReadPoint(isRelative));
                }

                private void ParseHorizontalLines(GraphicsPath path, bool isRelative)
                {
                    while (HasNumber())
                    {
                        float x = ReadNumber();
                        if (isRelative)
                            x += _currentPoint.X;

                        AddLine(path, new PointF(x, _currentPoint.Y));
                    }
                }

                private void ParseVerticalLines(GraphicsPath path, bool isRelative)
                {
                    while (HasNumber())
                    {
                        float y = ReadNumber();
                        if (isRelative)
                            y += _currentPoint.Y;

                        AddLine(path, new PointF(_currentPoint.X, y));
                    }
                }

                private void ParseCubicCurves(GraphicsPath path, bool isRelative)
                {
                    while (HasNumber())
                    {
                        PointF firstControl = ReadPoint(isRelative);
                        PointF secondControl = ReadPoint(isRelative);
                        PointF endPoint = ReadPoint(isRelative);
                        path.AddBezier(_currentPoint, firstControl, secondControl, endPoint);
                        _currentPoint = endPoint;
                    }
                }

                private void ParseArcs(GraphicsPath path, bool isRelative)
                {
                    while (HasNumber())
                    {
                        float radiusX = ReadNumber();
                        float radiusY = ReadNumber();
                        float xAxisRotation = ReadNumber();
                        bool largeArc = Math.Abs(ReadNumber()) > float.Epsilon;
                        bool sweep = Math.Abs(ReadNumber()) > float.Epsilon;
                        PointF endPoint = ReadPoint(isRelative);
                        AddArc(path, _currentPoint, endPoint, radiusX, radiusY, xAxisRotation, largeArc, sweep);
                        _currentPoint = endPoint;
                    }
                }

                private void AddLine(GraphicsPath path, PointF endPoint)
                {
                    path.AddLine(_currentPoint, endPoint);
                    _currentPoint = endPoint;
                }

                private static void AddArc(
                    GraphicsPath path,
                    PointF start,
                    PointF end,
                    float radiusX,
                    float radiusY,
                    float xAxisRotation,
                    bool largeArc,
                    bool sweep)
                {
                    if (radiusX <= 0F || radiusY <= 0F || start == end)
                    {
                        path.AddLine(start, end);
                        return;
                    }

                    if (Math.Abs(xAxisRotation) > 0.001F)
                    {
                        path.AddLine(start, end);
                        return;
                    }

                    double rx = Math.Abs(radiusX);
                    double ry = Math.Abs(radiusY);
                    double x1Prime = (start.X - end.X) / 2D;
                    double y1Prime = (start.Y - end.Y) / 2D;
                    double lambda = (x1Prime * x1Prime) / (rx * rx) + (y1Prime * y1Prime) / (ry * ry);
                    if (lambda > 1D)
                    {
                        double scale = Math.Sqrt(lambda);
                        rx *= scale;
                        ry *= scale;
                    }

                    double numerator = rx * rx * ry * ry - rx * rx * y1Prime * y1Prime - ry * ry * x1Prime * x1Prime;
                    double denominator = rx * rx * y1Prime * y1Prime + ry * ry * x1Prime * x1Prime;
                    double coefficient = denominator == 0D
                        ? 0D
                        : (largeArc == sweep ? -1D : 1D) * Math.Sqrt(Math.Max(0D, numerator / denominator));
                    double centerXPrime = coefficient * rx * y1Prime / ry;
                    double centerYPrime = coefficient * -ry * x1Prime / rx;
                    double centerX = centerXPrime + (start.X + end.X) / 2D;
                    double centerY = centerYPrime + (start.Y + end.Y) / 2D;

                    double ux = (x1Prime - centerXPrime) / rx;
                    double uy = (y1Prime - centerYPrime) / ry;
                    double vx = (-x1Prime - centerXPrime) / rx;
                    double vy = (-y1Prime - centerYPrime) / ry;
                    double startAngle = Math.Atan2(uy, ux);
                    double sweepAngle = VectorAngle(ux, uy, vx, vy);
                    if (!sweep && sweepAngle > 0D)
                        sweepAngle -= Math.PI * 2D;
                    else if (sweep && sweepAngle < 0D)
                        sweepAngle += Math.PI * 2D;

                    path.AddArc(
                        (float)(centerX - rx),
                        (float)(centerY - ry),
                        (float)(rx * 2D),
                        (float)(ry * 2D),
                        (float)(startAngle * 180D / Math.PI),
                        (float)(sweepAngle * 180D / Math.PI));
                }

                private PointF ReadPoint(bool isRelative)
                {
                    float x = ReadNumber();
                    float y = ReadNumber();
                    if (isRelative)
                    {
                        x += _currentPoint.X;
                        y += _currentPoint.Y;
                    }

                    return new PointF(x, y);
                }

                private bool HasNumber()
                {
                    SkipSeparators();
                    return _index < _data.Length &&
                        (char.IsDigit(_data[_index]) || _data[_index] == '-' || _data[_index] == '+' || _data[_index] == '.');
                }

                private float ReadNumber()
                {
                    SkipSeparators();
                    int start = _index;

                    if (_index < _data.Length && (_data[_index] == '-' || _data[_index] == '+'))
                        _index++;

                    while (_index < _data.Length && char.IsDigit(_data[_index]))
                        _index++;

                    if (_index < _data.Length && _data[_index] == '.')
                    {
                        _index++;
                        while (_index < _data.Length && char.IsDigit(_data[_index]))
                            _index++;
                    }

                    if (_index < _data.Length && (_data[_index] == 'e' || _data[_index] == 'E'))
                    {
                        _index++;
                        if (_index < _data.Length && (_data[_index] == '-' || _data[_index] == '+'))
                            _index++;

                        while (_index < _data.Length && char.IsDigit(_data[_index]))
                            _index++;
                    }

                    return float.Parse(_data[start.._index], CultureInfo.InvariantCulture);
                }

                private void SkipSeparators()
                {
                    while (_index < _data.Length && (char.IsWhiteSpace(_data[_index]) || _data[_index] == ','))
                        _index++;
                }

                private static double VectorAngle(double ux, double uy, double vx, double vy)
                {
                    double dot = ux * vx + uy * vy;
                    double length = Math.Sqrt((ux * ux + uy * uy) * (vx * vx + vy * vy));
                    if (length == 0D)
                        return 0D;

                    double value = Math.Clamp(dot / length, -1D, 1D);
                    double sign = ux * vy - uy * vx < 0D ? -1D : 1D;
                    return sign * Math.Acos(value);
                }

                private static bool IsCommand(char value) =>
                    (value >= 'A' && value <= 'Z') || (value >= 'a' && value <= 'z');
            }

        }

        private sealed class TopologyCanvas : Panel
        {
            private const int ElementWidth = 134;
            private const int ElementHeight = 118;
            private const int IconSize = 46;
            private const int GridSize = 24;
            private const int LinkStubLength = 22;
            private const int LinkObstaclePadding = 12;
            private const int LinkPortInset = 22;
            private const int LinkParallelLanePadding = 10;
            private const int LinkAlignmentTolerance = GridSize / 2;
            private const int CanvasContentPadding = 96;
            private const int ElementMinCoordinate = 0;
            private const float MinZoomFactor = 0.35F;
            private const float MaxZoomFactor = 2.5F;
            private const float ZoomStepFactor = 1.1F;
            private const double LinkHitTolerance = 7D;

            private KbNetworkTopology _topology = new();
            private string _dragElementId = string.Empty;
            private Point _dragOffset;
            private bool _dragMoved;
            private string _dragLinkId = string.Empty;
            private bool _dragLinkFromEndpoint;
            private Point _dragLinkCurrentPoint;
            private bool _dragLinkMoved;
            private float _zoomFactor = 1F;
            private Size _viewportSize = Size.Empty;

            public TopologyCanvas()
            {
                DoubleBuffered = true;
                BackColor = Color.White;
                BorderStyle = BorderStyle.None;
                Cursor = Cursors.Default;
                TabStop = true;
                SetStyle(ControlStyles.Selectable, true);
            }

            public KbNetworkTopology Topology
            {
                get => _topology;
                set
                {
                    _topology = value ?? new KbNetworkTopology();
                    UpdateCanvasSize();
                    Invalidate();
                }
            }

            public float ZoomFactor => _zoomFactor;

            public string SelectedElementId { get; set; } = string.Empty;

            public string SelectedLinkId { get; set; } = string.Empty;

            public string PendingLinkSourceElementId { get; set; } = string.Empty;

            public KbNetworkLinkKind PendingLinkKind { get; set; } = KbNetworkLinkKind.CopperProfinet;

            public event EventHandler<NetworkElementEventArgs>? SelectionChanged;

            public event EventHandler<NetworkLinkEventArgs>? LinkSelectionChanged;

            public event EventHandler? ElementMoved;

            public event EventHandler<NetworkElementEventArgs>? ElementEditRequested;

            public event EventHandler<NetworkElementEventArgs>? LinkTargetSelected;

            public event EventHandler? LinkEndpointMoved;

            public event EventHandler<NetworkCanvasContextMenuEventArgs>? CanvasContextMenuRequested;

            public event EventHandler? ZoomFactorChanged;

            public void UpdateViewportSize(Size viewportSize)
            {
                _viewportSize = viewportSize;
                UpdateCanvasSize();
                Invalidate();
            }

            public void ZoomAtScreenPoint(Point screenPoint, bool zoomIn) =>
                ZoomAt(PointToClient(screenPoint), zoomIn);

            public void ZoomInFromCenter() =>
                ZoomAt(GetViewportCenterPoint(), zoomIn: true);

            public void ZoomOutFromCenter() =>
                ZoomAt(GetViewportCenterPoint(), zoomIn: false);

            public void ResetZoom() =>
                ZoomToPercent(100);

            public void ZoomToPercent(int zoomPercent) =>
                SetZoomFactor(zoomPercent / 100F, GetViewportCenterPoint());

            public Point GetElementDropLocation(Point location)
            {
                return GetSnappedElementLocation(new Point(
                    location.X - ElementWidth / 2,
                    location.Y - ElementHeight / 2));
            }

            public Point GetSnappedElementLocation(Point location)
            {
                Size logicalSize = GetLogicalCanvasSize();
                int maxX = Math.Max(ElementMinCoordinate, logicalSize.Width - ElementWidth - ElementMinCoordinate);
                int maxY = Math.Max(ElementMinCoordinate, logicalSize.Height - ElementHeight - ElementMinCoordinate);
                return ClampAndSnapElementLocation(location, maxX, maxY);
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                base.OnPaint(e);
                e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                e.Graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
                e.Graphics.ScaleTransform(_zoomFactor, _zoomFactor);
                DrawGrid(e.Graphics);
                DrawLinks(e.Graphics);

                foreach (KbNetworkElement element in Topology.Elements)
                    DrawElement(e.Graphics, element);

                if (Topology.Elements.Count == 0)
                {
                    using var emptyStateFont = new Font("Segoe UI", 14F, FontStyle.Regular);
                    using var emptyStateBrush = new SolidBrush(Color.FromArgb(117, 129, 141));
                    using StringFormat emptyStateFormat = CreateSingleLineStringFormat(
                        StringAlignment.Center,
                        StringAlignment.Center);
                    e.Graphics.DrawString(
                        "Сеть пока пуста",
                        emptyStateFont,
                        emptyStateBrush,
                        GetLogicalClientRectangle(),
                        emptyStateFormat);
                }
            }

            protected override void OnMouseEnter(EventArgs e)
            {
                base.OnMouseEnter(e);
                if (CanFocus)
                    Focus();
            }

            protected override void OnMouseWheel(MouseEventArgs e)
            {
                if ((ModifierKeys & Keys.Control) == Keys.Control)
                {
                    ZoomAt(e.Location, e.Delta > 0);
                    return;
                }

                ScrollParentByWheel(e.Delta);
            }

            protected override void OnMouseDown(MouseEventArgs e)
            {
                base.OnMouseDown(e);
                if (CanFocus)
                    Focus();

                Point location = ToLogicalPoint(e.Location);
                if (e.Button == MouseButtons.Right)
                {
                    KbNetworkElement? contextElement = HitTestElement(location);
                    LinkHit? contextLink = contextElement == null
                        ? HitTestLink(location)
                        : null;
                    if (contextElement != null || string.IsNullOrWhiteSpace(PendingLinkSourceElementId))
                    {
                        SelectedElementId = contextElement?.ElementId ?? string.Empty;
                        SelectedLinkId = contextLink?.Link.LinkId ?? string.Empty;
                        SelectionChanged?.Invoke(this, new NetworkElementEventArgs(SelectedElementId));
                        LinkSelectionChanged?.Invoke(this, new NetworkLinkEventArgs(SelectedLinkId));
                        Invalidate();
                    }

                    CanvasContextMenuRequested?.Invoke(
                        this,
                        new NetworkCanvasContextMenuEventArgs(
                            e.Location,
                            location,
                            contextElement?.ElementId ?? string.Empty,
                            contextLink?.Link.LinkId ?? string.Empty));
                    return;
                }

                if (e.Button != MouseButtons.Left)
                    return;

                KbNetworkElement? element = HitTestElement(location);
                if (!string.IsNullOrWhiteSpace(PendingLinkSourceElementId))
                {
                    if (element != null)
                        LinkTargetSelected?.Invoke(this, new NetworkElementEventArgs(element.ElementId));
                    return;
                }

                if (element == null)
                {
                    LinkHit? linkHit = HitTestLink(location);
                    if (linkHit.HasValue)
                    {
                        SelectedElementId = string.Empty;
                        SelectedLinkId = linkHit.Value.Link.LinkId;
                        SelectionChanged?.Invoke(this, new NetworkElementEventArgs(SelectedElementId));
                        LinkSelectionChanged?.Invoke(this, new NetworkLinkEventArgs(SelectedLinkId));
                        Invalidate();
                        BeginLinkEndpointDrag(linkHit.Value, location);
                        return;
                    }
                }

                SelectedElementId = element?.ElementId ?? string.Empty;
                SelectedLinkId = string.Empty;
                SelectionChanged?.Invoke(this, new NetworkElementEventArgs(SelectedElementId));
                LinkSelectionChanged?.Invoke(this, new NetworkLinkEventArgs(SelectedLinkId));
                Invalidate();

                if (element == null)
                    return;

                Rectangle bounds = GetElementBounds(element);
                _dragElementId = element.ElementId;
                _dragOffset = new Point(location.X - bounds.X, location.Y - bounds.Y);
                _dragMoved = false;
                Cursor = Cursors.SizeAll;
            }

            protected override void OnMouseMove(MouseEventArgs e)
            {
                base.OnMouseMove(e);
                Point location = ToLogicalPoint(e.Location);
                if (!string.IsNullOrWhiteSpace(_dragLinkId))
                {
                    _dragLinkCurrentPoint = location;
                    _dragLinkMoved = true;
                    Cursor = IsValidLinkEndpointTarget(HitTestElement(location)) ? Cursors.Cross : Cursors.No;
                    Invalidate();
                    return;
                }

                if (string.IsNullOrWhiteSpace(_dragElementId))
                {
                    Cursor = HitTestElement(location) != null
                        ? Cursors.Hand
                        : HitTestLink(location).HasValue
                            ? Cursors.Cross
                            : Cursors.Default;
                    return;
                }

                KbNetworkElement? element = Topology.Elements.FirstOrDefault(candidate =>
                    string.Equals(candidate.ElementId, _dragElementId, StringComparison.Ordinal));
                if (element == null)
                    return;

                Point nextLocation = SnapElementLocation(new Point(
                    location.X - _dragOffset.X,
                    location.Y - _dragOffset.Y));
                if (element.X == nextLocation.X && element.Y == nextLocation.Y)
                    return;

                element.X = nextLocation.X;
                element.Y = nextLocation.Y;
                _dragMoved = true;
                UpdateCanvasSize();
                Invalidate();
            }

            protected override void OnMouseUp(MouseEventArgs e)
            {
                base.OnMouseUp(e);
                if (!string.IsNullOrWhiteSpace(_dragLinkId))
                {
                    CompleteLinkEndpointDrag(ToLogicalPoint(e.Location));
                    return;
                }

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

            private void ZoomAt(Point canvasPoint, bool zoomIn)
            {
                float nextZoom = Math.Clamp(
                    _zoomFactor * (zoomIn ? ZoomStepFactor : 1F / ZoomStepFactor),
                    MinZoomFactor,
                    MaxZoomFactor);
                SetZoomFactor(nextZoom, canvasPoint);
            }

            private void SetZoomFactor(float zoomFactor, Point canvasPoint)
            {
                float nextZoom = Math.Clamp(zoomFactor, MinZoomFactor, MaxZoomFactor);
                if (Math.Abs(nextZoom - _zoomFactor) < 0.001F)
                    return;

                PointF logicalAnchor = ToLogicalPointF(canvasPoint);
                ScrollableControl? viewport = Parent as ScrollableControl;
                Point viewportPoint = viewport?.PointToClient(PointToScreen(canvasPoint)) ?? canvasPoint;

                _zoomFactor = nextZoom;
                UpdateCanvasSize();
                if (viewport != null)
                {
                    int scrollX = Math.Max(0, (int)Math.Round(logicalAnchor.X * _zoomFactor) - viewportPoint.X);
                    int scrollY = Math.Max(0, (int)Math.Round(logicalAnchor.Y * _zoomFactor) - viewportPoint.Y);
                    viewport.AutoScrollPosition = new Point(scrollX, scrollY);
                }

                Invalidate();
                ZoomFactorChanged?.Invoke(this, EventArgs.Empty);
            }

            private Point GetViewportCenterPoint()
            {
                if (Parent is ScrollableControl viewport)
                {
                    Point center = new(viewport.ClientSize.Width / 2, viewport.ClientSize.Height / 2);
                    return PointToClient(viewport.PointToScreen(center));
                }

                return new Point(ClientSize.Width / 2, ClientSize.Height / 2);
            }

            private void ScrollParentByWheel(int delta)
            {
                if (Parent is not ScrollableControl viewport)
                    return;

                int lines = SystemInformation.MouseWheelScrollLines > 0
                    ? SystemInformation.MouseWheelScrollLines
                    : 3;
                int step = Math.Max(48, lines * 24);
                int currentX = -viewport.AutoScrollPosition.X;
                int currentY = -viewport.AutoScrollPosition.Y;
                int targetY = Math.Max(0, currentY + (delta > 0 ? -step : step));
                viewport.AutoScrollPosition = new Point(currentX, targetY);
            }

            private Point ToLogicalPoint(Point point) =>
                new(
                    (int)Math.Round(point.X / _zoomFactor),
                    (int)Math.Round(point.Y / _zoomFactor));

            private PointF ToLogicalPointF(Point point) =>
                new(point.X / _zoomFactor, point.Y / _zoomFactor);

            private Rectangle GetLogicalClientRectangle()
            {
                Size logicalSize = GetLogicalCanvasSize();
                return new Rectangle(Point.Empty, logicalSize);
            }

            private void UpdateCanvasSize()
            {
                Size logicalSize = GetLogicalCanvasSize();
                Size scaledSize = new(
                    Math.Max(1, (int)Math.Ceiling(logicalSize.Width * _zoomFactor)),
                    Math.Max(1, (int)Math.Ceiling(logicalSize.Height * _zoomFactor)));
                if (Size != scaledSize)
                    Size = scaledSize;
            }

            private Size GetLogicalCanvasSize()
            {
                Size viewportSize = !_viewportSize.IsEmpty
                    ? _viewportSize
                    : Parent?.ClientSize ?? ClientSize;
                int width = Math.Max(1, (int)Math.Ceiling(viewportSize.Width / _zoomFactor));
                int height = Math.Max(1, (int)Math.Ceiling(viewportSize.Height / _zoomFactor));

                foreach (KbNetworkElement element in Topology.Elements)
                {
                    width = Math.Max(width, element.X + ElementWidth + CanvasContentPadding);
                    height = Math.Max(height, element.Y + ElementHeight + CanvasContentPadding);
                }

                return new Size(width, height);
            }

            private static Point ClampAndSnapElementLocation(Point location, int maxX, int maxY) =>
                new(
                    SnapElementCoordinate(location.X, ElementMinCoordinate, maxX),
                    SnapElementCoordinate(location.Y, ElementMinCoordinate, maxY));

            private static Point SnapElementLocation(Point location) =>
                new(
                    Math.Max(ElementMinCoordinate, SnapToGrid(location.X)),
                    Math.Max(ElementMinCoordinate, SnapToGrid(location.Y)));

            private static int SnapElementCoordinate(int value, int min, int max)
            {
                if (max < min)
                    return min;

                int bounded = Math.Clamp(value, min, max);
                int snapped = SnapToGrid(bounded);
                if (snapped > max)
                    snapped = (max / GridSize) * GridSize;

                return Math.Clamp(snapped, min, max);
            }

            private static int SnapToGrid(int value) =>
                (int)Math.Round(value / (double)GridSize, MidpointRounding.AwayFromZero) * GridSize;

            private void DrawGrid(Graphics graphics)
            {
                Rectangle logicalBounds = GetLogicalClientRectangle();
                using var pen = new Pen(Color.FromArgb(238, 242, 246), 1F);
                for (int x = 0; x < logicalBounds.Width; x += GridSize)
                    graphics.DrawLine(pen, x, 0, x, logicalBounds.Height);
                for (int y = 0; y < logicalBounds.Height; y += GridSize)
                    graphics.DrawLine(pen, 0, y, logicalBounds.Width, y);
            }

            private void DrawLinks(Graphics graphics)
            {
                foreach (KbNetworkLink link in Topology.Links)
                {
                    if (string.Equals(link.LinkId, _dragLinkId, StringComparison.Ordinal))
                        continue;

                    KbNetworkElement? from = FindElement(link.FromElementId);
                    KbNetworkElement? to = FindElement(link.ToElementId);
                    if (from == null || to == null)
                        continue;

                    using Pen linkPen = CreateLinkPen(link.Kind);
                    if (string.Equals(link.LinkId, SelectedLinkId, StringComparison.Ordinal))
                        linkPen.Width = 4F;

                    DrawOrthogonalLink(
                        graphics,
                        linkPen,
                        GetOrthogonalLinkPoints(link, from, to, Topology.Elements, Topology.Links));
                }

                KbNetworkElement? pendingSource = FindElement(PendingLinkSourceElementId);
                if (pendingSource != null)
                {
                    using Pen pendingPen = CreateLinkPen(PendingLinkKind, forceDash: true);
                    Rectangle bounds = GetElementBounds(pendingSource);
                    bounds.Inflate(5, 5);
                    graphics.DrawRectangle(pendingPen, bounds);
                }

                DrawDraggedLinkEndpoint(graphics);
            }

            private static void DrawOrthogonalLink(Graphics graphics, Pen pen, Point[] points)
            {
                if (points.Length >= 2)
                    graphics.DrawLines(pen, points);
            }

            private static Pen CreateLinkPen(KbNetworkLinkKind kind, bool forceDash = false)
            {
                (Color color, DashStyle dashStyle) = KnowledgeBaseNetworkTopologyScreenControl.GetLinkStyle(kind);
                var pen = new Pen(color, 2.4F)
                {
                    StartCap = LineCap.Round,
                    EndCap = LineCap.Round,
                    LineJoin = LineJoin.Round,
                    DashStyle = forceDash ? DashStyle.Dash : dashStyle
                };
                if (pen.DashStyle == DashStyle.Dash)
                    pen.DashPattern = [4F, 3F];
                return pen;
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
                using (var shadowPath = CreateRoundedRectanglePath(shadowBounds, 8))
                using (var path = CreateRoundedRectanglePath(bounds, 8))
                {
                    graphics.FillPath(shadow, shadowPath);
                    graphics.FillPath(fill, path);
                    graphics.DrawPath(border, path);
                }

                if (element.Kind == KbNetworkElementKind.ExternalConnection)
                {
                    DrawExternalConnectionElement(graphics, element, bounds);
                    return;
                }

                IReadOnlyList<string> ipAddresses = GetDisplayIpAddresses(element);
                bool hasAdditionalIpAddresses = ipAddresses.Count > 1;
                int iconSize = hasAdditionalIpAddresses ? 36 : IconSize;
                int iconTop = hasAdditionalIpAddresses ? 56 : 36;
                Rectangle iconBounds = new(bounds.X + (ElementWidth - iconSize) / 2, bounds.Y + iconTop, iconSize, iconSize);
                NetworkIconPainter.DrawDeviceIcon(graphics, element.Kind, iconBounds);

                Rectangle ipBounds = new(bounds.X + 3, bounds.Y + 7, ElementWidth - 6, 22);
                Rectangle nameBounds = new(bounds.X + 5, bounds.Y + (hasAdditionalIpAddresses ? 95 : 92), ElementWidth - 10, 20);
                using var nameFont = new Font("Segoe UI", 9F, FontStyle.Regular);
                using var ipFont = new Font("Segoe UI", 9F, FontStyle.Bold);
                DrawIpAddresses(graphics, ipAddresses, ipFont, ipBounds);
                using var nameBrush = new SolidBrush(Color.FromArgb(21, 33, 45));
                using StringFormat nameFormat = CreateSingleLineStringFormat(
                    StringAlignment.Center,
                    StringAlignment.Center);
                graphics.DrawString(element.Name, nameFont, nameBrush, nameBounds, nameFormat);
            }

            private static void DrawExternalConnectionElement(Graphics graphics, KbNetworkElement element, Rectangle bounds)
            {
                Rectangle headerBounds = new(bounds.X + 8, bounds.Y + 8, ElementWidth - 16, 18);
                Rectangle fieldBounds = new(bounds.X + 10, bounds.Y + 32, ElementWidth - 20, ElementHeight - 44);
                using var headerFont = new Font("Segoe UI", 8F, FontStyle.Regular);
                using var headerBrush = new SolidBrush(Color.FromArgb(76, 91, 105));
                using StringFormat headerFormat = CreateSingleLineStringFormat(
                    StringAlignment.Center,
                    StringAlignment.Center);
                graphics.DrawString("Внешняя связь", headerFont, headerBrush, headerBounds, headerFormat);

                using var fieldFill = new SolidBrush(Color.FromArgb(247, 250, 252));
                using var fieldBorder = new Pen(Color.FromArgb(145, 168, 190), 1F);
                using (GraphicsPath fieldPath = CreateRoundedRectanglePath(fieldBounds, 6))
                {
                    graphics.FillPath(fieldFill, fieldPath);
                    graphics.DrawPath(fieldBorder, fieldPath);
                }

                Rectangle textBounds = new(fieldBounds.X + 6, fieldBounds.Y + 5, fieldBounds.Width - 12, fieldBounds.Height - 10);
                using var textFont = new Font("Segoe UI", 9F, FontStyle.Bold);
                using var textBrush = new SolidBrush(Color.FromArgb(21, 33, 45));
                using StringFormat textFormat = CreateWrappedStringFormat(
                    StringAlignment.Center,
                    StringAlignment.Center);
                graphics.DrawString(element.Name, textFont, textBrush, textBounds, textFormat);
            }

            private static IReadOnlyList<string> GetDisplayIpAddresses(KbNetworkElement element)
            {
                var result = new List<string>();
                AddDisplayIpAddress(result, element.IpAddress);
                foreach (string additionalIpAddress in element.AdditionalIpAddresses ?? [])
                    AddDisplayIpAddress(result, additionalIpAddress);

                return result;
            }

            private static void AddDisplayIpAddress(List<string> result, string ipAddress)
            {
                string normalized = ipAddress.Trim();
                if (string.IsNullOrWhiteSpace(normalized) ||
                    result.Contains(normalized, StringComparer.Ordinal))
                {
                    return;
                }

                result.Add(normalized);
            }

            private static void DrawIpAddresses(
                Graphics graphics,
                IReadOnlyList<string> ipAddresses,
                Font primaryFont,
                Rectangle primaryBounds)
            {
                if (ipAddresses.Count == 0)
                    return;

                DrawIpAddress(graphics, ipAddresses[0], primaryFont, primaryBounds);
                if (ipAddresses.Count == 1)
                    return;

                string secondaryText = ipAddresses.Count == 2
                    ? ipAddresses[1]
                    : FormattableString.Invariant($"{ipAddresses[1]} +{ipAddresses.Count - 2}");
                Rectangle secondaryBounds = new(primaryBounds.X + 6, primaryBounds.Bottom + 4, primaryBounds.Width - 12, 20);
                using var secondaryFont = new Font("Segoe UI", 8F, FontStyle.Bold);
                DrawIpAddress(graphics, secondaryText, secondaryFont, secondaryBounds);
            }

            private static void DrawIpAddress(Graphics graphics, string ipAddress, Font font, Rectangle bounds)
            {
                if (string.IsNullOrWhiteSpace(ipAddress))
                    return;

                using var badgeFill = new SolidBrush(Color.FromArgb(238, 246, 255));
                using var badgeBorder = new Pen(Color.FromArgb(142, 177, 211));
                using (var badgePath = CreateRoundedRectanglePath(bounds, 6))
                {
                    graphics.FillPath(badgeFill, badgePath);
                    graphics.DrawPath(badgeBorder, badgePath);
                }

                using var textBrush = new SolidBrush(Color.FromArgb(28, 55, 84));
                using StringFormat textFormat = CreateSingleLineStringFormat(
                    StringAlignment.Center,
                    StringAlignment.Center);
                graphics.DrawString(ipAddress, font, textBrush, bounds, textFormat);
            }

            private static StringFormat CreateSingleLineStringFormat(
                StringAlignment alignment,
                StringAlignment lineAlignment) =>
                new()
                {
                    Alignment = alignment,
                    LineAlignment = lineAlignment,
                    Trimming = StringTrimming.EllipsisCharacter,
                    FormatFlags = StringFormatFlags.NoWrap
                };

            private static StringFormat CreateWrappedStringFormat(
                StringAlignment alignment,
                StringAlignment lineAlignment) =>
                new()
                {
                    Alignment = alignment,
                    LineAlignment = lineAlignment,
                    Trimming = StringTrimming.EllipsisWord,
                    FormatFlags = StringFormatFlags.LineLimit
                };

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

            private LinkHit? HitTestLink(Point point)
            {
                double hitTolerance = LinkHitTolerance / _zoomFactor;
                for (int index = Topology.Links.Count - 1; index >= 0; index--)
                {
                    KbNetworkLink link = Topology.Links[index];
                    KbNetworkElement? from = FindElement(link.FromElementId);
                    KbNetworkElement? to = FindElement(link.ToElementId);
                    if (from == null || to == null)
                        continue;

                    Point[] points = GetOrthogonalLinkPoints(link, from, to, Topology.Elements, Topology.Links);
                    for (int segmentIndex = 0; segmentIndex < points.Length - 1; segmentIndex++)
                    {
                        if (DistanceToSegment(point, points[segmentIndex], points[segmentIndex + 1]) <= hitTolerance)
                        {
                            bool moveFromEndpoint = Distance(point, points[0]) <= Distance(point, points[^1]);
                            return new LinkHit(link, moveFromEndpoint);
                        }
                    }
                }

                return null;
            }

            private void BeginLinkEndpointDrag(LinkHit linkHit, Point location)
            {
                _dragLinkId = linkHit.Link.LinkId;
                _dragLinkFromEndpoint = linkHit.MoveFromEndpoint;
                _dragLinkCurrentPoint = location;
                _dragLinkMoved = false;
                Cursor = Cursors.Cross;
                Invalidate();
            }

            private void CompleteLinkEndpointDrag(Point location)
            {
                string linkId = _dragLinkId;
                bool moveFromEndpoint = _dragLinkFromEndpoint;
                KbNetworkElement? target = HitTestElement(location);
                KbNetworkLink? link = Topology.Links.FirstOrDefault(candidate =>
                    string.Equals(candidate.LinkId, linkId, StringComparison.Ordinal));
                if (link == null || !IsValidLinkEndpointTarget(target, link, moveFromEndpoint))
                {
                    ClearLinkEndpointDrag();
                    Invalidate();
                    return;
                }

                string fixedElementId = moveFromEndpoint ? link.ToElementId : link.FromElementId;
                if (Topology.Links.Any(candidate =>
                    !string.Equals(candidate.LinkId, link.LinkId, StringComparison.Ordinal) &&
                    LinksConnectSameElements(candidate, fixedElementId, target!.ElementId)))
                {
                    ClearLinkEndpointDrag();
                    Invalidate();
                    return;
                }

                if (moveFromEndpoint)
                    link.FromElementId = target!.ElementId;
                else
                    link.ToElementId = target!.ElementId;

                ClearLinkEndpointDrag();
                LinkEndpointMoved?.Invoke(this, EventArgs.Empty);
            }

            private void ClearLinkEndpointDrag()
            {
                _dragLinkId = string.Empty;
                _dragLinkFromEndpoint = false;
                _dragLinkCurrentPoint = Point.Empty;
                _dragLinkMoved = false;
                Cursor = Cursors.Default;
            }

            private bool IsValidLinkEndpointTarget(KbNetworkElement? target)
            {
                if (target == null || string.IsNullOrWhiteSpace(_dragLinkId))
                    return false;

                KbNetworkLink? link = Topology.Links.FirstOrDefault(candidate =>
                    string.Equals(candidate.LinkId, _dragLinkId, StringComparison.Ordinal));
                if (link == null)
                    return false;

                return IsValidLinkEndpointTarget(target, link, _dragLinkFromEndpoint);
            }

            private static bool IsValidLinkEndpointTarget(
                KbNetworkElement? target,
                KbNetworkLink link,
                bool moveFromEndpoint)
            {
                if (target == null)
                    return false;

                string fixedElementId = moveFromEndpoint ? link.ToElementId : link.FromElementId;
                return !string.Equals(target.ElementId, fixedElementId, StringComparison.Ordinal);
            }

            private void DrawDraggedLinkEndpoint(Graphics graphics)
            {
                if (string.IsNullOrWhiteSpace(_dragLinkId))
                    return;

                KbNetworkLink? link = Topology.Links.FirstOrDefault(candidate =>
                    string.Equals(candidate.LinkId, _dragLinkId, StringComparison.Ordinal));
                if (link == null)
                    return;

                string fixedElementId = _dragLinkFromEndpoint ? link.ToElementId : link.FromElementId;
                KbNetworkElement? fixedElement = FindElement(fixedElementId);
                if (fixedElement == null)
                    return;

                using Pen pen = CreateLinkPen(link.Kind, forceDash: true);
                LinkAnchor fixedAnchor = CreatePointerLinkAnchor(fixedElement, _dragLinkCurrentPoint);
                Point fixedStub = OffsetPoint(fixedAnchor.Point, fixedAnchor.Direction, LinkStubLength);
                Point[] route = NormalizeRoute(
                [
                    fixedAnchor.Point,
                    fixedStub,
                    new Point(_dragLinkCurrentPoint.X, fixedStub.Y),
                    _dragLinkCurrentPoint
                ]);
                DrawOrthogonalLink(graphics, pen, route);

                KbNetworkElement? target = HitTestElement(_dragLinkCurrentPoint);
                if (IsValidLinkEndpointTarget(target))
                {
                    Rectangle targetBounds = GetElementBounds(target!);
                    targetBounds.Inflate(5, 5);
                    graphics.DrawRectangle(pen, targetBounds);
                }
            }

            private KbNetworkElement? FindElement(string elementId) =>
                Topology.Elements.FirstOrDefault(element => string.Equals(element.ElementId, elementId, StringComparison.Ordinal));

            private static Rectangle GetElementBounds(KbNetworkElement element) =>
                new(element.X, element.Y, ElementWidth, ElementHeight);

            private static Point[] GetOrthogonalLinkPoints(
                KbNetworkLink link,
                KbNetworkElement from,
                KbNetworkElement to,
                IReadOnlyCollection<KbNetworkElement> elements,
                IReadOnlyCollection<KbNetworkLink> links)
            {
                LinkAnchor fromAnchor = CreateLinkAnchor(from, to, link.LinkId, elements, links);
                LinkAnchor toAnchor = CreateLinkAnchor(to, from, link.LinkId, elements, links);
                Point fromStub = OffsetPoint(fromAnchor.Point, fromAnchor.Direction, LinkStubLength);
                Point toStub = OffsetPoint(toAnchor.Point, toAnchor.Direction, LinkStubLength);
                List<Rectangle> obstacles = elements
                    .Where(element =>
                        !string.Equals(element.ElementId, from.ElementId, StringComparison.Ordinal) &&
                        !string.Equals(element.ElementId, to.ElementId, StringComparison.Ordinal))
                    .Select(GetElementBounds)
                    .Select(static bounds =>
                    {
                        bounds.Inflate(LinkObstaclePadding, LinkObstaclePadding);
                        return bounds;
                    })
                    .ToList();

                if (TryBuildAlignedFanRoute(
                    link,
                    from,
                    to,
                    fromAnchor,
                    toAnchor,
                    fromStub,
                    toStub,
                    elements,
                    links,
                    obstacles,
                    out Point[] fanRoute))
                {
                    return fanRoute;
                }

                return BuildBestOrthogonalRoute(fromAnchor.Point, fromStub, toStub, toAnchor.Point, obstacles);
            }

            private static bool TryBuildAlignedFanRoute(
                KbNetworkLink link,
                KbNetworkElement from,
                KbNetworkElement to,
                LinkAnchor fromAnchor,
                LinkAnchor toAnchor,
                Point fromStub,
                Point toStub,
                IReadOnlyCollection<KbNetworkElement> elements,
                IReadOnlyCollection<KbNetworkLink> links,
                IReadOnlyList<Rectangle> obstacles,
                out Point[] route)
            {
                if (TryBuildAlignedFanRouteForEndpoint(
                    link,
                    commonElement: from,
                    otherElement: to,
                    fromAnchor,
                    toAnchor,
                    fromStub,
                    toStub,
                    elements,
                    links,
                    obstacles,
                    out route))
                {
                    return true;
                }

                if (TryBuildAlignedFanRouteForEndpoint(
                    link,
                    commonElement: to,
                    otherElement: from,
                    fromAnchor,
                    toAnchor,
                    fromStub,
                    toStub,
                    elements,
                    links,
                    obstacles,
                    out route))
                {
                    return true;
                }

                route = [];
                return false;
            }

            private static bool TryBuildAlignedFanRouteForEndpoint(
                KbNetworkLink link,
                KbNetworkElement commonElement,
                KbNetworkElement otherElement,
                LinkAnchor fromAnchor,
                LinkAnchor toAnchor,
                Point fromStub,
                Point toStub,
                IReadOnlyCollection<KbNetworkElement> elements,
                IReadOnlyCollection<KbNetworkLink> links,
                IReadOnlyList<Rectangle> obstacles,
                out Point[] route)
            {
                Rectangle commonBounds = GetElementBounds(commonElement);
                Point otherCenter = GetRectangleCenter(GetElementBounds(otherElement));
                LinkSide commonSide = ResolveLinkSide(commonBounds, otherCenter);
                List<AlignedFanLinkCandidate> candidates = links
                    .Select(candidate => TryCreateAlignedFanCandidate(candidate, commonElement, otherElement, commonSide, elements))
                    .Where(static candidate => candidate.HasValue)
                    .Select(static candidate => candidate!.Value)
                    .OrderBy(static candidate => candidate.SortCoordinate)
                    .ThenBy(static candidate => candidate.LinkId, StringComparer.Ordinal)
                    .ToList();

                if (candidates.Count <= 1)
                {
                    route = [];
                    return false;
                }

                int index = candidates.FindIndex(candidate => string.Equals(candidate.LinkId, link.LinkId, StringComparison.Ordinal));
                if (index < 0)
                {
                    route = [];
                    return false;
                }

                if (commonSide is LinkSide.Top or LinkSide.Bottom)
                {
                    int laneY = ResolveParallelLaneCoordinate(fromStub.Y, toStub.Y, index, candidates.Count);
                    route = NormalizeRoute(
                    [
                        fromAnchor.Point,
                        fromStub,
                        new Point(fromStub.X, laneY),
                        new Point(toStub.X, laneY),
                        toStub,
                        toAnchor.Point
                    ]);
                }
                else
                {
                    int laneX = ResolveParallelLaneCoordinate(fromStub.X, toStub.X, index, candidates.Count);
                    route = NormalizeRoute(
                    [
                        fromAnchor.Point,
                        fromStub,
                        new Point(laneX, fromStub.Y),
                        new Point(laneX, toStub.Y),
                        toStub,
                        toAnchor.Point
                    ]);
                }

                return !RouteIntersectsAnyObstacle(route, obstacles);
            }

            private static AlignedFanLinkCandidate? TryCreateAlignedFanCandidate(
                KbNetworkLink link,
                KbNetworkElement commonElement,
                KbNetworkElement referenceOtherElement,
                LinkSide commonSide,
                IReadOnlyCollection<KbNetworkElement> elements)
            {
                string otherElementId;
                if (string.Equals(link.FromElementId, commonElement.ElementId, StringComparison.Ordinal))
                    otherElementId = link.ToElementId;
                else if (string.Equals(link.ToElementId, commonElement.ElementId, StringComparison.Ordinal))
                    otherElementId = link.FromElementId;
                else
                    return null;

                KbNetworkElement? otherElement = elements.FirstOrDefault(candidate =>
                    string.Equals(candidate.ElementId, otherElementId, StringComparison.Ordinal));
                if (otherElement == null)
                    return null;

                Rectangle commonBounds = GetElementBounds(commonElement);
                Point otherCenter = GetRectangleCenter(GetElementBounds(otherElement));
                if (ResolveLinkSide(commonBounds, otherCenter) != commonSide)
                    return null;

                Point referenceCenter = GetRectangleCenter(GetElementBounds(referenceOtherElement));
                bool aligned = commonSide is LinkSide.Top or LinkSide.Bottom
                    ? Math.Abs(otherCenter.Y - referenceCenter.Y) <= LinkAlignmentTolerance
                    : Math.Abs(otherCenter.X - referenceCenter.X) <= LinkAlignmentTolerance;
                if (!aligned)
                    return null;

                int sortCoordinate = commonSide is LinkSide.Top or LinkSide.Bottom ? otherCenter.X : otherCenter.Y;
                return new AlignedFanLinkCandidate(link.LinkId, sortCoordinate);
            }

            private static LinkAnchor CreateLinkAnchor(
                KbNetworkElement element,
                KbNetworkElement other,
                string linkId,
                IReadOnlyCollection<KbNetworkElement> elements,
                IReadOnlyCollection<KbNetworkLink> links)
            {
                Rectangle bounds = GetElementBounds(element);
                Point targetCenter = GetRectangleCenter(GetElementBounds(other));
                LinkSide side = ResolveLinkSide(bounds, targetCenter);
                int coordinate = ResolvePortCoordinate(element, other, linkId, side, bounds, elements, links);

                return side switch
                {
                    LinkSide.Top => new LinkAnchor(new Point(coordinate, bounds.Top), new Size(0, -1)),
                    LinkSide.Bottom => new LinkAnchor(new Point(coordinate, bounds.Bottom), new Size(0, 1)),
                    LinkSide.Left => new LinkAnchor(new Point(bounds.Left, coordinate), new Size(-1, 0)),
                    _ => new LinkAnchor(new Point(bounds.Right, coordinate), new Size(1, 0))
                };
            }

            private static LinkAnchor CreatePointerLinkAnchor(KbNetworkElement element, Point targetPoint)
            {
                Rectangle bounds = GetElementBounds(element);
                LinkSide side = ResolveLinkSide(bounds, targetPoint);
                int coordinate = side is LinkSide.Top or LinkSide.Bottom
                    ? ClampPortCoordinate(targetPoint.X, bounds.Left, bounds.Right)
                    : ClampPortCoordinate(targetPoint.Y, bounds.Top, bounds.Bottom);

                return side switch
                {
                    LinkSide.Top => new LinkAnchor(new Point(coordinate, bounds.Top), new Size(0, -1)),
                    LinkSide.Bottom => new LinkAnchor(new Point(coordinate, bounds.Bottom), new Size(0, 1)),
                    LinkSide.Left => new LinkAnchor(new Point(bounds.Left, coordinate), new Size(-1, 0)),
                    _ => new LinkAnchor(new Point(bounds.Right, coordinate), new Size(1, 0))
                };
            }

            private static LinkSide ResolveLinkSide(Rectangle bounds, Point targetCenter)
            {
                Point center = GetRectangleCenter(bounds);
                if (targetCenter.Y < bounds.Top)
                    return LinkSide.Top;

                if (targetCenter.Y > bounds.Bottom)
                    return LinkSide.Bottom;

                if (targetCenter.X < bounds.Left)
                    return LinkSide.Left;

                if (targetCenter.X > bounds.Right)
                    return LinkSide.Right;

                int dx = targetCenter.X - center.X;
                int dy = targetCenter.Y - center.Y;
                if (Math.Abs(dx) >= Math.Abs(dy))
                    return dx >= 0 ? LinkSide.Right : LinkSide.Left;

                return dy >= 0 ? LinkSide.Bottom : LinkSide.Top;
            }

            private static int ResolvePortCoordinate(
                KbNetworkElement element,
                KbNetworkElement other,
                string linkId,
                LinkSide side,
                Rectangle bounds,
                IReadOnlyCollection<KbNetworkElement> elements,
                IReadOnlyCollection<KbNetworkLink> links)
            {
                int low = side is LinkSide.Top or LinkSide.Bottom ? bounds.Left : bounds.Top;
                int high = side is LinkSide.Top or LinkSide.Bottom ? bounds.Right : bounds.Bottom;
                List<LinkPortCandidate> candidates = links
                    .Select(link => TryCreatePortCandidate(link, element, side, elements))
                    .Where(static candidate => candidate.HasValue)
                    .Select(static candidate => candidate!.Value)
                    .OrderBy(static candidate => candidate.SortCoordinate)
                    .ThenBy(static candidate => candidate.LinkId, StringComparer.Ordinal)
                    .ToList();

                if (candidates.Count <= 1)
                {
                    Point otherCenter = GetRectangleCenter(GetElementBounds(other));
                    int coordinate = side is LinkSide.Top or LinkSide.Bottom ? otherCenter.X : otherCenter.Y;
                    return ClampPortCoordinate(coordinate, low, high);
                }

                int index = candidates.FindIndex(candidate => string.Equals(candidate.LinkId, linkId, StringComparison.Ordinal));
                if (index < 0)
                    index = 0;

                int min = Math.Min(low + LinkPortInset, high);
                int max = Math.Max(high - LinkPortInset, low);
                if (min > max)
                    return low + ((high - low) / 2);

                double step = candidates.Count == 1 ? 0D : (max - min) / (double)(candidates.Count - 1);
                return (int)Math.Round(min + (index * step), MidpointRounding.AwayFromZero);
            }

            private static LinkPortCandidate? TryCreatePortCandidate(
                KbNetworkLink link,
                KbNetworkElement element,
                LinkSide side,
                IReadOnlyCollection<KbNetworkElement> elements)
            {
                string otherElementId;
                if (string.Equals(link.FromElementId, element.ElementId, StringComparison.Ordinal))
                    otherElementId = link.ToElementId;
                else if (string.Equals(link.ToElementId, element.ElementId, StringComparison.Ordinal))
                    otherElementId = link.FromElementId;
                else
                    return null;

                KbNetworkElement? other = elements.FirstOrDefault(candidate =>
                    string.Equals(candidate.ElementId, otherElementId, StringComparison.Ordinal));
                if (other == null)
                    return null;

                Rectangle bounds = GetElementBounds(element);
                Point otherCenter = GetRectangleCenter(GetElementBounds(other));
                if (ResolveLinkSide(bounds, otherCenter) != side)
                    return null;

                int sortCoordinate = side is LinkSide.Top or LinkSide.Bottom ? otherCenter.X : otherCenter.Y;
                return new LinkPortCandidate(link.LinkId, sortCoordinate);
            }

            private static int ClampPortCoordinate(int value, int low, int high)
            {
                int min = Math.Min(low + LinkPortInset, high);
                int max = Math.Max(high - LinkPortInset, low);
                return min <= max
                    ? Math.Clamp(value, min, max)
                    : low + ((high - low) / 2);
            }

            private static Point GetRectangleCenter(Rectangle bounds) =>
                new(bounds.X + bounds.Width / 2, bounds.Y + bounds.Height / 2);

            private static Point OffsetPoint(Point point, Size direction, int distance) =>
                new(point.X + (direction.Width * distance), point.Y + (direction.Height * distance));

            private static int ResolveParallelLaneCoordinate(int firstStubCoordinate, int secondStubCoordinate, int index, int count)
            {
                int low = Math.Min(firstStubCoordinate, secondStubCoordinate);
                int high = Math.Max(firstStubCoordinate, secondStubCoordinate);
                int distance = high - low;
                if (distance <= 0)
                    return firstStubCoordinate;

                int padding = Math.Min(LinkParallelLanePadding, distance / 3);
                int min = low + padding;
                int max = high - padding;
                if (min >= max)
                    return low + (distance / 2);

                double step = (max - min) / (double)(count + 1);
                return (int)Math.Round(min + ((index + 1) * step), MidpointRounding.AwayFromZero);
            }

            private static Point[] BuildBestOrthogonalRoute(
                Point start,
                Point startStub,
                Point endStub,
                Point end,
                IReadOnlyList<Rectangle> obstacles)
            {
                var candidates = new List<Point[]>();
                AddRouteCandidate(candidates, start, startStub, new Point(endStub.X, startStub.Y), endStub, end);
                AddRouteCandidate(candidates, start, startStub, new Point(startStub.X, endStub.Y), endStub, end);

                foreach (int x in GetRouteXCandidates(startStub, endStub, obstacles))
                {
                    AddRouteCandidate(
                        candidates,
                        start,
                        startStub,
                        new Point(x, startStub.Y),
                        new Point(x, endStub.Y),
                        endStub,
                        end);
                }

                foreach (int y in GetRouteYCandidates(startStub, endStub, obstacles))
                {
                    AddRouteCandidate(
                        candidates,
                        start,
                        startStub,
                        new Point(startStub.X, y),
                        new Point(endStub.X, y),
                        endStub,
                        end);
                }

                return candidates
                    .OrderBy(candidate => ScoreRoute(candidate, obstacles))
                    .ThenBy(static candidate => candidate.Length)
                    .First();
            }

            private static IEnumerable<int> GetRouteXCandidates(
                Point startStub,
                Point endStub,
                IReadOnlyList<Rectangle> obstacles)
            {
                int middleX = startStub.X + ((endStub.X - startStub.X) / 2);
                yield return middleX;
                foreach (Rectangle obstacle in obstacles)
                {
                    yield return obstacle.Left - LinkObstaclePadding;
                    yield return obstacle.Right + LinkObstaclePadding;
                }
            }

            private static IEnumerable<int> GetRouteYCandidates(
                Point startStub,
                Point endStub,
                IReadOnlyList<Rectangle> obstacles)
            {
                int middleY = startStub.Y + ((endStub.Y - startStub.Y) / 2);
                yield return middleY;
                foreach (Rectangle obstacle in obstacles)
                {
                    yield return obstacle.Top - LinkObstaclePadding;
                    yield return obstacle.Bottom + LinkObstaclePadding;
                }
            }

            private static void AddRouteCandidate(List<Point[]> candidates, params Point[] points) =>
                candidates.Add(NormalizeRoute(points));

            private static Point[] NormalizeRoute(Point[] points)
            {
                var normalized = new List<Point>();
                foreach (Point point in points)
                {
                    if (normalized.Count == 0 || normalized[^1] != point)
                        normalized.Add(point);
                }

                for (int index = normalized.Count - 2; index > 0; index--)
                {
                    Point previous = normalized[index - 1];
                    Point current = normalized[index];
                    Point next = normalized[index + 1];
                    bool sameVertical = previous.X == current.X && current.X == next.X;
                    bool sameHorizontal = previous.Y == current.Y && current.Y == next.Y;
                    if (sameVertical || sameHorizontal)
                        normalized.RemoveAt(index);
                }

                return normalized.ToArray();
            }

            private static double ScoreRoute(Point[] route, IReadOnlyList<Rectangle> obstacles)
            {
                int collisions = 0;
                double length = 0D;
                int bends = Math.Max(0, route.Length - 2);

                for (int index = 0; index < route.Length - 1; index++)
                {
                    Point start = route[index];
                    Point end = route[index + 1];
                    length += Distance(start, end);
                    foreach (Rectangle obstacle in obstacles)
                    {
                        if (SegmentIntersectsRectangle(start, end, obstacle))
                            collisions++;
                    }
                }

                return (collisions * 1_000_000D) + (bends * 2_000D) + length;
            }

            private static bool RouteIntersectsAnyObstacle(Point[] route, IReadOnlyList<Rectangle> obstacles)
            {
                for (int index = 0; index < route.Length - 1; index++)
                {
                    Point start = route[index];
                    Point end = route[index + 1];
                    foreach (Rectangle obstacle in obstacles)
                    {
                        if (SegmentIntersectsRectangle(start, end, obstacle))
                            return true;
                    }
                }

                return false;
            }

            private static bool SegmentIntersectsRectangle(Point start, Point end, Rectangle rectangle)
            {
                if (start.X == end.X)
                {
                    int top = Math.Min(start.Y, end.Y);
                    int bottom = Math.Max(start.Y, end.Y);
                    return start.X >= rectangle.Left &&
                        start.X <= rectangle.Right &&
                        bottom >= rectangle.Top &&
                        top <= rectangle.Bottom;
                }

                if (start.Y == end.Y)
                {
                    int left = Math.Min(start.X, end.X);
                    int right = Math.Max(start.X, end.X);
                    return start.Y >= rectangle.Top &&
                        start.Y <= rectangle.Bottom &&
                        right >= rectangle.Left &&
                        left <= rectangle.Right;
                }

                return false;
            }

            private static double DistanceToSegment(Point point, Point start, Point end)
            {
                double dx = end.X - start.X;
                double dy = end.Y - start.Y;
                if (Math.Abs(dx) < double.Epsilon && Math.Abs(dy) < double.Epsilon)
                    return Distance(point, start);

                double projection = ((point.X - start.X) * dx + (point.Y - start.Y) * dy) / ((dx * dx) + (dy * dy));
                projection = Math.Clamp(projection, 0D, 1D);
                var nearest = new PointF(
                    (float)(start.X + (projection * dx)),
                    (float)(start.Y + (projection * dy)));
                return Distance(point, nearest);
            }

            private static double Distance(Point point, PointF other)
            {
                double dx = point.X - other.X;
                double dy = point.Y - other.Y;
                return Math.Sqrt((dx * dx) + (dy * dy));
            }

            private enum LinkSide
            {
                Top,
                Right,
                Bottom,
                Left
            }

            private readonly record struct LinkAnchor(Point Point, Size Direction);

            private readonly record struct LinkPortCandidate(string LinkId, int SortCoordinate);

            private readonly record struct AlignedFanLinkCandidate(string LinkId, int SortCoordinate);

            private readonly record struct LinkHit(KbNetworkLink Link, bool MoveFromEndpoint);

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

        private sealed class NetworkLinkEventArgs : EventArgs
        {
            public NetworkLinkEventArgs(string linkId)
            {
                LinkId = linkId;
            }

            public string LinkId { get; }
        }

        private sealed class NetworkCanvasContextMenuEventArgs : EventArgs
        {
            public NetworkCanvasContextMenuEventArgs(Point location, Point canvasLocation, string elementId, string linkId)
            {
                Location = location;
                CanvasLocation = canvasLocation;
                ElementId = elementId;
                LinkId = linkId;
            }

            public Point Location { get; }

            public Point CanvasLocation { get; }

            public string ElementId { get; }

            public string LinkId { get; }
        }
    }
}
