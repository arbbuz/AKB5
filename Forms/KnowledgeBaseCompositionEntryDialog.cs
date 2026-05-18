using AsutpKnowledgeBase.Models;
using AsutpKnowledgeBase.Services;

namespace AsutpKnowledgeBase
{
    public sealed class KnowledgeBaseCompositionEntryDialog : Form
    {
        private readonly string _entryId;
        private readonly IReadOnlyList<KbEquipmentCatalogItem> _catalogItems;

        private ComboBox _cmbEntryKind = null!;
        private Label _lblRackNumber = null!;
        private NumericUpDown _numRackNumber = null!;
        private Label _lblSlotNumber = null!;
        private NumericUpDown _numSlotNumber = null!;
        private Label _lblSlotAdvisory = null!;
        private NumericUpDown _numPositionOrder = null!;
        private TextBox _txtComponentType = null!;
        private TextBox _txtModel = null!;
        private TextBox _txtOrderNumber = null!;
        private TextBox _txtFirmware = null!;
        private TextBox _txtMpiDpPnAddress = null!;
        private TextBox _txtInputAddress = null!;
        private TextBox _txtOutputAddress = null!;
        private TextBox _txtIpAddress = null!;
        private readonly DateTime? _existingLastCalibrationAt;
        private readonly DateTime? _existingNextCalibrationAt;
        private readonly string _existingComment;
        private readonly string _existingInterfaceRows;
        private readonly string _existingNotes;

