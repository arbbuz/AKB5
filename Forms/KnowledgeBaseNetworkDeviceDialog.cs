using AsutpKnowledgeBase.Models;

namespace AsutpKnowledgeBase
{
    public sealed class KnowledgeBaseNetworkDeviceDialog : Form
    {
        private sealed class NodeComboItem
        {
            public string NodeId { get; init; } = string.Empty;

            public string Text { get; init; } = string.Empty;

            public override string ToString() => Text;
        }

        private readonly string _networkDeviceId;
        private readonly string _ownerNodeId;

        private ComboBox _cmbLinkedNode = null!;
        private TextBox _txtName = null!;
        private TextBox _txtRole = null!;
        private TextBox _txtVendor = null!;
        private TextBox _txtModel = null!;
        private TextBox _txtOrderNumber = null!;
        private TextBox _txtSerialNumber = null!;
        private TextBox _txtFirmware = null!;
        private TextBox _txtProfinetName = null!;
        private TextBox _txtMacAddress = null!;
        private TextBox _txtLocationText = null!;
        private TextBox _txtCabinetText = null!;
        private TextBox _txtNotes = null!;

        public KnowledgeBaseNetworkDeviceDialog(
            string title,
            KbNode ownerNode,
            KbNetworkDevice? existingDevice = null)
        {
            _networkDeviceId = existingDevice?.NetworkDeviceId?.Trim() ?? string.Empty;
            _ownerNodeId = ownerNode.NodeId?.Trim() ?? string.Empty;

            Text = title;
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MinimizeBox = false;
            MaximizeBox = false;
            ShowInTaskbar = false;
            ClientSize = new Size(760, 520);
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

            _txtName = AddTextBox(layout, "Наименование", 0, existingDevice?.Name);
            _txtRole = AddTextBox(layout, "Роль", 1, existingDevice?.Role);
            _txtVendor = AddTextBox(layout, "Производитель", 2, existingDevice?.Vendor);
            _txtModel = AddTextBox(layout, "Модель", 3, existingDevice?.Model);
            _txtOrderNumber = AddTextBox(layout, "Заказной номер", 4, existingDevice?.OrderNumber);
            _txtSerialNumber = AddTextBox(layout, "Серийный номер", 5, existingDevice?.SerialNumber);
            _txtFirmware = AddTextBox(layout, "Firmware", 6, existingDevice?.Firmware);
            _txtProfinetName = AddTextBox(layout, "PROFINET-name", 7, existingDevice?.ProfinetName);
            _txtMacAddress = AddTextBox(layout, "MAC", 8, existingDevice?.MacAddress);
            _txtLocationText = AddTextBox(layout, "Место", 9, existingDevice?.LocationText);
            _txtCabinetText = AddTextBox(layout, "Шкаф / щит", 10, existingDevice?.CabinetText);

            _cmbLinkedNode = new ComboBox
            {
                Dock = DockStyle.Fill,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Margin = new Padding(0, 0, 0, 8)
            };
            FillLinkedNodeItems(ownerNode, existingDevice?.LinkedNodeId);
            layout.Controls.Add(CreateLabel("Связанная карточка"), 0, 11);
            layout.Controls.Add(_cmbLinkedNode, 1, 11);

            _txtNotes = new TextBox
            {
                Dock = DockStyle.Fill,
                Multiline = true,
                Height = 64,
                ScrollBars = ScrollBars.Vertical,
                Text = existingDevice?.Notes ?? string.Empty,
                Margin = new Padding(0, 0, 0, 8)
            };
            layout.Controls.Add(CreateLabel("Примечание"), 0, 12);
            layout.Controls.Add(_txtNotes, 1, 12);

            rootLayout.Controls.Add(layout, 0, 0);
            rootLayout.Controls.Add(CreateButtonsPanel(), 0, 1);
            Controls.Add(rootLayout);
        }

        public KbNetworkDevice Result { get; private set; } = new();

        private void FillLinkedNodeItems(KbNode ownerNode, string? selectedLinkedNodeId)
        {
            _cmbLinkedNode.Items.Add(new NodeComboItem { Text = "(не связано)" });
            foreach (var item in CreateNodeItems(ownerNode))
                _cmbLinkedNode.Items.Add(item);

            string normalizedSelectedId = selectedLinkedNodeId?.Trim() ?? string.Empty;
            int selectedIndex = 0;
            for (int index = 0; index < _cmbLinkedNode.Items.Count; index++)
            {
                if (_cmbLinkedNode.Items[index] is NodeComboItem item &&
                    string.Equals(item.NodeId, normalizedSelectedId, StringComparison.Ordinal))
                {
                    selectedIndex = index;
                    break;
                }
            }

            _cmbLinkedNode.SelectedIndex = selectedIndex;
        }

        private static IReadOnlyList<NodeComboItem> CreateNodeItems(KbNode ownerNode)
        {
            var items = new List<NodeComboItem>();
            CollectNodeItems(ownerNode, items, level: 0);
            return items;
        }

        private static void CollectNodeItems(KbNode node, ICollection<NodeComboItem> items, int level)
        {
            string nodeId = node.NodeId?.Trim() ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(nodeId))
            {
                string prefix = level == 0 ? string.Empty : new string(' ', level * 2);
                items.Add(new NodeComboItem
                {
                    NodeId = nodeId,
                    Text = $"{prefix}{node.Name?.Trim() ?? nodeId}"
                });
            }

            foreach (var child in node.Children ?? new List<KbNode>())
                CollectNodeItems(child, items, level + 1);
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
                Margin = new Padding(0, 0, 0, 8)
            };
            layout.Controls.Add(CreateLabel(label), 0, row);
            layout.Controls.Add(textBox, 1, row);
            return textBox;
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
            string name = _txtName.Text.Trim();
            if (string.IsNullOrWhiteSpace(name))
            {
                MessageBox.Show(
                    this,
                    "Укажите наименование сетевого устройства.",
                    "Сеть",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            string linkedNodeId = _cmbLinkedNode.SelectedItem is NodeComboItem item
                ? item.NodeId
                : string.Empty;

            Result = new KbNetworkDevice
            {
                NetworkDeviceId = _networkDeviceId,
                OwnerNodeId = _ownerNodeId,
                LinkedNodeId = linkedNodeId,
                Name = name,
                Role = _txtRole.Text.Trim(),
                Vendor = _txtVendor.Text.Trim(),
                Model = _txtModel.Text.Trim(),
                OrderNumber = _txtOrderNumber.Text.Trim(),
                SerialNumber = _txtSerialNumber.Text.Trim(),
                Firmware = _txtFirmware.Text.Trim(),
                ProfinetName = _txtProfinetName.Text.Trim(),
                MacAddress = _txtMacAddress.Text.Trim(),
                LocationText = _txtLocationText.Text.Trim(),
                CabinetText = _txtCabinetText.Text.Trim(),
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
