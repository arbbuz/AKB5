using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text.Json;
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
        private KbConfig _config = KnowledgeBaseDataService.CreateDefaultConfig();
        private Dictionary<string, List<KbNode>> _workshopsData = new();
        private string _currentWorkshop = string.Empty;
        private readonly JsonStorageService _storageService;
        private readonly KnowledgeBaseTreeController _treeController;
        private readonly UndoRedoService _history = new(50);

        private string _lastSavedDirtySnapshot = string.Empty;
        private string _lastSavedWorkshop = string.Empty;
        private bool _isDirty;
        private bool _isBindingWorkshops;
        private bool _requiresSave;

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

        public MainForm()
        {
            _treeController = new KnowledgeBaseTreeController(_config, _workshopsData);
            InitializeComponent();
            _storageService = new JsonStorageService(GetDefaultJsonPath());
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

        private string CurrentDataPath => _storageService.SavePath;

        private string CurrentDataFileName => Path.GetFileName(CurrentDataPath);

        private KnowledgeBaseService CreateKnowledgeBaseService() => new(_config, _workshopsData);

        private void RebindTreeController() => _treeController.Bind(_config, _workshopsData);

        private void InitializeDefaultData()
        {
            ApplyLoadedData(
                KnowledgeBaseDataService.CreateDefaultData(),
                recordAsSavedState: false);
        }

        private bool LoadData(bool createDefaultIfMissing = true, bool fallbackToDefaultOnError = true)
        {
            var loadResult = _storageService.Load();

            if (loadResult.FileMissing)
            {
                if (!createDefaultIfMissing)
                {
                    MessageBox.Show(
                        $"Файл '{CurrentDataPath}' не найден.",
                        "Файл не найден",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                    UpdateUI();
                    lblInfo.Text = "⚠️ Файл базы не найден";
                    return false;
                }

                _history.Clear();
                _treeController.ClearClipboard();
                InitializeDefaultData();
                if (SaveAllData(showSuccessMessage: false, showErrorMessage: true))
                {
                    UpdateUI();
                    lblInfo.Text = "🆕 Создана новая база данных";
                }
                else
                {
                    _requiresSave = true;
                    UpdateUI();
                    lblInfo.Text = "⚠️ База создана в памяти, но не сохранена на диск";
                }

                return true;
            }

            if (!loadResult.IsSuccess)
            {
                if (!fallbackToDefaultOnError)
                {
                    MessageBox.Show(
                        BuildLoadFailureMessage(loadResult),
                        "Ошибка загрузки",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                    UpdateUI();
                    lblInfo.Text = "❌ Ошибка загрузки базы";
                    return false;
                }

                _history.Clear();
                _treeController.ClearClipboard();
                InitializeDefaultData();
                RecordSavedState();
                _requiresSave = true;
                UpdateUI();
                MessageBox.Show(
                    BuildLoadFailureMessage(loadResult),
                    "Ошибка загрузки",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                lblInfo.Text = "⚠️ Загружена пустая база из-за ошибки чтения";
                return true;
            }

            _history.Clear();
            _treeController.ClearClipboard();
            ApplyLoadedData(loadResult.Data!, recordAsSavedState: true);
            _requiresSave = loadResult.LoadedFromBackup;
            UpdateUI();

            if (loadResult.LoadedFromBackup)
            {
                MessageBox.Show(
                    BuildBackupLoadMessage(loadResult),
                    "Загружена резервная копия",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                lblInfo.Text = $"⚠️ Загружена резервная копия: {Path.GetFileName(loadResult.SourcePath)}";
            }
            else
            {
                lblInfo.Text = $"📂 Загружен цех: {_currentWorkshop}";
            }

            return true;
        }

        private void ApplyLoadedData(SavedData data, bool recordAsSavedState)
        {
            _config = KnowledgeBaseDataService.NormalizeConfig(data.Config);
            _workshopsData = KnowledgeBaseDataService.NormalizeWorkshops(data.Workshops);
            RebindTreeController();

            var service = CreateKnowledgeBaseService();
            foreach (var roots in _workshopsData.Values)
            {
                foreach (var root in roots)
                    service.ReindexSubtree(root, 0);
            }

            _currentWorkshop = KnowledgeBaseDataService.ResolveWorkshop(_workshopsData, data.LastWorkshop);
            BindWorkshops(_currentWorkshop);
            LoadCurrentWorkshop();
            ClearSearch();

            if (recordAsSavedState)
                RecordSavedState();
            else
                UpdateDirtyState();
        }

        private void BindWorkshops(string selectedWorkshop)
        {
            _isBindingWorkshops = true;
            cmbWorkshops.BeginUpdate();
            cmbWorkshops.Items.Clear();

            foreach (var workshop in _workshopsData.Keys)
                cmbWorkshops.Items.Add(workshop);

            if (cmbWorkshops.Items.Count > 0)
                cmbWorkshops.SelectedItem = selectedWorkshop;

            cmbWorkshops.EndUpdate();
            _isBindingWorkshops = false;
        }

        private string BuildLoadFailureMessage(JsonLoadResult loadResult)
        {
            string message = $"Ошибка загрузки файла '{CurrentDataPath}': {loadResult.ErrorMessage}";
            if (!string.IsNullOrWhiteSpace(loadResult.BackupPath))
                message += $"\nРезервная копия '{loadResult.BackupPath}' тоже не была загружена.";

            return message;
        }

        private string BuildBackupLoadMessage(JsonLoadResult loadResult)
        {
            return
                $"Основной файл '{CurrentDataPath}' не удалось прочитать: {loadResult.PrimaryErrorMessage}\n" +
                $"Загружена резервная копия '{loadResult.SourcePath}'. После проверки данных сохраните базу заново.";
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
            _storageService.SavePath = dialog.FileName;

            if (!LoadData(createDefaultIfMissing: false, fallbackToDefaultOnError: false))
            {
                _storageService.SavePath = previousPath;
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
            _storageService.SavePath = dialog.FileName;

            if (!SaveAllData(showSuccessMessage: true, showErrorMessage: true))
            {
                _storageService.SavePath = previousPath;
                UpdateUI();
                return;
            }

            lblInfo.Text = $"✅ База сохранена как: {CurrentDataFileName}";
        }

        private bool SaveAllData(bool showSuccessMessage, bool showErrorMessage)
        {
            SaveCurrentWorkshopState();

            var data = new SavedData
            {
                Config = _config,
                Workshops = _workshopsData,
                LastWorkshop = _currentWorkshop
            };

            if (_storageService.Save(data, out var errorMessage))
            {
                RecordSavedState();
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

            lblInfo.Text = $"❌ Ошибка сохранения: {errorMessage}";
            if (showErrorMessage)
            {
                MessageBox.Show(
                    $"Ошибка сохранения: {errorMessage}",
                    "Ошибка сохранения",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }

            return false;
        }

        private string SerializeStateSnapshot(bool includeCurrentWorkshop)
        {
            SaveCurrentWorkshopState();

            var data = new SavedData
            {
                Config = _config,
                Workshops = _workshopsData,
                LastWorkshop = includeCurrentWorkshop ? _currentWorkshop : string.Empty
            };

            return KnowledgeBaseDataService.SerializeSnapshot(
                _config,
                _workshopsData,
                _currentWorkshop,
                includeCurrentWorkshop);
        }

        private void RecordSavedState()
        {
            _lastSavedDirtySnapshot = SerializeStateSnapshot(includeCurrentWorkshop: false);
            _lastSavedWorkshop = _currentWorkshop;
            _isDirty = false;
            _requiresSave = false;
        }

        private void UpdateDirtyState()
        {
            _isDirty = SerializeStateSnapshot(includeCurrentWorkshop: false) != _lastSavedDirtySnapshot;
        }

        private void LoadCurrentWorkshop()
        {
            if (string.IsNullOrWhiteSpace(_currentWorkshop) || !_workshopsData.ContainsKey(_currentWorkshop))
                _currentWorkshop = KnowledgeBaseDataService.ResolveWorkshop(_workshopsData, null);

            tvTree.BeginUpdate();
            tvTree.SelectedNode = null;
            tvTree.Nodes.Clear();

            if (_workshopsData.TryGetValue(_currentWorkshop, out var nodes))
            {
                foreach (var node in nodes)
                    tvTree.Nodes.Add(BuildTreeNode(node));
            }

            tvTree.EndUpdate();
            ExpandTreeToLevel(1);
        }

        private void SaveCurrentWorkshopState()
        {
            if (string.IsNullOrWhiteSpace(_currentWorkshop))
                return;

            _workshopsData[_currentWorkshop] = GetCurrentTreeData();
        }

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

        private void SaveState()
        {
            _history.SaveState(SerializeStateSnapshot(includeCurrentWorkshop: true));
            UpdateUI();
        }

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

            SaveState();
            var newNode = _treeController.AddNode(_currentWorkshop, selectedData, dlg.Result);

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

            SaveState();
            _treeController.DeleteNode(_currentWorkshop, node);

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

            SaveState();
            var newNode = _treeController.PasteNode(parent);

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

            SaveState();
            _treeController.RenameNode(node, newName);
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

            SaveState();
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
            try
            {
                var data = JsonSerializer.Deserialize<SavedData>(
                    json,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (data == null)
                    return;

                ApplyLoadedData(data, recordAsSavedState: false);
                UpdateDirtyState();
                UpdateUI();
                lblInfo.Text = statusText;
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Ошибка восстановления состояния: {ex.Message}",
                    "Undo/Redo",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private void UpdateUI()
        {
            var selectedNode = tvTree.SelectedNode?.Tag as KbNode;
            bool hasSelection = selectedNode != null;

            btnUndo.Enabled = _history.CanUndo;
            btnRedo.Enabled = _history.CanRedo;
            btnSave.Enabled = _isDirty || _requiresSave || !File.Exists(CurrentDataPath) || _currentWorkshop != _lastSavedWorkshop;

            ctxCopy.Enabled = hasSelection;
            ctxRename.Enabled = hasSelection;
            ctxDelete.Enabled = hasSelection;
            ctxAdd.Enabled = _treeController.CanAddNode(selectedNode);
            ctxPaste.Enabled = hasSelection && _treeController.CanPasteNode(selectedNode!);

            btnSave.ToolTipText = $"Сохранить базу данных ({CurrentDataPath})";
            Text = _isDirty
                ? $"* База знаний АСУТП [{CurrentDataFileName}]"
                : $"База знаний АСУТП [{CurrentDataFileName}]";

            int totalNodes = tvTree.GetNodeCount(true);
            if (hasSelection)
            {
                string levelName = _config.LevelNames.Count > selectedNode!.LevelIndex
                    ? _config.LevelNames[selectedNode.LevelIndex]
                    : $"Ур. {selectedNode.LevelIndex + 1}";
                lblInfo.Text = $"Цех: {_currentWorkshop} | Всего: {totalNodes} | Выбрано: {selectedNode.Name} ({levelName})";
            }
            else
            {
                lblInfo.Text = $"Цех: {_currentWorkshop} | Всего узлов: {totalNodes} | Уровней: {_config.MaxLevels}";
            }
        }

        private void CmbWorkshops_SelectedIndexChanged(object? sender, EventArgs e)
        {
            if (_isBindingWorkshops)
                return;

            if (cmbWorkshops.SelectedItem is not string selected || selected == _currentWorkshop)
                return;

            SaveCurrentWorkshopState();
            _currentWorkshop = selected;
            LoadCurrentWorkshop();
            UpdateDirtyState();
            UpdateUI();
        }

        private void BtnAddWorkshop_Click(object? sender, EventArgs e)
        {
            using var dlg = new InputDialog("Введите название нового цеха:");
            if (dlg.ShowDialog() != DialogResult.OK || string.IsNullOrWhiteSpace(dlg.Result))
                return;

            string name = dlg.Result.Trim();
            if (_workshopsData.Keys.Any(existing => string.Equals(existing, name, StringComparison.CurrentCultureIgnoreCase)))
            {
                MessageBox.Show(
                    "Цех с таким названием уже существует.",
                    "Ошибка",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            SaveState();
            SaveCurrentWorkshopState();

            _workshopsData[name] = new List<KbNode>();
            _currentWorkshop = name;
            BindWorkshops(name);
            LoadCurrentWorkshop();

            UpdateDirtyState();
            UpdateUI();
            lblInfo.Text = $"🏭 Добавлен цех: {name}";
        }

        private void BtnSetup_Click(object? sender, EventArgs e)
        {
            using var setup = new SetupForm(_config);
            if (setup.ShowDialog() != DialogResult.OK)
                return;

            var newConfig = KnowledgeBaseDataService.NormalizeConfig(setup.Config);
            var validationService = new KnowledgeBaseService(newConfig, _workshopsData);
            int maxUsedLevel = -1;
            foreach (var roots in _workshopsData.Values)
                maxUsedLevel = Math.Max(maxUsedLevel, validationService.GetMaxLevelIndex(roots));

            if (maxUsedLevel >= newConfig.MaxLevels)
            {
                MessageBox.Show(
                    $"Нельзя уменьшить количество уровней до {newConfig.MaxLevels}. В базе уже используется уровень {maxUsedLevel + 1}.",
                    "Некорректная конфигурация",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            SaveState();
            _config = newConfig;
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

            if (!_isDirty)
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

            if (_isDirty)
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

            if (_currentWorkshop != _lastSavedWorkshop &&
                !SaveAllData(showSuccessMessage: false, showErrorMessage: true))
            {
                e.Cancel = true;
            }
        }
    }
}
