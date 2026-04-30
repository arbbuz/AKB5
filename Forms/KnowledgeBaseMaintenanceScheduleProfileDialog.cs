using AsutpKnowledgeBase.Models;

namespace AsutpKnowledgeBase
{
    public sealed class KnowledgeBaseMaintenanceScheduleProfileDialog : Form
    {
        private readonly string _maintenanceProfileId;
        private readonly string _ownerNodeId;
        private CheckBox _chkIncludedInSchedule = null!;
        private CheckBox _chkUseYearSchedule = null!;
        private NumericUpDown _numTo1Hours = null!;
        private NumericUpDown _numTo2Hours = null!;
        private NumericUpDown _numTo3Hours = null!;
        private readonly Dictionary<int, ComboBox> _yearScheduleEditors = new();

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
            ClientSize = new Size(620, 470);

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(12),
                ColumnCount = 2,
                RowCount = 7
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 175F));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            for (int rowIndex = 0; rowIndex < 6; rowIndex++)
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

            _chkUseYearSchedule = new CheckBox
            {
                Text = "Использовать ручное годовое размещение ТО",
                AutoSize = true,
                Checked = existingProfile?.YearScheduleEntries?.Count > 0,
                Margin = new Padding(0, 8, 0, 8)
            };
            _chkUseYearSchedule.CheckedChanged += (_, _) => UpdateYearScheduleEditorsEnabled();
            layout.Controls.Add(_chkUseYearSchedule, 0, 4);
            layout.SetColumnSpan(_chkUseYearSchedule, 2);

            Control yearScheduleControl = CreateYearScheduleEditor(existingProfile?.YearScheduleEntries);
            layout.Controls.Add(yearScheduleControl, 0, 5);
            layout.SetColumnSpan(yearScheduleControl, 2);

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
            layout.Controls.Add(buttonsPanel, 0, 6);
            layout.SetColumnSpan(buttonsPanel, 2);

            Controls.Add(layout);

            AcceptButton = btnOk;
            CancelButton = btnCancel;
            UpdateYearScheduleEditorsEnabled();
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
                To3Hours = Decimal.ToInt32(_numTo3Hours.Value),
                YearScheduleEntries = BuildYearScheduleEntries()
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

        private Control CreateYearScheduleEditor(IReadOnlyList<KbMaintenanceYearScheduleEntry>? existingEntries)
        {
            var group = new GroupBox
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Padding = new Padding(10),
                Text = "Годовое размещение"
            };

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                ColumnCount = 4
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 85F));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 85F));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));

            Dictionary<int, KbMaintenanceWorkKind> existingByMonth = (existingEntries ?? Array.Empty<KbMaintenanceYearScheduleEntry>())
                .Where(static entry => entry.Month >= 1 &&
                                       entry.Month <= 12 &&
                                       Enum.IsDefined(typeof(KbMaintenanceWorkKind), entry.WorkKind))
                .GroupBy(static entry => entry.Month)
                .ToDictionary(static group => group.Key, static group => group.Last().WorkKind);

            for (int month = 1; month <= 12; month++)
            {
                int row = (month - 1) / 2;
                int columnOffset = month % 2 == 1 ? 0 : 2;
                if (layout.RowCount <= row)
                    layout.RowCount = row + 1;

                layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
                layout.Controls.Add(CreateMonthLabel(month), columnOffset, row);

                ComboBox editor = CreateWorkKindEditor();
                KbMaintenanceWorkKind selectedWorkKind =
                    existingByMonth.TryGetValue(month, out KbMaintenanceWorkKind workKind)
                        ? workKind
                        : KbMaintenanceWorkKind.To1;
                editor.SelectedIndex = Math.Clamp((int)selectedWorkKind - 1, 0, editor.Items.Count - 1);
                layout.Controls.Add(editor, columnOffset + 1, row);
                _yearScheduleEditors[month] = editor;
            }

            group.Controls.Add(layout);
            return group;
        }

        private List<KbMaintenanceYearScheduleEntry> BuildYearScheduleEntries()
        {
            if (!_chkUseYearSchedule.Checked)
                return new List<KbMaintenanceYearScheduleEntry>();

            return _yearScheduleEditors
                .OrderBy(static pair => pair.Key)
                .Select(static pair => new KbMaintenanceYearScheduleEntry
                {
                    Month = pair.Key,
                    WorkKind = ((WorkKindOption)pair.Value.SelectedItem!).WorkKind
                })
                .ToList();
        }

        private void UpdateYearScheduleEditorsEnabled()
        {
            bool enabled = _chkUseYearSchedule.Checked;
            foreach (ComboBox editor in _yearScheduleEditors.Values)
                editor.Enabled = enabled;
        }

        private static Label CreateMonthLabel(int month) =>
            new()
            {
                Text = GetMonthName(month),
                AutoSize = true,
                Margin = new Padding(0, 6, 10, 8)
            };

        private static ComboBox CreateWorkKindEditor()
        {
            var editor = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Dock = DockStyle.Top,
                Margin = new Padding(0, 0, 10, 8)
            };
            editor.Items.Add(new WorkKindOption(KbMaintenanceWorkKind.To1, "ТО1"));
            editor.Items.Add(new WorkKindOption(KbMaintenanceWorkKind.To2, "ТО2"));
            editor.Items.Add(new WorkKindOption(KbMaintenanceWorkKind.To3, "ТО3"));
            return editor;
        }

        private static string GetMonthName(int month) =>
            month switch
            {
                1 => "Январь",
                2 => "Февраль",
                3 => "Март",
                4 => "Апрель",
                5 => "Май",
                6 => "Июнь",
                7 => "Июль",
                8 => "Август",
                9 => "Сентябрь",
                10 => "Октябрь",
                11 => "Ноябрь",
                12 => "Декабрь",
                _ => month.ToString()
            };

        private sealed class WorkKindOption
        {
            public WorkKindOption(KbMaintenanceWorkKind workKind, string text)
            {
                WorkKind = workKind;
                Text = text;
            }

            public KbMaintenanceWorkKind WorkKind { get; }

            public string Text { get; }

            public override string ToString() => Text;

            public override bool Equals(object? obj) =>
                obj is WorkKindOption option && option.WorkKind == WorkKind;

            public override int GetHashCode() => (int)WorkKind;
        }

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
