using AsutpKnowledgeBase.Models;

namespace AsutpKnowledgeBase
{
    public sealed class KnowledgeBaseCompositionEntryDialog : Form
    {
        private readonly string _entryId;

        private ComboBox _cmbEntryKind = null!;
        private Label _lblSlotNumber = null!;
        private NumericUpDown _numSlotNumber = null!;
        private NumericUpDown _numPositionOrder = null!;
        private TextBox _txtComponentType = null!;
        private TextBox _txtModel = null!;
        private TextBox _txtIpAddress = null!;
        private DateTimePicker _dtpLastCalibration = null!;
        private DateTimePicker _dtpNextCalibration = null!;
        private TextBox _txtNotes = null!;

        public KnowledgeBaseCompositionEntryDialog(string title, KbCompositionEntry? existingEntry = null)
        {
            _entryId = existingEntry?.EntryId?.Trim() ?? string.Empty;

            Text = title;
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MinimizeBox = false;
            MaximizeBox = false;
            ShowInTaskbar = false;
            ClientSize = new Size(620, 430);
            AppIconProvider.Apply(this);

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(12),
                ColumnCount = 2,
                RowCount = 9
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 170F));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            for (int rowIndex = 0; rowIndex < 8; rowIndex++)
                layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            _cmbEntryKind = new ComboBox
            {
                Dock = DockStyle.Fill,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            _cmbEntryKind.Items.AddRange(["Слот", "Оборудование"]);
            _cmbEntryKind.SelectedIndexChanged += (_, _) => UpdateEntryKindState();
            layout.Controls.Add(CreateLabel("Тип позиции"), 0, 0);
            layout.Controls.Add(_cmbEntryKind, 1, 0);

            _lblSlotNumber = CreateLabel("Номер слота");
            _numSlotNumber = CreateNumericInput(
                minimum: 1,
                maximum: 512,
                value: existingEntry?.SlotNumber ?? 1);
            layout.Controls.Add(_lblSlotNumber, 0, 1);
            layout.Controls.Add(_numSlotNumber, 1, 1);

            _numPositionOrder = CreateNumericInput(
                minimum: 1,
                maximum: 512,
                value: (existingEntry?.PositionOrder ?? 0) + 1);
            layout.Controls.Add(CreateLabel("Порядок"), 0, 2);
            layout.Controls.Add(_numPositionOrder, 1, 2);

            _txtComponentType = new TextBox
            {
                Dock = DockStyle.Fill,
                Text = existingEntry?.ComponentType ?? string.Empty
            };
            layout.Controls.Add(CreateLabel("Тип компонента"), 0, 3);
            layout.Controls.Add(_txtComponentType, 1, 3);

            _txtModel = new TextBox
            {
                Dock = DockStyle.Fill,
                Text = existingEntry?.Model ?? string.Empty
            };
            layout.Controls.Add(CreateLabel("Модель"), 0, 4);
            layout.Controls.Add(_txtModel, 1, 4);

            _txtIpAddress = new TextBox
            {
                Dock = DockStyle.Fill,
                Text = existingEntry?.IpAddress ?? string.Empty
            };
            layout.Controls.Add(CreateLabel("IP-адрес"), 0, 5);
            layout.Controls.Add(_txtIpAddress, 1, 5);

            _dtpLastCalibration = CreateDatePicker(existingEntry?.LastCalibrationAt);
            layout.Controls.Add(CreateLabel("Последняя калибровка"), 0, 6);
            layout.Controls.Add(_dtpLastCalibration, 1, 6);

            _dtpNextCalibration = CreateDatePicker(existingEntry?.NextCalibrationAt);
            layout.Controls.Add(CreateLabel("Следующая калибровка"), 0, 7);
            layout.Controls.Add(_dtpNextCalibration, 1, 7);

            _txtNotes = new TextBox
            {
                Dock = DockStyle.Fill,
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                Height = 120,
                Text = existingEntry?.Notes ?? string.Empty
            };
            layout.Controls.Add(CreateLabel("Примечание"), 0, 8);
            layout.Controls.Add(_txtNotes, 1, 8);

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
                SlotNumber = IsSlotted ? (int)_numSlotNumber.Value : null,
                PositionOrder = (int)_numPositionOrder.Value - 1,
                ComponentType = componentType,
                Model = model,
                IpAddress = _txtIpAddress.Text.Trim(),
                LastCalibrationAt = _dtpLastCalibration.Checked ? _dtpLastCalibration.Value.Date : null,
                NextCalibrationAt = _dtpNextCalibration.Checked ? _dtpNextCalibration.Value.Date : null,
                Notes = _txtNotes.Text.Trim()
            };

            DialogResult = DialogResult.OK;
            Close();
        }

        private void UpdateEntryKindState()
        {
            _lblSlotNumber.Enabled = IsSlotted;
            _numSlotNumber.Enabled = IsSlotted;
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

        private static DateTimePicker CreateDatePicker(DateTime? value)
        {
            var picker = new DateTimePicker
            {
                Dock = DockStyle.Left,
                Width = 160,
                Format = DateTimePickerFormat.Custom,
                CustomFormat = "yyyy-MM-dd",
                ShowCheckBox = true
            };

            if (value.HasValue)
            {
                picker.Value = value.Value.Date;
                picker.Checked = true;
            }
            else
            {
                picker.Value = DateTime.Today;
                picker.Checked = false;
            }

            return picker;
        }
    }
}
