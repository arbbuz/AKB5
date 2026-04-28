using AsutpKnowledgeBase.Models;

namespace AsutpKnowledgeBase
{
    public sealed class KnowledgeBaseCompositionTemplateDialog : Form
    {
        private readonly bool _collectNodeName;

        private ComboBox _cmbTemplates = null!;
        private TextBox _txtNodeName = null!;
        private TextBox _txtDescription = null!;

        public KnowledgeBaseCompositionTemplateDialog(
            string title,
            string prompt,
            IReadOnlyList<KbCompositionTemplate> templates,
            bool collectNodeName,
            string defaultNodeName = "",
            string inheritedLocation = "")
        {
            _collectNodeName = collectNodeName;

            Text = title;
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MinimizeBox = false;
            MaximizeBox = false;
            ShowInTaskbar = false;
            ClientSize = new Size(620, collectNodeName ? 320 : 250);
            AppIconProvider.Apply(this);

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(12),
                ColumnCount = 2,
                RowCount = collectNodeName ? 5 : 3
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 170F));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));

            layout.Controls.Add(CreateLabel(prompt), 0, 0);
            layout.SetColumnSpan(layout.Controls[layout.Controls.Count - 1], 2);

            _cmbTemplates = new ComboBox
            {
                Dock = DockStyle.Fill,
                DropDownStyle = ComboBoxStyle.DropDownList,
                DisplayMember = nameof(KbCompositionTemplate.DisplayName),
                DataSource = templates.ToList()
            };
            _cmbTemplates.SelectedIndexChanged += (_, _) => UpdateTemplateDetails();
            layout.Controls.Add(CreateLabel("Шаблон"), 0, 1);
            layout.Controls.Add(_cmbTemplates, 1, 1);

            int nextRow = 2;
            if (collectNodeName)
            {
                _txtNodeName = new TextBox
                {
                    Dock = DockStyle.Fill,
                    Text = defaultNodeName
                };
                layout.Controls.Add(CreateLabel("Имя узла"), 0, nextRow);
                layout.Controls.Add(_txtNodeName, 1, nextRow);
                nextRow++;

                if (!string.IsNullOrWhiteSpace(inheritedLocation))
                {
                    var txtInheritedLocation = new TextBox
                    {
                        Dock = DockStyle.Fill,
                        ReadOnly = true,
                        BorderStyle = BorderStyle.FixedSingle,
                        BackColor = Color.White,
                        Text = inheritedLocation
                    };
                    layout.Controls.Add(CreateLabel("Унаследованное место"), 0, nextRow);
                    layout.Controls.Add(txtInheritedLocation, 1, nextRow);
                    nextRow++;
                }
            }
            else
            {
                _txtNodeName = new TextBox();
            }

            _txtDescription = new TextBox
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = Color.White
            };
            layout.Controls.Add(CreateLabel("Описание"), 0, nextRow);
            layout.Controls.Add(_txtDescription, 1, nextRow);

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
                Text = "OK",
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
                if (_collectNodeName)
                {
                    _txtNodeName.SelectAll();
                    _txtNodeName.Focus();
                }
                else
                {
                    _cmbTemplates.Focus();
                }
            };
        }

        public string SelectedTemplateId { get; private set; } = string.Empty;

        public string NodeName { get; private set; } = string.Empty;

        private void BtnOk_Click(object? sender, EventArgs e)
        {
            if (_cmbTemplates.SelectedItem is not KbCompositionTemplate template)
            {
                MessageBox.Show(
                    this,
                    "Выберите шаблон.",
                    "Состав",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            string nodeName = _txtNodeName.Text.Trim();
            if (_collectNodeName && string.IsNullOrWhiteSpace(nodeName))
            {
                MessageBox.Show(
                    this,
                    "Укажите имя нового узла.",
                    "Состав",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            SelectedTemplateId = template.TemplateId;
            NodeName = nodeName;
            DialogResult = DialogResult.OK;
            Close();
        }

        private void UpdateTemplateDetails()
        {
            if (_cmbTemplates.SelectedItem is not KbCompositionTemplate template)
            {
                _txtDescription.Text = string.Empty;
                return;
            }

            _txtDescription.Text =
                $"{template.Description}{Environment.NewLine}{Environment.NewLine}Элементов состава: {template.Entries.Count}";

            if (_collectNodeName && string.IsNullOrWhiteSpace(_txtNodeName.Text))
                _txtNodeName.Text = template.SuggestedNodeName;
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
