using AsutpKnowledgeBase.Models;

namespace AsutpKnowledgeBase
{
    public sealed class KnowledgeBaseMaintenanceScheduleProfileDialog : Form
    {
        private readonly string _maintenanceProfileId;
        private readonly string _ownerNodeId;
        private List<KbMaintenanceYearScheduleEntry> _yearScheduleEntries;
        private CheckBox _chkIncludedInSchedule = null!;
        private NumericUpDown _numTo1Hours = null!;
        private NumericUpDown _numTo2Hours = null!;
        private NumericUpDown _numTo3Hours = null!;
        private Label _yearScheduleStateLabel = null!;
        private readonly Dictionary<int, ComboBox> _yearScheduleEditors = new();
        private readonly Dictionary<int, NumericUpDown> _yearScheduleHourEditors = new();
        private readonly Dictionary<int, bool> _yearScheduleUsesProfileHours = new();
        private bool _isUpdatingYearScheduleHours;

        public KnowledgeBaseMaintenanceScheduleProfileDialog(
            string title,
            KbMaintenanceScheduleProfile? existingProfile = null)
        {
            _maintenanceProfileId = existingProfile?.MaintenanceProfileId?.Trim() ?? string.Empty;
            _ownerNodeId = existingProfile?.OwnerNodeId?.Trim() ?? string.Empty;
            _yearScheduleEntries = CloneYearScheduleEntries(existingProfile?.YearScheduleEntries);

            Text = title;
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            ClientSize = new Size(780, 630);

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(12),
                ColumnCount = 1,
                RowCount = 3
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 48F));

            layout.Controls.Add(CreateProfileEditor(existingProfile), 0, 0);
            layout.Controls.Add(CreateYearScheduleEditor(_yearScheduleEntries), 0, 1);