        public KnowledgeBaseCompositionEntryDialog(
            string title,
            KbCompositionEntry? existingEntry = null,
            IReadOnlyList<KbEquipmentCatalogItem>? catalogItems = null)
        {
            _entryId = existingEntry?.EntryId?.Trim() ?? string.Empty;
            _catalogItems = catalogItems ?? Array.Empty<KbEquipmentCatalogItem>();
            _existingLastCalibrationAt = existingEntry?.LastCalibrationAt;
            _existingNextCalibrationAt = existingEntry?.NextCalibrationAt;
            _existingComment = existingEntry?.Comment ?? string.Empty;
            _existingInterfaceRows = existingEntry?.InterfaceRows ?? string.Empty;
            _existingNotes = existingEntry?.Notes ?? string.Empty;

            Text = title;
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MinimizeBox = false;
            MaximizeBox = false;
            ShowInTaskbar = false;
            ClientSize = new Size(700, 500);
            AppIconProvider.Apply(this);

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(12),
                ColumnCount = 2,
                RowCount = 14
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 190F));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            for (int rowIndex = 0; rowIndex < 14; rowIndex++)
                layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            _cmbEntryKind = new ComboBox
            {
                Dock = DockStyle.Fill,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            _cmbEntryKind.Items.AddRange(["Слот", "Оборудование"]);
            _cmbEntryKind.SelectedIndexChanged += (_, _) => UpdateEntryKindState();
            layout.Controls.Add(CreateLabel("Тип позиции"), 0, 0);
            layout.Controls.Add(_cmbEntryKind, 1, 0);

            var btnSelectFromCatalog = new Button
            {
                Text = "Выбрать из каталога...",
                AutoSize = true,
                Enabled = _catalogItems.Count > 0,
                Margin = new Padding(0, 0, 0, 8)
            };
            btnSelectFromCatalog.Click += (_, _) => SelectFromCatalog();
            layout.Controls.Add(new Label(), 0, 1);
            layout.Controls.Add(btnSelectFromCatalog, 1, 1);

            _lblRackNumber = CreateLabel("Rack");
            _numRackNumber = CreateNumericInput(
                minimum: 0,
                maximum: 32,
                value: existingEntry?.RackNumber ?? 0);
            _numRackNumber.ValueChanged += (_, _) => UpdateSlotAdvisory();
            layout.Controls.Add(_lblRackNumber, 0, 2);
            layout.Controls.Add(_numRackNumber, 1, 2);

            _lblSlotNumber = CreateLabel("Номер слота");
            _numSlotNumber = CreateNumericInput(
                minimum: 1,
                maximum: 512,
                value: existingEntry?.SlotNumber ?? 1);
            _numSlotNumber.ValueChanged += (_, _) => UpdateSlotAdvisory();
            layout.Controls.Add(_lblSlotNumber, 0, 3);
            layout.Controls.Add(_numSlotNumber, 1, 3);

            _lblSlotAdvisory = new Label
            {
                Dock = DockStyle.Fill,
                AutoSize = true,
                ForeColor = Color.DimGray,
                Margin = new Padding(0, 0, 0, 8)
            };
            layout.Controls.Add(CreateLabel("Проверка"), 0, 4);
            layout.Controls.Add(_lblSlotAdvisory, 1, 4);

            _numPositionOrder = CreateNumericInput(
                minimum: 1,
                maximum: 512,
                value: (existingEntry?.PositionOrder ?? 0) + 1);
            layout.Controls.Add(CreateLabel("Порядок"), 0, 5);
            layout.Controls.Add(_numPositionOrder, 1, 5);

            _txtComponentType = new TextBox
            {
                Dock = DockStyle.Fill,
                Text = existingEntry?.ComponentType ?? string.Empty
            };
            _txtComponentType.TextChanged += (_, _) => UpdateSlotAdvisory();
            layout.Controls.Add(CreateLabel("Тип компонента"), 0, 6);
            layout.Controls.Add(_txtComponentType, 1, 6);

            _txtModel = new TextBox
            {
                Dock = DockStyle.Fill,
                Text = existingEntry?.Model ?? string.Empty
            };
            _txtModel.TextChanged += (_, _) => UpdateSlotAdvisory();
            layout.Controls.Add(CreateLabel("Модель"), 0, 7);
            layout.Controls.Add(_txtModel, 1, 7);

            _txtOrderNumber = new TextBox
            {
                Dock = DockStyle.Fill,
                Text = existingEntry?.OrderNumber ?? string.Empty
            };
            layout.Controls.Add(CreateLabel("Заказной номер"), 0, 8);
            layout.Controls.Add(_txtOrderNumber, 1, 8);

            _txtFirmware = new TextBox
            {
                Dock = DockStyle.Fill,
                Text = existingEntry?.Firmware ?? string.Empty
            };
            layout.Controls.Add(CreateLabel("Firmware"), 0, 9);
            layout.Controls.Add(_txtFirmware, 1, 9);

            _txtMpiDpPnAddress = new TextBox
            {
                Dock = DockStyle.Fill,
                Text = existingEntry?.MpiDpPnAddress ?? string.Empty
            };
            layout.Controls.Add(CreateLabel("MPI/DP/PN"), 0, 10);
            layout.Controls.Add(_txtMpiDpPnAddress, 1, 10);

            _txtInputAddress = new TextBox
            {
                Dock = DockStyle.Fill,
                Text = existingEntry?.InputAddress ?? string.Empty
            };
            layout.Controls.Add(CreateLabel("I address"), 0, 11);
            layout.Controls.Add(_txtInputAddress, 1, 11);

            _txtOutputAddress = new TextBox
            {
                Dock = DockStyle.Fill,
                Text = existingEntry?.OutputAddress ?? string.Empty
            };
            layout.Controls.Add(CreateLabel("Q address"), 0, 12);
            layout.Controls.Add(_txtOutputAddress, 1, 12);

            _txtIpAddress = new TextBox
            {
                Dock = DockStyle.Fill,
                Text = existingEntry?.IpAddress ?? string.Empty
            };
            layout.Controls.Add(CreateLabel("IP-адрес"), 0, 13);
            layout.Controls.Add(_txtIpAddress, 1, 13);

            var buttonsPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                FlowDirection = FlowDirection.RightToLeft,
                WrapContents = false,
                AutoSize = true,
                Padding = new Padding(12, 0, 12, 12)
            };

            var btnCancel = new Button
            {
                Text = "Отмена",
                DialogResult = DialogResult.Cancel,
                AutoSize = true
            };
            var btnOk = new Button
            {
                Text = "Сохранить",
                AutoSize = true
            };
            btnOk.Click += BtnOk_Click;

            buttonsPanel.Controls.Add(btnCancel);
            buttonsPanel.Controls.Add(btnOk);

            Controls.Add(layout);
            Controls.Add(buttonsPanel);

            AcceptButton = btnOk;
            CancelButton = btnCancel;

