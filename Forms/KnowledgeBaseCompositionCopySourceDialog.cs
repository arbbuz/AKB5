using AsutpKnowledgeBase.Models;

namespace AsutpKnowledgeBase
{
    public sealed class KnowledgeBaseCompositionCopySourceOption
    {
        public KbNode Node { get; init; } = new();

        public string DisplayText { get; init; } = string.Empty;

        public string Description { get; init; } = string.Empty;
    }

    public sealed class KnowledgeBaseCompositionCopySourceDialog : Form
    {
        private ComboBox _cmbSources = null!;
        private TextBox _txtDescription = null!;

        public KnowledgeBaseCompositionCopySourceDialog(
            IReadOnlyList<KnowledgeBaseCompositionCopySourceOption> options)
        {
            Text = "Копировать состав из существующего объекта";
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MinimizeBox = false;
            MaximizeBox = false;
            ShowInTaskbar = false;
            ClientSize = new Size(620, 260);
            AppIconProvider.Apply(this);

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(12),
                ColumnCount = 2,
                RowCount = 3
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150F));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));

            layout.Controls.Add(CreateLabel("Источник"), 0, 0);
            _cmbSources = new ComboBox
            {
                Dock = DockStyle.Fill,
                DropDownStyle = ComboBoxStyle.DropDownList,
                DisplayMember = nameof(KnowledgeBaseCompositionCopySourceOption.DisplayText),
                DataSource = options.ToList()
            };
            _cmbSources.SelectedIndexChanged += (_, _) => UpdateDescription();
            layout.Controls.Add(_cmbSources, 1, 0);

            layout.Controls.Add(CreateLabel("Описание"), 0, 1);
            _txtDescription = new TextBox
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = Color.White
            };
            layout.Controls.Add(_txtDescription, 1, 1);

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
                Text = "Выбрать",
                AutoSize = true
            };
            btnOk.Click += BtnOk_Click;

            buttonsPanel.Controls.Add(btnCancel);
            buttonsPanel.Controls.Add(btnOk);

            Controls.Add(layout);
            Controls.Add(buttonsPanel);

            AcceptButton = btnOk;
            CancelButton = btnCancel;

            UpdateDescription();
        }

        public KbNode? SelectedSourceNode { get; private set; }

        private void BtnOk_Click(object? sender, EventArgs e)
        {
            if (_cmbSources.SelectedItem is not KnowledgeBaseCompositionCopySourceOption option)
            {
                MessageBox.Show(
                    this,
                    "Выберите объект-источник.",
                    "Состав",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            SelectedSourceNode = option.Node;
            DialogResult = DialogResult.OK;
            Close();
        }

        private void UpdateDescription()
        {
            _txtDescription.Text = _cmbSources.SelectedItem is KnowledgeBaseCompositionCopySourceOption option
                ? option.Description
                : string.Empty;
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
