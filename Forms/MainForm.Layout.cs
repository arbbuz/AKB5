using AsutpKnowledgeBase.Services;
using AsutpKnowledgeBase.UiServices;

namespace AsutpKnowledgeBase
{
    public partial class MainForm
    {
        private const int ToolbarItemHeight = 30;
        private const int ToolbarIconSize = 22;
        private const int ToolbarHeight = ToolbarItemHeight + 4;

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
                AutoSize = false,
                Dock = DockStyle.Top,
                GripStyle = ToolStripGripStyle.Hidden,
                Height = ToolbarHeight,
                ImageScalingSize = new Size(ToolbarIconSize, ToolbarIconSize),
                Padding = new Padding(4, 2, 4, 2),
                Renderer = ModernToolbarRenderer.Instance
            };

            menuFile = CreateTopToolbarMenuButton("Файл");
            menuMaintenance = CreateTopToolbarMenuButton("ТО");
            menuReferences = CreateTopToolbarMenuButton("Каталог");
            menuService = CreateTopToolbarMenuButton("Сервис");
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

            btnSave = CreatePrimaryToolbarButton("\ue161", "Сохранить", "Сохранить базу данных");
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
                RowCount = 2
            };
            leftLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            leftLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            leftLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            leftLayout.Controls.Add(CreateWorkshopSelectorPanel(), 0, 0);

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

            var treeToolStrip = new ToolStrip
            {
                AutoSize = false,
                Dock = DockStyle.Top,
                GripStyle = ToolStripGripStyle.Hidden,
                Height = ToolbarHeight,
                ImageScalingSize = new Size(ToolbarIconSize, ToolbarIconSize),
                Padding = new Padding(0, 2, 0, 2),
                Renderer = ModernToolbarRenderer.Instance
            };
            btnCollapseTree = CreateIconOnlyToolbarButton("\ue944", "Свернуть дерево до корневых элементов");
            btnCollapseTree.Alignment = ToolStripItemAlignment.Right;
            treeToolStrip.Items.Add(btnCollapseTree);

            toolTip.SetToolTip(tvTree, "Перетаскивание для перемещения, правая кнопка мыши для меню");
            grpTree.Controls.Add(tvTree);
            grpTree.Controls.Add(treeToolStrip);
            leftLayout.Controls.Add(grpTree, 0, 1);

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

            layout.Controls.Add(label, 0, 0);
            layout.Controls.Add(cmbWorkshops, 0, 1);
            panel.Controls.Add(layout);
            return panel;
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
                Margin = new Padding(0, 1, 1, 1),
                Padding = new Padding(8, 0, 8, 0),
                Size = new Size(textSize.Width + 28, ToolbarItemHeight),
                TextAlign = ContentAlignment.MiddleCenter
            };
        }

        private static ToolStripButton CreatePrimaryToolbarButton(string materialSymbolCodePoint, string text, string toolTipText) =>
            new PressFeedbackToolStripButton(CreateMaterialSymbolIcon(materialSymbolCodePoint))
            {
                AutoSize = false,
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
                TextImageRelation = TextImageRelation.ImageBeforeText,
                ToolTipText = toolTipText
            };

        private static ToolStripButton CreateIconOnlyToolbarButton(string materialSymbolCodePoint, string toolTipText) =>
            new PressFeedbackToolStripButton(CreateMaterialSymbolIcon(materialSymbolCodePoint))
            {
                AutoSize = false,
                DisplayStyle = ToolStripItemDisplayStyle.Image,
                ImageAlign = ContentAlignment.MiddleCenter,
                ImageScaling = ToolStripItemImageScaling.None,
                Margin = new Padding(0, 1, 1, 1),
                Size = new Size(ToolbarItemHeight, ToolbarItemHeight),
                Text = toolTipText,
                ToolTipText = toolTipText
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

        private sealed class ModernSectionPanel : Panel
        {
            public ModernSectionPanel()
            {
                DoubleBuffered = true;
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                base.OnPaint(e);

                var bounds = ClientRectangle;
                bounds.Width -= 1;
                bounds.Height -= 1;

                using var path = CreateRoundedRectanglePath(bounds, 5);
                using var brush = new SolidBrush(Color.FromArgb(248, 251, 253));
                using var borderPen = new Pen(Color.FromArgb(211, 219, 227));

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
