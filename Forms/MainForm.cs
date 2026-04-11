using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using AsutpKnowledgeBase.Models;
using AsutpKnowledgeBase.Services;

namespace AsutpKnowledgeBase
{
    /// <summary>
    /// Главная форма приложения. Отвечает за UI и координацию работы сервисов.
    /// </summary>
    public partial class MainForm : Form
    {
        private readonly KnowledgeBaseSessionService _session = new();
        private readonly KnowledgeBaseFileWorkflowService _fileWorkflowService;
        private readonly KnowledgeBaseSessionWorkflowService _sessionWorkflowService;
        private readonly KnowledgeBaseTreeController _treeController;
        private readonly KnowledgeBaseConfigurationWorkflowService _configurationWorkflowService = new();
        private readonly KnowledgeBaseFormStateService _formStateService = new();
        private readonly UndoRedoService _history = new(50);

        private bool _isBindingWorkshops;

        private readonly List<TreeNode> _searchResults = new();
        private int _currentSearchIndex = -1;

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
            _fileWorkflowService = new KnowledgeBaseFileWorkflowService(
                _session,
                new JsonStorageService(GetDefaultJsonPath()));
            _sessionWorkflowService = new KnowledgeBaseSessionWorkflowService(_session);
            FormClosing += MainForm_FormClosing;
            LoadData();
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

