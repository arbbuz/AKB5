using System.Globalization;
using AsutpKnowledgeBase.Models;
using AsutpKnowledgeBase.Services;

namespace AsutpKnowledgeBase
{
    public sealed class KnowledgeBaseEquipmentCatalogItemDialog : Form
    {
        private readonly string _catalogItemId;
        private TextBox _txtEquipmentKind = null!;
        private TextBox _txtManufacturer = null!;
        private TextBox _txtSeries = null!;
        private TextBox _txtModel = null!;
        private ComboBox _cmbDefaultNodeType = null!;
        private TextBox _txtDescription = null!;
        private DataGridView _gridProperties = null!;

        public KnowledgeBaseEquipmentCatalogItemDialog(
            string title,
            KbEquipmentCatalogItem? existingItem = null)
        {
            _catalogItemId = existingItem?.CatalogItemId?.Trim() ?? string.Empty;

            Text = title;
            StartPosition = FormStartPosition.CenterParent;
            MinimizeBox = false;
            ShowInTaskbar = false;
            Size = new Size(760, 620);
            MinimumSize = new Size(680, 540);
            AppIconProvider.Apply(this);

            Controls.Add(CreateLayout(existingItem));

            AcceptButton = Controls
                .Find("btnOk", searchAllChildren: true)
                .OfType<Button>()
                .FirstOrDefault();
            CancelButton = Controls
                .Find("btnCancel", searchAllChildren: true)
                .OfType<Button>()
                .FirstOrDefault();
        }

        public KbEquipmentCatalogItem Result { get; private set; } = new();

        private TableLayoutPanel CreateLayout(KbEquipmentCatalogItem? existingItem)
        {
            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(12),
                ColumnCount = 2,
                RowCount = 8
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 160F));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            for (int rowIndex = 0; rowIndex < 6; rowIndex++)
                layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            _txtEquipmentKind = CreateTextBox(existingItem?.EquipmentKind);
            AddFieldRow(layout, 0, "Вид оборудования", _txtEquipmentKind);

            _txtManufacturer = CreateTextBox(existingItem?.Manufacturer);
            AddFieldRow(layout, 1, "Производитель", _txtManufacturer);

            _txtSeries = CreateTextBox(existingItem?.Series);
            AddFieldRow(layout, 2, "Серия", _txtSeries);

            _txtModel = CreateTextBox(existingItem?.Model);
            AddFieldRow(layout, 3, "Модель", _txtModel);

            _cmbDefaultNodeType = CreateNodeTypeComboBox(existingItem?.DefaultNodeType ?? KbNodeType.Device);
            AddFieldRow(layout, 4, "Тип узла", _cmbDefaultNodeType);

            _txtDescription = new TextBox
            {
                Dock = DockStyle.Fill,
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                Height = 72,
                Text = existingItem?.Description ?? string.Empty
            };
            AddFieldRow(layout, 5, "Описание", _txtDescription);

            var propertiesGroup = new GroupBox
            {
                Text = "Свойства",
                Dock = DockStyle.Fill,
                Padding = new Padding(10),
                Margin = new Padding(0, 6, 0, 0)
            };
            _gridProperties = CreatePropertiesGrid();
            BindProperties(existingItem?.Properties);
            propertiesGroup.Controls.Add(_gridProperties);
            layout.Controls.Add(propertiesGroup, 0, 6);
            layout.SetColumnSpan(propertiesGroup, 2);

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
            layout.Controls.Add(buttonsPanel, 0, 7);
            layout.SetColumnSpan(buttonsPanel, 2);

