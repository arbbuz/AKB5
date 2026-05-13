using AsutpKnowledgeBase.Models;
using AsutpKnowledgeBase.Services;
using AsutpKnowledgeBase.UiServices;

namespace AsutpKnowledgeBase
{
    /// <summary>
    /// Главная форма приложения. Отвечает за UI и координацию работы сервисов.
    /// </summary>
    public partial class MainForm : Form
    {
        private const int DefaultSplitterDistance = 340;
        private const int NavigationPanelMinSize = 260;
        private const int DetailsPanelMinSize = 480;

        private readonly IAppLogger _appLogger;
        private readonly KnowledgeBaseSessionService _session = new();
        private readonly KnowledgeBaseExcelUiWorkflowService _excelUiWorkflowService;
        private readonly KnowledgeBaseMaintenanceWorkbookUiWorkflowService _maintenanceWorkbookUiWorkflowService;
        private readonly KnowledgeBaseFileUiWorkflowService _fileUiWorkflowService;
        private readonly KnowledgeBaseSessionWorkflowService _sessionWorkflowService;
        private readonly KnowledgeBaseTreeMutationUiWorkflowService _treeMutationUiWorkflowService;
        private readonly KnowledgeBaseWorkshopUiWorkflowService _workshopUiWorkflowService;
        private readonly KnowledgeBaseTreeController _treeController;
        private readonly KnowledgeBaseTreeMutationWorkflowService _treeMutationWorkflowService;
        private readonly KnowledgeBaseCompositionMutationService _compositionMutationService = new();
        private readonly KnowledgeBaseCompositionTemplateService _compositionTemplateService = new();
        private readonly KnowledgeBaseDocsAndSoftwareMutationService _docsAndSoftwareMutationService = new();
        private readonly KnowledgeBaseNetworkMutationService _networkMutationService = new();
        private readonly KnowledgeBaseMaintenanceScheduleProfileMutationService _maintenanceScheduleProfileMutationService = new();
        private readonly KnowledgeBaseMaintenanceScheduleNormImportService _maintenanceScheduleNormImportService = new();
        private readonly KnowledgeBaseMaintenanceYearScheduleSourceService _maintenanceYearScheduleSourceService = new();
        private readonly KnowledgeBaseMaintenanceYearScheduleSourceExchangeService _maintenanceYearScheduleSourceExchangeService = new();
        private readonly KnowledgeBaseProductionCalendarJsonImportService _productionCalendarJsonImportService = new();
        private readonly KnowledgeBaseProductionCalendarPdfImportService _productionCalendarPdfImportService = new();
        private readonly KnowledgeBaseCatalogTemplateExchangeService _catalogTemplateExchangeService = new();
        private readonly KnowledgeBaseFormStateService _formStateService = new();
        private readonly KnowledgeBaseNodePresentationService _nodePresentationService = new();
        private readonly KnowledgeBaseTreeViewService _treeViewService = new();
        private readonly UndoRedoService _history = new(50);
        private readonly KnowledgeBasePortableStorageSettingsService _storageSettingsService;
        private readonly KnowledgeBaseWindowLayoutStateService _windowLayoutStateService;
        private int? _savedSplitterDistance;
        private string? _lastSelectedWorkspaceNodeId;

        private bool _isBindingWorkshops;
        private bool _isApplyingSelectedNodeState;
        private bool _isApplyingDeferredLayout;

