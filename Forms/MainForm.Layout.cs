using AsutpKnowledgeBase.Services;
using AsutpKnowledgeBase.UiServices;

namespace AsutpKnowledgeBase
{
    public partial class MainForm
    {
        private const int ToolbarItemHeight = 30;
        private const int ToolbarIconSize = 22;
        private const int ToolbarHeight = ToolbarItemHeight + 4;
        private const int StatusBarHeight = 24;
        private static readonly Color AppSurfaceBackColor = Color.White;
        private static readonly Color AppChromeBackColor = Color.FromArgb(247, 249, 251);
        private static readonly Color AppPanelBackColor = Color.FromArgb(251, 253, 254);
        private static readonly Color AppHairlineColor = Color.FromArgb(54, 119, 138, 156);

        private void InitializeComponent()
        {
            Text = "База знаний АСУТП";
            Size = new Size(1320, 820);
            StartPosition = FormStartPosition.CenterScreen;
            MinimumSize = new Size(1080, 640);

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
                AutoSize = false,
                Dock = DockStyle.Top,
                GripStyle = ToolStripGripStyle.Hidden,
                Height = ToolbarHeight,
                ImageScalingSize = new Size(ToolbarIconSize, ToolbarIconSize),
                Padding = new Padding(4, 2, 4, 2),
                Renderer = ModernToolbarRenderer.Instance,
                ShowItemToolTips = false
            };

            menuFile = CreateTopToolbarMenuButton("Файл");
            menuMaintenance = CreateTopToolbarMenuButton("ТО");
            menuReferences = CreateTopToolbarMenuButton("Каталог");
            menuService = CreateTopToolbarMenuButton("Сервис");
            menuSave = CreateMaterialMenuItem("Сохранить", "\ue161", BtnSave_Click);
            menuNewWorkshop = CreateMaterialMenuItem("Новый цех", "\ue145", BtnAddWorkshop_Click);
            menuDeleteWorkshop = CreateMaterialMenuItem("Удалить цех", "\ue872", BtnDeleteWorkshop_Click);
            menuRenameWorkshop = CreateMaterialMenuItem("Переименовать цех", "\ue3c9", BtnRenameWorkshop_Click);
            menuEditEquipmentCatalog = CreateMaterialMenuItem("Каталог оборудования", "\ue1a1", EditEquipmentCatalog);
            menuExportCatalogTemplates = CreateMaterialMenuItem("Экспорт справочников и шаблонов", "\ue2c6", ExportCatalogTemplates);
            menuImportCatalogTemplates = CreateMaterialMenuItem("Импорт справочников и шаблонов", "\ue2c4", ImportCatalogTemplates);
            menuExportDatabaseJson = CreateMaterialMenuItem("Экспорт текущей базы в JSON", "\ue2c6", ExportDatabaseJson);
            menuImportDatabaseJson = CreateMaterialMenuItem("Заменить текущую базу из JSON", "\ue2c4", ImportDatabaseJson);
            var menuOpenDb = CreateMaterialMenuItem("Открыть базу", "\ue2c8", BtnOpen_Click);
            var menuReloadDb = CreateMaterialMenuItem("Перезагрузить текущую базу из файла", "\ue5d5", BtnLoad_Click);
            var menuSaveAs = CreateMaterialMenuItem("Сохранить как", "\ue161", BtnSaveAs_Click);
            menuCreateSnapshot = CreateMaterialMenuItem("Создать снимок базы", "\ue14d", BtnCreateSnapshot_Click);
            menuBrowseSnapshots = CreateMaterialMenuItem("Просмотреть снимки базы", "\ue889", BtnBrowseSnapshots_Click);
            menuRestoreSnapshot = CreateMaterialMenuItem("Восстановить из снимка", "\ue8b3", BtnRestoreSnapshot_Click);
            menuCompareSnapshots = CreateMaterialMenuItem("Сравнить снимки", "\ue915", BtnCompareSnapshots_Click);
            menuBrowseChangeHistory = CreateMaterialMenuItem("История изменений", "\ue889", BtnBrowseChangeHistory_Click);
            menuSnapshotsAndHistory = CreateMaterialMenuItem("Снимки и история базы", "\ue889", BtnBrowseSnapshotsAndHistory_Click);
            menuImportMaintenanceNorms = CreateMaterialMenuItem("Импорт норм ТО", "\ue2c4", ImportMaintenanceScheduleNorms);
            menuExportMaintenanceYearScheduleSource = CreateMaterialMenuItem("Экспорт плана ТО по месяцам", "\ue2c6", ExportMaintenanceYearScheduleSource);
            menuImportMaintenanceYearScheduleSource = CreateMaterialMenuItem("Импорт плана ТО по месяцам", "\ue2c4", ImportMaintenanceYearScheduleSource);
            menuEditProductionCalendar = CreateMaterialMenuItem("Производственный календарь", "\ue878", EditProductionCalendar);
            menuImportProductionCalendarPdf = CreateMaterialMenuItem("Импорт производственного календаря PDF", "\ue2c4", ImportProductionCalendarPdf);
            menuImportProductionCalendar = CreateMaterialMenuItem("Импорт производственного календаря JSON", "\ue2c4", ImportProductionCalendar);
            menuExportMaintenanceMonthWorkbook = CreateMaterialMenuItem("Сформировать график ТО на месяц", "\ue878", ExportMaintenanceMonthWorkbook);
            menuExportMaintenanceYearWorkbook = CreateMaterialMenuItem("Сформировать годовой график ТО", "\ue878", ExportMaintenanceYearWorkbook);
            menuExportMaintenanceYearMonthlyWorkbook = CreateMaterialMenuItem("Сформировать график ТО помесячно", "\ue878", ExportMaintenanceYearMonthlyWorkbook);
            menuRecalculateMaintenanceYearWorkbook = CreateMaterialMenuItem("Пересчитать график ТО до конца года", "\ue5d5", RecalculateMaintenanceYearWorkbookToDecember);