            bool isSlotted = existingEntry?.SlotNumber.HasValue != false;
            _cmbEntryKind.SelectedIndex = isSlotted ? 0 : 1;
            UpdateEntryKindState();
        }

        public KbCompositionEntry Result { get; private set; } = new();

        private bool IsSlotted => _cmbEntryKind.SelectedIndex == 0;

        private void SelectFromCatalog()
        {
            using var dialog = new KnowledgeBaseEquipmentCatalogSelectionDialog(_catalogItems);
            if (dialog.ShowDialog(this) != DialogResult.OK || dialog.SelectedItem == null)
                return;

            KbEquipmentCatalogItem item = dialog.SelectedItem;
            _txtComponentType.Text = item.EquipmentKind;
            _txtModel.Text = FormatCatalogModel(item);
            _txtOrderNumber.Text = item.Model?.Trim() ?? string.Empty;
        }

        private void BtnOk_Click(object? sender, EventArgs e)
        {
            string componentType = _txtComponentType.Text.Trim();
            string model = _txtModel.Text.Trim();
            if (string.IsNullOrWhiteSpace(componentType) && string.IsNullOrWhiteSpace(model))
            {
                MessageBox.Show(
                    this,
                    "Укажите тип компонента или модель.",
                    "Состав",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            Result = new KbCompositionEntry
            {
                EntryId = _entryId,
                RackNumber = IsSlotted ? (int)_numRackNumber.Value : 0,
                SlotNumber = IsSlotted ? (int)_numSlotNumber.Value : null,
                PositionOrder = (int)_numPositionOrder.Value - 1,
                ComponentType = componentType,
                Model = model,
                OrderNumber = _txtOrderNumber.Text.Trim(),
                Firmware = _txtFirmware.Text.Trim(),
                MpiDpPnAddress = _txtMpiDpPnAddress.Text.Trim(),
                InputAddress = _txtInputAddress.Text.Trim(),
                OutputAddress = _txtOutputAddress.Text.Trim(),
                Comment = _existingComment,
                InterfaceRows = _existingInterfaceRows,
                IpAddress = _txtIpAddress.Text.Trim(),
                LastCalibrationAt = _existingLastCalibrationAt,
                NextCalibrationAt = _existingNextCalibrationAt,
                Notes = _existingNotes
            };

            DialogResult = DialogResult.OK;
            Close();
        }

        private void UpdateEntryKindState()
        {
            _lblRackNumber.Enabled = IsSlotted;
            _numRackNumber.Enabled = IsSlotted;
            _lblSlotNumber.Enabled = IsSlotted;
            _numSlotNumber.Enabled = IsSlotted;
            _lblSlotAdvisory.Visible = IsSlotted;
            UpdateSlotAdvisory();
        }

        private void UpdateSlotAdvisory()
        {
            if (_lblSlotAdvisory == null)
                return;

            if (!IsSlotted)
            {
                _lblSlotAdvisory.Text = string.Empty;
                return;
            }

            int rackNumber = (int)_numRackNumber.Value;
            int slotNumber = (int)_numSlotNumber.Value;
            string slotRole = KnowledgeBaseCompositionRackSlotRulesService.GetSlotRoleText(rackNumber, slotNumber);
            var advisory = KnowledgeBaseCompositionRackSlotRulesService.GetSlotAdvisory(
                rackNumber,
                slotNumber,
                _txtComponentType?.Text,
                _txtModel?.Text);

            if (advisory.Severity == KnowledgeBaseCompositionSlotAdvisorySeverity.Warning)
            {
                _lblSlotAdvisory.ForeColor = Color.DarkOrange;
                _lblSlotAdvisory.Text = $"{slotRole}: {advisory.Text}";
                return;
            }

            if (advisory.Severity == KnowledgeBaseCompositionSlotAdvisorySeverity.Hint)
            {
                _lblSlotAdvisory.ForeColor = Color.SteelBlue;
                _lblSlotAdvisory.Text = $"{slotRole}: {advisory.Text}";
                return;
            }

            _lblSlotAdvisory.ForeColor = Color.DimGray;
            _lblSlotAdvisory.Text = $"Роль слота: {slotRole}";
        }

        private static string FormatCatalogModel(KbEquipmentCatalogItem item)
        {
            string manufacturer = item.Manufacturer?.Trim() ?? string.Empty;
            string orderNumber = item.Model?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(manufacturer))
                return orderNumber;

            if (string.IsNullOrWhiteSpace(orderNumber))
                return manufacturer;

            return $"{manufacturer} {orderNumber}";
        }

        private static Label CreateLabel(string text) =>
            new()
            {
                Text = text,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                Margin = new Padding(0, 0, 8, 8)
            };

        private static NumericUpDown CreateNumericInput(decimal minimum, decimal maximum, decimal value) =>
            new()
            {
                Dock = DockStyle.Left,
                Width = 120,
                Minimum = minimum,
                Maximum = maximum,
                Value = Math.Min(maximum, Math.Max(minimum, value))
            };

    }
}