        private ToolStrip toolStrip = null!;
        private ToolStripButton btnUndo = null!;
        private ToolStripButton btnRedo = null!;
        private ToolStripButton btnSave = null!;
        private ToolStripButton btnCollapseTree = null!;
        private ToolStripMenuItem menuFile = null!;
        private ToolStripMenuItem menuMaintenance = null!;
        private ToolStripMenuItem menuReferences = null!;
        private ToolStripMenuItem menuService = null!;
        private ToolStripMenuItem menuSave = null!;
        private ToolStripMenuItem menuNewWorkshop = null!;
        private ToolStripMenuItem menuRenameWorkshop = null!;
        private ToolStripMenuItem menuDeleteWorkshop = null!;
        private ToolStripMenuItem menuEditEquipmentCatalog = null!;
        private ToolStripMenuItem menuExportCatalogTemplates = null!;
        private ToolStripMenuItem menuImportCatalogTemplates = null!;
        private ToolStripMenuItem menuExportDatabaseJson = null!;
        private ToolStripMenuItem menuImportDatabaseJson = null!;
        private ToolStripMenuItem menuSnapshotsAndHistory = null!;
        private ToolStripMenuItem menuCreateSnapshot = null!;
        private ToolStripMenuItem menuBrowseSnapshots = null!;
        private ToolStripMenuItem menuRestoreSnapshot = null!;
        private ToolStripMenuItem menuCompareSnapshots = null!;
        private ToolStripMenuItem menuBrowseChangeHistory = null!;
        private ToolStripMenuItem menuImportMaintenanceNorms = null!;
        private ToolStripMenuItem menuEditMaintenanceYearScheduleSource = null!;
        private ToolStripMenuItem menuExportMaintenanceYearScheduleSource = null!;
        private ToolStripMenuItem menuImportMaintenanceYearScheduleSource = null!;
        private ToolStripMenuItem menuEditProductionCalendar = null!;
        private ToolStripMenuItem menuImportProductionCalendar = null!;
        private ToolStripMenuItem menuImportProductionCalendarPdf = null!;
        private ToolStripMenuItem menuExportMaintenanceMonthWorkbook = null!;
        private ToolStripMenuItem menuExportMaintenanceYearWorkbook = null!;
        private ToolStripMenuItem menuExportMaintenanceYearMonthlyWorkbook = null!;
        private ToolStripMenuItem menuRecalculateMaintenanceYearWorkbook = null!;

        private SplitContainer splitMain = null!;
        private ComboBox cmbWorkshops = null!;
        private TreeView tvTree = null!;
        private ToolStripComboBox cmbSearchScope = null!;
        private ToolStripTextBox txtSearch = null!;
        private ToolStripButton btnSearchPrev = null!;
        private ToolStripButton btnSearchNext = null!;
        private ToolStripButton btnSearch = null!;
        private StatusStrip ssStatus = null!;
        private ToolStripStatusLabel lblSessionInfo = null!;
        private ToolStripStatusLabel lblSelectionInfo = null!;
        private ToolStripStatusLabel lblLastAction = null!;
        private ToolTip toolTip = null!;
        private ToolStripMenuItem ctxAdd = null!;
        private ToolStripMenuItem ctxAddChild = null!;
        private ToolStripMenuItem ctxTemplates = null!;
        private ToolStripMenuItem ctxCreateObjectFromTemplateAtRoot = null!;
        private ToolStripMenuItem ctxCreateObjectFromTemplate = null!;
        private ToolStripMenuItem ctxSaveObjectAsTemplate = null!;
        private ToolStripMenuItem ctxCopy = null!;
        private ToolStripMenuItem ctxPaste = null!;
        private ToolStripMenuItem ctxRename = null!;
        private ToolStripMenuItem ctxDelete = null!;
        private ToolStripSeparator ctxEditSeparator = null!;
        private ToolStripSeparator ctxDeleteSeparator = null!;
        private Label lblSelectedNodeEmptyState = null!;
        private Panel pnlSelectedNodeContextHeader = null!;
        private Label lblSelectedNodeContextName = null!;
        private TextBox txtSelectedNodeContextPath = null!;
        private Panel pnlSelectedNodeWorkspaceHost = null!;
        private Panel pnlSelectedNodeInfoScreen = null!;
        private TabControl tabSelectedNodeWorkspace = null!;
        private TabPage tabSelectedNodeInfo = null!;
        private TabPage tabSelectedNodeComposition = null!;
        private TabPage tabSelectedNodeDocsAndSoftware = null!;
        private TabPage tabSelectedNodeNetwork = null!;
        private TabPage tabSelectedNodeMaintenance = null!;
        private KnowledgeBaseInfoScreenControl selectedNodeInfoScreen = null!;
        private KnowledgeBaseCompositionScreenControl selectedNodeCompositionScreen = null!;
        private KnowledgeBaseDocsAndSoftwareScreenControl selectedNodeDocsAndSoftwareScreen = null!;
        private KnowledgeBaseNetworkScreenControl selectedNodeNetworkScreen = null!;
        private KnowledgeBaseMaintenanceScheduleScreenControl selectedNodeMaintenanceScreen = null!;
        private Label lblSelectedNodeDocsPlaceholder = null!;

