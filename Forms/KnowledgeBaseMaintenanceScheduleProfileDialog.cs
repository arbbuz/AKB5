using AsutpKnowledgeBase.Models;

namespace AsutpKnowledgeBase
{
    public sealed class KnowledgeBaseMaintenanceScheduleProfileDialog : Form
    {
        private readonly string _maintenanceProfileId;
        private readonly string _ownerNodeId;
        private CheckBox _chkIncludedInSchedule = null!;
        private NumericUpDown _numTo1Hours = null!;
        private NumericUpDown _numTo2Hours = null!;
        private NumericUpDown _numTo3Hours = null!;

        public KnowledgeBaseMaintenanceScheduleProfileDialog(
            string title,
            KbMaintenanceScheduleProfile? existingProfile = null)
        {
            _maintenanceProfileId = existingProfile?.MaintenanceProfileId?.Trim() ?? string.Empty;
            _ownerNodeId = existingProfile?.OwnerNodeId?.Trim() ?? string.Empty;

            Text = title;
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            ClientSize = new Size(430, 250);

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(12),
                ColumnCount = 2,
                RowCount = 5
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 175F));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            for (int rowIndex = 0; rowIndex < 4; rowIndex++)
                layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            _chkIncludedInSchedule = new CheckBox
            {
                Text = "Включать узел в график ТО",
                AutoSize = true,
                Checked = existingProfile?.IsIncludedInSchedule ?? true,
                Margin = new Padding(0, 4, 0, 12)
            };
            layout.Controls.Add(_chkIncludedInSchedule, 0, 0);
            layout.SetColumnSpan(_chkIncludedInSchedule, 2);

            _numTo1Hours = CreateHoursEditor(existingProfile?.To1Hours ?? 0);
            _numTo2Hours = CreateHoursEditor(existingProfile?.To2Hours ?? 0);
            _numTo3Hours = CreateHoursEditor(existingProfile?.To3Hours ?? 0);

            AddHoursRow(layout, 1, "Норма часов ТО1", _numTo1Hours);
            AddHoursRow(layout, 2, "Норма часов ТО2", _numTo2Hours);
            AddHoursRow(layout, 3, "Норма часов ТО3", _numTo3Hours);

            var buttonsPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.RightToLeft,
                Margin = new Padding(0, 16, 0, 0)
            };

            var btnOk = new Button
            {
                Text = "Сохранить",
                AutoSize = true
            };
            btnOk.Click += (_, _) => Submit();

            var btnCancel = new Button
            {
                Text = "Отмена",
                AutoSize = true,
                DialogResult = DialogResult.Cancel
            };

            buttonsPanel.Controls.Add(btnOk);
            buttonsPanel.Controls.Add(btnCancel);
            layout.Controls.Add(buttonsPanel, 0, 4);
            layout.SetColumnSpan(buttonsPanel, 2);

            Controls.Add(layout);

            AcceptButton = btnOk;
            CancelButton = btnCancel;
        }

        public KbMaintenanceScheduleProfile Result { get; private set; } = new();

        private void Submit()
        {
            Result = new KbMaintenanceScheduleProfile
            {
                MaintenanceProfileId = _maintenanceProfileId,
                OwnerNodeId = _ownerNodeId,
                IsIncludedInSchedule = _chkIncludedInSchedule.Checked,
                To1Hours = Decimal.ToInt32(_numTo1Hours.Value),
                To2Hours = Decimal.ToInt32(_numTo2Hours.Value),
                To3Hours = Decimal.ToInt32(_numTo3Hours.Value)
            };

            DialogResult = DialogResult.OK;
            Close();
        }

        private static NumericUpDown CreateHoursEditor(int value) =>
            new()
            {
                Dock = DockStyle.Top,
                Minimum = 0,
                Maximum = 999,
                DecimalPlaces = 0,
                Value = Math.Min(999, Math.Max(0, value))
            };

        private static void AddHoursRow(
            TableLayoutPanel layout,
            int rowIndex,
            string labelText,
            Control editor)
        {
            var label = new Label
            {
                Text = labelText,
                AutoSize = true,
                Margin = new Padding(0, 6, 10, 10)
            };

            editor.Margin = new Padding(0, 0, 0, 8);
            layout.Controls.Add(label, 0, rowIndex);
            layout.Controls.Add(editor, 1, rowIndex);
        }
    }
}
