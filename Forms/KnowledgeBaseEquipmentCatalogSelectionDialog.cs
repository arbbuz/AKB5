using AsutpKnowledgeBase.Models;
using AsutpKnowledgeBase.Services;

namespace AsutpKnowledgeBase
{
    public sealed class KnowledgeBaseEquipmentCatalogSelectionDialog : Form
    {
        private const string EquipmentKindColumnName = "EquipmentKind";
        private const string ManufacturerColumnName = "Manufacturer";
        private const string ModelColumnName = "Model";
        private const string DescriptionColumnName = "Description";

        private readonly KnowledgeBaseEquipmentCatalogService _catalogService = new();
        private readonly KnowledgeBaseWindowLayoutStateService _layoutStateService = new();
        private readonly List<KbEquipmentCatalogItem> _items;
        private TextBox _txtSearch = null!;
        private DataGridView _grid = null!;
        private Label _lblSummary = null!;
        private string _sortColumnName = EquipmentKindColumnName;
        private bool _sortAscending = true;

        public KnowledgeBaseEquipmentCatalogSelectionDialog(IReadOnlyList<KbEquipmentCatalogItem>? catalogItems)
        {
            _items = KnowledgeBaseDataService.NormalizeEquipmentCatalogItems(catalogItems)
                .Select(KnowledgeBaseEquipmentCatalogService.CloneCatalogItem)
                .ToList();

            Text = "Выбор оборудования из каталога";
            StartPosition = FormStartPosition.CenterParent;
            MinimizeBox = false;
            ShowInTaskbar = false;
            Size = new Size(980, 640);
            MinimumSize = new Size(760, 460);
            AppIconProvider.Apply(this);

            Controls.Add(CreateLayout());
            RestoreSavedLayout();
            RefreshGrid();
            FormClosing += (_, _) => SaveCurrentLayout();

            AcceptButton = Controls
                .Find("btnOk", searchAllChildren: true)
                .OfType<Button>()
                .FirstOrDefault();
            CancelButton = Controls
                .Find("btnCancel", searchAllChildren: true)
                .OfType<Button>()
                .FirstOrDefault();
        }

        public KbEquipmentCatalogItem? SelectedItem { get; private set; }

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
                ColumnCount = 3,
                Margin = new Padding(0, 0, 0, 8)
            };
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
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

            var btnClear = new Button
            {
                Text = "Очистить",
                AutoSize = true
            };
            btnClear.Click += (_, _) =>
            {
                _txtSearch.Clear();
                _txtSearch.Focus();
            };
            panel.Controls.Add(btnClear, 2, 0);

