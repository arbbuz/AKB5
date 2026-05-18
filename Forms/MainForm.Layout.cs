using AsutpKnowledgeBase.Services;
using AsutpKnowledgeBase.UiServices;

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

            menuFile = new ToolStripMenuItem("📃 Файл");
            menuMaintenance = new ToolStripMenuItem("🗓 ТО");
            menuReferences = new ToolStripMenuItem("📚 Справочники");
            menuService = new ToolStripMenuItem("🔧 Сервис");
            menuSave = new ToolStripMenuItem("💾 Сохранить", null, BtnSave_Click);
            menuNewWorkshop = new ToolStripMenuItem("🏭 Новый цех", null, BtnAddWorkshop_Click);
            menuDeleteWorkshop = new ToolStripMenuItem("🗑 Удалить цех", null, BtnDeleteWorkshop_Click);
            menuRenameWorkshop = new ToolStripMenuItem("✏️ Переименовать цех", null, BtnRenameWorkshop_Click);
            menuEditEquipmentCatalog = new ToolStripMenuItem("📚 Каталог оборудования", null, EditEquipmentCatalog);
            menuExportCatalogTemplates = new ToolStripMenuItem("📤 Экспорт справочников и шаблонов", null, ExportCatalogTemplates);
            menuImportCatalogTemplates = new ToolStripMenuItem("📥 Импорт справочников и шаблонов", null, ImportCatalogTemplates);
            menuExportDatabaseJson = new ToolStripMenuItem("📤 Экспорт текущей базы в JSON", null, ExportDatabaseJson);
            menuImportDatabaseJson = new ToolStripMenuItem("📥 Заменить текущую базу из JSON", null, ImportDatabaseJson);
            var menuOpenDb = new ToolStripMenuItem("📂 Открыть базу", null, BtnOpen_Click);
            var menuReloadDb = new ToolStripMenuItem("🔄 Перезагрузить текущую базу из файла", null, BtnLoad_Click);
            var menuSaveAs = new ToolStripMenuItem("💾 Сохранить как", null, BtnSaveAs_Click);
            menuCreateSnapshot = new ToolStripMenuItem("Создать снимок базы", null, BtnCreateSnapshot_Click);
            menuBrowseSnapshots = new ToolStripMenuItem("Просмотреть снимки базы", null, BtnBrowseSnapshots_Click);
            menuRestoreSnapshot = new ToolStripMenuItem("Восстановить из снимка", null, BtnRestoreSnapshot_Click);
            menuCompareSnapshots = new ToolStripMenuItem("Сравнить снимки", null, BtnCompareSnapshots_Click);
            menuBrowseChangeHistory = new ToolStripMenuItem("История изменений", null, BtnBrowseChangeHistory_Click);
            menuSnapshotsAndHistory = new ToolStripMenuItem("Снимки и история базы", null, BtnBrowseSnapshotsAndHistory_Click);
            menuImportMaintenanceNorms = new ToolStripMenuItem("📥 Импорт норм ТО", null, ImportMaintenanceScheduleNorms);
            menuExportMaintenanceYearScheduleSource = new ToolStripMenuItem("📤 Экспорт плана ТО по месяцам", null, ExportMaintenanceYearScheduleSource);
            menuImportMaintenanceYearScheduleSource = new ToolStripMenuItem("📥 Импорт плана ТО по месяцам", null, ImportMaintenanceYearScheduleSource);
            menuEditProductionCalendar = new ToolStripMenuItem("🗓 Производственный календарь", null, EditProductionCalendar);
            menuImportProductionCalendarPdf = new ToolStripMenuItem("📥 Импорт производственного календаря PDF", null, ImportProductionCalendarPdf);
            menuImportProductionCalendar = new ToolStripMenuItem("📥 Импорт производственного календаря JSON", null, ImportProductionCalendar);
            menuExportMaintenanceMonthWorkbook = new ToolStripMenuItem("🗓 Сформировать график ТО на месяц", null, ExportMaintenanceMonthWorkbook);
            menuExportMaintenanceYearWorkbook = new ToolStripMenuItem("🗓 Сформировать годовой график ТО", null, ExportMaintenanceYearWorkbook);
            menuExportMaintenanceYearMonthlyWorkbook = new ToolStripMenuItem("🗓 Сформировать график ТО помесячно", null, ExportMaintenanceYearMonthlyWorkbook);
            menuRecalculateMaintenanceYearWorkbook = new ToolStripMenuItem("🗓 Пересчитать график ТО до конца года", null, RecalculateMaintenanceYearWorkbookToDecember);

            menuEditMaintenanceYearScheduleSource = new ToolStripMenuItem("✏️ План ТО по месяцам", null, EditMaintenanceYearScheduleSource);

            menuFile.DropDownItems.AddRange(new ToolStripItem[]
            {
                menuSave,
                menuOpenDb,
                menuSaveAs,
                new ToolStripSeparator(),
                menuNewWorkshop,
                menuDeleteWorkshop,
                menuRenameWorkshop,
                new ToolStripSeparator(),
                menuSnapshotsAndHistory
            });

            menuReferences.DropDownItems.AddRange(new ToolStripItem[]
            {
                menuEditEquipmentCatalog
            });

            menuService.DropDownItems.AddRange(new ToolStripItem[]
            {
                menuReloadDb,
                new ToolStripSeparator(),
                menuExportCatalogTemplates,
                menuImportCatalogTemplates,
                new ToolStripSeparator(),
                menuExportDatabaseJson,
                menuImportDatabaseJson
            });

            var menuMaintenancePlanningData = new ToolStripMenuItem("Данные для планирования");
            menuMaintenancePlanningData.DropDownItems.AddRange(new ToolStripItem[]
            {
                menuImportMaintenanceNorms,
                menuEditMaintenanceYearScheduleSource,
                menuExportMaintenanceYearScheduleSource,
                menuImportMaintenanceYearScheduleSource
            });

            var menuMaintenanceProductionCalendar = new ToolStripMenuItem("Производственный календарь");
            menuMaintenanceProductionCalendar.DropDownItems.AddRange(new ToolStripItem[]
            {
                menuEditProductionCalendar,
                menuImportProductionCalendarPdf
            });

            var menuMaintenanceWorkbookGeneration = new ToolStripMenuItem("Формирование графиков");
            menuMaintenanceWorkbookGeneration.DropDownItems.AddRange(new ToolStripItem[]
            {
                menuExportMaintenanceMonthWorkbook,
                menuExportMaintenanceYearWorkbook,
                menuExportMaintenanceYearMonthlyWorkbook,
                menuRecalculateMaintenanceYearWorkbook
            });

            menuMaintenance.DropDownItems.AddRange(new ToolStripItem[]
            {
                menuMaintenancePlanningData,
                menuMaintenanceProductionCalendar,
                menuMaintenanceWorkbookGeneration
            });
            toolStrip.Items.Add(menuFile);
            toolStrip.Items.Add(menuMaintenance);
            toolStrip.Items.Add(menuReferences);
            toolStrip.Items.Add(menuService);
            toolStrip.Items.Add(new ToolStripSeparator());

            btnSave = new ToolStripButton("💾 Сохранить") { ToolTipText = "Сохранить базу данных" };
            btnSave.Click += BtnSave_Click;
            toolStrip.Items.Add(btnSave);

            toolStrip.Items.Add(new ToolStripSeparator());

            btnUndo = new ToolStripButton("↩ Отменить") { Enabled = false, ToolTipText = "Отменить (Ctrl+Z)" };
            btnRedo = new ToolStripButton("↪ Повторить") { Enabled = false, ToolTipText = "Повторить (Ctrl+Y)" };
            btnCollapseTree = new ToolStripButton("🗂 Свернуть")
            {
                ToolTipText = "Свернуть дерево до корневых элементов"
            };
            toolStrip.Items.AddRange(new ToolStripItem[] { btnUndo, btnRedo, btnCollapseTree });

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

            tvTree = new KnowledgeBaseTreeView
            {
                Dock = DockStyle.Fill,
                CheckBoxes = false,
                Margin = new Padding(0),
                AllowDrop = true,
                ImageList = KnowledgeBaseTreeNodeVisuals.CreateImageList()
            };

            toolTip.SetToolTip(tvTree, "Перетаскивание для перемещения, правая кнопка мыши для меню");
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

            cmbSearchScope = new ToolStripComboBox
            {
                Alignment = ToolStripItemAlignment.Right,
                AutoSize = false,
                Size = new Size(130, 25),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Margin = new Padding(0, 1, 6, 1),
                ToolTipText = "Область поиска"
            };
            cmbSearchScope.Items.AddRange(new object[]
            {
                new SearchScopeOption(KnowledgeBaseSearchScope.All, "Все"),
                new SearchScopeOption(KnowledgeBaseSearchScope.Tree, "Дерево"),
                new SearchScopeOption(KnowledgeBaseSearchScope.Card, "Карточка"),
                new SearchScopeOption(KnowledgeBaseSearchScope.Composition, "Состав"),
                new SearchScopeOption(KnowledgeBaseSearchScope.DocsAndSoftware, "Документация и ПО")
            });
            cmbSearchScope.SelectedIndex = 0;

            txtSearch = new ToolStripTextBox
            {
                Alignment = ToolStripItemAlignment.Right,
                AutoSize = false,
                Size = new Size(220, 25),
                Margin = new Padding(0, 1, 4, 1),
                ToolTipText = "Поиск по выбранной области"
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
                cmbSearchScope,
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

            var rightLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
                Padding = new Padding(0)
            };
            rightLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            rightLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            rightLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            pnlSelectedNodeContextHeader = new Panel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                BackColor = Color.FromArgb(250, 250, 250),
                Padding = new Padding(14, 6, 14, 6),
                Visible = false
            };

            var contextHeaderLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                ColumnCount = 1,
                RowCount = 1
            };
            contextHeaderLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            contextHeaderLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            lblSelectedNodeContextName = new Label
            {
                Dock = DockStyle.Top,
                AutoSize = false,
                Height = 22,
                AutoEllipsis = true,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                ForeColor = Color.FromArgb(30, 30, 30),
                Margin = new Padding(0),
                Text = string.Empty
            };

            txtSelectedNodeContextPath = new TextBox
            {
                Dock = DockStyle.Top,
                BorderStyle = BorderStyle.None,
                BackColor = Color.FromArgb(250, 250, 250),
                ReadOnly = true,
                TabStop = false,
                ForeColor = Color.DimGray,
                Text = string.Empty,
                Visible = false
            };

            contextHeaderLayout.Controls.Add(lblSelectedNodeContextName, 0, 0);
            pnlSelectedNodeContextHeader.Controls.Add(contextHeaderLayout);

            pnlSelectedNodeWorkspaceHost = new Panel
            {
                Dock = DockStyle.Fill
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

            pnlSelectedNodeInfoScreen = new Panel
            {
                Dock = DockStyle.Fill,
                Visible = false
            };

            tabSelectedNodeWorkspace = new TabControl
            {
                Dock = DockStyle.Fill,
                Visible = false,
                TabStop = false
            };

            tabSelectedNodeInfo = new TabPage("Карточка")
            {
                Tag = KnowledgeBaseNodeWorkspaceTabKind.Info
            };
            tabSelectedNodeComposition = new TabPage("Состав")
            {
                Tag = KnowledgeBaseNodeWorkspaceTabKind.Composition
            };
            tabSelectedNodeAdditionalEquipment = new TabPage("Доп. оборудование")
            {
                Tag = KnowledgeBaseNodeWorkspaceTabKind.AdditionalEquipment
            };
            tabSelectedNodeDocsAndSoftware = new TabPage("Документация и ПО")
            {
                Tag = KnowledgeBaseNodeWorkspaceTabKind.DocsAndSoftware
            };
            tabSelectedNodeNetwork = new TabPage("Сеть")
            {
                Tag = KnowledgeBaseNodeWorkspaceTabKind.Network
            };

            tabSelectedNodeMaintenance = new TabPage("График ТО")
            {
                Tag = KnowledgeBaseNodeWorkspaceTabKind.Maintenance
            };

            lblSelectedNodeDocsPlaceholder = CreateWorkspacePlaceholderLabel();

            selectedNodeCompositionScreen = new KnowledgeBaseCompositionScreenControl
            {
                Dock = DockStyle.Fill
            };
            selectedNodeAdditionalEquipmentScreen = new KnowledgeBaseAdditionalEquipmentScreenControl
            {
                Dock = DockStyle.Fill
            };
            selectedNodeDocsAndSoftwareScreen = new KnowledgeBaseDocsAndSoftwareScreenControl
            {
                Dock = DockStyle.Fill
            };
            selectedNodeNetworkScreen = new KnowledgeBaseNetworkScreenControl
            {
                Dock = DockStyle.Fill
            };
            selectedNodeMaintenanceScreen = new KnowledgeBaseMaintenanceScheduleScreenControl
            {
                Dock = DockStyle.Fill
            };

            tabSelectedNodeComposition.Controls.Add(selectedNodeCompositionScreen);
            tabSelectedNodeAdditionalEquipment.Controls.Add(selectedNodeAdditionalEquipmentScreen);
            tabSelectedNodeDocsAndSoftware.Controls.Add(selectedNodeDocsAndSoftwareScreen);
            tabSelectedNodeNetwork.Controls.Add(selectedNodeNetworkScreen);
            tabSelectedNodeMaintenance.Controls.Add(selectedNodeMaintenanceScreen);

            selectedNodeInfoScreen = new KnowledgeBaseInfoScreenControl
            {
                Dock = DockStyle.Fill,
                Visible = false
            };

            pnlSelectedNodeInfoScreen.Controls.Add(selectedNodeInfoScreen);
            tabSelectedNodeWorkspace.TabPages.Add(tabSelectedNodeInfo);
            tabSelectedNodeWorkspace.TabPages.Add(tabSelectedNodeComposition);
            tabSelectedNodeWorkspace.TabPages.Add(tabSelectedNodeAdditionalEquipment);
            tabSelectedNodeWorkspace.TabPages.Add(tabSelectedNodeDocsAndSoftware);
            tabSelectedNodeWorkspace.TabPages.Add(tabSelectedNodeNetwork);
            tabSelectedNodeWorkspace.TabPages.Add(tabSelectedNodeMaintenance);

            pnlSelectedNodeWorkspaceHost.Controls.Add(pnlSelectedNodeInfoScreen);
            pnlSelectedNodeWorkspaceHost.Controls.Add(tabSelectedNodeWorkspace);
            pnlSelectedNodeWorkspaceHost.Controls.Add(lblSelectedNodeEmptyState);
            rightLayout.Controls.Add(pnlSelectedNodeContextHeader, 0, 0);
            rightLayout.Controls.Add(pnlSelectedNodeWorkspaceHost, 0, 1);
            pnlRight.Controls.Add(rightLayout);
            splitMain.Panel2.Controls.Add(pnlRight);
        }

        private void InitializeStatusBar()
        {
            lblSessionInfo = new ToolStripStatusLabel
            {
                Text = string.Empty,
                BorderSides = ToolStripStatusLabelBorderSides.Right,
                AutoSize = false,
                Width = 540,
                TextAlign = ContentAlignment.MiddleLeft,
                Visible = false
            };
            lblSelectionInfo = new ToolStripStatusLabel
            {
                Text = string.Empty,
                BorderSides = ToolStripStatusLabelBorderSides.None,
                AutoSize = false,
                Width = 0,
                TextAlign = ContentAlignment.MiddleLeft,
                Visible = false
            };
            lblLastAction = new ToolStripStatusLabel
            {
                Text = "Последнее действие: ожидание загрузки",
                Spring = true,
                TextAlign = ContentAlignment.MiddleLeft
            };

            ssStatus = new StatusStrip();
            ssStatus.Items.AddRange(new ToolStripItem[] { lblSessionInfo, lblLastAction });
        }

        private void InitializeContextMenu()
        {
            var ctxMenu = new ContextMenuStrip();

            ctxAdd = new ToolStripMenuItem("➕ Добавить отдел", null, (s, e) => AddNode());
            ctxCreateObjectFromCatalogAtRoot = new ToolStripMenuItem(
                "Создать объект из каталога",
                null,
                (s, e) => CreateObjectFromCatalog());
            ctxCreateObjectFromTemplateAtRoot = new ToolStripMenuItem(
                "Создать объект из шаблона",
                null,
                (s, e) => CreateObjectFromTemplate());
            ctxAddChild = new ToolStripMenuItem("↳ Добавить сюда", null, (s, e) => AddChildNode());
            ctxCreateObjectFromCatalog = new ToolStripMenuItem(
                "Создать объект из каталога",
                null,
                (s, e) => CreateObjectFromCatalog());
            ctxTemplates = new ToolStripMenuItem("Шаблоны");
            ctxCopy = new ToolStripMenuItem("📋 Копировать", null, (s, e) => CopyNode());
            ctxPaste = new ToolStripMenuItem("📌 Вставить", null, (s, e) => PasteNode());
            ctxRename = new ToolStripMenuItem("✏️ Переименовать", null, (s, e) => RenameNode());
            ctxDelete = new ToolStripMenuItem("🗑 Удалить", null, (s, e) => DeleteNode());
            ctxEditSeparator = new ToolStripSeparator();
            ctxDeleteSeparator = new ToolStripSeparator();

            ctxMenu.Items.Add(ctxAdd);
            ctxMenu.Items.Add(ctxCreateObjectFromCatalogAtRoot);
            ctxMenu.Items.Add(ctxCreateObjectFromTemplateAtRoot);
            ctxMenu.Items.Add(ctxAddChild);
            ctxMenu.Items.Add(ctxCreateObjectFromCatalog);
            ctxMenu.Items.Add(ctxTemplates);
            ctxMenu.Items.Add(ctxEditSeparator);
            ctxMenu.Items.Add(ctxCopy);
            ctxMenu.Items.Add(ctxPaste);
            ctxMenu.Items.Add(ctxRename);
            ctxMenu.Items.Add(ctxDeleteSeparator);
            ctxMenu.Items.Add(ctxDelete);
            ctxMenu.Opening += (_, _) => ApplyTreeContextMenuVisibility();

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

        private static Label CreateWorkspacePlaceholderLabel() =>
            new()
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(24),
                TextAlign = ContentAlignment.MiddleCenter,
                ForeColor = Color.DimGray
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