        private KbConfig _config => _session.Config;
        private string _currentWorkshop => _session.CurrentWorkshop;
        private string _lastSavedWorkshop => _session.LastSavedWorkshop;
        private bool _isDirty => _session.IsDirty;
        private bool _requiresSave => _session.RequiresSave;

        public MainForm()
            : this(NullAppLogger.Instance)
        {
        }

        public MainForm(IAppLogger appLogger)
        {
            _appLogger = appLogger ?? NullAppLogger.Instance;
            _storageSettingsService = new KnowledgeBasePortableStorageSettingsService(AppContext.BaseDirectory);
            _treeController = new KnowledgeBaseTreeController(_session);
            _windowLayoutStateService = new KnowledgeBaseWindowLayoutStateService(logger: _appLogger);
            InitializeComponent();
            InitializeTemplateContextMenuItem();
            AppIconProvider.Apply(this);
            _savedSplitterDistance = _windowLayoutStateService.LoadSplitterDistance();
            RestoreSavedWindowLayout();
            var startupStorage = CreateStartupStorageService();
            var fileWorkflowService = new KnowledgeBaseFileWorkflowService(
                _session,
                startupStorage.StorageService,
                _appLogger);
            _excelUiWorkflowService = new KnowledgeBaseExcelUiWorkflowService(
                new KnowledgeBaseExcelExchangeService(_appLogger));
            _maintenanceWorkbookUiWorkflowService = new KnowledgeBaseMaintenanceWorkbookUiWorkflowService();
            _fileUiWorkflowService = new KnowledgeBaseFileUiWorkflowService(
                fileWorkflowService,
                _formStateService);
            _sessionWorkflowService = new KnowledgeBaseSessionWorkflowService(_session);
            _workshopUiWorkflowService = new KnowledgeBaseWorkshopUiWorkflowService(
                _session,
                _sessionWorkflowService,
                _history);
            _treeMutationWorkflowService = new KnowledgeBaseTreeMutationWorkflowService(
                _session,
                _sessionWorkflowService,
                _treeController,
                _history);
            _treeMutationUiWorkflowService = new KnowledgeBaseTreeMutationUiWorkflowService(
                _treeMutationWorkflowService);
            FormClosing += MainForm_FormClosing;
            _fileUiWorkflowService.LoadData(CreateFileUiWorkflowContext());
            if (!string.IsNullOrWhiteSpace(startupStorage.StatusText))
                SetLastActionText(startupStorage.StatusText);
        }

        private static string GetDefaultJsonPath() =>
            KnowledgeBaseStoragePaths.GetLegacyJsonPath();

