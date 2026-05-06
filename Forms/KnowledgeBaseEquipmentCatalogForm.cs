using AsutpKnowledgeBase.Models;
using AsutpKnowledgeBase.Services;

namespace AsutpKnowledgeBase
{
    public sealed class KnowledgeBaseEquipmentCatalogForm : Form
    {
        private readonly KnowledgeBaseEquipmentCatalogService _catalogService = new();
        private List<KbEquipmentCatalogItem> _items;
        private TextBox _txtSearch = null!;
        private DataGridView _grid = null!;
        private Label _lblSummary = null!;

        public KnowledgeBaseEquipmentCatalogForm(IReadOnlyList<KbEquipmentCatalogItem>? catalogItems)
        {
            _items = KnowledgeBaseDataService.NormalizeEquipmentCatalogItems(catalogItems)
                .Select(KnowledgeBaseEquipmentCatalogService.CloneCatalogItem)
                .ToList();

            Text = "Каталог оборудования";
            StartPosition = FormStartPosition.CenterParent;
            MinimizeBox = false;
            ShowInTaskbar = false;
            Size = new Size(1120, 680);
            MinimumSize = new Size(900, 560);
            AppIconProvider.Apply(this);

            Controls.Add(CreateLayout());
            RefreshGrid();

            AcceptButton = Controls
                .Find("btnOk", searchAllChildren: true)
                .OfType<Button>()
                .FirstOrDefault();
            CancelButton = Controls
                .Find("btnCancel", searchAllChildren: true)
                .OfType<Button>()
                .FirstOrDefault();
        }

        public List<KbEquipmentCatalogItem> ResultItems { get; private set; } = new();

        private TableLayoutPanel CreateLayout()
        {
            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(12),
                ColumnCount = 1,
                RowCount = 4
            };
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            layout.Controls.Add(CreateTopPanel(), 0, 0);

            _lblSummary = new Label
            {
                AutoSize = true,
                Margin = new Padding(0, 0, 0, 8)
            };
            layout.Controls.Add(_lblSummary, 0, 1);

            _grid = CreateGrid();
            layout.Controls.Add(_grid, 0, 2);

            layout.Controls.Add(CreateBottomPanel(), 0, 3);

            return layout;
        }

        private TableLayoutPanel CreateTopPanel()
        {
            var panel = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                ColumnCount = 6,
                Margin = new Padding(0, 0, 0, 8)
            };
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

            panel.Controls.Add(new Label
            {
                Text = "Поиск",
                AutoSize = true,
                Margin = new Padding(0, 6, 8, 0)
            }, 0, 0);

