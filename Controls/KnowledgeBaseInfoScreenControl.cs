using AsutpKnowledgeBase.Services;

namespace AsutpKnowledgeBase
{
    public sealed class KnowledgeBaseInfoScreenControl : UserControl
    {
        private TextBox _txtSelectedNodePath = null!;
        private Label _lblSelectedNodeChildrenValue = null!;
        private Label _lblNodeInventoryNumber = null!;
        private TextBox _txtNodeInventoryNumber = null!;
        private TextBox _txtNodeDescription = null!;
        private TextBox _txtNodeIpAddress = null!;
        private TextBox _txtNodeSchemaLink = null!;
        private TableLayoutPanel _tblDetailsLeftColumn = null!;
        private KnowledgeBaseWorkspaceVisuals.SectionPanel _grpTechnicalFields = null!;

        public KnowledgeBaseInfoScreenControl()
        {
            Dock = DockStyle.Fill;
            BackColor = KnowledgeBaseWorkspaceVisuals.SurfaceColor;

            var selectedNodeCard = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                BackColor = KnowledgeBaseWorkspaceVisuals.SurfaceColor,
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

            var grpSummary = new KnowledgeBaseWorkspaceVisuals.SectionPanel
            {
                Text = "Сводка",
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Margin = new Padding(0, 0, 0, 12)
            };
            grpSummary.Controls.Add(CreateSummaryLayout());
            _tblDetailsLeftColumn.Controls.Add(grpSummary, 0, 0);

            var grpCommonFields = new KnowledgeBaseWorkspaceVisuals.SectionPanel
            {
                Text = "Карточка объекта",
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 0, 0, 12)
            };
            grpCommonFields.Controls.Add(CreateCommonFieldsLayout());
            _tblDetailsLeftColumn.Controls.Add(grpCommonFields, 0, 1);

            _grpTechnicalFields = new KnowledgeBaseWorkspaceVisuals.SectionPanel
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

        public event EventHandler? InventoryNumberChangedByUser;

        public event EventHandler? IpAddressChangedByUser;

        public event EventHandler? SchemaLinkChangedByUser;

        public string DescriptionText => _txtNodeDescription.Text;

        public string InventoryNumberText => _txtNodeInventoryNumber.Text;

        public string IpAddressText => _txtNodeIpAddress.Text;

        public string SchemaLinkText => _txtNodeSchemaLink.Text;

        public void ApplyState(KnowledgeBaseSelectedNodeState selectedNodeState)
        {
            _txtSelectedNodePath.Text = selectedNodeState.FullPath;
            _lblSelectedNodeChildrenValue.Text = selectedNodeState.ChildrenCountText;
            _txtNodeDescription.Text = selectedNodeState.Description;
            _txtNodeInventoryNumber.Text = selectedNodeState.InventoryNumber;
            _txtNodeIpAddress.Text = selectedNodeState.IpAddress;
            _txtNodeSchemaLink.Text = selectedNodeState.SchemaLink;
            SetInventoryNumberVisibility(selectedNodeState.ShowInventoryNumber);
            SetTechnicalFieldsVisibility(selectedNodeState.ShowTechnicalFields);
        }

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
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                ColumnCount = 2,
                RowCount = 2,
                Padding = new Padding(10, 8, 10, 10)
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110F));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 26F));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 118F));

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

            return layout;
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
            Control? inventoryFrame = _txtNodeInventoryNumber.Parent;
            if (inventoryFrame?.Parent is TableLayoutPanel summaryLayout && summaryLayout.RowStyles.Count > 2)
            {
                summaryLayout.RowStyles[2].Height = visible ? 30F : 0F;
                summaryLayout.PerformLayout();
            }

            _lblNodeInventoryNumber.Visible = visible;
            _txtNodeInventoryNumber.Visible = visible;
            _txtNodeInventoryNumber.Enabled = visible;
            inventoryFrame!.Visible = visible;
            inventoryFrame.Enabled = visible;

            if (!visible)
                _txtNodeInventoryNumber.Text = string.Empty;

            inventoryFrame.PerformLayout();
            _tblDetailsLeftColumn.PerformLayout();
        }

        private static ModernInfoFieldPanel CreateTextFieldFrame(TextBox textBox, bool multiline = false)
        {
            textBox.BorderStyle = BorderStyle.None;
            textBox.Margin = new Padding(0);
            textBox.BackColor = KnowledgeBaseWorkspaceVisuals.SurfaceColor;

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
                ForeColor = KnowledgeBaseWorkspaceVisuals.MutedTextColor,
                Margin = new Padding(0, 0, 8, 0)
            };

        private static Label CreateReadOnlyValueLabel() =>
            new()
            {
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                ForeColor = KnowledgeBaseWorkspaceVisuals.TextColor,
                AutoEllipsis = true
            };

        private sealed class ModernInfoFieldPanel : Panel
        {
            public ModernInfoFieldPanel()
            {
                DoubleBuffered = true;
                BackColor = KnowledgeBaseWorkspaceVisuals.SurfaceColor;
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
                    : KnowledgeBaseWorkspaceVisuals.HairlineColor;
                var borderWidth = ContainsFocus ? 1F : 0.25F;

                using var pen = new Pen(borderColor, borderWidth);
                e.Graphics.DrawRectangle(pen, bounds);
            }
        }
    }
}