            return layout;
        }

        private void Submit()
        {
            _gridProperties.EndEdit();

            string equipmentKind = _txtEquipmentKind.Text.Trim();
            string manufacturer = _txtManufacturer.Text.Trim();
            string series = _txtSeries.Text.Trim();
            string model = _txtModel.Text.Trim();
            if (string.IsNullOrWhiteSpace(equipmentKind) &&
                string.IsNullOrWhiteSpace(manufacturer) &&
                string.IsNullOrWhiteSpace(series) &&
                string.IsNullOrWhiteSpace(model))
            {
                MessageBox.Show(
                    this,
                    "Укажите вид оборудования, производителя, серию или модель.",
                    "Каталог оборудования",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            Result = new KbEquipmentCatalogItem
            {
                CatalogItemId = _catalogItemId,
                EquipmentKind = equipmentKind,
                Manufacturer = manufacturer,
                Series = series,
                Model = model,
                DefaultNodeType = (_cmbDefaultNodeType.SelectedItem as NodeTypeOption)?.NodeType ?? KbNodeType.Device,
                Description = _txtDescription.Text.Trim(),
                Properties = ReadProperties()
            };

            DialogResult = DialogResult.OK;
            Close();
        }

        private List<KbEquipmentCatalogProperty> ReadProperties()
        {
            var properties = new List<KbEquipmentCatalogProperty>();
            foreach (DataGridViewRow row in _gridProperties.Rows)
            {
                if (row.IsNewRow)
                    continue;

                string name = Convert.ToString(row.Cells["Name"].Value, CultureInfo.InvariantCulture)?.Trim() ?? string.Empty;
                string value = Convert.ToString(row.Cells["Value"].Value, CultureInfo.InvariantCulture)?.Trim() ?? string.Empty;
                if (string.IsNullOrWhiteSpace(name))
                    continue;

                properties.Add(new KbEquipmentCatalogProperty
                {
                    Name = name,
                    Value = value
                });
            }

            return properties;
        }

        private void BindProperties(IReadOnlyList<KbEquipmentCatalogProperty>? properties)
        {
            foreach (KbEquipmentCatalogProperty property in properties ?? Array.Empty<KbEquipmentCatalogProperty>())
            {
                int rowIndex = _gridProperties.Rows.Add();
                _gridProperties.Rows[rowIndex].Cells["Name"].Value = property.Name;
                _gridProperties.Rows[rowIndex].Cells["Value"].Value = property.Value;
            }
        }

        private static TextBox CreateTextBox(string? text) =>
            new()
            {
                Dock = DockStyle.Fill,
                Text = text ?? string.Empty
            };

        private static DataGridView CreatePropertiesGrid()
        {
            var grid = new DataGridView
            {
                Dock = DockStyle.Fill,
                AllowUserToAddRows = true,
                AllowUserToDeleteRows = true,
                AllowUserToResizeRows = false,
                AutoGenerateColumns = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                BackgroundColor = SystemColors.Window,
                BorderStyle = BorderStyle.Fixed3D,
                RowHeadersVisible = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect
            };
            grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Name",
                HeaderText = "Параметр",
                FillWeight = 42,
                SortMode = DataGridViewColumnSortMode.NotSortable
            });
            grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Value",
                HeaderText = "Значение",
                FillWeight = 58,
                SortMode = DataGridViewColumnSortMode.NotSortable
            });
            grid.DataError += (_, e) => e.ThrowException = false;
            return grid;
        }

        private static ComboBox CreateNodeTypeComboBox(KbNodeType selectedNodeType)
        {
            var comboBox = new ComboBox
            {
                Dock = DockStyle.Left,
                Width = 220,
                DropDownStyle = ComboBoxStyle.DropDownList
            };

            comboBox.Items.Add(new NodeTypeOption(KbNodeType.System, "Система"));
            comboBox.Items.Add(new NodeTypeOption(KbNodeType.Cabinet, "Шкаф"));
            comboBox.Items.Add(new NodeTypeOption(KbNodeType.Device, "Устройство"));
            comboBox.Items.Add(new NodeTypeOption(KbNodeType.Controller, "Контроллер"));
            comboBox.Items.Add(new NodeTypeOption(KbNodeType.Module, "Модуль"));
            comboBox.Items.Add(new NodeTypeOption(KbNodeType.DocumentNode, "Документ/папка"));

            NodeTypeOption selectedOption = comboBox.Items
                .OfType<NodeTypeOption>()
                .FirstOrDefault(option => option.NodeType == selectedNodeType) ??
                comboBox.Items.OfType<NodeTypeOption>().First(option => option.NodeType == KbNodeType.Device);
            comboBox.SelectedItem = selectedOption;
            return comboBox;
        }

        private static void AddFieldRow(
            TableLayoutPanel layout,
            int rowIndex,
            string labelText,
            Control editor)
        {
            var label = new Label
            {
                Text = labelText,
                AutoSize = true,
                Margin = new Padding(0, 6, 10, 8)
            };
            editor.Margin = new Padding(0, 0, 0, 8);
            layout.Controls.Add(label, 0, rowIndex);
            layout.Controls.Add(editor, 1, rowIndex);
        }

        private sealed class NodeTypeOption
        {
            public NodeTypeOption(KbNodeType nodeType, string text)
            {
                NodeType = nodeType;
                Text = text;
            }

            public KbNodeType NodeType { get; }

            private string Text { get; }

            public override string ToString() => Text;
        }
    }
}