            _txtSearch = new TextBox
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 0, 8, 0)
            };
            _txtSearch.TextChanged += (_, _) => RefreshGrid();
            panel.Controls.Add(_txtSearch, 1, 0);

            panel.Controls.Add(CreateActionButton("Добавить", AddItem), 2, 0);
            panel.Controls.Add(CreateActionButton("Изменить", EditSelectedItem), 3, 0);
            panel.Controls.Add(CreateActionButton("Удалить", DeleteSelectedItem), 4, 0);

            var btnClear = CreateActionButton("Очистить", ClearSearch);
            panel.Controls.Add(btnClear, 5, 0);

            return panel;
        }

        private FlowLayoutPanel CreateBottomPanel()
        {
            var buttonsPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoSize = true,
                FlowDirection = FlowDirection.RightToLeft,
                Margin = new Padding(0, 12, 0, 0)
            };

            var btnOk = new Button
            {
                Name = "btnOk",
                Text = "Сохранить",
                AutoSize = true
            };
            btnOk.Click += (_, _) => Submit();

            var btnCancel = new Button
            {
                Name = "btnCancel",
                Text = "Отмена",
                AutoSize = true,
                DialogResult = DialogResult.Cancel
            };

            buttonsPanel.Controls.Add(btnOk);
            buttonsPanel.Controls.Add(btnCancel);
            return buttonsPanel;
        }

        private DataGridView CreateGrid()
        {
            var grid = new DataGridView
            {
                Dock = DockStyle.Fill,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToResizeRows = false,
                AutoGenerateColumns = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None,
                BackgroundColor = SystemColors.Window,
                BorderStyle = BorderStyle.Fixed3D,
                MultiSelect = false,
                ReadOnly = true,
                RowHeadersVisible = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect
            };

            grid.Columns.Add(CreateTextColumn("CatalogItemId", "Id", 80, visible: false));
            grid.Columns.Add(CreateTextColumn("EquipmentKind", "Вид оборудования", 160));
            grid.Columns.Add(CreateTextColumn("Manufacturer", "Производитель", 140));
            grid.Columns.Add(CreateTextColumn("Series", "Серия", 120));
            grid.Columns.Add(CreateTextColumn("Model", "Модель", 170));
            grid.Columns.Add(CreateTextColumn("DefaultNodeType", "Тип узла", 110));
            grid.Columns.Add(CreateTextColumn("Description", "Описание", 220));
            grid.Columns.Add(CreateTextColumn("Properties", "Свойства", 260));

            grid.CellDoubleClick += (_, e) =>
            {
                if (e.RowIndex >= 0)
                    EditSelectedItem();
            };
            grid.DataError += (_, e) => e.ThrowException = false;

            return grid;
        }

        private void RefreshGrid(string? preferredCatalogItemId = null)
        {
            string? selectedId = preferredCatalogItemId ?? GetSelectedItem()?.CatalogItemId;
            List<KbEquipmentCatalogItem> visibleItems = _catalogService.Search(_items, _txtSearch?.Text);

            _grid?.Rows.Clear();
            if (_grid != null)
            {
                foreach (KbEquipmentCatalogItem item in visibleItems)
                {
                    int rowIndex = _grid.Rows.Add();
                    DataGridViewRow row = _grid.Rows[rowIndex];
                    row.Tag = item;
                    row.Cells["CatalogItemId"].Value = item.CatalogItemId;
                    row.Cells["EquipmentKind"].Value = item.EquipmentKind;
                    row.Cells["Manufacturer"].Value = item.Manufacturer;
                    row.Cells["Series"].Value = item.Series;
                    row.Cells["Model"].Value = item.Model;
                    row.Cells["DefaultNodeType"].Value = FormatNodeType(item.DefaultNodeType);
                    row.Cells["Description"].Value = item.Description;
                    row.Cells["Properties"].Value = FormatProperties(item.Properties);

                    if (!string.IsNullOrWhiteSpace(selectedId) &&
                        string.Equals(item.CatalogItemId, selectedId, StringComparison.Ordinal))
                    {
                        row.Selected = true;
                        _grid.CurrentCell = row.Cells["EquipmentKind"];
                    }
                }

                if (_grid.Rows.Count > 0 && _grid.SelectedRows.Count == 0)
                {
                    _grid.Rows[0].Selected = true;
                    _grid.CurrentCell = _grid.Rows[0].Cells["EquipmentKind"];
                }
            }

            if (_lblSummary != null)
            {
                _lblSummary.Text = string.IsNullOrWhiteSpace(_txtSearch?.Text)
                    ? $"Записей каталога: {_items.Count}"
                    : $"Найдено: {visibleItems.Count} из {_items.Count}";
            }
        }

        private void AddItem()
        {
            using var dialog = new KnowledgeBaseEquipmentCatalogItemDialog("Добавить запись каталога");
            if (dialog.ShowDialog(this) != DialogResult.OK)
                return;

            ApplyMutation(_catalogService.UpsertItem(_items, dialog.Result));
        }

        private void EditSelectedItem()
        {
            KbEquipmentCatalogItem? selectedItem = GetSelectedItem();
            if (selectedItem == null)
            {
                MessageBox.Show(
                    this,
                    "Выберите запись каталога для изменения.",
                    "Каталог оборудования",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            using var dialog = new KnowledgeBaseEquipmentCatalogItemDialog(
                "Изменить запись каталога",
                KnowledgeBaseEquipmentCatalogService.CloneCatalogItem(selectedItem));
            if (dialog.ShowDialog(this) != DialogResult.OK)
                return;

            ApplyMutation(_catalogService.UpsertItem(_items, dialog.Result), selectedItem.CatalogItemId);
        }

        private void DeleteSelectedItem()
        {
            KbEquipmentCatalogItem? selectedItem = GetSelectedItem();
            if (selectedItem == null)
            {
                MessageBox.Show(
                    this,
                    "Выберите запись каталога для удаления.",
                    "Каталог оборудования",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            DialogResult confirmResult = MessageBox.Show(
                this,
                $"Удалить запись каталога \"{GetDisplayName(selectedItem)}\"?",
                "Каталог оборудования",
                MessageBoxButtons.OKCancel,
                MessageBoxIcon.Warning);
            if (confirmResult != DialogResult.OK)
                return;

            ApplyMutation(_catalogService.DeleteItem(_items, selectedItem.CatalogItemId));
        }

        private void ApplyMutation(
            KnowledgeBaseEquipmentCatalogMutationResult result,
            string? preferredCatalogItemId = null)
        {
            if (!result.IsSuccess)
            {
                MessageBox.Show(
                    this,
                    result.ErrorMessage,
                    "Каталог оборудования",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            _items = result.EquipmentCatalogItems
                .Select(KnowledgeBaseEquipmentCatalogService.CloneCatalogItem)
                .ToList();
            RefreshGrid(preferredCatalogItemId);
        }

        private void ClearSearch()
        {
            _txtSearch.Clear();
            RefreshGrid();
            _txtSearch.Focus();
        }

        private void Submit()
        {
            ResultItems = KnowledgeBaseDataService.NormalizeEquipmentCatalogItems(_items);
            DialogResult = DialogResult.OK;
            Close();
        }

        private KbEquipmentCatalogItem? GetSelectedItem()
        {
            if (_grid.SelectedRows.Count > 0 &&
                _grid.SelectedRows[0].Tag is KbEquipmentCatalogItem selectedItem)
            {
                return selectedItem;
            }

            return _grid.CurrentRow?.Tag as KbEquipmentCatalogItem;
        }

        private static Button CreateActionButton(string text, Action action)
        {
            var button = new Button
            {
                Text = text,
                AutoSize = true,
                Margin = new Padding(0, 0, 6, 0)
            };
            button.Click += (_, _) => action();
            return button;
        }

        private static DataGridViewTextBoxColumn CreateTextColumn(
            string name,
            string headerText,
            int width,
            bool visible = true) =>
            new()
            {
                Name = name,
                HeaderText = headerText,
                Width = width,
                Visible = visible,
                SortMode = DataGridViewColumnSortMode.NotSortable
            };

        private static string FormatProperties(IEnumerable<KbEquipmentCatalogProperty> properties) =>
            string.Join(
                "; ",
                properties
                    .Where(static property => !string.IsNullOrWhiteSpace(property.Name))
                    .Select(static property => string.IsNullOrWhiteSpace(property.Value)
                        ? property.Name.Trim()
                        : $"{property.Name.Trim()}: {property.Value.Trim()}"));

        private static string GetDisplayName(KbEquipmentCatalogItem item)
        {
            string[] parts =
            {
                item.EquipmentKind?.Trim() ?? string.Empty,
                item.Manufacturer?.Trim() ?? string.Empty,
                item.Series?.Trim() ?? string.Empty,
                item.Model?.Trim() ?? string.Empty
            };
            string displayName = string.Join(" ", parts.Where(static part => !string.IsNullOrWhiteSpace(part)));
            return string.IsNullOrWhiteSpace(displayName) ? "без названия" : displayName;
        }

        private static string FormatNodeType(KbNodeType nodeType) =>
            nodeType switch
            {
                KbNodeType.System => "Система",
                KbNodeType.Cabinet => "Шкаф",
                KbNodeType.Device => "Устройство",
                KbNodeType.Controller => "Контроллер",
                KbNodeType.Module => "Модуль",
                KbNodeType.DocumentNode => "Документ/папка",
                KbNodeType.Department => "Подразделение",
                KbNodeType.WorkshopRoot => "Цех",
                _ => "Не задано"
            };
    }
}
