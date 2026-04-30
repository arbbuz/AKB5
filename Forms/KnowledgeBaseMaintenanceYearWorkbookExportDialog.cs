using System.Globalization;
using AsutpKnowledgeBase.Services;

namespace AsutpKnowledgeBase
{
    public sealed class KnowledgeBaseMaintenanceYearWorkbookExportDialog : Form
    {
        private readonly Func<int, int, KnowledgeBaseMaintenanceMonthDemandSummary> _demandSummaryProvider;
        private readonly NumericUpDown _numYear;
        private readonly NumericUpDown _numMonthlyBudget;
        private readonly DataGridView _gridDemand;
        private readonly Label _lblYearSummary;
        private readonly Label _lblBudgetStatus;
        private readonly Button _btnContinue;
        private bool _isUpdatingBudgetValue;
        private bool _budgetEditedByUser;
        private int _maxMonthDemandHours;
        private int _totalYearDemandHours;

        public KnowledgeBaseMaintenanceYearWorkbookExportDialog(
            string workshopName,
            int initialYear,
            int initialMonthlyBudget,
            Func<int, int, KnowledgeBaseMaintenanceMonthDemandSummary> demandSummaryProvider)
        {
            _demandSummaryProvider = demandSummaryProvider
                ?? throw new ArgumentNullException(nameof(demandSummaryProvider));

            Text = "Сформировать годовой график ТО";
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.Sizable;
            MaximizeBox = true;
            MinimizeBox = false;
            ShowInTaskbar = false;
            MinimumSize = new Size(820, 620);
            ClientSize = new Size(880, 660);

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(12),
                ColumnCount = 2,
                RowCount = 6
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 220F));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            var lblWorkshopTitle = new Label
            {
                Text = "Текущий цех",
                AutoSize = true,
                Margin = new Padding(0, 6, 10, 10)
            };
            var lblWorkshopValue = new Label
            {
                Text = string.IsNullOrWhiteSpace(workshopName) ? "-" : workshopName,
                AutoSize = true,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                Margin = new Padding(0, 6, 0, 10)
            };
            layout.Controls.Add(lblWorkshopTitle, 0, 0);
            layout.Controls.Add(lblWorkshopValue, 1, 0);

            _numYear = new NumericUpDown
            {
                Dock = DockStyle.Top,
                Minimum = 2020,
                Maximum = 9999,
                DecimalPlaces = 0,
                Value = Math.Min(9999, Math.Max(2020, initialYear))
            };
            _numYear.ValueChanged += (_, _) => RefreshDemandSummary();

            _numMonthlyBudget = new NumericUpDown
            {
                Dock = DockStyle.Top,
                Minimum = 0,
                Maximum = 1000000,
                DecimalPlaces = 0,
                Value = Math.Min(1000000, Math.Max(0, initialMonthlyBudget))
            };
            _numMonthlyBudget.ValueChanged += (_, _) =>
            {
                if (!_isUpdatingBudgetValue)
                    _budgetEditedByUser = true;

                UpdateBudgetStatus();
            };

            AddEditorRow(layout, 1, "Год", _numYear);
            AddEditorRow(layout, 2, "Доступный месячный фонд цеха, ч", _numMonthlyBudget);

