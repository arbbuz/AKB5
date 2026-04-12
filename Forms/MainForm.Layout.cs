using System.Drawing;
using System.Windows.Forms;

namespace AsutpKnowledgeBase
{
    public partial class MainForm
    {
        private void InitializeComponent()
        {
            Text = "База знаний АСУТП";
            Size = new Size(1120, 760);
            StartPosition = FormStartPosition.CenterScreen;
            MinimumSize = new Size(920, 580);

            toolTip = new ToolTip();
            InitializeToolbar();
            InitializeLeftPanel();
            InitializeRightPanel();
            InitializeStatusBar();
            InitializeContextMenu();
            InitializeEvents();
            Controls.Add(toolStrip);
        }

        private void InitializeToolbar()
        {
            toolStrip = new ToolStrip
            {
                Dock = DockStyle.Top,
                GripStyle = ToolStripGripStyle.Hidden
            };

            menuFile = new ToolStripMenuItem("📁 Файл");
            menuNewWorkshop = new ToolStripMenuItem("🏭 Новый цех", null, BtnAddWorkshop_Click);
            var menuSetupLevels = new ToolStripMenuItem("⚙️ Настроить уровни", null, BtnSetup_Click);
            var menuOpenDb = new ToolStripMenuItem("📂 Открыть базу...", null, BtnOpen_Click);
            var menuReloadDb = new ToolStripMenuItem("🔄 Обновить из файла", null, BtnLoad_Click);
            var menuSaveAs = new ToolStripMenuItem("💾 Сохранить как...", null, BtnSaveAs_Click);
            var menuImportExcel = new ToolStripMenuItem("📥 Импорт из Excel...", null, BtnImportExcel_Click);
            var menuExportExcel = new ToolStripMenuItem("📊 Экспорт в Excel...", null, BtnExportExcel_Click);

            menuFile.DropDownItems.AddRange(new ToolStripItem[]
            {
                menuNewWorkshop,
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
        }

        private void InitializeLeftPanel()
        {
            var pnlLeft = new Panel { Dock = DockStyle.Fill };
            tvTree = new TreeView
            {
                Dock = DockStyle.Fill,
                CheckBoxes = false,
                Margin = new Padding(0),
                Font = new Font("Microsoft Sans Serif", 10),
                AllowDrop = true
            };

            toolTip.SetToolTip(tvTree, "Drag & Drop для перемещения, ПКМ для меню");
            pnlLeft.Controls.Add(tvTree);
            Controls.Add(pnlLeft);
        }

        private void InitializeRightPanel()
        {
            var pnlRight = new Panel
            {
                Dock = DockStyle.Right,
                Width = 320,
                AutoScroll = true,
                BackColor = Color.FromArgb(245, 245, 245)
            };

            int y = 15;

            var grpSession = new GroupBox
            {
                Text = "Сеанс",
                Location = new Point(15, y),
                Size = new Size(285, 215),
                Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top
            };

            int groupY = 24;
            grpSession.Controls.Add(CreateFieldCaption("Файл", 12, groupY));
            groupY += 18;

            lblCurrentFileNameValue = CreateValueLabel(12, groupY, 257);
            grpSession.Controls.Add(lblCurrentFileNameValue);
            groupY += 24;

            grpSession.Controls.Add(CreateFieldCaption("Полный путь к JSON", 12, groupY));
            groupY += 18;

            txtCurrentFilePath = new TextBox
            {
                Location = new Point(12, groupY),
                Size = new Size(257, 48),
                Multiline = true,
                ReadOnly = true,
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = Color.White,
                ScrollBars = ScrollBars.Vertical,
                TabStop = false,
                Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top
            };
            grpSession.Controls.Add(txtCurrentFilePath);
            groupY += 58;

            grpSession.Controls.Add(CreateFieldCaption("Состояние", 12, groupY));
            groupY += 18;

            lblSaveStateValue = CreateValueLabel(12, groupY, 257);
            grpSession.Controls.Add(lblSaveStateValue);
            groupY += 28;

            grpSession.Controls.Add(CreateFieldCaption("Текущий цех", 12, groupY));
            groupY += 18;

            cmbWorkshops = new ComboBox
            {
                Location = new Point(12, groupY),
                Size = new Size(257, 25),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top
            };
            grpSession.Controls.Add(cmbWorkshops);

            pnlRight.Controls.Add(grpSession);
            y += grpSession.Height + 15;

            var lblSearch = new Label
            {
                Text = "Поиск по имени, пути и уровню",
                AutoSize = true,
                Location = new Point(15, y)
            };
            pnlRight.Controls.Add(lblSearch);
            y += 22;

            txtSearch = new TextBox
            {
                Location = new Point(15, y),
                Size = new Size(285, 25),
                Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top
            };
            pnlRight.Controls.Add(txtSearch);
            y += 35;

            var pnlSearchNav = new TableLayoutPanel
            {
                Location = new Point(15, y),
                Size = new Size(285, 30),
                ColumnCount = 3,
                RowCount = 1,
                Padding = new Padding(0),
                Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top
            };
            pnlSearchNav.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33F));
            pnlSearchNav.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33F));
            pnlSearchNav.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33F));

            btnSearchPrev = new Button
            {
                Text = "◀ Пред.",
                Dock = DockStyle.Fill,
                Enabled = false,
                Margin = new Padding(0, 0, 2, 0)
            };
            btnSearchNext = new Button
            {
                Text = "След. ▶",
                Dock = DockStyle.Fill,
                Enabled = false,
                Margin = new Padding(2, 0, 2, 0)
            };
            btnSearch = new Button
            {
                Text = "🔍",
                Dock = DockStyle.Fill,
                Margin = new Padding(2, 0, 0, 0)
            };

            pnlSearchNav.Controls.Add(btnSearchPrev, 0, 0);
            pnlSearchNav.Controls.Add(btnSearchNext, 1, 0);
            pnlSearchNav.Controls.Add(btnSearch, 2, 0);
            pnlRight.Controls.Add(pnlSearchNav);
            y += 42;

            var grpSelectedNode = new GroupBox
            {
                Text = "Выбранный узел",
                Location = new Point(15, y),
                Size = new Size(285, 210),
                Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top
            };

            lblSelectedNodeEmptyState = new Label
            {
                Location = new Point(12, 28),
                Size = new Size(257, 42),
                Text = "Ничего не выбрано. Выберите узел в дереве слева.",
                AutoEllipsis = true
            };
            grpSelectedNode.Controls.Add(lblSelectedNodeEmptyState);

            tblSelectedNodeDetails = new TableLayoutPanel
            {
                Location = new Point(12, 24),
                Size = new Size(257, 172),
                ColumnCount = 2,
                RowCount = 4,
                Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top
            };
            tblSelectedNodeDetails.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 92F));
            tblSelectedNodeDetails.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tblSelectedNodeDetails.RowStyles.Add(new RowStyle(SizeType.Absolute, 26F));
            tblSelectedNodeDetails.RowStyles.Add(new RowStyle(SizeType.Absolute, 26F));
            tblSelectedNodeDetails.RowStyles.Add(new RowStyle(SizeType.Absolute, 70F));
            tblSelectedNodeDetails.RowStyles.Add(new RowStyle(SizeType.Absolute, 26F));

            tblSelectedNodeDetails.Controls.Add(CreateFieldCaption("Имя", 0, 0), 0, 0);
            lblSelectedNodeNameValue = CreateTableValueLabel();
            tblSelectedNodeDetails.Controls.Add(lblSelectedNodeNameValue, 1, 0);

            tblSelectedNodeDetails.Controls.Add(CreateFieldCaption("Уровень", 0, 0), 0, 1);
            lblSelectedNodeLevelValue = CreateTableValueLabel();
            tblSelectedNodeDetails.Controls.Add(lblSelectedNodeLevelValue, 1, 1);

            tblSelectedNodeDetails.Controls.Add(CreateFieldCaption("Полный путь", 0, 0), 0, 2);
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
            tblSelectedNodeDetails.Controls.Add(txtSelectedNodePath, 1, 2);

            tblSelectedNodeDetails.Controls.Add(CreateFieldCaption("Дочерних", 0, 0), 0, 3);
            lblSelectedNodeChildrenValue = CreateTableValueLabel();
            tblSelectedNodeDetails.Controls.Add(lblSelectedNodeChildrenValue, 1, 3);

            grpSelectedNode.Controls.Add(tblSelectedNodeDetails);
            pnlRight.Controls.Add(grpSelectedNode);

            Controls.Add(pnlRight);
        }

        private void InitializeStatusBar()
        {
            lblSessionInfo = new ToolStripStatusLabel
            {
                Text = "Файл: —",
                BorderSides = ToolStripStatusLabelBorderSides.Right,
                AutoSize = false,
                Width = 360,
                TextAlign = ContentAlignment.MiddleLeft
            };
            lblSelectionInfo = new ToolStripStatusLabel
            {
                Text = "Выбранный узел: нет",
                BorderSides = ToolStripStatusLabelBorderSides.Right,
                AutoSize = false,
                Width = 300,
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
            Controls.Add(ssStatus);
        }

        private void InitializeContextMenu()
        {
            var ctxMenu = new ContextMenuStrip();

            ctxAdd = new ToolStripMenuItem("➕ Добавить сюда", null, (s, e) => AddNode());
            ctxCopy = new ToolStripMenuItem("📋 Копировать", null, (s, e) => CopyNode());
            ctxPaste = new ToolStripMenuItem("📌 Вставить", null, (s, e) => PasteNode());
            ctxRename = new ToolStripMenuItem("✏️ Переименовать", null, (s, e) => RenameNode());
            ctxDelete = new ToolStripMenuItem("🗑 Удалить", null, (s, e) => DeleteNode());

            ctxMenu.Items.Add(ctxAdd);
            ctxMenu.Items.Add("-");
            ctxMenu.Items.Add(ctxCopy);
            ctxMenu.Items.Add(ctxPaste);
            ctxMenu.Items.Add(ctxRename);
            ctxMenu.Items.Add("-");
            ctxMenu.Items.Add(ctxDelete);

            tvTree.ContextMenuStrip = ctxMenu;
        }

        private static Label CreateFieldCaption(string text, int x, int y) =>
            new()
            {
                Text = text,
                AutoSize = true,
                Location = new Point(x, y)
            };

        private static Label CreateValueLabel(int x, int y, int width) =>
            new()
            {
                Location = new Point(x, y),
                Size = new Size(width, 18),
                AutoEllipsis = true
            };

        private static Label CreateTableValueLabel() =>
            new()
            {
                AutoEllipsis = true,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft
            };
    }
}