            menuEditMaintenanceYearScheduleSource = CreateMaterialMenuItem("План ТО по месяцам", "\ue3c9", EditMaintenanceYearScheduleSource);

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

            var menuMaintenancePlanningData = CreateMaterialMenuItem("Данные для планирования", "\ue0ee");
            menuMaintenancePlanningData.DropDownItems.AddRange(new ToolStripItem[]
            {
                menuImportMaintenanceNorms,
                menuEditMaintenanceYearScheduleSource,
                menuExportMaintenanceYearScheduleSource,
                menuImportMaintenanceYearScheduleSource
            });

            var menuMaintenanceProductionCalendar = CreateMaterialMenuItem("Производственный календарь", "\ue878");
            menuMaintenanceProductionCalendar.DropDownItems.AddRange(new ToolStripItem[]
            {
                menuEditProductionCalendar,
                menuImportProductionCalendarPdf
            });

            var menuMaintenanceWorkbookGeneration = CreateMaterialMenuItem("Формирование графиков", "\uebcc");
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

            btnSave = CreatePrimaryToolbarButton("\ue161", "Сохранить");
            btnSave.Click += BtnSave_Click;
            toolStrip.Items.Add(btnSave);

            toolStrip.Items.Add(new ToolStripSeparator());

            toolStrip.Items.Add(menuFile);
            toolStrip.Items.Add(menuMaintenance);
            toolStrip.Items.Add(menuReferences);
            toolStrip.Items.Add(menuService);

            toolStrip.Items.Add(new ToolStripSeparator());

            btnUndo = CreateIconOnlyToolbarButton("\ue166", "Отменить (Ctrl+Z)");
            btnUndo.Enabled = false;
            btnRedo = CreateIconOnlyToolbarButton("\ue15a", "Повторить (Ctrl+Y)");
            btnRedo.Enabled = false;
            toolStrip.Items.AddRange(new ToolStripItem[] { btnUndo, btnRedo });

            InitializeSearchToolbarItems();
        }

        private void InitializeMainLayout()
        {
            splitMain = new KnowledgeBaseThinSplitContainer
            {
                Dock = DockStyle.Fill,
                FixedPanel = FixedPanel.Panel1,
                BackColor = AppChromeBackColor,
                SplitterWidth = 6,
                SplitterFillColor = AppChromeBackColor,
                SplitterLineColor = AppHairlineColor
            };

            InitializeLeftPanel();
            InitializeRightPanel();
        }

        private void InitializeLeftPanel()
        {
            var pnlLeft = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = AppChromeBackColor
            };

