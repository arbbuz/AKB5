namespace AsutpKnowledgeBase
{
    public enum KnowledgeBaseActDocumentConflictAction
    {
        Cancel = 0,
        OpenExisting = 1,
        Overwrite = 2,
        SaveCopy = 3
    }

    public sealed class KnowledgeBaseActDocumentConflictDialog : Form
    {
        public KnowledgeBaseActDocumentConflictDialog(string documentPath)
        {
            Text = "DOCX уже существует";
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MinimizeBox = false;
            MaximizeBox = false;
            ShowInTaskbar = false;
            ClientSize = new Size(660, 190);
            AppIconProvider.Apply(this);

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3,
                Padding = new Padding(14)
            };
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            var message = new Label
            {
                AutoSize = true,
                Text = "Файл DOCX уже существует. Выберите действие:"
            };

            var pathBox = new TextBox
            {
                Dock = DockStyle.Fill,
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Vertical,
                Text = documentPath
            };

            var buttonsPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.RightToLeft,
                WrapContents = false,
                AutoSize = true,
                Padding = new Padding(0, 10, 0, 0)
            };

            Button btnCancel = CreateButton("Отмена", KnowledgeBaseActDocumentConflictAction.Cancel);
            Button btnSaveCopy = CreateButton("Сохранить копию", KnowledgeBaseActDocumentConflictAction.SaveCopy);
            Button btnOverwrite = CreateButton("Перезаписать", KnowledgeBaseActDocumentConflictAction.Overwrite);
            Button btnOpenExisting = CreateButton("Открыть существующий", KnowledgeBaseActDocumentConflictAction.OpenExisting);

            buttonsPanel.Controls.Add(btnCancel);
            buttonsPanel.Controls.Add(btnSaveCopy);
            buttonsPanel.Controls.Add(btnOverwrite);
            buttonsPanel.Controls.Add(btnOpenExisting);

            layout.Controls.Add(message, 0, 0);
            layout.Controls.Add(pathBox, 0, 1);
            layout.Controls.Add(buttonsPanel, 0, 2);
            Controls.Add(layout);

            AcceptButton = btnOpenExisting;
            CancelButton = btnCancel;
        }

        public KnowledgeBaseActDocumentConflictAction SelectedAction { get; private set; }

        private Button CreateButton(string text, KnowledgeBaseActDocumentConflictAction action)
        {
            var button = new Button
            {
                Text = text,
                AutoSize = true
            };
            button.Click += (_, _) =>
            {
                SelectedAction = action;
                DialogResult = action == KnowledgeBaseActDocumentConflictAction.Cancel
                    ? DialogResult.Cancel
                    : DialogResult.OK;
                Close();
            };
            return button;
        }
    }
}
