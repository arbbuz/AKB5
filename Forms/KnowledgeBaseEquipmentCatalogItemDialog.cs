using AsutpKnowledgeBase.Models;
using AsutpKnowledgeBase.Services;

namespace AsutpKnowledgeBase
{
    public sealed class KnowledgeBaseEquipmentCatalogItemDialog : Form
    {
        private readonly string _catalogItemId;
        private readonly string _series;
        private readonly KbNodeType _defaultNodeType;
        private readonly List<KbEquipmentCatalogProperty> _properties;
        private TextBox _txtEquipmentKind = null!;
        private TextBox _txtManufacturer = null!;
        private TextBox _txtModel = null!;
        private TextBox _txtDescription = null!;

        public KnowledgeBaseEquipmentCatalogItemDialog(
            string title,
            KbEquipmentCatalogItem? existingItem = null)
        {
            _catalogItemId = existingItem?.CatalogItemId?.Trim() ?? string.Empty;
            _series = existingItem?.Series?.Trim() ?? string.Empty;
            _defaultNodeType = Enum.IsDefined(typeof(KbNodeType), existingItem?.DefaultNodeType ?? KbNodeType.Device)
                ? existingItem?.DefaultNodeType ?? KbNodeType.Device
                : KbNodeType.Device;
            _properties = (existingItem?.Properties ?? new List<KbEquipmentCatalogProperty>())
                .Select(static property => new KbEquipmentCatalogProperty
                {
                    Name = property.Name?.Trim() ?? string.Empty,
                    Value = property.Value?.Trim() ?? string.Empty
                })
                .ToList();

            Text = title;
            StartPosition = FormStartPosition.CenterParent;
            MinimizeBox = false;
            ShowInTaskbar = false;
            Size = new Size(760, 420);
            MinimumSize = new Size(680, 360);
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
                RowCount = 5
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 160F));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            for (int rowIndex = 0; rowIndex < 3; rowIndex++)
                layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            _txtEquipmentKind = CreateTextBox(existingItem?.EquipmentKind);
            AddFieldRow(layout, 0, "Наименование", _txtEquipmentKind);

            _txtManufacturer = CreateTextBox(existingItem?.Manufacturer);
            AddFieldRow(layout, 1, "Производитель", _txtManufacturer);

            _txtModel = CreateTextBox(existingItem?.Model);
            AddFieldRow(layout, 2, "Заказной №", _txtModel);

            _txtDescription = new TextBox
            {
                Dock = DockStyle.Fill,
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                Text = existingItem?.Description ?? string.Empty
            };
            AddFieldRow(layout, 3, "Примечание", _txtDescription);

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
            layout.Controls.Add(buttonsPanel, 0, 4);
            layout.SetColumnSpan(buttonsPanel, 2);

            return layout;
        }

        private void Submit()
        {
            string equipmentKind = _txtEquipmentKind.Text.Trim();
            string manufacturer = _txtManufacturer.Text.Trim();
            string model = _txtModel.Text.Trim();
            if (string.IsNullOrWhiteSpace(equipmentKind) &&
                string.IsNullOrWhiteSpace(manufacturer) &&
                string.IsNullOrWhiteSpace(model))
            {
                MessageBox.Show(
                    this,
                    "Укажите наименование, производителя или заказной номер.",
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
                Series = _series,
                Model = model,
                DefaultNodeType = _defaultNodeType,
                Description = _txtDescription.Text.Trim(),
                Properties = _properties
            };

            DialogResult = DialogResult.OK;
            Close();
        }

        private static TextBox CreateTextBox(string? text) =>
            new()
            {
                Dock = DockStyle.Fill,
                Text = text ?? string.Empty
            };

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

    }
}
