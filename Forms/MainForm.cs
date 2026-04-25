using System.Runtime.InteropServices;
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
        private const int WmSetRedraw = 0x000B;
        private const int WmEnterSizeMove = 0x0231;
        private const int WmExitSizeMove = 0x0232;
        private const uint RdwInvalidate = 0x0001;
        private const uint RdwErase = 0x0004;
        private const uint RdwAllChildren = 0x0080;
        private const uint RdwFrame = 0x0400;

        private readonly IAppLogger _appLogger;
        private readonly KnowledgeBaseSessionService _session = new();
        private readonly KnowledgeBaseExcelUiWorkflowService _excelUiWorkflowService;
        private readonly KnowledgeBaseFileUiWorkflowService _fileUiWorkflowService;
        private readonly KnowledgeBaseSessionWorkflowService _sessionWorkflowService;
        private readonly KnowledgeBaseTreeMutationUiWorkflowService _treeMutationUiWorkflowService;
        private readonly KnowledgeBaseWorkshopUiWorkflowService _workshopUiWorkflowService;
        private readonly KnowledgeBaseTreeController _treeController;
        private readonly KnowledgeBaseTreeMutationWorkflowService _treeMutationWorkflowService;
        private readonly KnowledgeBaseCompositionMutationService _compositionMutationService = new();
        private readonly KnowledgeBaseFormStateService _formStateService = new();
        private readonly KnowledgeBaseTreeViewService _treeViewService = new();
        private readonly UndoRedoService _history = new(50);
        private readonly KnowledgeBaseWindowLayoutStateService _windowLayoutStateService;
        private int? _savedSplitterDistance;

        private bool _isBindingWorkshops;
        private bool _isApplyingSelectedNodeState;
        private bool _isApplyingDeferredLayout;
        private bool _isInInteractiveWindowMoveOrResize;
        private bool _hasDeferredLayoutPendingAfterInteractiveMoveOrResize;
        private readonly List<Control> _interactiveMoveRedrawSuspendedControls = new();

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
        private Panel pnlSelectedNodeInfoScreen = null!;
        private TabControl tabSelectedNodeWorkspace = null!;
        private TabPage tabSelectedNodeInfo = null!;
        private TabPage tabSelectedNodeComposition = null!;
        private TabPage tabSelectedNodeDocsAndSoftware = null!;
        private TabPage tabSelectedNodeNetwork = null!;
        private KnowledgeBaseInfoScreenControl selectedNodeInfoScreen = null!;
        private KnowledgeBaseCompositionScreenControl selectedNodeCompositionScreen = null!;
        private Label lblSelectedNodeDocsPlaceholder = null!;
        private Label lblSelectedNodeNetworkPlaceholder = null!;

        private KbConfig _config => _session.Config;
        private string _currentWorkshop => _session.CurrentWorkshop;
        private string _lastSavedWorkshop => _session.LastSavedWorkshop;
        private bool _isDirty => _session.IsDirty;
        private bool _requiresSave => _session.RequiresSave;

        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool RedrawWindow(
            IntPtr hWnd,
            IntPtr lprcUpdate,
            IntPtr hrgnUpdate,
            uint flags);

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
                selectedNodeInfoScreen.Visible = hasSelection;
                ApplyWorkspaceState(selectedNodeState);

                lblSelectedNodeEmptyState.Text = selectedNodeState.EmptyStateText;
                if (hasSelection)
                {
                    selectedNodeInfoScreen.ApplyState(selectedNodeState);
                    selectedNodeCompositionScreen.ApplyState(selectedNodeState.Composition);
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
                _session.CompositionEntries);
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
            if (!IsHandleCreated || IsDisposed)
                return;

            if (_isInInteractiveWindowMoveOrResize)
            {
                _hasDeferredLayoutPendingAfterInteractiveMoveOrResize = true;
                return;
            }

            if (_isApplyingDeferredLayout)
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

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WmEnterSizeMove)
                BeginInteractiveWindowMoveOrResize();

            base.WndProc(ref m);

            if (m.Msg == WmExitSizeMove)
                EndInteractiveWindowMoveOrResize();
        }

        private void BeginInteractiveWindowMoveOrResize()
        {
            if (_isInInteractiveWindowMoveOrResize || IsDisposed)
                return;

            _isInInteractiveWindowMoveOrResize = true;
            _interactiveMoveRedrawSuspendedControls.Clear();

            foreach (Control control in EnumerateInteractiveMoveRedrawControls(this))
                SuspendControlRedraw(control);
        }

        private void EndInteractiveWindowMoveOrResize()
        {
            if (!_isInInteractiveWindowMoveOrResize || IsDisposed)
                return;

            _isInInteractiveWindowMoveOrResize = false;

            for (int index = _interactiveMoveRedrawSuspendedControls.Count - 1; index >= 0; index--)
                ResumeControlRedraw(_interactiveMoveRedrawSuspendedControls[index]);

            _interactiveMoveRedrawSuspendedControls.Clear();

            if (IsHandleCreated)
            {
                RedrawWindow(
                    Handle,
                    IntPtr.Zero,
                    IntPtr.Zero,
                    RdwInvalidate | RdwErase | RdwAllChildren | RdwFrame);
            }

            bool hadPendingDeferredLayout = _hasDeferredLayoutPendingAfterInteractiveMoveOrResize;
            _hasDeferredLayoutPendingAfterInteractiveMoveOrResize = false;

            if (hadPendingDeferredLayout || WindowState == FormWindowState.Normal)
                ScheduleDeferredLayout();
        }

        private static IEnumerable<Control> EnumerateInteractiveMoveRedrawControls(Control root)
        {
            yield return root;

            foreach (Control child in root.Controls)
            {
                foreach (Control nestedChild in EnumerateInteractiveMoveRedrawControls(child))
                    yield return nestedChild;
            }
        }

        private void SuspendControlRedraw(Control control)
        {
            if (control.IsDisposed || !control.IsHandleCreated)
                return;

            SendMessage(control.Handle, WmSetRedraw, IntPtr.Zero, IntPtr.Zero);
            _interactiveMoveRedrawSuspendedControls.Add(control);
        }

        private static void ResumeControlRedraw(Control control)
        {
            if (control.IsDisposed || !control.IsHandleCreated)
                return;

            SendMessage(control.Handle, WmSetRedraw, (IntPtr)1, IntPtr.Zero);
            control.Invalidate(invalidateChildren: true);
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

            KnowledgeBaseTreeViewService.RefreshTreeViewVisuals(tvTree);
        }

        private bool IsSearchTextInputFocused() =>
            txtSearch?.TextBox is { IsDisposed: false } searchTextBox &&
            searchTextBox.ContainsFocus;

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

            int clampedDistance = Math.Clamp(desiredDistance, minimumDistance, maximumDistance);
            if (splitContainer.SplitterDistance != clampedDistance)
                splitContainer.SplitterDistance = clampedDistance;
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