        private StartupStorageServiceSelection CreateStartupStorageService()
        {
            string startupPath = ResolveStartupDatabasePath();
            string legacyJsonPath = GetDefaultJsonPath();
            string statusText = string.Empty;

            TryCopyPreviousDefaultDatabase(startupPath, ref statusText);

            var migrationService = new KnowledgeBaseFirstLaunchMigrationService(_appLogger);
            KnowledgeBaseFirstLaunchMigrationPlan migrationPlan =
                migrationService.CreatePlan(startupPath, legacyJsonPath);

            if (migrationPlan.ShouldOfferMigration)
            {
                DialogResult confirmation = MessageBox.Show(
                    this,
                    $"Найдена старая JSON-база:\n{legacyJsonPath}\n\n" +
                    $"Перенести данные в новую базу SQLite?\n{startupPath}\n\n" +
                    "Старый JSON-файл останется без изменений.",
                    "Переход на базу .akb",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

                if (confirmation == DialogResult.Yes)
                {
                    KnowledgeBaseFirstLaunchMigrationResult migrationResult =
                        migrationService.Migrate(migrationPlan);
                    if (migrationResult.IsSuccess)
                    {
                        startupPath = migrationResult.TargetSqlitePath;
                        RememberDatabasePath(startupPath, showErrorMessage: true);
                        statusText = $"База перенесена в .akb: {Path.GetFileName(startupPath)}";
                        MessageBox.Show(
                            this,
                            $"Миграция завершена.\n\nНовая база:\n{migrationResult.TargetSqlitePath}\n\n" +
                            $"Контрольный JSON-экспорт:\n{migrationResult.SafetyJsonExportPath}",
                            "Миграция базы",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Information);
                    }
                    else
                    {
                        startupPath = legacyJsonPath;
                        statusText = "Миграция не выполнена; загружается старая JSON-база.";
                        MessageBox.Show(
                            this,
                            $"Не удалось перенести базу в SQLite.\n\n{migrationResult.ErrorMessage}\n\n" +
                            "Старый JSON-файл не изменён, приложение загрузит его как раньше.",
                            "Ошибка миграции базы",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Error);
                    }
                }
                else
                {
                    startupPath = legacyJsonPath;
                    statusText = "Миграция отложена; загружается старая JSON-база.";
                }
            }

            return new StartupStorageServiceSelection(
                KnowledgeBaseStorageServiceFactory.CreateFileStorage(startupPath, _appLogger),
                statusText);
        }

        private string ResolveStartupDatabasePath()
        {
            KnowledgeBasePortableStorageSettingsLoadResult settingsLoadResult =
                _storageSettingsService.Load();

            if (settingsLoadResult.IsSuccess && settingsLoadResult.Settings != null)
                return _storageSettingsService.ResolveDatabasePath(settingsLoadResult.Settings);

            if (!settingsLoadResult.FileMissing && !string.IsNullOrWhiteSpace(settingsLoadResult.ErrorMessage))
            {
                MessageBox.Show(
                    this,
                    $"Не удалось прочитать файл настроек хранения:\n{_storageSettingsService.SettingsPath}\n\n" +
                    $"{settingsLoadResult.ErrorMessage}\n\nБудет предложен новый путь базы.",
                    "Настройки хранения",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }

            string selectedPath = PromptInitialDatabasePath();
            RememberDatabasePath(selectedPath, showErrorMessage: true);
            return selectedPath;
        }

        private string PromptInitialDatabasePath()
        {
            string defaultPath = _storageSettingsService.DefaultDatabasePath;
            DialogResult result = MessageBox.Show(
                this,
                "Выберите место хранения базы AKB5.\n\n" +
                $"Да - хранить рядом с программой:\n{defaultPath}\n\n" +
                "Нет - выбрать другую папку для базы.",
                "Первый запуск AKB5",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question,
                MessageBoxDefaultButton.Button1);

            if (result == DialogResult.No)
            {
                using var dialog = new FolderBrowserDialog
                {
                    Description = "Выберите папку для хранения базы AKB5",
                    UseDescriptionForTitle = true,
                    ShowNewFolderButton = true
                };

                if (dialog.ShowDialog(this) == DialogResult.OK &&
                    !string.IsNullOrWhiteSpace(dialog.SelectedPath))
                {
                    return Path.Combine(
                        dialog.SelectedPath,
                        KnowledgeBaseStoragePaths.DefaultSqliteFileName);
                }
            }

            return defaultPath;
        }

        private void TryCopyPreviousDefaultDatabase(string startupPath, ref string statusText)
        {
            string previousPath = KnowledgeBaseStoragePaths.GetDefaultSqlitePath();
            if (PathsEqual(previousPath, startupPath) ||
                File.Exists(startupPath) ||
                !File.Exists(previousPath))
            {
                return;
            }

            DialogResult copyConfirmation = MessageBox.Show(
                this,
                $"Найдена прежняя база AKB5:\n{previousPath}\n\n" +
                $"Скопировать её в выбранное место?\n{startupPath}",
                "Перенос базы",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question,
                MessageBoxDefaultButton.Button1);

            if (copyConfirmation != DialogResult.Yes)
                return;

            try
            {
                string? directory = Path.GetDirectoryName(startupPath);
                if (!string.IsNullOrWhiteSpace(directory))
                    Directory.CreateDirectory(directory);

                File.Copy(previousPath, startupPath, overwrite: false);
                statusText = $"База скопирована в новое место: {Path.GetFileName(startupPath)}";
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    this,
                    $"Не удалось скопировать прежнюю базу:\n{ex.Message}",
                    "Перенос базы",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private void RememberDatabasePath(string databasePath, bool showErrorMessage)
        {
            if (_storageSettingsService.SaveDatabasePath(databasePath, out string? errorMessage))
                return;

            string message =
                $"Не удалось сохранить путь базы в файл настроек:\n{_storageSettingsService.SettingsPath}\n\n" +
                $"{errorMessage}\n\n" +
                "Проверьте, что папка программы доступна для записи.";

            _appLogger.Log(
                "PortableStorageSettingsSaveFailed",
                AppLogLevel.Warning,
                message,
                properties: new Dictionary<string, object?>
                {
                    ["settingsPath"] = _storageSettingsService.SettingsPath,
                    ["databasePath"] = databasePath,
                    ["errorMessage"] = errorMessage ?? string.Empty
                });

            if (showErrorMessage)
            {
                MessageBox.Show(
                    this,
                    message,
                    "Настройки хранения",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }
        }

        private static bool PathsEqual(string firstPath, string secondPath) =>
            string.Equals(
                Path.GetFullPath(firstPath).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar),
                Path.GetFullPath(secondPath).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar),
                StringComparison.OrdinalIgnoreCase);

        private string CurrentDataPath => _fileUiWorkflowService.CurrentDataPath;

        private void UpdateUI(bool refreshSelectedNodeState = true)
        {
            var formState = BuildFormState();
            ApplyFormState(formState, refreshSelectedNodeState);
        }

        private void ApplySelectedNodeState(KnowledgeBaseSelectedNodeState selectedNodeState)
        {
            _isApplyingSelectedNodeState = true;
            try
            {
                bool hasSelection = selectedNodeState.HasSelection;
                lblSelectedNodeEmptyState.Visible = !hasSelection;
                pnlSelectedNodeContextHeader.Visible = hasSelection;
                selectedNodeInfoScreen.Visible = hasSelection;
                ApplyWorkspaceState(selectedNodeState);

                lblSelectedNodeEmptyState.Text = selectedNodeState.EmptyStateText;
                if (hasSelection)
                {
                    lblSelectedNodeContextName.Text = selectedNodeState.Name;
                    txtSelectedNodeContextPath.Text = selectedNodeState.FullPath;
                    selectedNodeInfoScreen.ApplyState(selectedNodeState);
                    selectedNodeCompositionScreen.ApplyState(selectedNodeState.Composition);
                    selectedNodeDocsAndSoftwareScreen.ApplyState(selectedNodeState.DocsAndSoftware);
                    selectedNodeNetworkScreen.ApplyState(selectedNodeState.Network);
                    selectedNodeMaintenanceScreen.ApplyState(selectedNodeState.MaintenanceSchedule);
                }
                else
                {
                    lblSelectedNodeContextName.Text = string.Empty;
                    txtSelectedNodeContextPath.Text = string.Empty;
                }

                ScheduleDeferredLayout();
            }
            finally
            {
                _isApplyingSelectedNodeState = false;
            }
        }

        private void SetLastActionText(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return;

            lblLastAction.Text = text;
        }

        private void SetSessionStatusText(string text)
        {
            bool hasText = !string.IsNullOrWhiteSpace(text);
            lblSessionInfo.Text = text;
            lblSessionInfo.Visible = hasText;
            lblSessionInfo.BorderSides = hasText
                ? ToolStripStatusLabelBorderSides.Right
                : ToolStripStatusLabelBorderSides.None;
        }

        private KnowledgeBaseFormState BuildFormState()
        {
            var currentRoots = GetVisibleTreeData();
            return _formStateService.Build(
                _isDirty,
                _requiresSave,
                CurrentDataPath,
                _currentWorkshop,
                _lastSavedWorkshop,
                tvTree.GetNodeCount(true),
                currentRoots,
                tvTree.SelectedNode?.Tag as KbNode,
                _session.CompositionEntries,
                _session.DocumentLinks,
                _session.SoftwareRecords,
                _session.NetworkFileReferences,
                _session.MaintenanceScheduleProfiles);
        }

        private void ApplyFormState(KnowledgeBaseFormState formState, bool refreshSelectedNodeState)
        {
            var selectedNode = tvTree.SelectedNode?.Tag as KbNode;
            bool hasSelection = selectedNode != null;

            btnUndo.Enabled = _treeMutationWorkflowService.CanUndo;
            btnRedo.Enabled = _treeMutationWorkflowService.CanRedo;
            btnSave.Enabled = formState.CanSave;

            ctxCopy.Enabled = hasSelection;
            ctxRename.Enabled = hasSelection;
            ctxDelete.Enabled = hasSelection;
            ctxAdd.Enabled = _treeMutationWorkflowService.CanAddNode(GetEffectiveParentForRootOperations());
            ctxCreateObjectFromTemplateAtRoot.Enabled =
                _treeMutationWorkflowService.CanCreateObjectFromTemplate(GetEffectiveParentForRootOperations());
            ctxAddChild.Enabled = hasSelection && _treeMutationWorkflowService.CanAddNode(selectedNode!);
            ctxCreateObjectFromTemplate.Enabled = _treeMutationWorkflowService.CanCreateObjectFromTemplate(
                hasSelection ? selectedNode : null);
            ctxSaveObjectAsTemplate.Enabled = hasSelection &&
                KnowledgeBaseTreeMutationWorkflowService.CanSaveObjectAsTemplate(selectedNode);
            ctxTemplates.Enabled =
                ctxCreateObjectFromTemplate.Enabled ||
                ctxSaveObjectAsTemplate.Enabled;
            ctxPaste.Enabled = hasSelection && _treeMutationWorkflowService.CanPasteNode(selectedNode!);
            bool hasCurrentWorkshop = !string.IsNullOrWhiteSpace(_currentWorkshop);
            menuRenameWorkshop.Enabled = hasCurrentWorkshop;
            menuDeleteWorkshop.Enabled = hasCurrentWorkshop && _session.Workshops.Count > 1;
            menuEditEquipmentCatalog.Enabled = true;
            menuExportCatalogTemplates.Enabled = true;
            menuImportCatalogTemplates.Enabled = true;
            menuExportDatabaseJson.Enabled = true;
            menuImportDatabaseJson.Enabled = true;
            menuSnapshotsAndHistory.Enabled = !string.IsNullOrWhiteSpace(CurrentDataPath);
            menuCreateSnapshot.Enabled = !string.IsNullOrWhiteSpace(CurrentDataPath);
            menuBrowseSnapshots.Enabled = !string.IsNullOrWhiteSpace(CurrentDataPath);
            menuRestoreSnapshot.Enabled = !string.IsNullOrWhiteSpace(CurrentDataPath);
            menuCompareSnapshots.Enabled = !string.IsNullOrWhiteSpace(CurrentDataPath);
            menuBrowseChangeHistory.Enabled = !string.IsNullOrWhiteSpace(CurrentDataPath);
            menuImportMaintenanceNorms.Enabled = hasCurrentWorkshop;
            menuEditMaintenanceYearScheduleSource.Enabled = hasCurrentWorkshop;
            menuExportMaintenanceYearScheduleSource.Enabled = hasCurrentWorkshop;
            menuImportMaintenanceYearScheduleSource.Enabled = hasCurrentWorkshop;
            menuEditProductionCalendar.Enabled = true;
            menuImportProductionCalendar.Enabled = true;
            menuImportProductionCalendarPdf.Enabled = true;
            menuExportMaintenanceMonthWorkbook.Enabled = hasCurrentWorkshop;
            menuExportMaintenanceYearWorkbook.Enabled = hasCurrentWorkshop;
            menuRecalculateMaintenanceYearWorkbook.Enabled = hasCurrentWorkshop;

            menuSave.Enabled = formState.CanSave;
            btnSave.ToolTipText = formState.SaveToolTip;
            menuSave.ToolTipText = formState.SaveToolTip;
            Text = formState.WindowTitle;
            SetSessionStatusText(formState.SessionStatusText);

            if (refreshSelectedNodeState)
                ApplySelectedNodeState(formState.SelectedNode);
        }

        private void ScheduleDeferredLayout()
        {
            if (!IsHandleCreated || _isApplyingDeferredLayout)
                return;

            _isApplyingDeferredLayout = true;
            BeginInvoke((MethodInvoker)(() =>
            {
                try
                {
                    ApplyDeferredLayout();
                }
                finally
                {
                    _isApplyingDeferredLayout = false;
                }
            }));
        }

        private void ApplyDeferredLayout()
        {
            ApplySplitLayout(
                splitMain,
                panel1MinSize: NavigationPanelMinSize,
                panel2MinSize: DetailsPanelMinSize,
                desiredDistance: GetPreferredSplitterDistance());
        }

        private int GetPreferredSplitterDistance()
        {
            return _savedSplitterDistance ?? DefaultSplitterDistance;
        }

        private void SaveCurrentSplitterDistance()
        {
            if (_savedSplitterDistance == splitMain.SplitterDistance)
            {
                return;
            }

            _savedSplitterDistance = splitMain.SplitterDistance;
            _windowLayoutStateService.SaveSplitterDistance(splitMain.SplitterDistance);
        }

        private void RestoreSavedWindowLayout()
        {
            var placement = _windowLayoutStateService.LoadWindowPlacement();
            if (placement == null)
                return;

            Rectangle requestedBounds = new(
                placement.Left,
                placement.Top,
                placement.Width,
                placement.Height);
            Rectangle workingArea = Screen.FromRectangle(requestedBounds).WorkingArea;
            Rectangle fittedBounds = KnowledgeBaseWindowLayoutStateService.FitWindowBounds(
                requestedBounds,
                workingArea,
                MinimumSize);

            StartPosition = FormStartPosition.Manual;
            WindowState = FormWindowState.Normal;
            DesktopBounds = fittedBounds;

            if (placement.IsMaximized)
                WindowState = FormWindowState.Maximized;
        }

        private void SaveCurrentWindowLayout()
        {
            Rectangle bounds = WindowState == FormWindowState.Normal
                ? DesktopBounds
                : RestoreBounds;
            if (bounds.Width <= 0 || bounds.Height <= 0)
                return;

            _windowLayoutStateService.SaveWindowPlacement(
                new KnowledgeBaseWindowPlacement
                {
                    Left = bounds.Left,
                    Top = bounds.Top,
                    Width = bounds.Width,
                    Height = bounds.Height,
                    IsMaximized = WindowState == FormWindowState.Maximized
                });
        }

        protected override bool ProcessDialogKey(Keys keyData)
        {
            if (keyData == Keys.Escape && IsSearchTextInputFocused())
            {
                ClearSearchInput();
                return true;
            }

            return base.ProcessDialogKey(keyData);
        }

        private void CollapseTreeToRoots()
        {
            TreeNode? preferredRootNode = tvTree.SelectedNode is { } selectedNode
                ? GetRootTreeNode(selectedNode)
                : null;
            bool hadTreeFocus = tvTree.ContainsFocus;

            tvTree.BeginUpdate();
            try
            {
                tvTree.CollapseAll();
            }
            finally
            {
                tvTree.EndUpdate();
            }

            TreeNode? nextSelectedNode =
                preferredRootNode != null && ReferenceEquals(preferredRootNode.TreeView, tvTree)
                    ? preferredRootNode
                    : tvTree.Nodes.Count > 0
                        ? tvTree.Nodes[0]
                        : null;

            if (!ReferenceEquals(tvTree.SelectedNode, nextSelectedNode))
                tvTree.SelectedNode = nextSelectedNode;

            if (hadTreeFocus)
                tvTree.Focus();

            UpdateUI();
            KnowledgeBaseTreeViewService.RefreshTreeViewVisuals(tvTree);
        }

        private bool IsSearchTextInputFocused() =>
            txtSearch?.TextBox is { IsDisposed: false } searchTextBox &&
            searchTextBox.ContainsFocus;

        private KnowledgeBaseSearchScope GetSelectedSearchScope() =>
            cmbSearchScope?.SelectedItem is SearchScopeOption option
                ? option.Scope
                : KnowledgeBaseSearchScope.All;

        private void ApplySearchNavigationResult(KnowledgeBaseTreeSearchNavigationResult? result)
        {
            if (result == null)
                return;

            if (!string.IsNullOrWhiteSpace(result.StatusText))
                SetLastActionText(result.StatusText);

            if (result.HasActiveResult)
                SelectWorkspaceTab(result.PreferredTabKind);
        }

        private static void ApplySplitLayout(
            SplitContainer splitContainer,
            int panel1MinSize,
            int panel2MinSize,
            int desiredDistance)
        {
            if (splitContainer.Width <= 0 || splitContainer.Height <= 0)
                return;

            splitContainer.Panel1MinSize = panel1MinSize;
            splitContainer.Panel2MinSize = panel2MinSize;

            int available = splitContainer.Orientation == Orientation.Vertical
                ? splitContainer.ClientSize.Width
                : splitContainer.ClientSize.Height;

            int minimumDistance = panel1MinSize;
            int maximumDistance = available - splitContainer.SplitterWidth - panel2MinSize;
            if (maximumDistance < minimumDistance)
                return;

            splitContainer.SplitterDistance = Math.Clamp(desiredDistance, minimumDistance, maximumDistance);
        }

        private static TreeNode GetRootTreeNode(TreeNode node)
        {
            var current = node;
            while (current.Parent != null)
                current = current.Parent;

            return current;
        }

        private void InitializeTemplateContextMenuItem()
        {
            if (tvTree.ContextMenuStrip == null || ctxTemplates == null)
                return;

            ctxCreateObjectFromTemplate = new ToolStripMenuItem(
                "Создать объект из шаблона...",
                null,
                (s, e) => CreateObjectFromTemplate());
            ctxSaveObjectAsTemplate = new ToolStripMenuItem(
                "Сохранить как шаблон объекта...",
                null,
                (s, e) => SaveObjectAsTemplate());
            ctxTemplates.DropDownItems.AddRange(new ToolStripItem[]
            {
                ctxCreateObjectFromTemplate,
                ctxSaveObjectAsTemplate
            });
            ApplyTreeContextMenuVisibility();
        }

        private void ApplyTreeContextMenuVisibility()
        {
            bool hasSelection = tvTree.SelectedNode?.Tag is KbNode;

            ctxAdd.Visible = !hasSelection;
            ctxCreateObjectFromTemplateAtRoot.Visible = !hasSelection;
            ctxAddChild.Visible = hasSelection;
            ctxTemplates.Visible = hasSelection;
            ctxEditSeparator.Visible = hasSelection;
            ctxCopy.Visible = hasSelection;
            ctxPaste.Visible = hasSelection;
            ctxRename.Visible = hasSelection;
            ctxDeleteSeparator.Visible = hasSelection;
            ctxDelete.Visible = hasSelection;
        }

        private sealed record SearchScopeOption(KnowledgeBaseSearchScope Scope, string DisplayText)
        {
            public override string ToString() => DisplayText;
        }

        private sealed record StartupStorageServiceSelection(
            IKnowledgeBaseStorageService StorageService,
            string StatusText);
    }
}