            var leftLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(10, 10, 10, 4),
                ColumnCount = 1,
                RowCount = 2
            };
            leftLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            leftLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            leftLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            leftLayout.Controls.Add(CreateWorkshopSelectorPanel(), 0, 0);
            splitMain.Panel1.Resize += (_, _) => RefreshWorkshopSelectorLayout();

            var treePanel = new ModernSectionPanel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(8, 0, 8, 8)
            };

            var treeLayout = new TableLayoutPanel
            {
                BackColor = Color.Transparent,
                ColumnCount = 1,
                Dock = DockStyle.Fill,
                RowCount = 2
            };
            treeLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            treeLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, ToolbarHeight));
            treeLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            var treeHeader = new TableLayoutPanel
            {
                BackColor = Color.Transparent,
                ColumnCount = 2,
                Dock = DockStyle.Fill,
                Margin = new Padding(0),
                RowCount = 1
            };
            treeHeader.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            treeHeader.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            treeHeader.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            var treeTitle = new Label
            {
                AutoSize = false,
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = Color.FromArgb(28, 38, 49),
                Margin = new Padding(0),
                Text = "Дерево объектов",
                TextAlign = ContentAlignment.MiddleLeft
            };

            tvTree = new KnowledgeBaseTreeView
            {
                Dock = DockStyle.Fill,
                CheckBoxes = false,
                Margin = new Padding(0),
                AllowDrop = true,
                BackColor = AppSurfaceBackColor,
                ImageList = KnowledgeBaseTreeNodeVisuals.CreateImageList()
            };

            var treeToolStrip = new ToolStrip
            {
                AutoSize = false,
                Dock = DockStyle.Fill,
                GripStyle = ToolStripGripStyle.Hidden,
                Height = ToolbarHeight,
                ImageScalingSize = new Size(ToolbarIconSize, ToolbarIconSize),
                Padding = new Padding(0, 2, 0, 2),
                Renderer = ModernSurfaceToolbarRenderer.Instance,
                ShowItemToolTips = false
            };
            btnCollapseTree = CreateIconOnlyToolbarButton("\ue944", "Свернуть дерево до корневых элементов");
            btnCollapseTree.Alignment = ToolStripItemAlignment.Right;
            treeToolStrip.Items.Add(btnCollapseTree);

            treeHeader.Controls.Add(treeTitle, 0, 0);
            treeHeader.Controls.Add(treeToolStrip, 1, 0);
            treeLayout.Controls.Add(treeHeader, 0, 0);
            treeLayout.Controls.Add(tvTree, 0, 1);
            treePanel.Controls.Add(treeLayout);
            leftLayout.Controls.Add(treePanel, 0, 1);

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
                Margin = new Padding(0, 1, 6, 1)
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
                Margin = new Padding(0, 1, 4, 1)
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
                BackColor = AppChromeBackColor,
                Padding = new Padding(6, 10, 10, 6)
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

            pnlSelectedNodeContextHeader = new ModernWorkspaceHeaderPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                BackColor = AppPanelBackColor,
                Padding = new Padding(14, 10, 14, 10),
                Visible = false
            };

            var contextHeaderLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                ColumnCount = 2,
                RowCount = 1
            };
            contextHeaderLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 44F));
            contextHeaderLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            contextHeaderLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            picSelectedNodeContextIcon = new PictureBox
            {
                Dock = DockStyle.Top,
                Size = new Size(36, 36),
                Margin = new Padding(0, 1, 8, 0),
                SizeMode = PictureBoxSizeMode.CenterImage,
                TabStop = false
            };

            var contextHeaderTextLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                ColumnCount = 1,
                RowCount = 2,
                Margin = new Padding(0)
            };
            contextHeaderTextLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            contextHeaderTextLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            contextHeaderTextLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            lblSelectedNodeContextName = new Label
            {
                Dock = DockStyle.Top,
                AutoSize = false,
                Height = 23,
                AutoEllipsis = true,
                Font = new Font("Segoe UI Semibold", 12F, FontStyle.Bold),
                ForeColor = Color.FromArgb(10, 15, 20),
                Margin = new Padding(0),
                Text = string.Empty
            };

            lblSelectedNodeContextMeta = new Label
            {
                Dock = DockStyle.Top,
                AutoSize = false,
                Height = 18,
                AutoEllipsis = true,
                Font = new Font("Segoe UI", 9F, FontStyle.Regular),
                ForeColor = Color.FromArgb(94, 107, 119),
                Margin = new Padding(0),
                Text = string.Empty
            };

            txtSelectedNodeContextPath = new TextBox
            {
                Dock = DockStyle.Top,
                BorderStyle = BorderStyle.None,
                BackColor = AppPanelBackColor,
                ReadOnly = true,
                TabStop = false,
                ForeColor = Color.DimGray,
                Text = string.Empty,
                Visible = false
            };

            contextHeaderTextLayout.Controls.Add(lblSelectedNodeContextName, 0, 0);
            contextHeaderTextLayout.Controls.Add(lblSelectedNodeContextMeta, 0, 1);
            contextHeaderLayout.Controls.Add(picSelectedNodeContextIcon, 0, 0);
            contextHeaderLayout.Controls.Add(contextHeaderTextLayout, 1, 0);
            pnlSelectedNodeContextHeader.Controls.Add(contextHeaderLayout);

            pnlSelectedNodeWorkspaceHost = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = AppSurfaceBackColor
            };

            pnlSelectedNodeWorkspaceSurface = new ModernWorkspaceSurfacePanel
            {
                Dock = DockStyle.Fill,
                BackColor = AppSurfaceBackColor,
                Padding = new Padding(1)
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
                BackColor = AppSurfaceBackColor,
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
                BackColor = AppSurfaceBackColor,
                UseVisualStyleBackColor = false,
                Tag = KnowledgeBaseNodeWorkspaceTabKind.Info
            };
            tabSelectedNodeComposition = new TabPage("Состав")
            {
                BackColor = AppSurfaceBackColor,
                UseVisualStyleBackColor = false,
                Tag = KnowledgeBaseNodeWorkspaceTabKind.Composition
            };
            tabSelectedNodeAdditionalEquipment = new TabPage("Доп. оборудование")
            {
                BackColor = AppSurfaceBackColor,
                UseVisualStyleBackColor = false,
                Tag = KnowledgeBaseNodeWorkspaceTabKind.AdditionalEquipment
            };
            tabSelectedNodeDocsAndSoftware = new TabPage("Документация и ПО")
            {
                BackColor = AppSurfaceBackColor,
                UseVisualStyleBackColor = false,
                Tag = KnowledgeBaseNodeWorkspaceTabKind.DocsAndSoftware
            };
            tabSelectedNodeNetwork = new TabPage("Сеть")
            {
                BackColor = AppSurfaceBackColor,
                UseVisualStyleBackColor = false,
                Tag = KnowledgeBaseNodeWorkspaceTabKind.Network
            };
            tabSelectedNodeMaintenance = new TabPage("График ТО")
            {
                BackColor = AppSurfaceBackColor,
                UseVisualStyleBackColor = false,
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
            selectedNodeNetworkScreen = new KnowledgeBaseNetworkTopologyScreenControl
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
            pnlSelectedNodeWorkspaceSurface.Controls.Add(rightLayout);
            pnlRight.Controls.Add(pnlSelectedNodeWorkspaceSurface);
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
                Height = StatusBarHeight - 2,
                Margin = new Padding(0),
                Padding = new Padding(0),
                TextAlign = ContentAlignment.MiddleLeft,
                Visible = false
            };
            lblSelectionInfo = new ToolStripStatusLabel
            {
                Text = string.Empty,
                BorderSides = ToolStripStatusLabelBorderSides.None,
                AutoSize = false,
                Width = 0,
                Height = StatusBarHeight - 2,
                Margin = new Padding(0),
                Padding = new Padding(0),
                TextAlign = ContentAlignment.MiddleLeft,
                Visible = false
            };
            lblLastAction = new ToolStripStatusLabel
            {
                Text = "Последнее действие: ожидание загрузки",
                AutoSize = false,
                Height = StatusBarHeight - 2,
                Margin = new Padding(0),
                Padding = new Padding(0),
                Spring = true,
                TextAlign = ContentAlignment.MiddleLeft
            };

            ssStatus = new StatusStrip
            {
                AutoSize = false,
                Dock = DockStyle.Bottom,
                Height = StatusBarHeight,
                Padding = new Padding(6, 0, 6, 0),
                SizingGrip = false,
                Stretch = false
            };
            ssStatus.Items.AddRange(new ToolStripItem[] { lblSessionInfo, lblLastAction });
            ssStatus.Resize += (_, _) => ApplyStatusBarHeight();
            ApplyStatusBarHeight();
        }

        private void ApplyStatusBarHeight()
        {
            if (ssStatus is not { IsDisposed: false })
                return;

            if (ssStatus.Height != StatusBarHeight)
                ssStatus.Height = StatusBarHeight;

            foreach (ToolStripItem item in ssStatus.Items)
            {
                item.Height = StatusBarHeight - 2;
                item.Margin = new Padding(0);
            }
        }

        private void InitializeContextMenu()
        {
            var ctxMenu = new ContextMenuStrip();

            ctxAdd = CreateMaterialMenuItem("Добавить отдел", "\ue145", (s, e) => AddNode());
            ctxCreateObjectFromCatalogAtRoot = CreateMaterialMenuItem(
                "Создать объект из каталога",
                "\ue1a1",
                (s, e) => CreateObjectFromCatalog());
            ctxCreateObjectFromTemplateAtRoot = CreateMaterialMenuItem(
                "Создать объект из шаблона",
                "\uef42",
                (s, e) => CreateObjectFromTemplate());
            ctxAddChild = CreateMaterialMenuItem("Добавить сюда", "\ue5da", (s, e) => AddChildNode());
            ctxCreateObjectFromCatalog = CreateMaterialMenuItem(
                "Создать объект из каталога",
                "\ue1a1",
                (s, e) => CreateObjectFromCatalog());
            ctxTemplates = CreateMaterialMenuItem("Шаблоны", "\uef42");
            ctxCopy = CreateMaterialMenuItem("Копировать", "\ue14d", (s, e) => CopyNode());
            ctxPaste = CreateMaterialMenuItem("Вставить", "\ue14f", (s, e) => PasteNode());
            ctxRename = CreateMaterialMenuItem("Переименовать", "\ue3c9", (s, e) => RenameNode());
            ctxDelete = CreateMaterialMenuItem("Удалить", "\ue872", (s, e) => DeleteNode());
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

        private Control CreateWorkshopSelectorPanel()
        {
            var panel = new ModernSectionPanel
            {
                Dock = DockStyle.Top,
                Height = 62,
                Margin = new Padding(0, 0, 0, 10),
                Padding = new Padding(8, 6, 8, 8)
            };

            var layout = new TableLayoutPanel
            {
                BackColor = Color.Transparent,
                ColumnCount = 1,
                Dock = DockStyle.Fill,
                RowCount = 2
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 18F));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            var label = new Label
            {
                AutoSize = false,
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = Color.FromArgb(28, 38, 49),
                Margin = new Padding(0),
                Text = "Текущий цех",
                TextAlign = ContentAlignment.MiddleLeft
            };

            cmbWorkshops = new ComboBox
            {
                Dock = DockStyle.Fill,
                DropDownStyle = ComboBoxStyle.DropDownList,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9F),
                Margin = new Padding(0, 3, 0, 0)
            };
            cmbWorkshops.SizeChanged += (_, _) => RefreshWorkshopSelectorLayout();
            layout.SizeChanged += (_, _) => RefreshWorkshopSelectorLayout();
            panel.SizeChanged += (_, _) => RefreshWorkshopSelectorLayout();

            layout.Controls.Add(label, 0, 0);
            layout.Controls.Add(cmbWorkshops, 0, 1);
            panel.Controls.Add(layout);
            return panel;
        }

        private void RefreshWorkshopSelectorLayout()
        {
            if (cmbWorkshops is not { IsDisposed: false } comboBox)
                return;

            int dropDownWidth = GetPreferredComboBoxDropDownWidth(comboBox);
            if (comboBox.DropDownWidth != dropDownWidth)
                comboBox.DropDownWidth = dropDownWidth;

            comboBox.Invalidate();
            comboBox.Parent?.Invalidate();
            comboBox.Parent?.Parent?.Invalidate();
        }

        private static int GetPreferredComboBoxDropDownWidth(ComboBox comboBox)
        {
            int width = comboBox.Width;
            foreach (object? item in comboBox.Items)
            {
                string text = comboBox.GetItemText(item);
                if (string.IsNullOrEmpty(text))
                    continue;

                width = Math.Max(
                    width,
                    TextRenderer.MeasureText(text, comboBox.Font).Width + SystemInformation.VerticalScrollBarWidth + 16);
            }

            return Math.Max(width, 1);
        }

        private static Label CreateWorkspacePlaceholderLabel() =>
            new()
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(24),
                TextAlign = ContentAlignment.MiddleCenter,
                ForeColor = Color.DimGray
            };

        private static readonly Lazy<System.Drawing.Text.PrivateFontCollection?> MaterialSymbolsFontCollection = new(LoadMaterialSymbolsFontCollection);

        private static ToolStripDropDownButton CreateTopToolbarMenuButton(string text)
        {
            var textSize = TextRenderer.MeasureText(text, SystemFonts.MenuFont);
            return new ToolStripDropDownButton(text)
            {
                AutoSize = false,
                DisplayStyle = ToolStripItemDisplayStyle.Text,
                AutoToolTip = false,
                Margin = new Padding(0, 1, 1, 1),
                Padding = new Padding(8, 0, 8, 0),
                Size = new Size(textSize.Width + 28, ToolbarItemHeight),
                TextAlign = ContentAlignment.MiddleCenter
            };
        }

        private static ToolStripMenuItem CreateMaterialMenuItem(
            string text,
            string materialSymbolCodePoint,
            EventHandler? onClick = null) =>
            new(text, CreateMaterialSymbolIcon(materialSymbolCodePoint), onClick)
            {
                AutoToolTip = false,
                ImageScaling = ToolStripItemImageScaling.None
            };

        private static ToolStripButton CreatePrimaryToolbarButton(string materialSymbolCodePoint, string text) =>
            new PressFeedbackToolStripButton(CreateMaterialSymbolIcon(materialSymbolCodePoint))
            {
                AutoSize = false,
                AutoToolTip = false,
                DisplayStyle = ToolStripItemDisplayStyle.ImageAndText,
                ImageAlign = ContentAlignment.MiddleLeft,
                ImageScaling = ToolStripItemImageScaling.None,
                IsPrimary = true,
                Margin = new Padding(0, 1, 1, 1),
                Padding = new Padding(9, 0, 12, 0),
                Size = new Size(
                    ToolbarIconSize + TextRenderer.MeasureText(text, SystemFonts.MenuFont).Width + 38,
                    ToolbarItemHeight),
                Text = text,
                TextAlign = ContentAlignment.MiddleRight,
                TextImageRelation = TextImageRelation.ImageBeforeText
            };

        private static ToolStripButton CreateIconOnlyToolbarButton(string materialSymbolCodePoint, string accessibleText) =>
            new PressFeedbackToolStripButton(CreateMaterialSymbolIcon(materialSymbolCodePoint))
            {
                AutoSize = false,
                AutoToolTip = false,
                DisplayStyle = ToolStripItemDisplayStyle.Image,
                ImageAlign = ContentAlignment.MiddleCenter,
                ImageScaling = ToolStripItemImageScaling.None,
                Margin = new Padding(0, 1, 1, 1),
                Size = new Size(ToolbarItemHeight, ToolbarItemHeight),
                Text = accessibleText
            };

        private static System.Drawing.Text.PrivateFontCollection? LoadMaterialSymbolsFontCollection()
        {
            var fontPath = Path.Combine(AppContext.BaseDirectory, "resources", "fonts", "MaterialSymbolsOutlined.ttf");
            if (!File.Exists(fontPath))
            {
                return null;
            }

            try
            {
                var collection = new System.Drawing.Text.PrivateFontCollection();
                collection.AddFontFile(fontPath);
                return collection.Families.Length > 0 ? collection : null;
            }
            catch
            {
                return null;
            }
        }

        private static Bitmap CreateMaterialSymbolIcon(string materialSymbolCodePoint)
        {
            const int iconSize = ToolbarIconSize;
            var bitmap = new Bitmap(iconSize, iconSize);
            using var graphics = Graphics.FromImage(bitmap);

            graphics.Clear(Color.Transparent);
            graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;

            var fontFamily = MaterialSymbolsFontCollection.Value?.Families.FirstOrDefault();
            if (fontFamily is null)
            {
                return bitmap;
            }

            using var font = new Font(fontFamily, 22F, FontStyle.Regular, GraphicsUnit.Pixel);
            using var brush = new SolidBrush(SystemColors.ControlText);
            using var format = new StringFormat
            {
                Alignment = StringAlignment.Center,
                LineAlignment = StringAlignment.Center
            };

            graphics.DrawString(materialSymbolCodePoint, font, brush, new RectangleF(0, 1, iconSize, iconSize), format);
            return bitmap;
        }

        private sealed class PressFeedbackToolStripButton : ToolStripButton
        {
            private readonly Image normalImage;
            private readonly Image pressedImage;
            private readonly System.Windows.Forms.Timer feedbackTimer = new() { Interval = 120 };

            public PressFeedbackToolStripButton(Image image)
            {
                normalImage = image;
                pressedImage = CreatePressedImage(image);
                Image = normalImage;
                feedbackTimer.Tick += (_, _) =>
                {
                    feedbackTimer.Stop();
                    IsPressFeedbackActive = false;
                    Image = normalImage;
                    Invalidate();
                };
            }

            public bool IsPressFeedbackActive { get; private set; }

            public bool IsPrimary { get; init; }

            protected override void OnMouseDown(MouseEventArgs e)
            {
                base.OnMouseDown(e);
                if (Enabled && e.Button == MouseButtons.Left)
                {
                    SetPressFeedback(true);
                }
            }

            protected override void OnMouseUp(MouseEventArgs e)
            {
                base.OnMouseUp(e);
                if (Enabled && e.Button == MouseButtons.Left)
                {
                    SetPressFeedback(true);
                    feedbackTimer.Stop();
                    feedbackTimer.Start();
                }
            }

            protected override void OnMouseLeave(EventArgs e)
            {
                base.OnMouseLeave(e);
                if (!feedbackTimer.Enabled)
                {
                    SetPressFeedback(false);
                }
            }

            protected override void Dispose(bool disposing)
            {
                if (disposing)
                {
                    feedbackTimer.Dispose();
                    pressedImage.Dispose();
                }

                base.Dispose(disposing);
            }

            private void SetPressFeedback(bool active)
            {
                IsPressFeedbackActive = active;
                Image = active ? pressedImage : normalImage;
                Invalidate();
            }

            private static Bitmap CreatePressedImage(Image image)
            {
                var bitmap = new Bitmap(ToolbarIconSize, ToolbarIconSize);
                using var graphics = Graphics.FromImage(bitmap);

                graphics.Clear(Color.Transparent);
                graphics.DrawImage(image, new Rectangle(1, 1, ToolbarIconSize - 1, ToolbarIconSize - 1));
                return bitmap;
            }
        }

        private sealed class ModernToolbarRenderer : ToolStripProfessionalRenderer
        {
            public static readonly ModernToolbarRenderer Instance = new();

            protected override void OnRenderToolStripBackground(ToolStripRenderEventArgs e)
            {
                using var brush = new System.Drawing.Drawing2D.LinearGradientBrush(
                    e.AffectedBounds,
                    Color.FromArgb(250, 252, 254),
                    Color.FromArgb(242, 246, 250),
                    System.Drawing.Drawing2D.LinearGradientMode.Vertical);

                e.Graphics.FillRectangle(brush, e.AffectedBounds);
            }

            protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e)
            {
                using var pen = new Pen(Color.FromArgb(211, 219, 227));
                e.Graphics.DrawLine(pen, 0, e.ToolStrip.Height - 1, e.ToolStrip.Width, e.ToolStrip.Height - 1);
            }

            protected override void OnRenderButtonBackground(ToolStripItemRenderEventArgs e)
            {
                var feedbackButton = e.Item as PressFeedbackToolStripButton;
                RenderCommandBackground(
                    e.Graphics,
                    e.Item,
                    e.Item.Pressed || feedbackButton?.IsPressFeedbackActive == true,
                    e.Item.Selected,
                    feedbackButton?.IsPrimary == true);
            }

            protected override void OnRenderDropDownButtonBackground(ToolStripItemRenderEventArgs e)
            {
                RenderCommandBackground(e.Graphics, e.Item, e.Item.Pressed, e.Item.Selected, false);
            }

            protected override void OnRenderSeparator(ToolStripSeparatorRenderEventArgs e)
            {
                var x = e.Item.Width / 2;
                using var shadowPen = new Pen(Color.FromArgb(211, 219, 227));
                using var highlightPen = new Pen(Color.White);

                e.Graphics.DrawLine(shadowPen, x, 6, x, e.Item.Height - 7);
                e.Graphics.DrawLine(highlightPen, x + 1, 6, x + 1, e.Item.Height - 7);
            }

            private static void RenderCommandBackground(Graphics graphics, ToolStripItem item, bool isPressed, bool isSelected, bool isPrimary)
            {
                var bounds = new Rectangle(Point.Empty, item.Size);
                bounds.Inflate(-1, -2);

                if (!item.Enabled)
                {
                    if (!isPrimary)
                    {
                        return;
                    }

                    DrawRoundedCommand(graphics, bounds, Color.FromArgb(238, 243, 247), Color.FromArgb(207, 216, 225), false);
                    return;
                }

                if (isPrimary)
                {
                    var fill = isPressed
                        ? Color.FromArgb(201, 227, 246)
                        : isSelected
                            ? Color.FromArgb(215, 236, 249)
                            : Color.FromArgb(228, 241, 251);
                    var border = isPressed ? Color.FromArgb(87, 138, 180) : Color.FromArgb(134, 182, 222);
                    DrawRoundedCommand(graphics, bounds, fill, border, isPressed);
                    return;
                }

                if (!isPressed && !isSelected)
                {
                    return;
                }

                DrawRoundedCommand(
                    graphics,
                    bounds,
                    isPressed ? Color.FromArgb(216, 232, 244) : Color.FromArgb(233, 243, 251),
                    isPressed ? Color.FromArgb(140, 183, 218) : Color.FromArgb(190, 213, 232),
                    isPressed);
            }

            private static void DrawRoundedCommand(Graphics graphics, Rectangle bounds, Color fillColor, Color borderColor, bool isPressed)
            {
                using var path = CreateRoundedRectanglePath(bounds, 4);
                using var brush = new SolidBrush(fillColor);
                using var borderPen = new Pen(borderColor);

                graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                graphics.FillPath(brush, path);
                graphics.DrawPath(borderPen, path);

                if (isPressed)
                {
                    using var insetPen = new Pen(Color.FromArgb(96, 125, 152));
                    graphics.DrawLine(insetPen, bounds.Left + 4, bounds.Top + 1, bounds.Right - 4, bounds.Top + 1);
                }
            }

            private static System.Drawing.Drawing2D.GraphicsPath CreateRoundedRectanglePath(Rectangle rectangle, int radius)
            {
                var path = new System.Drawing.Drawing2D.GraphicsPath();
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

        private sealed class ModernSurfaceToolbarRenderer : ToolStripProfessionalRenderer
        {
            public static readonly ModernSurfaceToolbarRenderer Instance = new();

            protected override void OnRenderToolStripBackground(ToolStripRenderEventArgs e)
            {
                using var brush = new SolidBrush(AppPanelBackColor);
                e.Graphics.FillRectangle(brush, e.AffectedBounds);
            }

            protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e)
            {
            }

            protected override void OnRenderButtonBackground(ToolStripItemRenderEventArgs e)
            {
                if (!e.Item.Enabled || (!e.Item.Selected && !e.Item.Pressed))
                    return;

                var bounds = new Rectangle(Point.Empty, e.Item.Size);
                bounds.Inflate(-2, -3);

                using var path = CreateRoundedRectanglePath(bounds, 4);
                using var fill = new SolidBrush(e.Item.Pressed ? Color.FromArgb(231, 241, 248) : Color.FromArgb(239, 246, 250));
                using var pen = new Pen(Color.FromArgb(130, 190, 213, 232), 0.5F);

                e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                e.Graphics.FillPath(fill, path);
                e.Graphics.DrawPath(pen, path);
            }

            private static System.Drawing.Drawing2D.GraphicsPath CreateRoundedRectanglePath(Rectangle rectangle, int radius)
            {
                var path = new System.Drawing.Drawing2D.GraphicsPath();
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

        private sealed class ModernSectionPanel : Panel
        {
            public ModernSectionPanel()
            {
                DoubleBuffered = true;
                SetStyle(ControlStyles.ResizeRedraw, true);
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                base.OnPaint(e);

                var bounds = ClientRectangle;
                bounds.Width -= 1;
                bounds.Height -= 1;

                using var path = CreateRoundedRectanglePath(bounds, 5);
                using var brush = new SolidBrush(AppPanelBackColor);
                using var borderPen = new Pen(AppHairlineColor, 0.25F);

                e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                e.Graphics.FillPath(brush, path);
                e.Graphics.DrawPath(borderPen, path);
            }

            private static System.Drawing.Drawing2D.GraphicsPath CreateRoundedRectanglePath(Rectangle rectangle, int radius)
            {
                var path = new System.Drawing.Drawing2D.GraphicsPath();
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

        private sealed class ModernWorkspaceSurfacePanel : Panel
        {
            public ModernWorkspaceSurfacePanel()
            {
                DoubleBuffered = true;
                SetStyle(ControlStyles.ResizeRedraw, true);
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                base.OnPaint(e);

                var bounds = ClientRectangle;
                bounds.Width -= 1;
                bounds.Height -= 1;
                if (bounds.Width <= 0 || bounds.Height <= 0)
                    return;

                using var path = CreateRoundedRectanglePath(bounds, 8);
                using var pen = new Pen(AppHairlineColor, 0.25F);

                e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                e.Graphics.DrawPath(pen, path);
            }

            private static System.Drawing.Drawing2D.GraphicsPath CreateRoundedRectanglePath(Rectangle rectangle, int radius)
            {
                var path = new System.Drawing.Drawing2D.GraphicsPath();
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

        private sealed class ModernWorkspaceHeaderPanel : Panel
        {
            public ModernWorkspaceHeaderPanel()
            {
                DoubleBuffered = true;
                SetStyle(ControlStyles.ResizeRedraw, true);
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                base.OnPaint(e);

                using var pen = new Pen(AppHairlineColor, 0.25F);
                e.Graphics.DrawLine(pen, 0, Height - 1, Width, Height - 1);
            }
        }

        private static ToolStripButton CreateSearchToolbarButton(Image image, string accessibleText) =>
            new()
            {
                Alignment = ToolStripItemAlignment.Right,
                AutoSize = false,
                AutoToolTip = false,
                DisplayStyle = ToolStripItemDisplayStyle.Image,
                Image = image,
                Margin = new Padding(0, 1, 0, 1),
                Size = new Size(28, 28),
                Text = accessibleText
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
