using AsutpKnowledgeBase.Models;

namespace AsutpKnowledgeBase
{
    public sealed class KnowledgeBaseNetworkInterfaceDialog : Form
    {
        private sealed class DeviceComboItem
        {
            public string NetworkDeviceId { get; init; } = string.Empty;

            public string Text { get; init; } = string.Empty;

            public override string ToString() => Text;
        }

        private readonly string _networkInterfaceId;

        private ComboBox _cmbDevice = null!;
        private TextBox _txtInterfaceName = null!;
        private TextBox _txtPortNumber = null!;
        private TextBox _txtMacAddress = null!;
        private TextBox _txtIpAddress = null!;
        private TextBox _txtSubnetMask = null!;
        private TextBox _txtGateway = null!;
        private TextBox _txtVlan = null!;
        private ComboBox _cmbProtocol = null!;
        private TextBox _txtMpiDpPnAddress = null!;
        private TextBox _txtSpeed = null!;
        private ComboBox _cmbMedium = null!;
        private TextBox _txtNotes = null!;

        public KnowledgeBaseNetworkInterfaceDialog(
            string title,
            IReadOnlyList<KbNetworkDevice> devices,
            KbNetworkInterface? existingInterface = null)
        {
            _networkInterfaceId = existingInterface?.NetworkInterfaceId?.Trim() ?? string.Empty;

            Text = title;
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MinimizeBox = false;
            MaximizeBox = false;
            ShowInTaskbar = false;
            ClientSize = new Size(740, 500);
            AppIconProvider.Apply(this);

            var rootLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2
            };
            rootLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            rootLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(12),
                ColumnCount = 2,
                RowCount = 13
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 180F));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            for (int rowIndex = 0; rowIndex < 13; rowIndex++)
                layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            _cmbDevice = new ComboBox
            {
                Dock = DockStyle.Fill,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Margin = new Padding(0, 0, 0, 8)
            };
            FillDeviceItems(devices, existingInterface?.NetworkDeviceId);
            layout.Controls.Add(CreateLabel("Устройство"), 0, 0);
            layout.Controls.Add(_cmbDevice, 1, 0);

            _txtInterfaceName = AddTextBox(layout, "Интерфейс", 1, existingInterface?.InterfaceName);
            _txtPortNumber = AddTextBox(layout, "Порт", 2, existingInterface?.PortNumber);
            _txtMacAddress = AddTextBox(layout, "MAC", 3, existingInterface?.MacAddress);
            _txtIpAddress = AddTextBox(layout, "IP-адрес", 4, existingInterface?.IpAddress);
            _txtSubnetMask = AddTextBox(layout, "Маска", 5, existingInterface?.SubnetMask);
            _txtGateway = AddTextBox(layout, "Шлюз", 6, existingInterface?.Gateway);
            _txtVlan = AddTextBox(layout, "VLAN", 7, existingInterface?.Vlan);
            _cmbProtocol = AddPresetComboBox(
                layout,
                "Протокол",
                8,
                existingInterface?.Protocol,
                ["PROFINET", "PROFIBUS", "MPI"]);
            _txtMpiDpPnAddress = AddTextBox(layout, "MPI/DP/PN", 9, existingInterface?.MpiDpPnAddress);
            _txtSpeed = AddTextBox(layout, "Скорость", 10, existingInterface?.Speed);
            _cmbMedium = AddPresetComboBox(
                layout,
                "Среда",
                11,
                existingInterface?.Medium,
                ["Медь", "Оптика"]);

            _txtNotes = new TextBox
            {
                Dock = DockStyle.Fill,
                Multiline = true,
                Height = 64,
                ScrollBars = ScrollBars.Vertical,
                AccessibleName = "Примечание",
                Text = existingInterface?.Notes ?? string.Empty,
                Margin = new Padding(0, 0, 0, 8)
            };
            layout.Controls.Add(CreateLabel("Примечание"), 0, 12);
            layout.Controls.Add(_txtNotes, 1, 12);

            rootLayout.Controls.Add(layout, 0, 0);
            rootLayout.Controls.Add(CreateButtonsPanel(), 0, 1);
            Controls.Add(rootLayout);
        }

        public KbNetworkInterface Result { get; private set; } = new();

        private void FillDeviceItems(IReadOnlyList<KbNetworkDevice> devices, string? selectedDeviceId)
        {
            foreach (var device in devices)
            {
                string deviceId = device.NetworkDeviceId?.Trim() ?? string.Empty;
                if (string.IsNullOrWhiteSpace(deviceId))
                    continue;

                _cmbDevice.Items.Add(new DeviceComboItem
                {
                    NetworkDeviceId = deviceId,
                    Text = string.IsNullOrWhiteSpace(device.Name)
                        ? deviceId
                        : device.Name.Trim()
                });
            }

            string normalizedSelectedId = selectedDeviceId?.Trim() ?? string.Empty;
            int selectedIndex = _cmbDevice.Items.Count > 0 ? 0 : -1;
            for (int index = 0; index < _cmbDevice.Items.Count; index++)
            {
                if (_cmbDevice.Items[index] is DeviceComboItem item &&
                    string.Equals(item.NetworkDeviceId, normalizedSelectedId, StringComparison.Ordinal))
                {
                    selectedIndex = index;
                    break;
                }
            }

            _cmbDevice.SelectedIndex = selectedIndex;
        }

        private TextBox AddTextBox(
            TableLayoutPanel layout,
            string label,
            int row,
            string? value)
        {
            var textBox = new TextBox
            {
                Dock = DockStyle.Fill,
                Text = value ?? string.Empty,
                AccessibleName = label,
                Margin = new Padding(0, 0, 0, 8)
            };
            layout.Controls.Add(CreateLabel(label), 0, row);
            layout.Controls.Add(textBox, 1, row);
            return textBox;
        }

        private ComboBox AddPresetComboBox(
            TableLayoutPanel layout,
            string label,
            int row,
            string? value,
            IReadOnlyList<string> presets)
        {
            var comboBox = new ComboBox
            {
                Dock = DockStyle.Fill,
                DropDownStyle = ComboBoxStyle.DropDown,
                AutoCompleteMode = AutoCompleteMode.SuggestAppend,
                AutoCompleteSource = AutoCompleteSource.ListItems,
                AccessibleName = label,
                Text = value ?? string.Empty,
                Margin = new Padding(0, 0, 0, 8)
            };
            comboBox.Items.AddRange(presets.Cast<object>().ToArray());
            layout.Controls.Add(CreateLabel(label), 0, row);
            layout.Controls.Add(comboBox, 1, row);
            return comboBox;
        }

        private Control CreateButtonsPanel()
        {
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

            AcceptButton = btnOk;
            CancelButton = btnCancel;
            return buttonsPanel;
        }

        private void BtnOk_Click(object? sender, EventArgs e)
        {
            if (_cmbDevice.SelectedItem is not DeviceComboItem deviceItem)
            {
                MessageBox.Show(
                    this,
                    "Выберите сетевое устройство.",
                    "Сеть",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            if (string.IsNullOrWhiteSpace(_txtInterfaceName.Text) &&
                string.IsNullOrWhiteSpace(_txtPortNumber.Text) &&
                string.IsNullOrWhiteSpace(_txtIpAddress.Text) &&
                string.IsNullOrWhiteSpace(_txtMacAddress.Text))
            {
                MessageBox.Show(
                    this,
                    "Укажите имя интерфейса, порт, IP-адрес или MAC-адрес.",
                    "Сеть",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            Result = new KbNetworkInterface
            {
                NetworkInterfaceId = _networkInterfaceId,
                NetworkDeviceId = deviceItem.NetworkDeviceId,
                InterfaceName = _txtInterfaceName.Text.Trim(),
                PortNumber = _txtPortNumber.Text.Trim(),
                MacAddress = _txtMacAddress.Text.Trim(),
                IpAddress = _txtIpAddress.Text.Trim(),
                SubnetMask = _txtSubnetMask.Text.Trim(),
                Gateway = _txtGateway.Text.Trim(),
                Vlan = _txtVlan.Text.Trim(),
                Protocol = _cmbProtocol.Text.Trim(),
                MpiDpPnAddress = _txtMpiDpPnAddress.Text.Trim(),
                Speed = _txtSpeed.Text.Trim(),
                Medium = _cmbMedium.Text.Trim(),
                Notes = _txtNotes.Text.Trim()
            };

            DialogResult = DialogResult.OK;
            Close();
        }

        private static Label CreateLabel(string text) =>
            new()
            {
                Text = text,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                Margin = new Padding(0, 0, 8, 8)
            };
    }
}
