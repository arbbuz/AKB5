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
        private readonly Dictionary<string, int> _splitterDistancesByContext = new(StringComparer.Ordinal);

        private bool _isBindingWorkshops;
        private bool _isApplyingSelectedNodeState;
        private bool _isApplyingDeferredLayout;

        private ToolStrip toolStrip = null!;
        private ToolStripButton btnUndo = null!;
        private ToolStripButton btnRedo = null!;
        private ToolStripButton btnSave = null!;
        private ToolStripMenuItem menuFile = null!;
        private ToolStripMenuItem menuNewWorkshop = null!;
        private ToolStripMenuItem menuRenameWorkshop = null!;
        private ToolStripMenuItem menuDeleteWorkshop = null!;

        private SplitContainer splitMain = null!;
        private TableLayoutPanel tblDetailsLeftColumn = null!;
        private ComboBox cmbWorkshops = null!;
        private TreeView tvTree = null!;
        private TextBox txtSearch = null!;
        private Button btnSearchPrev = null!;
        private Button btnSearchNext = null!;
        private Button btnSearch = null!;
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
            foreach (var pair in _windowLayoutStateService.LoadSplitterDistancesByWorkshop())
                _splitterDistancesByContext[pair.Key] = pair.Value;
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
            lblSessionInfo.Text = formState.SessionStatusText;
            lblSelectionInfo.Text = formState.SelectionStatusText;

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
            string? contextKey = GetCurrentSplitterContextKey();
            if (contextKey != null && _splitterDistancesByContext.TryGetValue(contextKey, out int savedDistance))
                return savedDistance;

            return DefaultSplitterDistance;
        }

        private void SaveCurrentSplitterDistance()
        {
            string? contextKey = GetCurrentSplitterContextKey();
            if (contextKey == null)
                return;

            if (_splitterDistancesByContext.TryGetValue(contextKey, out int existingDistance) &&
                existingDistance == splitMain.SplitterDistance)
            {
                return;
            }

            _splitterDistancesByContext[contextKey] = splitMain.SplitterDistance;
            _windowLayoutStateService.SaveSplitterDistancesByWorkshop(_splitterDistancesByContext);
        }

        private void RenameWorkshopSplitterDistance(string oldWorkshop, string newWorkshop)
        {
            string oldKey = oldWorkshop.Trim();
            string newKey = newWorkshop.Trim();
            if (string.IsNullOrWhiteSpace(oldKey) || string.IsNullOrWhiteSpace(newKey))
                return;

            if (!_splitterDistancesByContext.Remove(oldKey, out int splitterDistance))
                return;

            _splitterDistancesByContext[newKey] = splitterDistance;
            _windowLayoutStateService.SaveSplitterDistancesByWorkshop(_splitterDistancesByContext);
        }

        private void RemoveWorkshopSplitterDistance(string workshop)
        {
            string key = workshop.Trim();
            if (string.IsNullOrWhiteSpace(key))
                return;

            if (!_splitterDistancesByContext.Remove(key))
                return;

            _windowLayoutStateService.SaveSplitterDistancesByWorkshop(_splitterDistancesByContext);
        }

        private string? GetCurrentSplitterContextKey()
        {
            string workshop = _currentWorkshop.Trim();
            return string.IsNullOrWhiteSpace(workshop) ? null : workshop;
        }

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
    }
}
