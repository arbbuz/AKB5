using AsutpKnowledgeBase.Models;

namespace AsutpKnowledgeBase
{
    public sealed class KnowledgeBaseObjectTemplateCreateDialog : Form
    {
        private readonly string _parentText;
        private string _lastSuggestedRootName = string.Empty;

        private ComboBox _cmbTemplates = null!;
        private TextBox _txtRootName = null!;
        private TextBox _txtParent = null!;
        private TextBox _txtDescription = null!;

        public KnowledgeBaseObjectTemplateCreateDialog(
            IReadOnlyList<KbObjectTemplate> templates,
            string parentText)
        {
            _parentText = parentText;

            Text = "Создать объект из шаблона";
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MinimizeBox = false;
            MaximizeBox = false;
            ShowInTaskbar = false;
            ClientSize = new Size(680, 430);
            AppIconProvider.Apply(this);

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(12),
                ColumnCount = 2,
                RowCount = 4
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 170F));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34F));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34F));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34F));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            layout.Controls.Add(CreateLabel("Шаблон"), 0, 0);
            _cmbTemplates = new ComboBox
            {
                Dock = DockStyle.Fill,
                DropDownStyle = ComboBoxStyle.DropDownList,
                DisplayMember = nameof(KbObjectTemplate.DisplayName),
                DataSource = templates.ToList()
            };
            _cmbTemplates.SelectedIndexChanged += (_, _) => UpdateTemplateDetails();
            layout.Controls.Add(_cmbTemplates, 1, 0);

            layout.Controls.Add(CreateLabel("Имя объекта"), 0, 1);
            _txtRootName = new TextBox
            {
                Dock = DockStyle.Fill
            };
            layout.Controls.Add(_txtRootName, 1, 1);

            layout.Controls.Add(CreateLabel("Родитель"), 0, 2);
            _txtParent = new TextBox
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = Color.White,
                Text = _parentText
            };
            layout.Controls.Add(_txtParent, 1, 2);

            layout.Controls.Add(CreateLabel("Состав шаблона"), 0, 3);
            _txtDescription = new TextBox
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                WordWrap = true,
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = Color.White
            };
            layout.Controls.Add(_txtDescription, 1, 3);

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
                Text = "Создать",
                AutoSize = true
            };
            btnOk.Click += BtnOk_Click;

            buttonsPanel.Controls.Add(btnCancel);
            buttonsPanel.Controls.Add(btnOk);

            Controls.Add(layout);
            Controls.Add(buttonsPanel);

            AcceptButton = btnOk;
            CancelButton = btnCancel;

            UpdateTemplateDetails();
            Shown += (_, _) =>
            {
                _txtRootName.SelectAll();
                _txtRootName.Focus();
            };
        }

        public string SelectedTemplateId { get; private set; } = string.Empty;

        public string RootNodeName { get; private set; } = string.Empty;

        private void BtnOk_Click(object? sender, EventArgs e)
        {
            if (_cmbTemplates.SelectedItem is not KbObjectTemplate template)
            {
                MessageBox.Show(
                    this,
                    "Выберите шаблон объекта.",
                    "Шаблоны объектов",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            SelectedTemplateId = template.TemplateId;
            RootNodeName = _txtRootName.Text.Trim();
            DialogResult = DialogResult.OK;
            Close();
        }

        private void UpdateTemplateDetails()
        {
            if (_cmbTemplates.SelectedItem is not KbObjectTemplate template)
            {
                _txtDescription.Text = string.Empty;
                return;
            }

            string suggestedName = template.RootNode?.Name?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(suggestedName))
                suggestedName = template.DisplayName?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(_txtRootName.Text) ||
                string.Equals(_txtRootName.Text, _lastSuggestedRootName, StringComparison.Ordinal))
            {
                _txtRootName.Text = suggestedName;
                _lastSuggestedRootName = suggestedName;
            }

            _txtDescription.Text = BuildTemplateDetails(template);
        }

        private static string BuildTemplateDetails(KbObjectTemplate template)
        {
            var lines = new List<string>();
            if (!string.IsNullOrWhiteSpace(template.Category))
                lines.Add($"Категория: {template.Category}");

            if (!string.IsNullOrWhiteSpace(template.Description))
            {
                if (lines.Count > 0)
                    lines.Add(string.Empty);

                lines.Add(template.Description);
            }

            if (lines.Count > 0)
                lines.Add(string.Empty);

            lines.Add($"Узлов дерева: {CountNodes(template.RootNode)}");
            lines.Add($"Записей состава: {template.CompositionEntries.Count}");
            lines.Add($"Документов: {template.DocumentLinks.Count}");
            lines.Add($"Записей ПО: {template.SoftwareRecords.Count}");
            lines.Add($"Профилей ТО: {template.MaintenanceScheduleProfiles.Count}");
            return string.Join(Environment.NewLine, lines);
        }

        private static int CountNodes(KbObjectTemplateNode? node)
        {
            if (node == null)
                return 0;

            return 1 + node.Children.Sum(CountNodes);
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