            menuFile.DropDownItems.AddRange(new ToolStripItem[]
            {
                menuNewWorkshop,
                menuSetupLevels,
                new ToolStripSeparator(),
                menuOpenDb,
                menuReloadDb,
                menuSaveAs
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

        private string CurrentDataPath => _fileWorkflowService.SavePath;

        private string CurrentDataFileName => Path.GetFileName(CurrentDataPath);

        private void RebindTreeController() => _treeController.Bind(_config, _workshopsData);

        private bool LoadData(bool createDefaultIfMissing = true, bool fallbackToDefaultOnError = true)
        {
            var result = _fileWorkflowService.Load(createDefaultIfMissing, fallbackToDefaultOnError);

            switch (result.Outcome)
            {
                case KnowledgeBaseFileLoadOutcome.FileMissingError:
                    MessageBox.Show(
                        $"Файл '{CurrentDataPath}' не найден.",
                        "Файл не найден",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                    UpdateUI();
                    lblInfo.Text = "⚠️ Файл базы не найден";
                    return false;

                case KnowledgeBaseFileLoadOutcome.LoadError:
                    MessageBox.Show(
                        BuildLoadFailureMessage(result),
                        "Ошибка загрузки",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                    UpdateUI();
                    lblInfo.Text = "❌ Ошибка загрузки базы";
                    return false;

                case KnowledgeBaseFileLoadOutcome.CreatedDefaultAfterError:
                    ResetTransientUiStateAfterLoad();
                    RebuildUiFromSession();
                    UpdateUI();
                    MessageBox.Show(
                        BuildLoadFailureMessage(result),
                        "Ошибка загрузки",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                    lblInfo.Text = "⚠️ Загружена пустая база из-за ошибки чтения";
                    return true;

                case KnowledgeBaseFileLoadOutcome.CreatedDefaultAndSaved:
                    ResetTransientUiStateAfterLoad();
                    RebuildUiFromSession();
                    UpdateUI();
                    lblInfo.Text = "🆕 Создана новая база данных";
                    return true;

                case KnowledgeBaseFileLoadOutcome.CreatedDefaultUnsaved:
                    ResetTransientUiStateAfterLoad();
                    RebuildUiFromSession();
                    UpdateUI();
                    lblInfo.Text = "⚠️ База создана в памяти, но не сохранена на диск";
                    if (!string.IsNullOrWhiteSpace(result.ErrorMessage))
                    {
                        MessageBox.Show(
                            $"Ошибка сохранения: {result.ErrorMessage}",
                            "Ошибка сохранения",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Error);
                    }

                    return true;

                case KnowledgeBaseFileLoadOutcome.LoadedBackup:
                    ResetTransientUiStateAfterLoad();
                    RebuildUiFromSession();
                    UpdateUI();
                    MessageBox.Show(
                        BuildBackupLoadMessage(result),
                        "Загружена резервная копия",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                    lblInfo.Text = $"⚠️ Загружена резервная копия: {Path.GetFileName(result.SourcePath)}";
                    return true;

                case KnowledgeBaseFileLoadOutcome.LoadedExisting:
                    ResetTransientUiStateAfterLoad();
                    RebuildUiFromSession();
                    UpdateUI();
                    lblInfo.Text = $"📂 Загружен цех: {_currentWorkshop}";
                    return true;

                default:
                    return false;
            }
        }

        private void BindWorkshops(IReadOnlyList<string> workshopNames, string selectedWorkshop)
        {
            _isBindingWorkshops = true;
            cmbWorkshops.BeginUpdate();
            cmbWorkshops.Items.Clear();

            foreach (var workshop in workshopNames)
                cmbWorkshops.Items.Add(workshop);

            if (cmbWorkshops.Items.Count > 0)
                cmbWorkshops.SelectedItem = selectedWorkshop;

            cmbWorkshops.EndUpdate();
            _isBindingWorkshops = false;
        }

        private string BuildLoadFailureMessage(KnowledgeBaseFileLoadResult loadResult)
        {
            string message = $"Ошибка загрузки файла '{CurrentDataPath}': {loadResult.ErrorMessage}";
            if (!string.IsNullOrWhiteSpace(loadResult.BackupPath))
                message += $"\nРезервная копия '{loadResult.BackupPath}' тоже не была загружена.";

            return message;
        }

        private string BuildBackupLoadMessage(KnowledgeBaseFileLoadResult loadResult)
        {
            return
                $"Основной файл '{CurrentDataPath}' не удалось прочитать: {loadResult.PrimaryErrorMessage}\n" +
                $"Загружена резервная копия '{loadResult.SourcePath}'. После проверки данных сохраните базу заново.";
        }

        private void ResetTransientUiStateAfterLoad()
        {
            _history.Clear();
            _treeController.ClearClipboard();
        }

        private void RebuildUiFromSession()
        {
            ApplySessionView(_sessionWorkflowService.BuildViewState(), clearSearch: true);
        }

        private void ConfigureJsonDialog(FileDialog dialog)
        {
            dialog.Filter = "JSON (*.json)|*.json|Все файлы (*.*)|*.*";
            dialog.DefaultExt = "json";
            dialog.AddExtension = true;

            string? directory = Path.GetDirectoryName(CurrentDataPath);
            if (!string.IsNullOrWhiteSpace(directory) && Directory.Exists(directory))
                dialog.InitialDirectory = directory;

            dialog.FileName = CurrentDataFileName;
        }

        private void BtnOpen_Click(object? sender, EventArgs e)
        {
            if (!ConfirmContinueWithUnsavedChanges("открытием другой базы"))
                return;

            using var dialog = new OpenFileDialog
            {
                Title = "Открыть базу знаний",
                CheckFileExists = true
            };
            ConfigureJsonDialog(dialog);

            if (dialog.ShowDialog() != DialogResult.OK)
                return;

            string previousPath = CurrentDataPath;
            _fileWorkflowService.SavePath = dialog.FileName;

            if (!LoadData(createDefaultIfMissing: false, fallbackToDefaultOnError: false))
            {
                _fileWorkflowService.SavePath = previousPath;
                UpdateUI();
                return;
            }
        }

        private void BtnLoad_Click(object? sender, EventArgs e)
        {
            if (!ConfirmContinueWithUnsavedChanges("перезагрузкой базы из файла"))
                return;

            LoadData();
        }

        private void BtnSave_Click(object? sender, EventArgs e)
        {
            if (SaveAllData(showSuccessMessage: true, showErrorMessage: true))
                lblInfo.Text = $"✅ Данные сохранены: {CurrentDataFileName}";
        }

        private void BtnSaveAs_Click(object? sender, EventArgs e)
        {
            using var dialog = new SaveFileDialog
            {
                Title = "Сохранить базу как",
                OverwritePrompt = true
            };
            ConfigureJsonDialog(dialog);

            if (dialog.ShowDialog() != DialogResult.OK)
                return;

            string previousPath = CurrentDataPath;
            _fileWorkflowService.SavePath = dialog.FileName;

            if (!SaveAllData(showSuccessMessage: true, showErrorMessage: true))
            {
                _fileWorkflowService.SavePath = previousPath;
                UpdateUI();
                return;
            }

            lblInfo.Text = $"✅ База сохранена как: {CurrentDataFileName}";
        }

        private bool SaveAllData(bool showSuccessMessage, bool showErrorMessage)
        {
            var saveResult = _fileWorkflowService.Save(GetCurrentTreeData());

            if (saveResult.IsSuccess)
            {
                UpdateUI();

                if (showSuccessMessage)
                {
                    MessageBox.Show(
                        "Данные сохранены.",
                        "Сохранение",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                }

                return true;
            }

            lblInfo.Text = $"❌ Ошибка сохранения: {saveResult.ErrorMessage}";
            if (showErrorMessage)
            {
                MessageBox.Show(
                    $"Ошибка сохранения: {saveResult.ErrorMessage}",
                    "Ошибка сохранения",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }

            return false;
        }

        private string SerializeStateSnapshot(bool includeCurrentWorkshop)
            => _session.SerializeSnapshot(GetCurrentTreeData(), includeCurrentWorkshop);

        private void UpdateDirtyState() =>
            _session.RefreshDirtyState(GetCurrentTreeData());

        private void ApplySessionView(KnowledgeBaseSessionViewState viewState, bool clearSearch)
        {
            RebindTreeController();
            BindWorkshops(viewState.WorkshopNames, viewState.CurrentWorkshop);
            tvTree.BeginUpdate();
            tvTree.SelectedNode = null;
            tvTree.Nodes.Clear();

            foreach (var node in viewState.CurrentRoots)
                tvTree.Nodes.Add(BuildTreeNode(node));

            tvTree.EndUpdate();
            ExpandTreeToLevel(1);

            if (clearSearch)
                ClearSearch();
        }

        private void SaveCurrentWorkshopState() =>
            _session.SyncCurrentWorkshop(GetCurrentTreeData());

        private List<KbNode> GetCurrentTreeData()
        {
            var list = new List<KbNode>();
            foreach (TreeNode treeNode in tvTree.Nodes)
            {
                if (treeNode.Tag is KbNode node)
                    list.Add(node);
            }

            return list;
        }

        private TreeNode BuildTreeNode(KbNode node)
        {
            var treeNode = new TreeNode(node.Name) { Tag = node };
            foreach (var child in node.Children)
                treeNode.Nodes.Add(BuildTreeNode(child));

            return treeNode;
        }

        private void ExpandTreeToLevel(int targetLevelIndex)
        {
            foreach (TreeNode node in tvTree.Nodes)
                ExpandNodeRecursive(node, targetLevelIndex);
        }

        private void ExpandNodeRecursive(TreeNode node, int targetLevelIndex)
        {
            if (node.Tag is not KbNode kbNode || kbNode.LevelIndex >= targetLevelIndex)
                return;

            node.Expand();
            foreach (TreeNode child in node.Nodes)
                ExpandNodeRecursive(child, targetLevelIndex);
        }

        private void PerformSearch()
        {
            string searchText = txtSearch.Text.Trim();
            if (string.IsNullOrEmpty(searchText))
            {
                ClearSearch();
                UpdateUI();
                return;
            }

            _searchResults.Clear();
            _currentSearchIndex = -1;
            FindAllMatches(tvTree.Nodes, searchText);

            if (_searchResults.Count == 0)
            {
                tvTree.SelectedNode = null;
                UpdateSearchButtons();
                lblInfo.Text = $"❌ Не найдено: {searchText}";
                return;
            }

            _currentSearchIndex = 0;
            SelectSearchResult(_currentSearchIndex);
            UpdateSearchButtons();
            lblInfo.Text = $"🔍 Найдено: {_searchResults.Count} | Показан 1 из {_searchResults.Count}";
        }

        private void FindAllMatches(TreeNodeCollection nodes, string searchText)
        {
            foreach (TreeNode node in nodes)
            {
                if (node.Text.Contains(searchText, StringComparison.CurrentCultureIgnoreCase))
                    _searchResults.Add(node);

                FindAllMatches(node.Nodes, searchText);
            }
        }

        private void SelectSearchResult(int index)
        {
            if (index < 0 || index >= _searchResults.Count)
                return;

            var node = _searchResults[index];
            ExpandToNode(node);
            tvTree.SelectedNode = node;
            tvTree.Focus();
        }

        private void NavigateSearch(int direction)
        {
            if (_searchResults.Count == 0)
                return;

            _currentSearchIndex += direction;
            if (_currentSearchIndex >= _searchResults.Count)
                _currentSearchIndex = 0;
            if (_currentSearchIndex < 0)
                _currentSearchIndex = _searchResults.Count - 1;

            SelectSearchResult(_currentSearchIndex);
            UpdateSearchButtons();
            lblInfo.Text = $"🔍 Найдено: {_searchResults.Count} | Показан {_currentSearchIndex + 1} из {_searchResults.Count}";
        }

        private void UpdateSearchButtons()
        {
            bool canNavigate = _searchResults.Count > 1;
            btnSearchPrev.Enabled = canNavigate;
            btnSearchNext.Enabled = canNavigate;
        }

        private void ClearSearch()
        {
            _searchResults.Clear();
            _currentSearchIndex = -1;
            UpdateSearchButtons();
            lblInfo.Text = "Готово";
        }

        private void RefreshSearchAfterMutation()
        {
            if (string.IsNullOrWhiteSpace(txtSearch.Text))
            {
                ClearSearch();
                return;
            }

            PerformSearch();
        }

        private static void ExpandToNode(TreeNode node)
        {
            TreeNode? current = node.Parent;
            while (current != null)
            {
                current.Expand();
                current = current.Parent;
            }

            node.Expand();
        }

        private string CaptureHistorySnapshot() =>
            SerializeStateSnapshot(includeCurrentWorkshop: true);

        private void PushHistorySnapshot(string snapshot) =>
            _history.SaveState(snapshot);

        private void AddNode()
        {
            if (_config.MaxLevels <= 0)
            {
                MessageBox.Show(
                    "Сначала настройте уровни.",
                    "Внимание",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            using var dlg = new InputDialog("Введите название нового объекта:");
            if (dlg.ShowDialog() != DialogResult.OK || string.IsNullOrWhiteSpace(dlg.Result))
                return;

            var selectedData = tvTree.SelectedNode?.Tag as KbNode;

            if (!_treeController.CanAddNode(selectedData))
            {
                MessageBox.Show(
                    $"Достигнута максимальная глубина ({_config.MaxLevels}).",
                    "Невозможно добавить",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            string historySnapshot = CaptureHistorySnapshot();
            var newNode = _treeController.AddNode(_currentWorkshop, selectedData, dlg.Result);
            PushHistorySnapshot(historySnapshot);

            if (selectedData == null)
            {
                var newTreeNode = BuildTreeNode(newNode);
                tvTree.Nodes.Add(newTreeNode);
                tvTree.SelectedNode = newTreeNode;
            }
            else
            {
                var newTreeNode = BuildTreeNode(newNode);
                tvTree.SelectedNode!.Nodes.Add(newTreeNode);
                tvTree.SelectedNode!.Expand();
                tvTree.SelectedNode = newTreeNode;
            }

            RefreshSearchAfterMutation();
            UpdateDirtyState();
            UpdateUI();
            lblInfo.Text = $"➕ Добавлено: {newNode.Name}";
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

            var selectedTreeNode = tvTree.SelectedNode!;
            TreeNode? nextSelection = selectedTreeNode.Parent;

            string historySnapshot = CaptureHistorySnapshot();
            if (!_treeController.DeleteNode(_currentWorkshop, node))
            {
                MessageBox.Show(
                    "Не удалось удалить выбранный узел.",
                    "Ошибка удаления",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return;
            }

            PushHistorySnapshot(historySnapshot);

            selectedTreeNode.Remove();
            tvTree.SelectedNode = nextSelection;

            RefreshSearchAfterMutation();
            UpdateDirtyState();
            UpdateUI();
            lblInfo.Text = $"🗑 Удалено: {node.Name}";
        }

        private void CopyNode()
        {
            if (tvTree.SelectedNode?.Tag is not KbNode node)
                return;

            _treeController.CopyNode(node);
            UpdateUI();
            lblInfo.Text = $"📋 Скопировано: {node.Name}";
        }

        private void PasteNode()
        {
            if (!_treeController.HasClipboardNode || tvTree.SelectedNode?.Tag is not KbNode parent)
                return;

            if (!_treeController.CanPasteNode(parent))
            {
                MessageBox.Show(
                    $"Поддерево не помещается в глубину {_config.MaxLevels}.",
                    "Ошибка вставки",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            string historySnapshot = CaptureHistorySnapshot();
            var newNode = _treeController.PasteNode(parent);
            PushHistorySnapshot(historySnapshot);

            var newTreeNode = BuildTreeNode(newNode);
            tvTree.SelectedNode.Nodes.Add(newTreeNode);
            tvTree.SelectedNode.Expand();
            tvTree.SelectedNode = newTreeNode;

            RefreshSearchAfterMutation();
            UpdateDirtyState();
            UpdateUI();
            lblInfo.Text = $"📌 Вставлено: {newNode.Name}";
        }

        private void RenameNode()
        {
            if (tvTree.SelectedNode?.Tag is not KbNode node)
                return;

            using var dlg = new InputDialog("Новое название:", node.Name);
            if (dlg.ShowDialog() != DialogResult.OK || string.IsNullOrWhiteSpace(dlg.Result))
                return;

            string newName = dlg.Result.Trim();
            if (string.Equals(node.Name, newName, StringComparison.CurrentCulture))
                return;

            string historySnapshot = CaptureHistorySnapshot();
            _treeController.RenameNode(node, newName);
            PushHistorySnapshot(historySnapshot);
            tvTree.SelectedNode.Text = newName;

            RefreshSearchAfterMutation();
            UpdateDirtyState();
            UpdateUI();
            lblInfo.Text = $"✏️ Переименовано в: {newName}";
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

            if (IsDescendant(draggedNode, targetNode))
            {
                MessageBox.Show(
                    "Нельзя переместить узел внутрь его потомка.",
                    "Ошибка",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            if (targetNode.Tag is not KbNode targetData || draggedNode.Tag is not KbNode draggedData)
                return;

            if (!_treeController.CanMoveNode(targetData, draggedData))
            {
                MessageBox.Show(
                    $"Поддерево не помещается в глубину {_config.MaxLevels}.",
                    "Внимание",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            string historySnapshot = CaptureHistorySnapshot();
            if (!_treeController.MoveNode(
                _currentWorkshop,
                draggedData,
                draggedNode.Parent?.Tag as KbNode,
                targetData))
            {
                MessageBox.Show(
                    "Не удалось переместить узел в новую позицию.",
                    "Ошибка перемещения",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return;
            }

            PushHistorySnapshot(historySnapshot);
            draggedNode.Remove();
            targetNode.Nodes.Add(draggedNode);
            targetNode.Expand();
            tvTree.SelectedNode = draggedNode;

            RefreshSearchAfterMutation();
            UpdateDirtyState();
            UpdateUI();
            lblInfo.Text = $"↕ Перемещено: {draggedData.Name}";
        }

        private static bool IsDescendant(TreeNode node, TreeNode potentialDescendant)
        {
            TreeNode? current = potentialDescendant;
            while (current != null)
            {
                if (current == node)
                    return true;

                current = current.Parent;
            }

            return false;
        }

        private void UndoAction()
        {
            var snapshot = _history.Undo(SerializeStateSnapshot(includeCurrentWorkshop: true));
            if (snapshot == null)
                return;

            RestoreState(snapshot, "↩ Выполнена отмена");
        }

        private void RedoAction()
        {
            var snapshot = _history.Redo(SerializeStateSnapshot(includeCurrentWorkshop: true));
            if (snapshot == null)
                return;

            RestoreState(snapshot, "↪ Выполнен повтор");
        }

        private void RestoreState(string json, string statusText)
        {
            var restoreResult = _sessionWorkflowService.RestoreSnapshot(json);
            if (!restoreResult.IsSuccess)
            {
                MessageBox.Show(
                    restoreResult.ErrorMessage,
                    "Undo/Redo",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return;
            }

            ApplySessionView(restoreResult.ViewState, clearSearch: true);
            UpdateDirtyState();
            UpdateUI();
            lblInfo.Text = statusText;
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

            btnUndo.Enabled = _history.CanUndo;
            btnRedo.Enabled = _history.CanRedo;
            btnSave.Enabled = formState.CanSave;

            ctxCopy.Enabled = hasSelection;
            ctxRename.Enabled = hasSelection;
            ctxDelete.Enabled = hasSelection;
            ctxAdd.Enabled = _treeController.CanAddNode(selectedNode);
            ctxPaste.Enabled = hasSelection && _treeController.CanPasteNode(selectedNode!);

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
            UpdateDirtyState();
            UpdateUI();
        }

        private void BtnAddWorkshop_Click(object? sender, EventArgs e)
        {
            using var dlg = new InputDialog("Введите название нового цеха:");
            if (dlg.ShowDialog() != DialogResult.OK || string.IsNullOrWhiteSpace(dlg.Result))
                return;

            string name = dlg.Result.Trim();
            string historySnapshot = CaptureHistorySnapshot();

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

            PushHistorySnapshot(historySnapshot);
            ApplySessionView(addWorkshopResult.ViewState, clearSearch: false);
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

            string historySnapshot = CaptureHistorySnapshot();
            _session.UpdateConfig(updateResult.Config);
            PushHistorySnapshot(historySnapshot);
            RebindTreeController();
            UpdateDirtyState();
            UpdateUI();
            lblInfo.Text = $"💡 Уровни: {string.Join(" → ", _config.LevelNames)}";
        }

        private void TvTree_AfterSelect(object? sender, TreeViewEventArgs e) => UpdateUI();

        private bool ConfirmContinueWithUnsavedChanges(string actionDescription)
        {
            SaveCurrentWorkshopState();
            UpdateDirtyState();

            if (!_formStateService.RequiresSavePromptBeforeContinue(_isDirty, _requiresSave))
                return true;

            var result = MessageBox.Show(
                $"Есть несохранённые изменения. Сохранить перед {actionDescription}?",
                "Несохранённые изменения",
                MessageBoxButtons.YesNoCancel,
                MessageBoxIcon.Warning);

            if (result == DialogResult.Cancel)
                return false;

            if (result == DialogResult.Yes)
                return SaveAllData(showSuccessMessage: false, showErrorMessage: true);

            return true;
        }

        private void MainForm_FormClosing(object? sender, FormClosingEventArgs e)
        {
            SaveCurrentWorkshopState();
            UpdateDirtyState();

            if (_formStateService.RequiresSavePromptOnClose(_isDirty, _requiresSave))
            {
                var result = MessageBox.Show(
                    "Есть несохранённые изменения. Сохранить перед закрытием?",
                    "Закрытие приложения",
                    MessageBoxButtons.YesNoCancel,
                    MessageBoxIcon.Warning);

                if (result == DialogResult.Cancel)
                {
                    e.Cancel = true;
                    return;
                }

                if (result == DialogResult.Yes &&
                    !SaveAllData(showSuccessMessage: false, showErrorMessage: true))
                {
                    e.Cancel = true;
                    return;
                }

                return;
            }

            if (_formStateService.ShouldSaveSilentlyOnClose(_currentWorkshop, _lastSavedWorkshop) &&
                !SaveAllData(showSuccessMessage: false, showErrorMessage: true))
            {
                e.Cancel = true;
            }
        }
    }
}
