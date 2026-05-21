using System.Drawing.Drawing2D;
using AsutpKnowledgeBase.Services;

namespace AsutpKnowledgeBase
{
    public sealed class KnowledgeBaseInfoScreenControl : UserControl
    {
        private TextBox _txtSelectedNodePath = null!;
        private Label _lblSelectedNodeChildrenValue = null!;
        private Label _lblNodeInventoryNumber = null!;
        private TextBox _txtNodeInventoryNumber = null!;
        private Label _lblNodeLocation = null!;
        private Label _lblNodePhotoPath = null!;
        private TextBox _txtNodeDescription = null!;
        private TextBox _txtNodeLocation = null!;
        private TextBox _txtNodePhotoPath = null!;
        private TextBox _txtNodeIpAddress = null!;
        private TextBox _txtNodeSchemaLink = null!;
        private TableLayoutPanel _tblDetailsLeftColumn = null!;
        private ModernInfoSectionPanel _grpTechnicalFields = null!;
        private FlowLayoutPanel _pnlPhotoButtons = null!;
        private Button _btnBrowsePhoto = null!;
        private Button _btnOpenPhoto = null!;

        public KnowledgeBaseInfoScreenControl()
        {
            Dock = DockStyle.Fill;

            var selectedNodeCard = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Padding = new Padding(14),
                ColumnCount = 1,
                RowCount = 1
            };
            selectedNodeCard.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            selectedNodeCard.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            _tblDetailsLeftColumn = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3,
                Margin = new Padding(0)
            };
            _tblDetailsLeftColumn.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            _tblDetailsLeftColumn.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            _tblDetailsLeftColumn.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            _tblDetailsLeftColumn.RowStyles.Add(new RowStyle(SizeType.Absolute, 0F));

            var grpSummary = new ModernInfoSectionPanel
            {
                Text = "Сводка",
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Margin = new Padding(0, 0, 0, 12)
            };
            grpSummary.Controls.Add(CreateSummaryLayout());
            _tblDetailsLeftColumn.Controls.Add(grpSummary, 0, 0);