            var buttonsPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.RightToLeft,
                Margin = new Padding(0, 12, 0, 0),
                AutoSize = false,
                WrapContents = false
            };

            var btnSaveProfile = new Button
            {
                Text = "Сохранить профиль",
                AutoSize = true
            };
            btnSaveProfile.Click += (_, _) => Submit(_yearScheduleEntries);

            var btnCancel = new Button
            {
                Text = "Отмена",
                AutoSize = true,
                DialogResult = DialogResult.Cancel
            };

            buttonsPanel.Controls.Add(btnSaveProfile);
            buttonsPanel.Controls.Add(btnCancel);
            layout.Controls.Add(buttonsPanel, 0, 2);

            Controls.Add(layout);

            AcceptButton = btnSaveProfile;
            CancelButton = btnCancel;
        }

        public KbMaintenanceScheduleProfile Result { get; private set; } = new();

        private Control CreateProfileEditor(KbMaintenanceScheduleProfile? existingProfile)
        {
            var group = new GroupBox
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Padding = new Padding(10),
                Text = "Параметры профиля",
                Margin = new Padding(0, 0, 0, 12)
            };

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                ColumnCount = 2
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 175F));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));

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
            _numTo1Hours.ValueChanged += (_, _) => RefreshProfileNormYearScheduleHours(KbMaintenanceWorkKind.To1);
            _numTo2Hours.ValueChanged += (_, _) => RefreshProfileNormYearScheduleHours(KbMaintenanceWorkKind.To2);
            _numTo3Hours.ValueChanged += (_, _) => RefreshProfileNormYearScheduleHours(KbMaintenanceWorkKind.To3);

            AddHoursRow(layout, 1, "Норма часов ТО1", _numTo1Hours);
            AddHoursRow(layout, 2, "Норма часов ТО2", _numTo2Hours);
            AddHoursRow(layout, 3, "Норма часов ТО3", _numTo3Hours);

            group.Controls.Add(layout);
            return group;
        }

        private void Submit(IReadOnlyList<KbMaintenanceYearScheduleEntry> yearScheduleEntries)
        {
            Result = new KbMaintenanceScheduleProfile
            {
                MaintenanceProfileId = _maintenanceProfileId,
                OwnerNodeId = _ownerNodeId,
                IsIncludedInSchedule = _chkIncludedInSchedule.Checked,
                To1Hours = Decimal.ToInt32(_numTo1Hours.Value),
                To2Hours = Decimal.ToInt32(_numTo2Hours.Value),
                To3Hours = Decimal.ToInt32(_numTo3Hours.Value),
                YearScheduleEntries = CloneYearScheduleEntries(yearScheduleEntries)
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
                Text = "Годовой план ТО"
            };

            var rootLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                ColumnCount = 1
            };
            rootLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));

            _yearScheduleStateLabel = new Label
            {
                Text = FormatYearScheduleMode(existingEntries),
                AutoSize = true,
                Margin = new Padding(0, 0, 0, 8)
            };
            rootLayout.Controls.Add(_yearScheduleStateLabel, 0, 0);

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                ColumnCount = 7,
                RowCount = 7,
                GrowStyle = TableLayoutPanelGrowStyle.FixedSize
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 96F));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 112F));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 62F));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 24F));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 96F));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 112F));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 62F));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 24F));
            for (int row = 1; row <= 6; row++)
                layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30F));

            AddYearScheduleHeader(layout, 0, 0);
            AddYearScheduleHeader(layout, 4, 0);

            Dictionary<int, KbMaintenanceYearScheduleEntry> existingByMonth = (existingEntries ?? Array.Empty<KbMaintenanceYearScheduleEntry>())
                .Where(static entry => entry.Month >= 1 &&
                                       entry.Month <= 12 &&
                                       Enum.IsDefined(typeof(KbMaintenanceWorkKind), entry.WorkKind))
                .GroupBy(static entry => entry.Month)
                .ToDictionary(static group => group.Key, static group => group.Last());

            for (int month = 1; month <= 12; month++)
            {
                int row = ((month - 1) % 6) + 1;
                int columnOffset = month <= 6 ? 0 : 4;
                layout.Controls.Add(CreateMonthLabel(month), columnOffset, row);

                ComboBox editor = CreateWorkKindEditor();
                int hours = 0;
                bool usesProfileHours = false;
                if (existingByMonth.TryGetValue(month, out KbMaintenanceYearScheduleEntry? existingEntry))
                {
                    SelectWorkKind(editor, existingEntry.WorkKind);
                    usesProfileHours = existingEntry.Hours <= 0;
                    hours = usesProfileHours
                        ? GetProfileNormHours(existingEntry.WorkKind)
                        : Math.Max(0, existingEntry.Hours);
                }
                else
                {
                    editor.SelectedIndex = 0;
                }

                layout.Controls.Add(editor, columnOffset + 1, row);
                _yearScheduleEditors[month] = editor;

                NumericUpDown hoursEditor = CreateYearScheduleHoursEditor(hours);
                layout.Controls.Add(hoursEditor, columnOffset + 2, row);
                _yearScheduleHourEditors[month] = hoursEditor;
                _yearScheduleUsesProfileHours[month] = usesProfileHours;

                int monthKey = month;
                hoursEditor.ValueChanged += (_, _) =>
                {
                    if (!_isUpdatingYearScheduleHours && hoursEditor.Enabled)
                        _yearScheduleUsesProfileHours[monthKey] = false;
                };
                editor.SelectedIndexChanged += (_, _) => UpdateYearScheduleHourEditor(monthKey, resetToProfileNorm: true);
                UpdateYearScheduleHourEditor(month, resetToProfileNorm: usesProfileHours);
            }

            rootLayout.Controls.Add(layout, 0, 1);

            var buttonsPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                FlowDirection = FlowDirection.RightToLeft,
                AutoSize = true,
                Margin = new Padding(0, 12, 0, 0)
            };

            var btnSaveYearSchedule = new Button
            {
                Text = "Применить годовой план",
                AutoSize = true
            };
            btnSaveYearSchedule.Click += (_, _) => ApplyYearSchedule(BuildYearScheduleEntries());

            var btnClearYearSchedule = new Button
            {
                Text = "Очистить годовой план",
                AutoSize = true
            };
            btnClearYearSchedule.Click += (_, _) => ClearYearSchedule();

            buttonsPanel.Controls.Add(btnSaveYearSchedule);
            buttonsPanel.Controls.Add(btnClearYearSchedule);
            rootLayout.Controls.Add(buttonsPanel, 0, 2);

            group.Controls.Add(rootLayout);
            return group;
        }

        private List<KbMaintenanceYearScheduleEntry> BuildYearScheduleEntries()
        {
            return _yearScheduleEditors
                .OrderBy(static pair => pair.Key)
                .Select(pair => new
                {
                    Month = pair.Key,
                    Option = (WorkKindOption)pair.Value.SelectedItem!
                })
                .Where(static pair => pair.Option.WorkKind.HasValue)
                .Select(pair => new KbMaintenanceYearScheduleEntry
                {
                    Month = pair.Month,
                    WorkKind = pair.Option.WorkKind!.Value,
                    Hours = _yearScheduleHourEditors.TryGetValue(pair.Month, out NumericUpDown? hoursEditor)
                        ? ResolveYearScheduleHours(pair.Month, hoursEditor)
                        : 0
                })
                .ToList();
        }

        private void ApplyYearSchedule(IReadOnlyList<KbMaintenanceYearScheduleEntry> yearScheduleEntries)
        {
            _yearScheduleEntries = CloneYearScheduleEntries(yearScheduleEntries);
            _yearScheduleStateLabel.Text = FormatYearScheduleMode(_yearScheduleEntries);
        }

        private void ClearYearSchedule()
        {
            foreach (ComboBox editor in _yearScheduleEditors.Values)
                editor.SelectedIndex = 0;

            foreach (NumericUpDown hoursEditor in _yearScheduleHourEditors.Values)
                SetYearScheduleHourValue(hoursEditor, 0);

            _yearScheduleUsesProfileHours.Clear();

            ApplyYearSchedule(Array.Empty<KbMaintenanceYearScheduleEntry>());
        }

        private void UpdateYearScheduleHourEditor(int month, bool resetToProfileNorm = false)
        {
            if (!_yearScheduleEditors.TryGetValue(month, out ComboBox? editor) ||
                !_yearScheduleHourEditors.TryGetValue(month, out NumericUpDown? hoursEditor))
            {
                return;
            }

            var option = (WorkKindOption)editor.SelectedItem!;
            bool enabled = option.WorkKind.HasValue;
            hoursEditor.Enabled = enabled;
            if (!enabled)
            {
                _yearScheduleUsesProfileHours[month] = false;
                SetYearScheduleHourValue(hoursEditor, 0);
                return;
            }

            if (resetToProfileNorm || IsUsingProfileHours(month))
            {
                _yearScheduleUsesProfileHours[month] = true;
                SetYearScheduleHourValue(hoursEditor, GetProfileNormHours(option.WorkKind.Value));
            }
        }

        private int ResolveYearScheduleHours(int month, NumericUpDown hoursEditor) =>
            IsUsingProfileHours(month)
                ? 0
                : Decimal.ToInt32(hoursEditor.Value);

        private bool IsUsingProfileHours(int month) =>
            _yearScheduleUsesProfileHours.TryGetValue(month, out bool usesProfileHours) && usesProfileHours;

        private void RefreshProfileNormYearScheduleHours(KbMaintenanceWorkKind workKind)
        {
            foreach ((int month, ComboBox editor) in _yearScheduleEditors)
            {
                if (!IsUsingProfileHours(month) ||
                    editor.SelectedItem is not WorkKindOption option ||
                    option.WorkKind != workKind ||
                    !_yearScheduleHourEditors.TryGetValue(month, out NumericUpDown? hoursEditor))
                {
                    continue;
                }

                SetYearScheduleHourValue(hoursEditor, GetProfileNormHours(workKind));
            }
        }

        private int GetProfileNormHours(KbMaintenanceWorkKind workKind) =>
            workKind switch
            {
                KbMaintenanceWorkKind.To1 => Decimal.ToInt32(_numTo1Hours.Value),
                KbMaintenanceWorkKind.To2 => Decimal.ToInt32(_numTo2Hours.Value),
                KbMaintenanceWorkKind.To3 => Decimal.ToInt32(_numTo3Hours.Value),
                _ => 0
            };

        private void SetYearScheduleHourValue(NumericUpDown hoursEditor, int value)
        {
            _isUpdatingYearScheduleHours = true;
            try
            {
                hoursEditor.Value = Math.Min(999, Math.Max(0, value));
            }
            finally
            {
                _isUpdatingYearScheduleHours = false;
            }
        }

        private static Label CreateMonthLabel(int month) =>
            new()
            {
                Text = GetMonthName(month),
                AutoSize = false,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                Margin = new Padding(0, 1, 8, 3)
            };

        private static Label CreateHeaderLabel(string text) =>
            new()
            {
                Text = text,
                AutoSize = false,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                Font = new Font(SystemFonts.DefaultFont, FontStyle.Bold),
                Margin = new Padding(0, 0, 8, 4)
            };

        private static void AddYearScheduleHeader(TableLayoutPanel layout, int columnOffset, int row)
        {
            layout.Controls.Add(CreateHeaderLabel("Месяц"), columnOffset, row);
            layout.Controls.Add(CreateHeaderLabel("Вид ТО"), columnOffset + 1, row);
            layout.Controls.Add(CreateHeaderLabel("Часы"), columnOffset + 2, row);
        }

        private static ComboBox CreateWorkKindEditor()
        {
            var editor = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 1, 8, 3)
            };
            editor.Items.Add(new WorkKindOption(null, "Авто"));
            editor.Items.Add(new WorkKindOption(KbMaintenanceWorkKind.To1, "ТО1"));
            editor.Items.Add(new WorkKindOption(KbMaintenanceWorkKind.To2, "ТО2"));
            editor.Items.Add(new WorkKindOption(KbMaintenanceWorkKind.To3, "ТО3"));
            return editor;
        }

        private static NumericUpDown CreateYearScheduleHoursEditor(int value) =>
            new()
            {
                Dock = DockStyle.Fill,
                Minimum = 0,
                Maximum = 999,
                DecimalPlaces = 0,
                Value = Math.Min(999, Math.Max(0, value)),
                Margin = new Padding(0, 1, 0, 3)
            };

        private static void SelectWorkKind(ComboBox editor, KbMaintenanceWorkKind workKind)
        {
            foreach (object item in editor.Items)
            {
                if (item is WorkKindOption option && option.WorkKind == workKind)
                {
                    editor.SelectedItem = option;
                    return;
                }
            }

            editor.SelectedIndex = 0;
        }

        private static List<KbMaintenanceYearScheduleEntry> CloneYearScheduleEntries(
            IReadOnlyList<KbMaintenanceYearScheduleEntry>? entries)
        {
            var clones = new List<KbMaintenanceYearScheduleEntry>();
            foreach (KbMaintenanceYearScheduleEntry entry in entries ?? Array.Empty<KbMaintenanceYearScheduleEntry>())
            {
                if (entry == null ||
                    entry.Month is < 1 or > 12 ||
                    !Enum.IsDefined(typeof(KbMaintenanceWorkKind), entry.WorkKind))
                {
                    continue;
                }

                clones.Add(new KbMaintenanceYearScheduleEntry
                {
                    Month = entry.Month,
                    WorkKind = entry.WorkKind,
                    Hours = Math.Max(0, entry.Hours)
                });
            }

            return clones
                .GroupBy(static entry => entry.Month)
                .Select(static group => group.Last())
                .OrderBy(static entry => entry.Month)
                .ToList();
        }

        private static string FormatYearScheduleMode(IReadOnlyList<KbMaintenanceYearScheduleEntry>? entries)
        {
            List<KbMaintenanceYearScheduleEntry> normalizedEntries = CloneYearScheduleEntries(entries);
            if (normalizedEntries.Count == 0)
                return "Годовой план: автоматически";

            int hoursCount = normalizedEntries.Count(static entry => entry.Hours > 0);
            return hoursCount > 0
                ? $"Годовой план: вручную, {normalizedEntries.Count} мес.; часы заданы для {hoursCount} мес."
                : $"Годовой план: вручную, {normalizedEntries.Count} мес.; часы берутся из норм профиля";
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
            public WorkKindOption(KbMaintenanceWorkKind? workKind, string text)
            {
                WorkKind = workKind;
                Text = text;
            }

            public KbMaintenanceWorkKind? WorkKind { get; }

            public string Text { get; }

            public override string ToString() => Text;

            public override bool Equals(object? obj) =>
                obj is WorkKindOption option && option.WorkKind == WorkKind;

            public override int GetHashCode() => WorkKind.HasValue ? (int)WorkKind.Value : 0;
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
