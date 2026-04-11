using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
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
        private readonly KnowledgeBaseSessionService _session = new();
        private readonly KnowledgeBaseExcelUiWorkflowService _excelUiWorkflowService;
        private readonly KnowledgeBaseFileUiWorkflowService _fileUiWorkflowService;
        private readonly KnowledgeBaseSessionWorkflowService _sessionWorkflowService;
        private readonly KnowledgeBaseTreeController _treeController;
        private readonly KnowledgeBaseTreeMutationWorkflowService _treeMutationWorkflowService;
        private readonly KnowledgeBaseConfigurationWorkflowService _configurationWorkflowService = new();
        private readonly KnowledgeBaseFormStateService _formStateService = new();
        private readonly KnowledgeBaseTreeViewService _treeViewService = new();
        private readonly UndoRedoService _history = new(50);

        private bool _isBindingWorkshops;

        private ToolStrip toolStrip = null!;
        private ToolStripButton btnUndo = null!;
        private ToolStripButton btnRedo = null!;
        private ToolStripButton btnSave = null!;
        private ToolStripMenuItem menuFile = null!;
        private ToolStripMenuItem menuNewWorkshop = null!;

        private ComboBox cmbWorkshops = null!;
        private TreeView tvTree = null!;
        private TextBox txtSearch = null!;
        private Button btnSearchPrev = null!;
        private Button btnSearchNext = null!;
        private Button btnSearch = null!;
        private StatusStrip ssStatus = null!;
        private ToolStripStatusLabel lblInfo = null!;
        private ToolTip toolTip = null!;
        private ToolStripMenuItem ctxAdd = null!;
        private ToolStripMenuItem ctxCopy = null!;
        private ToolStripMenuItem ctxPaste = null!;
        private ToolStripMenuItem ctxRename = null!;
        private ToolStripMenuItem ctxDelete = null!;

        private KbConfig _config => _session.Config;
        private Dictionary<string, List<KbNode>> _workshopsData => _session.Workshops;
        private string _currentWorkshop => _session.CurrentWorkshop;
        private string _lastSavedWorkshop => _session.LastSavedWorkshop;
        private bool _isDirty => _session.IsDirty;
        private bool _requiresSave => _session.RequiresSave;

        public MainForm()
        {
            _treeController = new KnowledgeBaseTreeController(_config, _workshopsData);
            InitializeComponent();
            var fileWorkflowService = new KnowledgeBaseFileWorkflowService(
                _session,
                new JsonStorageService(GetDefaultJsonPath()));
            _excelUiWorkflowService = new KnowledgeBaseExcelUiWorkflowService(
                new KnowledgeBaseExcelExchangeService());
            _fileUiWorkflowService = new KnowledgeBaseFileUiWorkflowService(
                fileWorkflowService,
                _formStateService);
            _sessionWorkflowService = new KnowledgeBaseSessionWorkflowService(_session);
            _treeMutationWorkflowService = new KnowledgeBaseTreeMutationWorkflowService(
                _session,
                _sessionWorkflowService,
                _treeController,
                _history);
            FormClosing += MainForm_FormClosing;
            _fileUiWorkflowService.LoadData(CreateFileUiWorkflowContext());
        }

        private void InitializeComponent()
        {
            Text = "База знаний АСУТП";
            Size = new Size(1050, 750);
            StartPosition = FormStartPosition.CenterScreen;
            MinimumSize = new Size(800, 550);

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
            var menuReloadDb = new ToolStripMenuItem("🔄 Перезагрузить текущую базу", null, BtnLoad_Click);
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
                Width = 280,
                AutoScroll = true,
                BackColor = Color.FromArgb(245, 245, 245)
            };

            int y = 15;

            cmbWorkshops = new ComboBox
            {
                Location = new Point(15, y),
                Size = new Size(250, 25),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top
            };
            pnlRight.Controls.Add(cmbWorkshops);
            y += 40;

            var lblSearch = new Label
            {
                Text = "🔍 Поиск:",
                AutoSize = true,
                Location = new Point(15, y)
            };
            pnlRight.Controls.Add(lblSearch);
            y += 22;

            txtSearch = new TextBox
            {
                Location = new Point(15, y),
                Size = new Size(250, 25),
                Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top
            };
            pnlRight.Controls.Add(txtSearch);
            y += 35;

            var pnlSearchNav = new TableLayoutPanel
            {
                Location = new Point(15, y),
                Size = new Size(250, 30),
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

            Controls.Add(pnlRight);
        }

        private void InitializeStatusBar()
        {
            lblInfo = new ToolStripStatusLabel { Text = "Готово | ПКМ по дереву → Добавить объект" };
            ssStatus = new StatusStrip { Items = { lblInfo } };
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

        private void InitializeEvents()
        {
            tvTree.ItemDrag += TvTree_ItemDrag;
            tvTree.DragEnter += TvTree_DragEnter;
            tvTree.DragDrop += TvTree_DragDrop;
            tvTree.AfterSelect += TvTree_AfterSelect;
            cmbWorkshops.SelectedIndexChanged += CmbWorkshops_SelectedIndexChanged;

            btnSearch.Click += (s, e) => PerformSearch();
            btnSearchPrev.Click += (s, e) => NavigateSearch(-1);
            btnSearchNext.Click += (s, e) => NavigateSearch(1);

            btnUndo.Click += (s, e) => UndoAction();
            btnRedo.Click += (s, e) => RedoAction();

            KeyPreview = true;
            KeyDown += (s, e) =>
            {
                if (e.Control && e.KeyCode == Keys.Z) { e.SuppressKeyPress = true; UndoAction(); }
                if (e.Control && e.KeyCode == Keys.Y) { e.SuppressKeyPress = true; RedoAction(); }
                if (e.Control && e.KeyCode == Keys.C) { e.SuppressKeyPress = true; CopyNode(); }
                if (e.Control && e.KeyCode == Keys.V) { e.SuppressKeyPress = true; PasteNode(); }
                if (e.KeyCode == Keys.F2) { e.SuppressKeyPress = true; RenameNode(); }
                if (e.KeyCode == Keys.Insert) { e.SuppressKeyPress = true; AddNode(); }
                if (e.Control && e.KeyCode == Keys.F) { e.SuppressKeyPress = true; txtSearch.Focus(); }
                if (e.KeyCode == Keys.Enter && txtSearch.Focused) { e.SuppressKeyPress = true; PerformSearch(); }
                if (e.KeyCode == Keys.Escape && txtSearch.Focused) { e.SuppressKeyPress = true; txtSearch.Clear(); ClearSearch(); UpdateUI(); }
            };
        }

        private static string GetDefaultJsonPath() =>
            Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "ASUTP_KnowledgeBase.json");

        private string CurrentDataPath => _fileUiWorkflowService.CurrentDataPath;

        private void RebindTreeController() => _treeController.Bind(_config, _workshopsData);

        private KnowledgeBaseFileUiWorkflowContext CreateFileUiWorkflowContext() =>
            new()
            {
                Owner = this,
                GetCurrentTreeData = GetCurrentTreeData,
                SaveCurrentWorkshopState = SaveCurrentWorkshopState,
                UpdateDirtyState = UpdateDirtyState,
                GetUiState = () => new KnowledgeBaseFileUiState
                {
                    IsDirty = _isDirty,
                    RequiresSave = _requiresSave,
                    CurrentWorkshop = _currentWorkshop,
                    LastSavedWorkshop = _lastSavedWorkshop
                },
                OnSuccessfulLoad = () =>
                {
                    ResetTransientUiStateAfterLoad();
                    RebuildUiFromSession();
                },
                UpdateUi = UpdateUI,
                SetStatusText = text => lblInfo.Text = text
            };

        private void BindWorkshops(IReadOnlyList<string> workshopNames, string selectedWorkshop)
        {
            _isBindingWorkshops = true;
            _treeViewService.BindWorkshops(cmbWorkshops, workshopNames, selectedWorkshop);
            _isBindingWorkshops = false;
        }

        private void ResetTransientUiStateAfterLoad()
        {
            _history.Clear();
            _treeMutationWorkflowService.ClearClipboard();
        }

        private void RebuildUiFromSession()
        {
            ApplySessionView(_sessionWorkflowService.BuildViewState(), clearSearch: true);
        }

        private void BtnOpen_Click(object? sender, EventArgs e)
            => _fileUiWorkflowService.OpenDatabase(CreateFileUiWorkflowContext());

        private void BtnLoad_Click(object? sender, EventArgs e)
            => _fileUiWorkflowService.ReloadDatabase(CreateFileUiWorkflowContext());

        private void BtnSave_Click(object? sender, EventArgs e)
            => _fileUiWorkflowService.SaveCurrentDatabase(CreateFileUiWorkflowContext());

        private void BtnSaveAs_Click(object? sender, EventArgs e)
            => _fileUiWorkflowService.SaveDatabaseAs(CreateFileUiWorkflowContext());

        private void BtnImportExcel_Click(object? sender, EventArgs e)
        {
            var fileContext = CreateFileUiWorkflowContext();
            _excelUiWorkflowService.Import(new KnowledgeBaseExcelImportUiWorkflowContext
            {
                Owner = this,
                CurrentDataPath = CurrentDataPath,
                ConfirmContinueBeforeImport = actionDescription =>
                    _fileUiWorkflowService.ConfirmContinueBeforeReplace(fileContext, actionDescription),
                ReplaceAllData = data => _fileUiWorkflowService.ReplaceAllData(fileContext, data),
                SetStatusText = text => lblInfo.Text = text
            });
        }

        private void BtnExportExcel_Click(object? sender, EventArgs e)
        {
            SaveCurrentWorkshopState();
            var data = _session.CreateSaveData(GetCurrentTreeData());
            _excelUiWorkflowService.Export(
                this,
                data,
                CurrentDataPath,
                text => lblInfo.Text = text);
        }

        private void UpdateDirtyState() =>
            _session.RefreshDirtyState(GetCurrentTreeData());

        private void ApplySessionView(
            KnowledgeBaseSessionViewState viewState,
            bool clearSearch,
            KbNode? nodeToSelect = null,
            ISet<KbNode>? expandedNodes = null)
        {
            RebindTreeController();
            BindWorkshops(viewState.WorkshopNames, viewState.CurrentWorkshop);
            _treeViewService.ApplySessionView(tvTree, viewState, clearSearch, nodeToSelect, expandedNodes);
            UpdateSearchButtons();
        }

        private void SaveCurrentWorkshopState() =>
            _session.SyncCurrentWorkshop(GetCurrentTreeData());

        private List<KbNode> GetCurrentTreeData()
            => _treeViewService.GetCurrentTreeData(tvTree);

        private void PerformSearch()
        {
            lblInfo.Text = _treeViewService.PerformSearch(tvTree, txtSearch.Text);
            UpdateSearchButtons();
        }

        private void NavigateSearch(int direction)
        {
            var statusText = _treeViewService.NavigateSearch(tvTree, direction);
            UpdateSearchButtons();
            if (!string.IsNullOrWhiteSpace(statusText))
                lblInfo.Text = statusText;
        }

        private void UpdateSearchButtons()
        {
            bool canNavigate = _treeViewService.CanNavigateSearch;
            btnSearchPrev.Enabled = canNavigate;
            btnSearchNext.Enabled = canNavigate;
        }

        private void ClearSearch()
        {
            lblInfo.Text = _treeViewService.ClearSearch();
            UpdateSearchButtons();
        }

        private void RefreshSearchAfterMutation()
        {
            _treeViewService.RefreshSearch(tvTree, txtSearch.Text);
            UpdateSearchButtons();
        }

        private HashSet<KbNode> CaptureExpandedNodes() =>
            _treeViewService.CaptureExpandedNodes(tvTree);

        private void ApplySuccessfulTreeMutation(
            KnowledgeBaseTreeMutationResult result,
            KbNode? nodeToSelect,
            ISet<KbNode> expandedNodes)
        {
            ApplySessionView(
                _sessionWorkflowService.BuildViewState(),
                clearSearch: false,
                nodeToSelect,
                expandedNodes);
            RefreshSearchAfterMutation();
            UpdateDirtyState();
            UpdateUI();

            if (!string.IsNullOrWhiteSpace(result.StatusMessage))
                lblInfo.Text = result.StatusMessage;
        }

        private string CaptureCurrentHistorySnapshot() =>
            _session.SerializeSnapshot(GetCurrentTreeData(), includeCurrentWorkshop: true);

        private void ShowMutationFailure(KnowledgeBaseTreeMutationResult result, string title)
        {
            if (string.IsNullOrWhiteSpace(result.ErrorMessage))
                return;

            MessageBoxIcon icon = result.Failure switch
            {
                KnowledgeBaseTreeMutationFailure.DeleteFailed => MessageBoxIcon.Error,
                KnowledgeBaseTreeMutationFailure.MoveFailed => MessageBoxIcon.Error,
                KnowledgeBaseTreeMutationFailure.RestoreFailed => MessageBoxIcon.Error,
                _ => MessageBoxIcon.Warning
            };

            MessageBox.Show(
                result.ErrorMessage,
                title,
                MessageBoxButtons.OK,
                icon);
        }

        private void AddNode()
        {
            using var dlg = new InputDialog("Введите название нового объекта:");
            if (dlg.ShowDialog() != DialogResult.OK || string.IsNullOrWhiteSpace(dlg.Result))
                return;

            var selectedData = tvTree.SelectedNode?.Tag as KbNode;
            var expandedNodes = CaptureExpandedNodes();
            var result = _treeMutationWorkflowService.AddNode(
                _currentWorkshop,
                selectedData,
                dlg.Result,
                GetCurrentTreeData());

            if (!result.IsSuccess)
            {
                ShowMutationFailure(result, "Невозможно добавить");
                return;
            }

            ApplySuccessfulTreeMutation(result, result.AffectedNode, expandedNodes);
        }

        private void DeleteNode()
        {
            if (tvTree.SelectedNode?.Tag is not KbNode node)
            {
                MessageBox.Show(
                    "Выберите узел для удаления.",
                    "Внимание",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            if (MessageBox.Show(
                    $"Удалить '{node.Name}' и все вложенные элементы?",
                    "Подтверждение",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning) != DialogResult.Yes)
            {
                return;
            }

            var nextSelectedData = tvTree.SelectedNode?.Parent?.Tag as KbNode;
            var expandedNodes = CaptureExpandedNodes();
            var result = _treeMutationWorkflowService.DeleteNode(
                _currentWorkshop,
                node,
                GetCurrentTreeData());

            if (!result.IsSuccess)
            {
                ShowMutationFailure(result, "Ошибка удаления");
                return;
            }

            ApplySuccessfulTreeMutation(result, nextSelectedData, expandedNodes);
        }

        private void CopyNode()
        {
            if (tvTree.SelectedNode?.Tag is not KbNode node)
                return;

            _treeMutationWorkflowService.CopyNode(node);
            UpdateUI();
            lblInfo.Text = $"📋 Скопировано: {node.Name}";
        }

        private void PasteNode()
        {
            if (!_treeMutationWorkflowService.HasClipboardNode || tvTree.SelectedNode?.Tag is not KbNode parent)
                return;

            var expandedNodes = CaptureExpandedNodes();
            var result = _treeMutationWorkflowService.PasteNode(parent, GetCurrentTreeData());
            if (!result.IsSuccess)
            {
                ShowMutationFailure(result, "Ошибка вставки");
                return;
            }

            ApplySuccessfulTreeMutation(result, result.AffectedNode, expandedNodes);
        }

        private void RenameNode()
        {
            if (tvTree.SelectedNode?.Tag is not KbNode node)
                return;

            using var dlg = new InputDialog("Новое название:", node.Name);
            if (dlg.ShowDialog() != DialogResult.OK || string.IsNullOrWhiteSpace(dlg.Result))
                return;

            var expandedNodes = CaptureExpandedNodes();
            var result = _treeMutationWorkflowService.RenameNode(node, dlg.Result, GetCurrentTreeData());
            if (!result.IsSuccess)
            {
                if (result.Failure != KnowledgeBaseTreeMutationFailure.NoChanges)
                    ShowMutationFailure(result, "Переименование");
                return;
            }

            ApplySuccessfulTreeMutation(result, result.AffectedNode, expandedNodes);
        }

        private void TvTree_ItemDrag(object? sender, ItemDragEventArgs e)
        {
            if (e.Button == MouseButtons.Left && e.Item != null)
                DoDragDrop(e.Item, DragDropEffects.Move);
        }

        private void TvTree_DragEnter(object? sender, DragEventArgs e) => e.Effect = DragDropEffects.Move;

        private void TvTree_DragDrop(object? sender, DragEventArgs e)
        {
            Point pt = tvTree.PointToClient(new Point(e.X, e.Y));
            TreeNode? targetNode = tvTree.GetNodeAt(pt);
            TreeNode? draggedNode = e.Data?.GetData(typeof(TreeNode)) as TreeNode;

            if (draggedNode == null || targetNode == null || draggedNode == targetNode)
                return;

            if (targetNode.Tag is not KbNode targetData || draggedNode.Tag is not KbNode draggedData)
                return;

            var expandedNodes = CaptureExpandedNodes();
            var result = _treeMutationWorkflowService.MoveNode(
                _currentWorkshop,
                draggedData,
                draggedNode.Parent?.Tag as KbNode,
                targetData,
                GetCurrentTreeData());

            if (!result.IsSuccess)
            {
                ShowMutationFailure(result, "Ошибка перемещения");
                return;
            }

            ApplySuccessfulTreeMutation(result, result.AffectedNode, expandedNodes);
        }

        private void UndoAction()
        {
            var result = _treeMutationWorkflowService.Undo(GetCurrentTreeData());
            if (!result.IsSuccess)
            {
                if (result.Failure != KnowledgeBaseTreeMutationFailure.NoChanges)
                    ShowMutationFailure(result, "Undo/Redo");
                return;
            }

            ApplySessionView(result.ViewState, clearSearch: true);
            UpdateDirtyState();
            UpdateUI();
            lblInfo.Text = result.StatusMessage ?? "↩ Выполнена отмена";
        }

        private void RedoAction()
        {
            var result = _treeMutationWorkflowService.Redo(GetCurrentTreeData());
            if (!result.IsSuccess)
            {
                if (result.Failure != KnowledgeBaseTreeMutationFailure.NoChanges)
                    ShowMutationFailure(result, "Undo/Redo");
                return;
            }

            ApplySessionView(result.ViewState, clearSearch: true);
            UpdateDirtyState();
            UpdateUI();
            lblInfo.Text = result.StatusMessage ?? "↪ Выполнен повтор";
        }

        private void UpdateUI()
        {
            var selectedNode = tvTree.SelectedNode?.Tag as KbNode;
            bool hasSelection = selectedNode != null;
            var formState = _formStateService.Build(
                _isDirty,
                _requiresSave,
                CurrentDataPath,
                _currentWorkshop,
                _lastSavedWorkshop,
                tvTree.GetNodeCount(true),
                _config,
                selectedNode);

            btnUndo.Enabled = _treeMutationWorkflowService.CanUndo;
            btnRedo.Enabled = _treeMutationWorkflowService.CanRedo;
            btnSave.Enabled = formState.CanSave;

            ctxCopy.Enabled = hasSelection;
            ctxRename.Enabled = hasSelection;
            ctxDelete.Enabled = hasSelection;
            ctxAdd.Enabled = _treeMutationWorkflowService.CanAddNode(selectedNode);
            ctxPaste.Enabled = hasSelection && _treeMutationWorkflowService.CanPasteNode(selectedNode!);

            btnSave.ToolTipText = formState.SaveToolTip;
            Text = formState.WindowTitle;
            lblInfo.Text = formState.StatusText;
        }

        private void CmbWorkshops_SelectedIndexChanged(object? sender, EventArgs e)
        {
            if (_isBindingWorkshops)
                return;

            if (cmbWorkshops.SelectedItem is not string selected)
                return;

            var switchResult = _sessionWorkflowService.SelectWorkshop(selected, GetCurrentTreeData());
            if (!switchResult.IsSuccess)
                return;

            ApplySessionView(switchResult.ViewState, clearSearch: false);
            RefreshSearchAfterMutation();
            UpdateDirtyState();
            UpdateUI();
        }

        private void BtnAddWorkshop_Click(object? sender, EventArgs e)
        {
            using var dlg = new InputDialog("Введите название нового цеха:");
            if (dlg.ShowDialog() != DialogResult.OK || string.IsNullOrWhiteSpace(dlg.Result))
                return;

            string name = dlg.Result.Trim();
            string historySnapshot = CaptureCurrentHistorySnapshot();

            var addWorkshopResult = _sessionWorkflowService.AddWorkshop(name, GetCurrentTreeData());
            if (!addWorkshopResult.IsSuccess)
            {
                if (!string.IsNullOrWhiteSpace(addWorkshopResult.ErrorMessage))
                {
                    MessageBox.Show(
                        addWorkshopResult.ErrorMessage,
                        "Ошибка",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                }

                return;
            }

            _history.SaveState(historySnapshot);
            ApplySessionView(addWorkshopResult.ViewState, clearSearch: false);
            RefreshSearchAfterMutation();
            UpdateDirtyState();
            UpdateUI();
            lblInfo.Text = $"🏭 Добавлен цех: {addWorkshopResult.ViewState.CurrentWorkshop}";
        }

        private void BtnSetup_Click(object? sender, EventArgs e)
        {
            using var setup = new SetupForm(_config);
            if (setup.ShowDialog() != DialogResult.OK)
                return;

            var updateResult = _configurationWorkflowService.ValidateAndNormalize(setup.Config, _workshopsData);
            if (!updateResult.IsSuccess)
            {
                MessageBox.Show(
                    updateResult.ErrorMessage,
                    "Некорректная конфигурация",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            string historySnapshot = CaptureCurrentHistorySnapshot();
            _session.UpdateConfig(updateResult.Config);
            _history.SaveState(historySnapshot);
            RebindTreeController();
            UpdateDirtyState();
            UpdateUI();
            lblInfo.Text = $"💡 Уровни: {string.Join(" → ", _config.LevelNames)}";
        }

        private void TvTree_AfterSelect(object? sender, TreeViewEventArgs e) => UpdateUI();

        private void MainForm_FormClosing(object? sender, FormClosingEventArgs e)
            => _fileUiWorkflowService.HandleFormClosing(CreateFileUiWorkflowContext(), e);
    }
}