            var summaryLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                ColumnCount = 2,
                Margin = new Padding(0, 0, 0, 10)
            };
            summaryLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 220F));
            summaryLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            AddValueRow(summaryLayout, 0, "Итого за год", out _lblYearSummary);
            AddValueRow(summaryLayout, 1, "Статус фонда", out _lblBudgetStatus);
            layout.Controls.Add(summaryLayout, 0, 3);
            layout.SetColumnSpan(summaryLayout, 2);

            _gridDemand = new DataGridView
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToResizeRows = false,
                RowHeadersVisible = false,
                MultiSelect = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                BackgroundColor = SystemColors.Window,
                BorderStyle = BorderStyle.FixedSingle
            };
            _gridDemand.Columns.Add("Month", "Месяц");
            _gridDemand.Columns.Add("To1", "ТО1");
            _gridDemand.Columns.Add("To2", "ТО2");
            _gridDemand.Columns.Add("To3", "ТО3");
            _gridDemand.Columns.Add("Total", "Итого");
            _gridDemand.Columns["Month"]!.FillWeight = 140F;

            var demandGroup = new GroupBox
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(10),
                Text = "Расчёт по месяцам"
            };
            demandGroup.Controls.Add(_gridDemand);
            layout.Controls.Add(demandGroup, 0, 4);
            layout.SetColumnSpan(demandGroup, 2);

            var buttonsPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.RightToLeft,
                Margin = new Padding(0, 16, 0, 0)
            };

            _btnContinue = new Button
            {
                Text = "Продолжить",
                AutoSize = true
            };
            _btnContinue.Click += (_, _) => Submit();

            var btnCancel = new Button
            {
                Text = "Отмена",
                AutoSize = true,
                DialogResult = DialogResult.Cancel
            };

            buttonsPanel.Controls.Add(_btnContinue);
            buttonsPanel.Controls.Add(btnCancel);
            layout.Controls.Add(buttonsPanel, 0, 5);
            layout.SetColumnSpan(buttonsPanel, 2);

            Controls.Add(layout);

            AcceptButton = _btnContinue;
            CancelButton = btnCancel;

            RefreshDemandSummary();
        }

        public int SelectedYear { get; private set; }

        public int MonthlyBudgetHours { get; private set; }

        private void Submit()
        {
            SelectedYear = Decimal.ToInt32(_numYear.Value);
            MonthlyBudgetHours = Decimal.ToInt32(_numMonthlyBudget.Value);

            DialogResult = DialogResult.OK;
            Close();
        }

        private void RefreshDemandSummary()
        {
            int selectedYear = Decimal.ToInt32(_numYear.Value);
            var monthSummaries = new List<KnowledgeBaseMaintenanceMonthDemandSummary>(12);
            for (int month = 1; month <= 12; month++)
                monthSummaries.Add(_demandSummaryProvider(selectedYear, month));

            _maxMonthDemandHours = monthSummaries.Count == 0 ? 0 : monthSummaries.Max(static summary => summary.TotalHours);
            _totalYearDemandHours = monthSummaries.Sum(static summary => summary.TotalHours);

            _gridDemand.Rows.Clear();
            IReadOnlyList<string> monthNames = GetMonthNames();
            for (int month = 1; month <= 12; month++)
            {
                KnowledgeBaseMaintenanceMonthDemandSummary summary = monthSummaries[month - 1];
                int rowIndex = _gridDemand.Rows.Add(
                    monthNames[month - 1],
                    BuildSummaryText(summary.To1ItemCount, summary.To1Hours),
                    BuildSummaryText(summary.To2ItemCount, summary.To2Hours),
                    BuildSummaryText(summary.To3ItemCount, summary.To3Hours),
                    BuildSummaryText(summary.TotalItemCount, summary.TotalHours));
                _gridDemand.Rows[rowIndex].Tag = summary.TotalHours;
            }

            _lblYearSummary.Text = $"{_totalYearDemandHours} ч, максимум месяца: {_maxMonthDemandHours} ч";

            if (!_budgetEditedByUser)
            {
                _isUpdatingBudgetValue = true;
                _numMonthlyBudget.Value = Math.Min(_numMonthlyBudget.Maximum, Math.Max(_numMonthlyBudget.Minimum, _maxMonthDemandHours));
                _isUpdatingBudgetValue = false;
            }

            UpdateBudgetStatus();
        }

        private void UpdateBudgetStatus()
        {
            int budgetHours = Decimal.ToInt32(_numMonthlyBudget.Value);
            int shortageHours = _maxMonthDemandHours - budgetHours;

            foreach (DataGridViewRow row in _gridDemand.Rows)
            {
                int monthDemandHours = row.Tag is int demandHours ? demandHours : 0;
                row.DefaultCellStyle.BackColor = monthDemandHours > budgetHours
                    ? Color.MistyRose
                    : SystemColors.Window;
            }

            if (_totalYearDemandHours == 0)
            {
                _lblBudgetStatus.Text = "За выбранный год работ по графику ТО нет.";
                _lblBudgetStatus.ForeColor = Color.DarkGreen;
                _btnContinue.Enabled = true;
                return;
            }

            if (shortageHours <= 0)
            {
                _lblBudgetStatus.Text = $"Фонд покрывает все месяцы. Резерв для максимального месяца: {Math.Abs(shortageHours)} ч.";
                _lblBudgetStatus.ForeColor = Color.DarkGreen;
                _btnContinue.Enabled = true;
                return;
            }

            _lblBudgetStatus.Text = $"Фонд меньше максимального месячного спроса. Нехватка: {shortageHours} ч.";
            _lblBudgetStatus.ForeColor = Color.Firebrick;
            _btnContinue.Enabled = false;
        }

        private static string BuildSummaryText(int itemCount, int hours) =>
            $"{itemCount} ед. / {hours} ч";

        private static IReadOnlyList<string> GetMonthNames()
        {
            var monthNames = new List<string>(12);
            var culture = new CultureInfo("ru-RU");
            for (int month = 1; month <= 12; month++)
                monthNames.Add(culture.DateTimeFormat.GetMonthName(month));

            return monthNames;
        }

        private static void AddEditorRow(
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

        private static void AddValueRow(
            TableLayoutPanel layout,
            int rowIndex,
            string labelText,
            out Label valueLabel)
        {
            if (layout.RowCount <= rowIndex)
                layout.RowCount = rowIndex + 1;

            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            var label = new Label
            {
                Text = labelText,
                AutoSize = true,
                Margin = new Padding(0, 4, 12, 8)
            };

            valueLabel = new Label
            {
                AutoSize = true,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                MaximumSize = new Size(520, 0),
                Margin = new Padding(0, 4, 0, 8)
            };

            layout.Controls.Add(label, 0, rowIndex);
            layout.Controls.Add(valueLabel, 1, rowIndex);
        }
    }
}