            return panel;
        }

        private BufferedDataGridView CreateGrid()
        {
            var grid = new BufferedDataGridView
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
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                ShowCellToolTips = false
            };

            grid.Columns.Add(CreateTextColumn("CatalogItemId", "Id", 80, visible: false, sortable: false));
            grid.Columns.Add(CreateTextColumn(EquipmentKindColumnName, "Наименование", 360));
            grid.Columns.Add(CreateTextColumn(ManufacturerColumnName, "Производитель", 140));
            grid.Columns.Add(CreateTextColumn(ModelColumnName, "Заказной №", 180));
            grid.Columns.Add(CreateTextColumn(DescriptionColumnName, "Примечание", 240));
            grid.ColumnHeaderMouseClick += (_, e) => SortByColumn(e.ColumnIndex);
            grid.CellDoubleClick += (_, e) =>
            {
                if (e.RowIndex >= 0)
                    Submit();
            };
            grid.DataError += (_, e) => e.ThrowException = false;
            return grid;
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
                Text = "Выбрать",
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

        private void RefreshGrid()
        {
            string? selectedId = GetSelectedItem()?.CatalogItemId;
            List<KbEquipmentCatalogItem> visibleItems = SortItems(
                _catalogService.Search(_items, _txtSearch?.Text));

            if (_grid != null)
            {
                using var redrawScope = ControlRedrawScope.Suspend(_grid);
                _grid.SuspendLayout();
                try
                {
                    _grid.Rows.Clear();
                    var rows = new List<DataGridViewRow>(visibleItems.Count);
                    DataGridViewRow? preferredRow = null;

                    foreach (KbEquipmentCatalogItem item in visibleItems)
                    {
                        var row = new DataGridViewRow();
                        row.CreateCells(
                            _grid,
                            item.CatalogItemId,
                            item.EquipmentKind,
                            item.Manufacturer,
                            item.Model,
                            item.Description);
                        row.Tag = item;
                        rows.Add(row);

                        if (!string.IsNullOrWhiteSpace(selectedId) &&
                            string.Equals(item.CatalogItemId, selectedId, StringComparison.Ordinal))
                        {
                            preferredRow = row;
                        }
                    }

                    if (rows.Count > 0)
                        _grid.Rows.AddRange(rows.ToArray());

                    _grid.ClearSelection();
                    _grid.CurrentCell = null;

                    preferredRow ??= rows.Count > 0 ? rows[0] : null;
                    if (preferredRow != null)
                    {
                        preferredRow.Selected = true;
                        _grid.CurrentCell = preferredRow.Cells[EquipmentKindColumnName];
                    }
                }
                finally
                {
                    _grid.ResumeLayout();
                }

                UpdateSortGlyph();
            }

            if (_lblSummary != null)
            {
                _lblSummary.Text = string.IsNullOrWhiteSpace(_txtSearch?.Text)
                    ? $"Записей каталога: {_items.Count}"
                    : $"Найдено: {visibleItems.Count} из {_items.Count}";
            }
        }

        private void Submit()
        {
            KbEquipmentCatalogItem? selectedItem = GetSelectedItem();
            if (selectedItem == null)
            {
                MessageBox.Show(
                    this,
                    "Выберите оборудование из каталога.",
                    "Каталог оборудования",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            SelectedItem = KnowledgeBaseEquipmentCatalogService.CloneCatalogItem(selectedItem);
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

        private static DataGridViewTextBoxColumn CreateTextColumn(
            string name,
            string headerText,
            int width,
            bool visible = true,
            bool sortable = true) =>
            new()
            {
                Name = name,
                HeaderText = headerText,
                Width = width,
                Visible = visible,
                SortMode = sortable
                    ? DataGridViewColumnSortMode.Programmatic
                    : DataGridViewColumnSortMode.NotSortable
            };

        private void RestoreSavedLayout()
        {
            RestoreSavedColumnWidths();

            KnowledgeBaseWindowPlacement? placement =
                _layoutStateService.LoadEquipmentCatalogSelectionWindowPlacement();
            if (placement == null)
            {
                WindowState = FormWindowState.Maximized;
                return;
            }

            Rectangle workingArea = Screen.FromPoint(new Point(placement.Left, placement.Top)).WorkingArea;
            Rectangle fittedBounds = KnowledgeBaseWindowLayoutStateService.FitWindowBounds(
                new Rectangle(placement.Left, placement.Top, placement.Width, placement.Height),
                workingArea,
                MinimumSize);

            StartPosition = FormStartPosition.Manual;
            WindowState = FormWindowState.Normal;
            Bounds = fittedBounds;
            if (placement.IsMaximized)
                WindowState = FormWindowState.Maximized;
        }

        private void RestoreSavedColumnWidths()
        {
            Dictionary<string, int> columnWidths =
                _layoutStateService.LoadEquipmentCatalogSelectionColumnWidths();
            if (columnWidths.Count == 0)
                return;

            foreach (DataGridViewColumn column in _grid.Columns)
            {
                if (!column.Visible)
                    continue;

                if (columnWidths.TryGetValue(column.Name, out int width) && width > 0)
                    column.Width = width;
            }
        }

        private void SaveCurrentLayout()
        {
            Rectangle bounds = WindowState == FormWindowState.Normal
                ? Bounds
                : RestoreBounds;
            if (bounds.Width <= 0 || bounds.Height <= 0)
                bounds = Bounds;

            _layoutStateService.SaveEquipmentCatalogSelectionLayout(
                new KnowledgeBaseWindowPlacement
                {
                    Left = bounds.Left,
                    Top = bounds.Top,
                    Width = bounds.Width,
                    Height = bounds.Height,
                    IsMaximized = WindowState == FormWindowState.Maximized
                },
                GetCurrentColumnWidths());
        }

        private Dictionary<string, int> GetCurrentColumnWidths()
        {
            var widths = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (DataGridViewColumn column in _grid.Columns)
            {
                if (column.Visible)
                    widths[column.Name] = column.Width;
            }

            return widths;
        }

        private void SortByColumn(int columnIndex)
        {
            if (columnIndex < 0 || columnIndex >= _grid.Columns.Count)
                return;

            string columnName = _grid.Columns[columnIndex].Name;
            if (!IsSortableColumn(columnName))
                return;

            if (string.Equals(_sortColumnName, columnName, StringComparison.Ordinal))
            {
                _sortAscending = !_sortAscending;
            }
            else
            {
                _sortColumnName = columnName;
                _sortAscending = true;
            }

            RefreshGrid();
        }

        private List<KbEquipmentCatalogItem> SortItems(List<KbEquipmentCatalogItem> items)
        {
            Func<KbEquipmentCatalogItem, string> selector = _sortColumnName switch
            {
                ManufacturerColumnName => static item => item.Manufacturer ?? string.Empty,
                ModelColumnName => static item => item.Model ?? string.Empty,
                DescriptionColumnName => static item => item.Description ?? string.Empty,
                _ => static item => item.EquipmentKind ?? string.Empty
            };

            IOrderedEnumerable<KbEquipmentCatalogItem> orderedItems = _sortAscending
                ? items.OrderBy(selector, KnowledgeBaseNaturalStringComparer.Instance)
                : items.OrderByDescending(selector, KnowledgeBaseNaturalStringComparer.Instance);

            return orderedItems
                .ThenBy(static item => item.EquipmentKind, KnowledgeBaseNaturalStringComparer.Instance)
                .ThenBy(static item => item.Manufacturer, KnowledgeBaseNaturalStringComparer.Instance)
                .ThenBy(static item => item.Model, KnowledgeBaseNaturalStringComparer.Instance)
                .ThenBy(static item => item.Description, KnowledgeBaseNaturalStringComparer.Instance)
                .ToList();
        }

        private void UpdateSortGlyph()
        {
            foreach (DataGridViewColumn column in _grid.Columns)
                column.HeaderCell.SortGlyphDirection = SortOrder.None;

            if (_grid.Columns.Contains(_sortColumnName))
            {
                _grid.Columns[_sortColumnName].HeaderCell.SortGlyphDirection = _sortAscending
                    ? SortOrder.Ascending
                    : SortOrder.Descending;
            }
        }

        private static bool IsSortableColumn(string columnName) =>
            string.Equals(columnName, EquipmentKindColumnName, StringComparison.Ordinal) ||
            string.Equals(columnName, ManufacturerColumnName, StringComparison.Ordinal) ||
            string.Equals(columnName, ModelColumnName, StringComparison.Ordinal) ||
            string.Equals(columnName, DescriptionColumnName, StringComparison.Ordinal);
    }
}
