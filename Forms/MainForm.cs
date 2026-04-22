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
        private readonly KnowledgeBaseFileUiWorkflowService _fileUiWorkflowService;
        private readonly KnowledgeBaseSessionWorkflowService _sessionWorkflowService;
        private readonly KnowledgeBaseTreeMutationUiWorkflowService _treeMutationUiWorkflowService;
        private readonly KnowledgeBaseWorkshopUiWorkflowService _workshopUiWorkflowService;
        private readonly KnowledgeBaseTreeController _treeController;
        private readonly KnowledgeBaseTreeMutationWorkflowService _treeMutationWorkflowService;
        private readonly KnowledgeBaseFormStateService _formStateService = new();
        private readonly KnowledgeBaseTreeViewService _treeViewService = new();
        private readonly UndoRedoService _history = new(50);
        private readonly KnowledgeBaseWindowLayoutStateService _windowLayoutStateService;
        private int? _savedSplitterDistance;

        private bool _isBindingWorkshops;
        private bool _isApplyingSelectedNodeState;
        private bool _isApplyingDeferredLayout;

        private ToolStrip toolStrip = null!;
        private ToolStripButton btnUndo = null!;
        private ToolStripButton btnRedo = null!;
        private ToolStripButton btnSave = null!;
        private ToolStripButton btnCollapseTree = null!;
        private ToolStripMenuItem menuFile = null!;
        private ToolStripMenuItem menuNewWorkshop = null!;
        private ToolStripMenuItem menuRenameWorkshop = null!;
        private ToolStripMenuItem menuDeleteWorkshop = null!;

        private SplitContainer splitMain = null!;
        private TableLayoutPanel tblDetailsLeftColumn = null!;
        private ComboBox cmbWorkshops = null!;
        private TreeView tvTree = null!;
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
        private ToolStripMenuItem ctxCopy = null!;
        private ToolStripMenuItem ctxPaste = null!;
        private ToolStripMenuItem ctxRename = null!;
        private ToolStripMenuItem ctxDelete = null!;
        private Label lblSelectedNodeEmptyState = null!;
        private TableLayoutPanel tblSelectedNodeCard = null!;
        private Label lblSelectedNodeNameValue = null!;
        private Label lblSelectedNodeLevelValue = null!;
        private TextBox txtSelectedNodePath = null!;
        private Label lblSelectedNodeChildrenValue = null!;
        private TextBox txtNodeDescription = null!;
        private TextBox txtNodeLocation = null!;
        private TextBox txtNodePhotoPath = null!;
        private TextBox txtNodeIpAddress = null!;
        private TextBox txtNodeSchemaLink = null!;
        private GroupBox grpTechnicalFields = null!;
        private Button btnBrowsePhoto = null!;
        private Button btnOpenPhoto = null!;

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
            _treeController = new KnowledgeBaseTreeController(_session);
            _windowLayoutStateService = new KnowledgeBaseWindowLayoutStateService(logger: _appLogger);
            InitializeComponent();
            AppIconProvider.Apply(this);
            _savedSplitterDistance = _windowLayoutStateService.LoadSplitterDistance();
            RestoreSavedWindowLayout();
            var storageService = new JsonStorageService(GetDefaultJsonPath(), _appLogger);
            var fileWorkflowService = new KnowledgeBaseFileWorkflowService(
                _session,
                storageService,
                _appLogger);
            _excelUiWorkflowService = new KnowledgeBaseExcelUiWorkflowService(
                new KnowledgeBaseExcelExchangeService(_appLogger));
            _fileUiWorkflowService = new KnowledgeBaseFileUiWorkflowService(
                fileWorkflowService,
                _formStateService);
            _sessionWorkflowService = new KnowledgeBaseSessionWorkflowService(_session);
            _workshopUiWorkflowService = new KnowledgeBaseWorkshopUiWorkflowService(
                _session,
                _sessionWorkflowService,
                new KnowledgeBaseConfigurationWorkflowService(),
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
        }

        private static string GetDefaultJsonPath() =>
            Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "ASUTP_KnowledgeBase.json");

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
                tblSelectedNodeCard.Visible = hasSelection;

                lblSelectedNodeEmptyState.Text = selectedNodeState.EmptyStateText;
                lblSelectedNodeNameValue.Text = selectedNodeState.Name;
                lblSelectedNodeLevelValue.Text = selectedNodeState.LevelName;
                txtSelectedNodePath.Text = selectedNodeState.FullPath;
                lblSelectedNodeChildrenValue.Text = selectedNodeState.ChildrenCountText;
                txtNodeDescription.Text = selectedNodeState.Description;
                txtNodeLocation.Text = selectedNodeState.Location;
                txtNodePhotoPath.Text = selectedNodeState.PhotoPath;
                txtNodeIpAddress.Text = selectedNodeState.IpAddress;
                txtNodeSchemaLink.Text = selectedNodeState.SchemaLink;
                SetTechnicalFieldsVisibility(hasSelection && selectedNodeState.ShowTechnicalFields);

                UpdatePhotoControlsState(selectedNodeState.PhotoPath);
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
                _config,
                currentRoots,
                tvTree.SelectedNode?.Tag as KbNode);
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
            ctxAddChild.Enabled = hasSelection && _treeMutationWorkflowService.CanAddNode(selectedNode!);
            ctxPaste.Enabled = hasSelection && _treeMutationWorkflowService.CanPasteNode(selectedNode!);
            menuRenameWorkshop.Enabled = !string.IsNullOrWhiteSpace(_currentWorkshop);
            menuDeleteWorkshop.Enabled = !string.IsNullOrWhiteSpace(_currentWorkshop) && _session.Workshops.Count > 1;

            btnSave.ToolTipText = formState.SaveToolTip;
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
            TreeNode? rootNodeToKeepVisible = tvTree.SelectedNode is { } selectedNode
                ? GetRootTreeNode(selectedNode)
                : null;
            bool hadTreeFocus = tvTree.Focused;

            tvTree.BeginUpdate();
            try
            {
                tvTree.CollapseAll();
                if (rootNodeToKeepVisible != null && !ReferenceEquals(tvTree.SelectedNode, rootNodeToKeepVisible))
                    tvTree.SelectedNode = rootNodeToKeepVisible;
            }
            finally
            {
                tvTree.EndUpdate();
            }

            if (hadTreeFocus)
                tvTree.Focus();
        }

        private bool IsSearchTextInputFocused() =>
            txtSearch?.TextBox is { IsDisposed: false } searchTextBox &&
            searchTextBox.ContainsFocus;

        private void SetTechnicalFieldsVisibility(bool visible)
        {
            grpTechnicalFields.Visible = visible;

            if (tblDetailsLeftColumn.RowStyles.Count <= 2)
                return;

            tblDetailsLeftColumn.RowStyles[2].Height = visible ? 150F : 0F;
            tblDetailsLeftColumn.PerformLayout();
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
    }
}
