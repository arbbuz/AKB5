using AsutpKnowledgeBase.Models;

namespace AsutpKnowledgeBase
{
    public sealed class KnowledgeBaseObjectTemplateSaveDialog : Form
    {
        private readonly KbNode _sourceNode;

        private TextBox _txtSource = null!;
        private TextBox _txtDisplayName = null!;
        private TextBox _txtCategory = null!;
        private TextBox _txtDescription = null!;
        private TextBox _txtSummary = null!;

        public KnowledgeBaseObjectTemplateSaveDialog(KbNode sourceNode)
        {
            _sourceNode = sourceNode;

            Text = "Сохранить объект как шаблон";
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MinimizeBox = false;
            MaximizeBox = false;
            ShowInTaskbar = false;
            ClientSize = new Size(680, 390);
            AppIconProvider.Apply(this);

            Controls.Add(CreateLayout());

            AcceptButton = Controls
                .Find("btnOk", searchAllChildren: true)
                .OfType<Button>()
                .FirstOrDefault();
            CancelButton = Controls
                .Find("btnCancel", searchAllChildren: true)
                .OfType<Button>()
                .FirstOrDefault();

            Shown += (_, _) =>
            {
                _txtDisplayName.SelectAll();
                _txtDisplayName.Focus();
            };
        }

        public string DisplayName { get; private set; } = string.Empty;

        public string Category { get; private set; } = string.Empty;

        public string Description { get; private set; } = string.Empty;

        private TableLayoutPanel CreateLayout()
        {
            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(12),
                ColumnCount = 2,
                RowCount = 6
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 170F));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            for (int rowIndex = 0; rowIndex < 3; rowIndex++)
                layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 55F));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 45F));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            _txtSource = CreateReadOnlyTextBox(_sourceNode.Name);
            AddFieldRow(layout, 0, "Исходный объект", _txtSource);

            _txtDisplayName = CreateTextBox(_sourceNode.Name);
            AddFieldRow(layout, 1, "Название шаблона", _txtDisplayName);

            _txtCategory = CreateTextBox(FormatNodeType(_sourceNode.NodeType));
            AddFieldRow(layout, 2, "Категория", _txtCategory);

            _txtDescription = new TextBox
            {
                Dock = DockStyle.Fill,
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                Text = _sourceNode.Details?.Description ?? string.Empty
            };
            AddFieldRow(layout, 3, "Описание", _txtDescription);

            _txtSummary = CreateReadOnlyTextBox(BuildSummary());
            _txtSummary.Multiline = true;
            _txtSummary.ScrollBars = ScrollBars.Vertical;
            AddFieldRow(layout, 4, "Состав", _txtSummary);

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
            layout.Controls.Add(buttonsPanel, 0, 5);
            layout.SetColumnSpan(buttonsPanel, 2);

            return layout;
        }

        private void Submit()
        {
            string displayName = _txtDisplayName.Text.Trim();
            if (string.IsNullOrWhiteSpace(displayName))
            {
                MessageBox.Show(
                    this,
                    "Укажите название шаблона.",
                    "Шаблоны объектов",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            DisplayName = displayName;
            Category = _txtCategory.Text.Trim();
            Description = _txtDescription.Text.Trim();
            DialogResult = DialogResult.OK;
            Close();
        }

        private string BuildSummary() =>
            string.Join(
                Environment.NewLine,
                $"Тип узла: {FormatNodeType(_sourceNode.NodeType)}",
                $"Узлов дерева: {CountNodes(_sourceNode)}");

        private static int CountNodes(KbNode node) =>
            1 + node.Children.Sum(CountNodes);

        private static TextBox CreateTextBox(string? text) =>
            new()
            {
                Dock = DockStyle.Fill,
                Text = text ?? string.Empty,
                Margin = new Padding(0, 0, 0, 8)
            };

        private static TextBox CreateReadOnlyTextBox(string? text) =>
            new()
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = Color.White,
                Text = text ?? string.Empty,
                Margin = new Padding(0, 0, 0, 8)
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
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                Margin = new Padding(0, 0, 8, 8)
            };
            layout.Controls.Add(label, 0, rowIndex);
            layout.Controls.Add(editor, 1, rowIndex);
        }

        private static string FormatNodeType(KbNodeType nodeType) => nodeType switch
        {
            KbNodeType.System => "Система",
            KbNodeType.Cabinet => "Шкаф",
            KbNodeType.Device => "Устройство",
            KbNodeType.Controller => "Контроллер",
            KbNodeType.Module => "Модуль",
            KbNodeType.DocumentNode => "Документ/папка",
            KbNodeType.Department => "Подразделение",
            KbNodeType.WorkshopRoot => "Цех",
            _ => "Объект"
        };
    }
}