            var grpCommonFields = new ModernInfoSectionPanel
            {
                Text = "Карточка объекта",
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 0, 0, 12)
            };
            grpCommonFields.Controls.Add(CreateCommonFieldsLayout());
            _tblDetailsLeftColumn.Controls.Add(grpCommonFields, 0, 1);

            _grpTechnicalFields = new ModernInfoSectionPanel
            {
                Text = "Технические поля",
                Dock = DockStyle.Fill,
                Margin = new Padding(0),
                Visible = false
            };
            _grpTechnicalFields.Controls.Add(CreateTechnicalFieldsLayout());
            _tblDetailsLeftColumn.Controls.Add(_grpTechnicalFields, 0, 2);

            selectedNodeCard.Controls.Add(_tblDetailsLeftColumn, 0, 0);
            Controls.Add(selectedNodeCard);
        }

        public event EventHandler? DescriptionChangedByUser;

        public event EventHandler? LocationChangedByUser;

        public event EventHandler? InventoryNumberChangedByUser;

        public event EventHandler? PhotoPathChangedByUser;

        public event EventHandler? IpAddressChangedByUser;

        public event EventHandler? SchemaLinkChangedByUser;

        public event EventHandler? BrowsePhotoRequested;

        public event EventHandler? OpenPhotoRequested;

        public string DescriptionText => _txtNodeDescription.Text;

        public string LocationText => _txtNodeLocation.Text;

        public string InventoryNumberText => _txtNodeInventoryNumber.Text;

        public string PhotoPathText
        {
            get => _txtNodePhotoPath.Text;
            set => _txtNodePhotoPath.Text = value;
        }

        public string IpAddressText => _txtNodeIpAddress.Text;

        public string SchemaLinkText => _txtNodeSchemaLink.Text;

        public void ApplyState(KnowledgeBaseSelectedNodeState selectedNodeState)
        {
            _txtSelectedNodePath.Text = selectedNodeState.FullPath;
            _lblSelectedNodeChildrenValue.Text = selectedNodeState.ChildrenCountText;
            _txtNodeDescription.Text = selectedNodeState.Description;
            _txtNodeLocation.Text = selectedNodeState.Location;
            _txtNodeInventoryNumber.Text = selectedNodeState.InventoryNumber;
            _txtNodePhotoPath.Text = selectedNodeState.PhotoPath;
            _txtNodeIpAddress.Text = selectedNodeState.IpAddress;
            _txtNodeSchemaLink.Text = selectedNodeState.SchemaLink;
            SetDescriptionLayout(!selectedNodeState.ShowLocation && !selectedNodeState.ShowPhoto);
            SetInventoryNumberVisibility(selectedNodeState.ShowInventoryNumber);
            SetLocationVisibility(selectedNodeState.ShowLocation);
            SetPhotoVisibility(selectedNodeState.ShowPhoto);
            SetTechnicalFieldsVisibility(selectedNodeState.ShowTechnicalFields);
            UpdatePhotoControlsState(selectedNodeState.PhotoPath);
        }

        public void UpdatePhotoControlsState(string photoPath) =>
            _btnOpenPhoto.Enabled =
                _pnlPhotoButtons.Visible &&
                !string.IsNullOrWhiteSpace(photoPath) &&
                File.Exists(photoPath.Trim());

        private TableLayoutPanel CreateSummaryLayout()
        {
            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                ColumnCount = 2,
                RowCount = 3,
                Padding = new Padding(10, 8, 10, 10)
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110F));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 78F));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 26F));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 0F));

            layout.Controls.Add(CreateFormLabel("Полный путь"), 0, 0);
            _txtSelectedNodePath = new TextBox
            {
                Dock = DockStyle.Fill,
                Multiline = true,
                ReadOnly = true,
                BackColor = Color.White,
                ScrollBars = ScrollBars.Vertical,
                TabStop = false
            };
            layout.Controls.Add(CreateTextFieldFrame(_txtSelectedNodePath, multiline: true), 1, 0);

            layout.Controls.Add(CreateFormLabel("Дочерних"), 0, 1);
            _lblSelectedNodeChildrenValue = CreateReadOnlyValueLabel();
            layout.Controls.Add(_lblSelectedNodeChildrenValue, 1, 1);

            _lblNodeInventoryNumber = CreateFormLabel("Инв. номер");
            _lblNodeInventoryNumber.Visible = false;
            layout.Controls.Add(_lblNodeInventoryNumber, 0, 2);

            _txtNodeInventoryNumber = new TextBox
            {
                Dock = DockStyle.Fill,
                Visible = false
            };
            _txtNodeInventoryNumber.TextChanged += (_, _) => InventoryNumberChangedByUser?.Invoke(this, EventArgs.Empty);
            layout.Controls.Add(CreateTextFieldFrame(_txtNodeInventoryNumber), 1, 2);

            return layout;
        }

        private TableLayoutPanel CreateCommonFieldsLayout()
        {
            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 7,
                Padding = new Padding(10, 8, 10, 10)
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110F));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 26F));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 118F));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 26F));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30F));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 26F));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30F));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40F));

            layout.Controls.Add(CreateFormLabel("Описание"), 0, 0);
            _txtNodeDescription = new TextBox
            {
                Dock = DockStyle.Fill,
                Multiline = true,
                ScrollBars = ScrollBars.Vertical
            };
            _txtNodeDescription.TextChanged += (_, _) => DescriptionChangedByUser?.Invoke(this, EventArgs.Empty);
            var descriptionField = CreateTextFieldFrame(_txtNodeDescription, multiline: true);
            layout.Controls.Add(descriptionField, 1, 0);
            layout.SetRowSpan(descriptionField, 2);

            _lblNodeLocation = CreateFormLabel("Местоположение");
            layout.Controls.Add(_lblNodeLocation, 0, 2);
            _txtNodeLocation = new TextBox { Dock = DockStyle.Fill };
            _txtNodeLocation.TextChanged += (_, _) => LocationChangedByUser?.Invoke(this, EventArgs.Empty);
            layout.Controls.Add(CreateTextFieldFrame(_txtNodeLocation), 1, 3);

            _lblNodePhotoPath = CreateFormLabel("Фото");
            layout.Controls.Add(_lblNodePhotoPath, 0, 4);
            _txtNodePhotoPath = new TextBox { Dock = DockStyle.Fill };
            _txtNodePhotoPath.TextChanged += (_, _) => PhotoPathChangedByUser?.Invoke(this, EventArgs.Empty);
            layout.Controls.Add(CreateTextFieldFrame(_txtNodePhotoPath), 1, 5);

            _pnlPhotoButtons = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                Margin = new Padding(0)
            };
            _btnBrowsePhoto = new Button { Text = "Выбрать фото", AutoSize = true };
            _btnBrowsePhoto.Click += (_, _) => BrowsePhotoRequested?.Invoke(this, EventArgs.Empty);
            _btnOpenPhoto = new Button { Text = "Открыть фото", AutoSize = true, Enabled = false };
            _btnOpenPhoto.Click += (_, _) => OpenPhotoRequested?.Invoke(this, EventArgs.Empty);
            _pnlPhotoButtons.Controls.Add(_btnBrowsePhoto);
            _pnlPhotoButtons.Controls.Add(_btnOpenPhoto);
            layout.Controls.Add(_pnlPhotoButtons, 1, 6);

            return layout;
        }

        private void SetDescriptionLayout(bool expanded)
        {
            if (_txtNodeDescription.Parent is not TableLayoutPanel layout || layout.RowStyles.Count <= 1)
                return;

            layout.RowStyles[1].Height = expanded ? 262F : 118F;
            _txtNodeDescription.MinimumSize = new Size(0, expanded ? 288 : 144);
            layout.PerformLayout();
        }

        private TableLayoutPanel CreateTechnicalFieldsLayout()
        {
            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 4,
                Padding = new Padding(10, 8, 10, 10)
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130F));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 26F));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30F));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 26F));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30F));

            layout.Controls.Add(CreateFormLabel("IP-адрес"), 0, 0);
            _txtNodeIpAddress = new TextBox { Dock = DockStyle.Fill };
            _txtNodeIpAddress.TextChanged += (_, _) => IpAddressChangedByUser?.Invoke(this, EventArgs.Empty);
            layout.Controls.Add(CreateTextFieldFrame(_txtNodeIpAddress), 1, 1);

            layout.Controls.Add(CreateFormLabel("Ссылка на схему"), 0, 2);
            _txtNodeSchemaLink = new TextBox { Dock = DockStyle.Fill };
            _txtNodeSchemaLink.TextChanged += (_, _) => SchemaLinkChangedByUser?.Invoke(this, EventArgs.Empty);
            layout.Controls.Add(CreateTextFieldFrame(_txtNodeSchemaLink), 1, 3);

            return layout;
        }

        private void SetTechnicalFieldsVisibility(bool visible)
        {
            _grpTechnicalFields.Visible = visible;
            _tblDetailsLeftColumn.RowStyles[2].Height = visible ? 150F : 0F;
            _tblDetailsLeftColumn.PerformLayout();
        }

        private void SetInventoryNumberVisibility(bool visible)
        {
            if (_txtNodeInventoryNumber.Parent is TableLayoutPanel summaryLayout && summaryLayout.RowStyles.Count > 2)
                summaryLayout.RowStyles[2].Height = visible ? 30F : 0F;

            _lblNodeInventoryNumber.Visible = visible;
            _txtNodeInventoryNumber.Visible = visible;
            _txtNodeInventoryNumber.Enabled = visible;
            _txtNodeInventoryNumber.Parent!.Visible = visible;
            _txtNodeInventoryNumber.Parent.Enabled = visible;

            if (!visible)
                _txtNodeInventoryNumber.Text = string.Empty;

            _txtNodeInventoryNumber.Parent?.PerformLayout();
            _tblDetailsLeftColumn.PerformLayout();
        }

        private void SetLocationVisibility(bool visible)
        {
            if (_txtNodeLocation.Parent is not TableLayoutPanel layout || layout.RowStyles.Count <= 3)
                return;

            layout.RowStyles[2].Height = visible ? 26F : 0F;
            layout.RowStyles[3].Height = visible ? 30F : 0F;
            _lblNodeLocation.Visible = visible;
            _txtNodeLocation.Visible = visible;
            _txtNodeLocation.Enabled = visible;
            _txtNodeLocation.Parent!.Visible = visible;
            _txtNodeLocation.Parent.Enabled = visible;

            if (!visible)
                _txtNodeLocation.Text = string.Empty;

            layout.PerformLayout();
        }

        private void SetPhotoVisibility(bool visible)
        {
            if (_txtNodePhotoPath.Parent is not TableLayoutPanel layout || layout.RowStyles.Count <= 6)
                return;

            layout.RowStyles[4].Height = visible ? 26F : 0F;
            layout.RowStyles[5].Height = visible ? 30F : 0F;
            layout.RowStyles[6].Height = visible ? 40F : 0F;
            _lblNodePhotoPath.Visible = visible;
            _txtNodePhotoPath.Visible = visible;
            _txtNodePhotoPath.Enabled = visible;
            _txtNodePhotoPath.Parent!.Visible = visible;
            _txtNodePhotoPath.Parent.Enabled = visible;
            _pnlPhotoButtons.Visible = visible;
            _btnBrowsePhoto.Enabled = visible;

            if (!visible)
            {
                _txtNodePhotoPath.Text = string.Empty;
                _btnOpenPhoto.Enabled = false;
            }
            else
            {
                UpdatePhotoControlsState(_txtNodePhotoPath.Text);
            }

            layout.PerformLayout();
        }

        private static ModernInfoFieldPanel CreateTextFieldFrame(TextBox textBox, bool multiline = false)
        {
            textBox.BorderStyle = BorderStyle.None;
            textBox.Margin = new Padding(0);
            textBox.BackColor = Color.White;

            var frame = new ModernInfoFieldPanel
            {
                Dock = DockStyle.Fill,
                Margin = textBox.Margin,
                Padding = multiline ? new Padding(6, 4, 4, 4) : new Padding(6, 5, 6, 3)
            };

            textBox.Dock = DockStyle.Fill;
            textBox.Enter += (_, _) => frame.Invalidate();
            textBox.Leave += (_, _) => frame.Invalidate();
            frame.Controls.Add(textBox);
            return frame;
        }

        private static Label CreateFormLabel(string text) =>
            new()
            {
                Text = text,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                Margin = new Padding(0, 0, 8, 0)
            };

        private static Label CreateReadOnlyValueLabel() =>
            new()
            {
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                AutoEllipsis = true
            };

        private sealed class ModernInfoSectionPanel : Panel
        {
            private static readonly Color FillColor = Color.FromArgb(251, 253, 254);
            private static readonly Color BorderColor = Color.FromArgb(54, 211, 219, 227);
            private static readonly Color TitleColor = Color.FromArgb(79, 95, 109);

            public ModernInfoSectionPanel()
            {
                DoubleBuffered = true;
                BackColor = FillColor;
                Padding = new Padding(10, 18, 10, 10);
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                base.OnPaint(e);

                var borderBounds = ClientRectangle;
                borderBounds.X += 1;
                borderBounds.Y += 8;
                borderBounds.Width -= 2;
                borderBounds.Height -= 9;
                if (borderBounds.Width <= 0 || borderBounds.Height <= 0)
                    return;

                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using var path = CreateRoundedRectanglePath(borderBounds, 6);
                using var borderPen = new Pen(BorderColor, 0.25F);
                e.Graphics.DrawPath(borderPen, path);

                if (string.IsNullOrWhiteSpace(Text))
                    return;

                var titleSize = TextRenderer.MeasureText(Text, Font);
                var titleBounds = new Rectangle(14, 0, titleSize.Width + 8, 18);
                using var titleBackBrush = new SolidBrush(FillColor);
                e.Graphics.FillRectangle(titleBackBrush, titleBounds);
                TextRenderer.DrawText(
                    e.Graphics,
                    Text,
                    Font,
                    new Point(18, 1),
                    TitleColor,
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

        private sealed class ModernInfoFieldPanel : Panel
        {
            public ModernInfoFieldPanel()
            {
                DoubleBuffered = true;
                BackColor = Color.White;
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                base.OnPaint(e);

                var bounds = ClientRectangle;
                bounds.Width -= 1;
                bounds.Height -= 1;
                if (bounds.Width <= 0 || bounds.Height <= 0)
                    return;

                var borderColor = ContainsFocus
                    ? Color.FromArgb(138, 187, 225)
                    : Color.FromArgb(231, 237, 243);
                var borderWidth = ContainsFocus ? 1F : 0.25F;

                using var pen = new Pen(borderColor, borderWidth);
                e.Graphics.DrawRectangle(pen, bounds);
            }
        }
    }
}
