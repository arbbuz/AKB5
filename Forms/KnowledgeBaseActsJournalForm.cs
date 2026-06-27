using AsutpKnowledgeBase.Services;

namespace AsutpKnowledgeBase
{
    public enum KnowledgeBaseActsJournalAction
    {
        None = 0,
        Open = 1,
        GenerateDocument = 2,
        DeleteDraft = 3,
        CancelAct = 4,
        AnnulAct = 5
    }

    public sealed class KnowledgeBaseActsJournalForm : Form
    {
        private readonly DataGridView _grid;
        private readonly Button _btnOpen;
        private readonly Button _btnGenerateDocument;
        private readonly Button _btnDeleteDraft;
        private readonly Button _btnCancelAct;
        private readonly Button _btnAnnulAct;

        public KnowledgeBaseActsJournalForm(
            IEnumerable<KnowledgeBaseActJournalRow> rows,
            string? preferredActId = null)
        {
            Text = "Журнал актов";
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.Sizable;
            MinimizeBox = false;
            MaximizeBox = true;
            ShowInTaskbar = false;
            MinimumSize = new Size(940, 520);
            ClientSize = new Size(1120, 620);
            WindowState = FormWindowState.Maximized;
            AppIconProvider.Apply(this);

            var rootLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2
            };
            rootLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            rootLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            _grid = CreateGrid();
            _grid.SelectionChanged += (_, _) => UpdateButtonStates();
            _grid.CellDoubleClick += (_, e) =>
            {
                if (e.RowIndex >= 0)
                    Complete(KnowledgeBaseActsJournalAction.Open);
            };

            var buttonsPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.RightToLeft,
                WrapContents = true,
                AutoSize = true,
                Padding = new Padding(12, 8, 12, 12)
            };

            var btnClose = new Button
            {
                Text = "Закрыть",
                DialogResult = DialogResult.Cancel,
                AutoSize = true
            };
            _btnAnnulAct = CreateButton("Аннулировать", KnowledgeBaseActsJournalAction.AnnulAct);
            _btnCancelAct = CreateButton("Отменить", KnowledgeBaseActsJournalAction.CancelAct);
            _btnDeleteDraft = CreateButton("Удалить черновик", KnowledgeBaseActsJournalAction.DeleteDraft);
            _btnGenerateDocument = CreateButton("Сформировать DOCX", KnowledgeBaseActsJournalAction.GenerateDocument);
            _btnOpen = CreateButton("Открыть", KnowledgeBaseActsJournalAction.Open);

            buttonsPanel.Controls.Add(btnClose);
            buttonsPanel.Controls.Add(_btnAnnulAct);
            buttonsPanel.Controls.Add(_btnCancelAct);
            buttonsPanel.Controls.Add(_btnDeleteDraft);
            buttonsPanel.Controls.Add(_btnGenerateDocument);
            buttonsPanel.Controls.Add(_btnOpen);

            rootLayout.Controls.Add(_grid, 0, 0);
            rootLayout.Controls.Add(buttonsPanel, 0, 1);
            Controls.Add(rootLayout);

            AcceptButton = _btnOpen;
            CancelButton = btnClose;
            ApplyRows(rows, preferredActId);
        }

        public string SelectedActId { get; private set; } = string.Empty;

        public KnowledgeBaseActsJournalAction SelectedAction { get; private set; }

        private static DataGridView CreateGrid()
        {
            var grid = new DataGridView
            {
                Dock = DockStyle.Fill,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToResizeRows = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.None,
                ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize,
                MultiSelect = false,
                ReadOnly = true,
                RowHeadersVisible = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect
            };

            AddColumn(grid, "ActDate", "Дата", 90);
            AddColumn(grid, "ActNumber", "Номер", 105);
            AddColumn(grid, "Status", "Статус", 115);
            AddColumn(grid, "ActType", "Тип", 150);
            AddColumn(grid, "Workshop", "Цех", 160);
            AddColumn(grid, "Object", "Объект", 180);
            AddColumn(grid, "Equipment", "Оборудование", 260, DataGridViewAutoSizeColumnMode.Fill);
            AddColumn(grid, "OrderNumber", "Заказной номер", 145);
            AddColumn(grid, "DocumentPath", "DOCX", 220);
            return grid;
        }

        private static void AddColumn(
            DataGridView grid,
            string name,
            string headerText,
            int width,
            DataGridViewAutoSizeColumnMode autoSizeMode = DataGridViewAutoSizeColumnMode.None)
        {
            grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = name,
                HeaderText = headerText,
                Width = width,
                MinimumWidth = Math.Min(80, width),
                AutoSizeMode = autoSizeMode,
                SortMode = DataGridViewColumnSortMode.NotSortable
            });
        }

        private Button CreateButton(string text, KnowledgeBaseActsJournalAction action)
        {
            var button = new Button
            {
                Text = text,
                AutoSize = true
            };
            button.Click += (_, _) => Complete(action);
            return button;
        }

        private void ApplyRows(
            IEnumerable<KnowledgeBaseActJournalRow> rows,
            string? preferredActId)
        {
            string selectedActId = preferredActId?.Trim() ?? string.Empty;
            _grid.Rows.Clear();

            int selectedIndex = -1;
            foreach (KnowledgeBaseActJournalRow row in rows)
            {
                int index = _grid.Rows.Add(
                    row.ActDateText,
                    row.ActNumberText,
                    row.StatusText,
                    row.ActTypeText,
                    row.WorkshopName,
                    row.ObjectName,
                    row.EquipmentName,
                    row.OrderNumber,
                    row.DocumentPath);
                _grid.Rows[index].Tag = row;
                if (!string.IsNullOrWhiteSpace(selectedActId) &&
                    string.Equals(row.ActId, selectedActId, StringComparison.Ordinal))
                {
                    selectedIndex = index;
                }
            }

            if (_grid.Rows.Count > 0)
            {
                int index = selectedIndex >= 0 ? selectedIndex : 0;
                _grid.ClearSelection();
                _grid.Rows[index].Selected = true;
                _grid.CurrentCell = _grid.Rows[index].Cells[0];
            }

            UpdateButtonStates();
        }

        private KnowledgeBaseActJournalRow? GetSelectedRow()
        {
            if (_grid.SelectedRows.Count == 0)
                return null;

            return _grid.SelectedRows[0].Tag as KnowledgeBaseActJournalRow;
        }

        private void UpdateButtonStates()
        {
            KnowledgeBaseActJournalRow? row = GetSelectedRow();
            bool hasSelection = row != null;
            _btnOpen.Enabled = hasSelection;
            _btnGenerateDocument.Enabled = row?.CanGenerateDocument == true;
            _btnDeleteDraft.Enabled = row?.CanDeletePhysically == true;
            _btnCancelAct.Enabled = row?.CanChangeStatus == true;
            _btnAnnulAct.Enabled = row?.CanChangeStatus == true;
        }

        private void Complete(KnowledgeBaseActsJournalAction action)
        {
            KnowledgeBaseActJournalRow? row = GetSelectedRow();
            if (row == null)
                return;

            SelectedActId = row.ActId;
            SelectedAction = action;
            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
