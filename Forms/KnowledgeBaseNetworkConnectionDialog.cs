using AsutpKnowledgeBase.Models;

namespace AsutpKnowledgeBase
{
    public sealed class KnowledgeBaseNetworkConnectionDialog : Form
    {
        private sealed class InterfaceComboItem
        {
            public string NetworkInterfaceId { get; init; } = string.Empty;

            public string Text { get; init; } = string.Empty;

            public override string ToString() => Text;
        }

        private readonly string _networkConnectionId;

        private ComboBox _cmbEndpointA = null!;
        private ComboBox _cmbEndpointB = null!;
        private TextBox _txtCableLabel = null!;
        private TextBox _txtCableType = null!;
        private ComboBox _cmbProtocol = null!;
        private ComboBox _cmbMedium = null!;
        private TextBox _txtLength = null!;
        private TextBox _txtRouteText = null!;
        private TextBox _txtStatus = null!;
        private TextBox _txtNotes = null!;

        public KnowledgeBaseNetworkConnectionDialog(
            string title,
            IReadOnlyList<KbNetworkDevice> devices,
            IReadOnlyList<KbNetworkInterface> interfaces,
            KbNetworkConnection? existingConnection = null)
        {
            _networkConnectionId = existingConnection?.NetworkConnectionId?.Trim() ?? string.Empty;

            Text = title;
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MinimizeBox = false;
            MaximizeBox = false;
            ShowInTaskbar = false;
            ClientSize = new Size(760, 430);
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
                RowCount = 10
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 180F));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            for (int rowIndex = 0; rowIndex < 10; rowIndex++)
                layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            _cmbEndpointA = CreateInterfaceComboBox();
            _cmbEndpointB = CreateInterfaceComboBox();
            FillInterfaceItems(_cmbEndpointA, devices, interfaces, existingConnection?.EndpointAInterfaceId);
            FillInterfaceItems(_cmbEndpointB, devices, interfaces, existingConnection?.EndpointBInterfaceId);
            layout.Controls.Add(CreateLabel("Интерфейс A"), 0, 0);
            layout.Controls.Add(_cmbEndpointA, 1, 0);
            layout.Controls.Add(CreateLabel("Интерфейс B"), 0, 1);
            layout.Controls.Add(_cmbEndpointB, 1, 1);

            _txtCableLabel = AddTextBox(layout, "Кабель", 2, existingConnection?.CableLabel);
            _txtCableType = AddTextBox(layout, "Тип кабеля", 3, existingConnection?.CableType);
            _cmbProtocol = AddPresetComboBox(
                layout,
                "Протокол",
                4,
                existingConnection?.Protocol,
                ["PROFINET", "PROFIBUS", "MPI"]);
            _cmbMedium = AddPresetComboBox(
                layout,
                "Среда",
                5,
                existingConnection?.Medium,
                ["Медь", "Оптика"]);
            _txtLength = AddTextBox(layout, "Длина", 6, existingConnection?.Length);
            _txtRouteText = AddTextBox(layout, "Трасса / место", 7, existingConnection?.RouteText);
            _txtStatus = AddTextBox(layout, "Статус", 8, existingConnection?.Status);

            _txtNotes = new TextBox
            {
                Dock = DockStyle.Fill,
                Multiline = true,
                Height = 64,
                ScrollBars = ScrollBars.Vertical,
                AccessibleName = "Примечание",
                Text = existingConnection?.Notes ?? string.Empty,
                Margin = new Padding(0, 0, 0, 8)
            };
            layout.Controls.Add(CreateLabel("Примечание"), 0, 9);
            layout.Controls.Add(_txtNotes, 1, 9);

            rootLayout.Controls.Add(layout, 0, 0);
            rootLayout.Controls.Add(CreateButtonsPanel(), 0, 1);
            Controls.Add(rootLayout);
        }

        public KbNetworkConnection Result { get; private set; } = new();

        private static ComboBox CreateInterfaceComboBox() =>
            new()
            {
                Dock = DockStyle.Fill,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Margin = new Padding(0, 0, 0, 8)
            };

        private static void FillInterfaceItems(
            ComboBox comboBox,
            IReadOnlyList<KbNetworkDevice> devices,
            IReadOnlyList<KbNetworkInterface> interfaces,
            string? selectedInterfaceId)
        {
            var devicesById = devices
                .Where(static device => !string.IsNullOrWhiteSpace(device.NetworkDeviceId))
                .ToDictionary(static device => device.NetworkDeviceId.Trim(), StringComparer.Ordinal);

            foreach (var networkInterface in interfaces)
            {
                string interfaceId = networkInterface.NetworkInterfaceId?.Trim() ?? string.Empty;
                if (string.IsNullOrWhiteSpace(interfaceId))
                    continue;

                devicesById.TryGetValue(networkInterface.NetworkDeviceId?.Trim() ?? string.Empty, out var device);
                comboBox.Items.Add(new InterfaceComboItem
                {
                    NetworkInterfaceId = interfaceId,
                    Text = FormatInterfaceText(networkInterface, device)
                });
            }

            string normalizedSelectedId = selectedInterfaceId?.Trim() ?? string.Empty;
            int selectedIndex = comboBox.Items.Count > 0 ? 0 : -1;
            for (int index = 0; index < comboBox.Items.Count; index++)
            {
                if (comboBox.Items[index] is InterfaceComboItem item &&
                    string.Equals(item.NetworkInterfaceId, normalizedSelectedId, StringComparison.Ordinal))
                {
                    selectedIndex = index;
                    break;
                }
            }

            comboBox.SelectedIndex = selectedIndex;
        }

        private static string FormatInterfaceText(KbNetworkInterface networkInterface, KbNetworkDevice? device)
        {
            string deviceName = device?.Name?.Trim() ?? networkInterface.NetworkDeviceId?.Trim() ?? string.Empty;
            string interfaceName = networkInterface.InterfaceName?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(interfaceName) && !string.IsNullOrWhiteSpace(networkInterface.PortNumber))
                interfaceName = $"Порт {networkInterface.PortNumber.Trim()}";

            string ipAddress = networkInterface.IpAddress?.Trim() ?? string.Empty;
            return string.Join(
                " / ",
                new[] { deviceName, interfaceName, ipAddress }
                    .Where(static part => !string.IsNullOrWhiteSpace(part)));
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
            if (_cmbEndpointA.SelectedItem is not InterfaceComboItem endpointA ||
                _cmbEndpointB.SelectedItem is not InterfaceComboItem endpointB)
            {
                MessageBox.Show(
                    this,
                    "Выберите оба интерфейса сетевого соединения.",
                    "Сеть",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            if (string.Equals(endpointA.NetworkInterfaceId, endpointB.NetworkInterfaceId, StringComparison.Ordinal))
            {
                MessageBox.Show(
                    this,
                    "Интерфейсы соединения должны быть разными.",
                    "Сеть",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            Result = new KbNetworkConnection
            {
                NetworkConnectionId = _networkConnectionId,
                EndpointAInterfaceId = endpointA.NetworkInterfaceId,
                EndpointBInterfaceId = endpointB.NetworkInterfaceId,
                CableLabel = _txtCableLabel.Text.Trim(),
                CableType = _txtCableType.Text.Trim(),
                Protocol = _cmbProtocol.Text.Trim(),
                Medium = _cmbMedium.Text.Trim(),
                Length = _txtLength.Text.Trim(),
                RouteText = _txtRouteText.Text.Trim(),
                Status = _txtStatus.Text.Trim(),
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
