using AsutpKnowledgeBase.Models;
using AsutpKnowledgeBase.Services;

namespace AsutpKnowledgeBase
{
    public sealed class KnowledgeBaseCompositionRackDialog : Form
    {
        private readonly string _rackId;
        private readonly string _parentNodeId;
        private readonly int _sortOrder;

        private NumericUpDown _numRackNumber = null!;
        private TextBox _txtRackType = null!;
        private TextBox _txtLabel = null!;
        private TextBox _txtNetworkLink = null!;
        private TextBox _txtNotes = null!;
        private Label _lblPreview = null!;

        public KnowledgeBaseCompositionRackDialog(string title, KbCompositionRack draftRack)
        {
            _rackId = draftRack.RackId?.Trim() ?? string.Empty;
            _parentNodeId = draftRack.ParentNodeId?.Trim() ?? string.Empty;
            _sortOrder = draftRack.SortOrder;

            Text = title;
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MinimizeBox = false;
            MaximizeBox = false;
            ShowInTaskbar = false;
            ClientSize = new Size(560, 330);
            AppIconProvider.Apply(this);

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(12),
                ColumnCount = 2,
                RowCount = 6
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150F));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            for (int rowIndex = 0; rowIndex < 5; rowIndex++)
                layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            _numRackNumber = new NumericUpDown
            {
                Dock = DockStyle.Left,
                Width = 120,
                Minimum = 0,
                Maximum = 64,
                Value = KnowledgeBaseCompositionRackSlotRulesService.NormalizeRackNumber(draftRack.RackNumber)
            };
            _numRackNumber.ValueChanged += (_, _) => UpdatePreview();
            layout.Controls.Add(CreateLabel("Rack"), 0, 0);
            layout.Controls.Add(_numRackNumber, 1, 0);

            _txtRackType = CreateSingleLineTextBox(draftRack.RackType);
            _txtRackType.TextChanged += (_, _) => UpdatePreview();
            layout.Controls.Add(CreateLabel("Тип Rack"), 0, 1);
            layout.Controls.Add(_txtRackType, 1, 1);

            _txtLabel = CreateSingleLineTextBox(draftRack.Label);
            _txtLabel.TextChanged += (_, _) => UpdatePreview();
            layout.Controls.Add(CreateLabel("Подпись"), 0, 2);
            layout.Controls.Add(_txtLabel, 1, 2);

            _txtNetworkLink = CreateSingleLineTextBox(draftRack.NetworkLink);
            layout.Controls.Add(CreateLabel("Сетевая связь"), 0, 3);
            layout.Controls.Add(_txtNetworkLink, 1, 3);

            _lblPreview = new Label
            {
                AutoSize = true,
                Dock = DockStyle.Fill,
                ForeColor = Color.DimGray,
                Margin = new Padding(0, 0, 0, 8)
            };
            layout.Controls.Add(CreateLabel("Заголовок"), 0, 4);
            layout.Controls.Add(_lblPreview, 1, 4);

            _txtNotes = new TextBox
            {
                Dock = DockStyle.Fill,
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                Text = draftRack.Notes?.Trim() ?? string.Empty
            };
            layout.Controls.Add(CreateLabel("Примечание"), 0, 5);
            layout.Controls.Add(_txtNotes, 1, 5);

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
            UpdatePreview();
        }

        public KbCompositionRack Result { get; private set; } = new();

        private void BtnOk_Click(object? sender, EventArgs e)
        {
            string rackType = _txtRackType.Text.Trim();
            if (string.IsNullOrWhiteSpace(rackType))
            {
                MessageBox.Show(
                    this,
                    "Укажите тип Rack.",
                    "Rack состава",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            Result = new KbCompositionRack
            {
                RackId = _rackId,
                ParentNodeId = _parentNodeId,
                RackNumber = (int)_numRackNumber.Value,
                SortOrder = _sortOrder,
                RackType = rackType,
                Label = _txtLabel.Text.Trim(),
                NetworkLink = _txtNetworkLink.Text.Trim(),
                Notes = _txtNotes.Text.Trim()
            };

            DialogResult = DialogResult.OK;
            Close();
        }

        private void UpdatePreview()
        {
            if (_lblPreview == null)
                return;

            _lblPreview.Text = KnowledgeBaseCompositionRackSlotRulesService.FormatRackTitle(
                (int)_numRackNumber.Value,
                _txtRackType?.Text,
                _txtLabel?.Text);
        }

        private static TextBox CreateSingleLineTextBox(string? text) =>
            new()
            {
                Dock = DockStyle.Fill,
                Text = text?.Trim() ?? string.Empty,
                Margin = new Padding(0, 0, 0, 8)
            };

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
