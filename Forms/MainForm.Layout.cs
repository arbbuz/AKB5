namespace AsutpKnowledgeBase
{
    public partial class MainForm
    {
        private void InitializeComponent()
        {
            Text = "База знаний АСУТП";
            Size = new Size(1320, 820);
            StartPosition = FormStartPosition.CenterScreen;
            MinimumSize = new Size(1080, 640);

            toolTip = new ToolTip();
            InitializeToolbar();
            InitializeMainLayout();
            InitializeStatusBar();
            InitializeContextMenu();
            InitializeEvents();

            Controls.Add(splitMain);
            Controls.Add(ssStatus);
            Controls.Add(toolStrip);

            Shown += (_, _) => ScheduleDeferredLayout();
            Resize += (_, _) => ScheduleDeferredLayout();
        }

        private void InitializeToolbar()
        {
            toolStrip = new ToolStrip
            {
                Dock = DockStyle.Top,
                GripStyle = ToolStripGripStyle.Hidden,
                ImageScalingSize = new Size(18, 18)
            };

            menuFile = new ToolStripMenuItem("📁 Файл");
            menuNewWorkshop = new ToolStripMenuItem("🏭 Новый цех", null, BtnAddWorkshop_Click);
            menuDeleteWorkshop = new ToolStripMenuItem("🗑 Удалить цех", null, BtnDeleteWorkshop_Click);
            menuRenameWorkshop = new ToolStripMenuItem("✏️ Переименовать цех", null, BtnRenameWorkshop_Click);
            var menuSetupLevels = new ToolStripMenuItem("⚙️ Настроить уровни", null, BtnSetup_Click);
            var menuOpenDb = new ToolStripMenuItem("📂 Открыть базу...", null, BtnOpen_Click);
            var menuReloadDb = new ToolStripMenuItem("🔄 Обновить из файла", null, BtnLoad_Click);
            var menuSaveAs = new ToolStripMenuItem("💾 Сохранить как...", null, BtnSaveAs_Click);
            var menuImportExcel = new ToolStripMenuItem("📥 Импорт из Excel...", null, BtnImportExcel_Click);
            var menuExportExcel = new ToolStripMenuItem("📊 Экспорт в Excel...", null, BtnExportExcel_Click);

            menuFile.DropDownItems.AddRange(new ToolStripItem[]
            {
                menuNewWorkshop,
                menuDeleteWorkshop,
                menuRenameWorkshop,
                menuSetupLevels,
                new ToolStripSeparator(),
                menuOpenDb,
                menuReloadDb,
                menuSaveAs,
                menuImportExcel,
                menuExportExcel
            });
            toolStrip.Items.Add(menuFile);
            toolStrip.Items.Add(new ToolStripSeparator());

            btnSave = new ToolStripButton("💾 Сохранить") { ToolTipText = "Сохранить базу данных" };
            btnSave.Click += BtnSave_Click;
            toolStrip.Items.Add(btnSave);

            toolStrip.Items.Add(new ToolStripSeparator());

            btnUndo = new ToolStripButton("↩ Отменить") { Enabled = false, ToolTipText = "Отменить (Ctrl+Z)" };
            btnRedo = new ToolStripButton("↪ Повторить") { Enabled = false, ToolTipText = "Повторить (Ctrl+Y)" };
            toolStrip.Items.AddRange(new ToolStripItem[] { btnUndo, btnRedo });

            InitializeSearchToolbarItems();
        }

        private void InitializeMainLayout()
        {
            splitMain = new SplitContainer
            {
                Dock = DockStyle.Fill,
                FixedPanel = FixedPanel.Panel1,
                BackColor = Color.FromArgb(230, 230, 230)
            };

            InitializeLeftPanel();
            InitializeRightPanel();
        }

        private void InitializeLeftPanel()
        {
            var pnlLeft = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(245, 245, 245)
            };

            var leftLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(12),
                ColumnCount = 1,
                RowCount = 3
            };
            leftLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            leftLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            leftLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            leftLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            leftLayout.Controls.Add(CreateSectionLabel("Текущий цех"), 0, 0);

            cmbWorkshops = new ComboBox
            {
                Dock = DockStyle.Top,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Margin = new Padding(0, 0, 0, 12)
            };
            leftLayout.Controls.Add(cmbWorkshops, 0, 1);

            var grpTree = new GroupBox
            {
                Text = "Дерево объектов",
                Dock = DockStyle.Fill,
                Padding = new Padding(10)
            };

            tvTree = new TreeView
            {
                Dock = DockStyle.Fill,
                CheckBoxes = false,
                Margin = new Padding(0),
                Font = new Font("Microsoft Sans Serif", 10),
                AllowDrop = true,
                HideSelection = false
            };

            toolTip.SetToolTip(tvTree, "Drag & Drop для перемещения, ПКМ для меню");
            grpTree.Controls.Add(tvTree);
            leftLayout.Controls.Add(grpTree, 0, 2);

            pnlLeft.Controls.Add(leftLayout);
            splitMain.Panel1.Controls.Add(pnlLeft);
        }

        private void InitializeSearchToolbarItems()
        {
            var searchSeparator = new ToolStripSeparator
            {
                Alignment = ToolStripItemAlignment.Right
            };

            txtSearch = new ToolStripTextBox
            {
                Alignment = ToolStripItemAlignment.Right,
                AutoSize = false,
                Size = new Size(220, 25),
                Margin = new Padding(0, 1, 4, 1),
                ToolTipText = "Поиск по имени, пути и уровню"
            };
            txtSearch.TextBox.PlaceholderText = "Поиск";

            btnSearch = CreateSearchToolbarButton(
                CreateSearchIcon(),
                "Найти");
            btnSearchPrev = CreateSearchToolbarButton(
                CreateChevronIcon(pointLeft: true),
                "Предыдущий результат");
            btnSearchPrev.Enabled = false;
            btnSearchNext = CreateSearchToolbarButton(
                CreateChevronIcon(pointLeft: false),
                "Следующий результат");
            btnSearchNext.Enabled = false;

            // Right-aligned ToolStrip items are rendered from right to left in insertion order.
            toolStrip.Items.AddRange(new ToolStripItem[]
            {
                btnSearchNext,
                btnSearchPrev,
                btnSearch,
                txtSearch,
                searchSeparator
            });
        }

        private void InitializeRightPanel()
        {
            var pnlRight = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White
            };

            lblSelectedNodeEmptyState = new Label
            {
                Dock = DockStyle.Fill,
                Text = "Ничего не выбрано. Выберите объект в дереве слева.",
                TextAlign = ContentAlignment.MiddleCenter,
                ForeColor = Color.DimGray,
                Font = new Font("Segoe UI", 14F, FontStyle.Regular),
                Padding = new Padding(24)
            };

            tblSelectedNodeCard = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(16),
                ColumnCount = 1,
                RowCount = 2,
                Visible = false
            };
            tblSelectedNodeCard.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tblSelectedNodeCard.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            tblSelectedNodeCard.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            var pnlHeader = new Panel
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 0, 0, 12),
                Padding = new Padding(0, 0, 0, 8)
            };

            lblSelectedNodeNameValue = new Label
            {
                Dock = DockStyle.Top,
                Height = 38,
                Font = new Font("Segoe UI Semibold", 18F, FontStyle.Bold),
                AutoEllipsis = true
            };
            lblSelectedNodeLevelValue = new Label
            {
                Dock = DockStyle.Top,
                Height = 24,
                ForeColor = Color.DimGray,
                Font = new Font("Segoe UI", 10F, FontStyle.Regular)
            };

            pnlHeader.Controls.Add(lblSelectedNodeLevelValue);
            pnlHeader.Controls.Add(lblSelectedNodeNameValue);
            tblSelectedNodeCard.Controls.Add(pnlHeader, 0, 0);

            tblDetailsLeftColumn = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3,
                Margin = new Padding(0)
            };
            tblDetailsLeftColumn.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tblDetailsLeftColumn.RowStyles.Add(new RowStyle(SizeType.Absolute, 144F));
            tblDetailsLeftColumn.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tblDetailsLeftColumn.RowStyles.Add(new RowStyle(SizeType.Absolute, 0F));

            var grpSummary = new GroupBox
            {
                Text = "Сводка",
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 0, 0, 12)
            };
            grpSummary.Controls.Add(CreateSummaryLayout());
            tblDetailsLeftColumn.Controls.Add(grpSummary, 0, 0);

            var grpCommonFields = new GroupBox
            {
                Text = "Карточка объекта",
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 0, 0, 12)
            };
            grpCommonFields.Controls.Add(CreateCommonFieldsLayout());
            tblDetailsLeftColumn.Controls.Add(grpCommonFields, 0, 1);

            grpTechnicalFields = new GroupBox
            {
                Text = "Технические поля",
                Dock = DockStyle.Fill,
                Margin = new Padding(0),
                Visible = false
            };
            grpTechnicalFields.Controls.Add(CreateTechnicalFieldsLayout());
            tblDetailsLeftColumn.Controls.Add(grpTechnicalFields, 0, 2);

            tblSelectedNodeCard.Controls.Add(tblDetailsLeftColumn, 0, 1);

            pnlRight.Controls.Add(tblSelectedNodeCard);
            pnlRight.Controls.Add(lblSelectedNodeEmptyState);
            splitMain.Panel2.Controls.Add(pnlRight);
        }

        private TableLayoutPanel CreateSummaryLayout()
        {
            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 2,
                Padding = new Padding(10, 8, 10, 10)
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110F));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 78F));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 26F));

            layout.Controls.Add(CreateFormLabel("Полный путь"), 0, 0);
            txtSelectedNodePath = new TextBox
            {
                Dock = DockStyle.Fill,
                Multiline = true,
                ReadOnly = true,
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = Color.White,
                ScrollBars = ScrollBars.Vertical,
                TabStop = false
            };
            layout.Controls.Add(txtSelectedNodePath, 1, 0);

            layout.Controls.Add(CreateFormLabel("Дочерних"), 0, 1);
            lblSelectedNodeChildrenValue = CreateReadOnlyValueLabel();
            layout.Controls.Add(lblSelectedNodeChildrenValue, 1, 1);

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
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130F));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 26F));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 118F));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 26F));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30F));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 26F));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30F));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40F));

            layout.Controls.Add(CreateFormLabel("Описание"), 0, 0);
            txtNodeDescription = new TextBox
            {
                Dock = DockStyle.Fill,
                Multiline = true,
                ScrollBars = ScrollBars.Vertical
            };
            layout.Controls.Add(txtNodeDescription, 1, 0);
            layout.SetRowSpan(txtNodeDescription, 2);

            layout.Controls.Add(CreateFormLabel("Местоположение"), 0, 2);
            txtNodeLocation = new TextBox { Dock = DockStyle.Fill };
            layout.Controls.Add(txtNodeLocation, 1, 3);

            layout.Controls.Add(CreateFormLabel("Фото"), 0, 4);
            txtNodePhotoPath = new TextBox { Dock = DockStyle.Fill };
            layout.Controls.Add(txtNodePhotoPath, 1, 5);

            var buttonPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                Margin = new Padding(0)
            };
            btnBrowsePhoto = new Button { Text = "Выбрать фото...", AutoSize = true };
            btnOpenPhoto = new Button { Text = "Открыть фото", AutoSize = true, Enabled = false };
            buttonPanel.Controls.Add(btnBrowsePhoto);
            buttonPanel.Controls.Add(btnOpenPhoto);
            layout.Controls.Add(buttonPanel, 1, 6);

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
            txtNodeIpAddress = new TextBox { Dock = DockStyle.Fill };
            layout.Controls.Add(txtNodeIpAddress, 1, 1);

            layout.Controls.Add(CreateFormLabel("Ссылка на схему"), 0, 2);
            txtNodeSchemaLink = new TextBox { Dock = DockStyle.Fill };
            layout.Controls.Add(txtNodeSchemaLink, 1, 3);

            return layout;
        }

        private void InitializeStatusBar()
        {
            lblSessionInfo = new ToolStripStatusLabel
            {
                Text = "Файл: —",
                BorderSides = ToolStripStatusLabelBorderSides.Right,
                AutoSize = false,
                Width = 430,
                TextAlign = ContentAlignment.MiddleLeft
            };
            lblSelectionInfo = new ToolStripStatusLabel
            {
                Text = "Выбранный узел: нет",
                BorderSides = ToolStripStatusLabelBorderSides.Right,
                AutoSize = false,
                Width = 360,
                TextAlign = ContentAlignment.MiddleLeft
            };
            lblLastAction = new ToolStripStatusLabel
            {
                Text = "Последнее действие: ожидание загрузки",
                Spring = true,
                TextAlign = ContentAlignment.MiddleLeft
            };

            ssStatus = new StatusStrip();
            ssStatus.Items.AddRange(new ToolStripItem[] { lblSessionInfo, lblSelectionInfo, lblLastAction });
        }

        private void InitializeContextMenu()
        {
            var ctxMenu = new ContextMenuStrip();

            ctxAdd = new ToolStripMenuItem("➕ Добавить на верхнем уровне", null, (s, e) => AddNode());
            ctxAddChild = new ToolStripMenuItem("↳ Добавить сюда", null, (s, e) => AddChildNode());
            ctxCopy = new ToolStripMenuItem("📋 Копировать", null, (s, e) => CopyNode());
            ctxPaste = new ToolStripMenuItem("📌 Вставить", null, (s, e) => PasteNode());
            ctxRename = new ToolStripMenuItem("✏️ Переименовать", null, (s, e) => RenameNode());
            ctxDelete = new ToolStripMenuItem("🗑 Удалить", null, (s, e) => DeleteNode());

            ctxMenu.Items.Add(ctxAdd);
            ctxMenu.Items.Add(ctxAddChild);
            ctxMenu.Items.Add("-");
            ctxMenu.Items.Add(ctxCopy);
            ctxMenu.Items.Add(ctxPaste);
            ctxMenu.Items.Add(ctxRename);
            ctxMenu.Items.Add("-");
            ctxMenu.Items.Add(ctxDelete);

            tvTree.ContextMenuStrip = ctxMenu;
        }

        private static Label CreateSectionLabel(string text) =>
            new()
            {
                Text = text,
                Dock = DockStyle.Top,
                AutoSize = true,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                Margin = new Padding(0, 0, 0, 6)
            };

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

        private static ToolStripButton CreateSearchToolbarButton(Image image, string toolTipText) =>
            new()
            {
                Alignment = ToolStripItemAlignment.Right,
                AutoSize = false,
                DisplayStyle = ToolStripItemDisplayStyle.Image,
                Image = image,
                Margin = new Padding(0, 1, 0, 1),
                Size = new Size(28, 28),
                Text = toolTipText,
                ToolTipText = toolTipText
            };

        private static Bitmap CreateSearchIcon()
        {
            var bitmap = new Bitmap(16, 16);
            using var graphics = Graphics.FromImage(bitmap);
            using var pen = new Pen(SystemColors.ControlText, 1.75f)
            {
                StartCap = System.Drawing.Drawing2D.LineCap.Round,
                EndCap = System.Drawing.Drawing2D.LineCap.Round
            };

            graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            graphics.DrawEllipse(pen, 2.5f, 2.5f, 7.5f, 7.5f);
            graphics.DrawLine(pen, 9.25f, 9.25f, 13f, 13f);
            return bitmap;
        }

        private static Bitmap CreateChevronIcon(bool pointLeft)
        {
            var bitmap = new Bitmap(16, 16);
            using var graphics = Graphics.FromImage(bitmap);
            using var pen = new Pen(SystemColors.ControlText, 1.9f)
            {
                StartCap = System.Drawing.Drawing2D.LineCap.Round,
                EndCap = System.Drawing.Drawing2D.LineCap.Round
            };

            graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            var points = pointLeft
                ? new[] { new PointF(10.25f, 3.5f), new PointF(5.75f, 8f), new PointF(10.25f, 12.5f) }
                : new[] { new PointF(5.75f, 3.5f), new PointF(10.25f, 8f), new PointF(5.75f, 12.5f) };

            graphics.DrawLines(pen, points);
            return bitmap;
        }
    }
}
