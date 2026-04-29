using System.Globalization;
using AsutpKnowledgeBase.Services;

namespace AsutpKnowledgeBase
{
    public sealed class KnowledgeBaseMaintenanceWorkbookExportDialog : Form
    {
        private readonly Func<int, int, KnowledgeBaseMaintenanceMonthDemandSummary> _demandSummaryProvider;
        private readonly ComboBox _cmbMonth;
        private readonly NumericUpDown _numYear;
        private readonly NumericUpDown _numMonthlyBudget;
        private readonly Label _lblTo1Summary;
        private readonly Label _lblTo2Summary;
        private readonly Label _lblTo3Summary;
        private readonly Label _lblTotalSummary;
        private readonly Label _lblBudgetStatus;
        private readonly Button _btnContinue;
        private bool _isUpdatingBudgetValue;
        private bool _budgetEditedByUser;

        public KnowledgeBaseMaintenanceWorkbookExportDialog(
            string workshopName,
            int initialYear,
            int initialMonth,
            int initialMonthlyBudget,
            Func<int, int, KnowledgeBaseMaintenanceMonthDemandSummary> demandSummaryProvider)
        {
            _demandSummaryProvider = demandSummaryProvider
                ?? throw new ArgumentNullException(nameof(demandSummaryProvider));

            Text = "Сформировать график ТО";
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.Sizable;
            MaximizeBox = true;
            MinimizeBox = false;
            ShowInTaskbar = false;
            MinimumSize = new Size(720, 620);
            ClientSize = new Size(760, 640);

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                Padding = new Padding(12),
                ColumnCount = 2,
                RowCount = 7
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 210F));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            for (int rowIndex = 0; rowIndex < 6; rowIndex++)
                layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

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

            _cmbMonth = new ComboBox
            {
                Dock = DockStyle.Top,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            foreach (string monthName in GetMonthNames())
                _cmbMonth.Items.Add(monthName);
            _cmbMonth.SelectedIndex = Math.Clamp(initialMonth, 1, 12) - 1;
            _cmbMonth.SelectedIndexChanged += (_, _) => RefreshDemandSummary();

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
                Maximum = 100000,
                DecimalPlaces = 0,
                Value = Math.Min(100000, Math.Max(0, initialMonthlyBudget))
            };
            _numMonthlyBudget.ValueChanged += (_, _) =>
            {
                if (!_isUpdatingBudgetValue)
                    _budgetEditedByUser = true;

                UpdateBudgetStatus();
            };

            AddEditorRow(layout, 1, "Месяц", _cmbMonth);
            AddEditorRow(layout, 2, "Год", _numYear);
            AddEditorRow(layout, 3, "Доступный фонд часов цеха, ч", _numMonthlyBudget);

            var helpLabel = new Label
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                ForeColor = Color.DimGray,
                Margin = new Padding(0, 6, 0, 12),
                MaximumSize = new Size(700, 0),
                Text = "Нормы ТО1/ТО2/ТО3 задаются на узлах как трудоёмкость одной операции. Ниже показано, что реально попадает в выбранный месяц."
            };
            layout.Controls.Add(helpLabel, 0, 4);
            layout.SetColumnSpan(helpLabel, 2);

            var demandGroup = new GroupBox
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Padding = new Padding(10),
                Text = "Расчёт на выбранный месяц"
            };

            var demandLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                ColumnCount = 2
            };
            demandLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 180F));
            demandLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));

            AddValueRow(demandLayout, 0, "ТО1", out _lblTo1Summary);
            AddValueRow(demandLayout, 1, "ТО2", out _lblTo2Summary);
            AddValueRow(demandLayout, 2, "ТО3", out _lblTo3Summary);
            AddValueRow(demandLayout, 3, "Итого спрос", out _lblTotalSummary);
            AddValueRow(demandLayout, 4, "Статус фонда", out _lblBudgetStatus);

            demandGroup.Controls.Add(demandLayout);
            layout.Controls.Add(demandGroup, 0, 5);
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
            layout.Controls.Add(buttonsPanel, 0, 6);
            layout.SetColumnSpan(buttonsPanel, 2);

            Controls.Add(layout);

            AcceptButton = _btnContinue;
            CancelButton = btnCancel;

            RefreshDemandSummary();
        }

        public int SelectedMonth { get; private set; }

        public int SelectedYear { get; private set; }

        public int MonthlyBudgetHours { get; private set; }

        private void Submit()
        {
            SelectedMonth = _cmbMonth.SelectedIndex + 1;
            SelectedYear = Decimal.ToInt32(_numYear.Value);
            MonthlyBudgetHours = Decimal.ToInt32(_numMonthlyBudget.Value);

            DialogResult = DialogResult.OK;
            Close();
        }

        private void RefreshDemandSummary()
        {
            int selectedYear = Decimal.ToInt32(_numYear.Value);
            int selectedMonth = _cmbMonth.SelectedIndex + 1;
            KnowledgeBaseMaintenanceMonthDemandSummary summary = _demandSummaryProvider(selectedYear, selectedMonth);

            _lblTo1Summary.Text = BuildSummaryText(summary.To1ItemCount, summary.To1Hours);
            _lblTo2Summary.Text = BuildSummaryText(summary.To2ItemCount, summary.To2Hours);
            _lblTo3Summary.Text = BuildSummaryText(summary.To3ItemCount, summary.To3Hours);
            _lblTotalSummary.Text = BuildSummaryText(summary.TotalItemCount, summary.TotalHours);

            if (!_budgetEditedByUser)
            {
                _isUpdatingBudgetValue = true;
                _numMonthlyBudget.Value = Math.Min(_numMonthlyBudget.Maximum, Math.Max(_numMonthlyBudget.Minimum, summary.TotalHours));
                _isUpdatingBudgetValue = false;
            }

            UpdateBudgetStatus();
        }

        private void UpdateBudgetStatus()
        {
            int selectedYear = Decimal.ToInt32(_numYear.Value);
            int selectedMonth = _cmbMonth.SelectedIndex + 1;
            KnowledgeBaseMaintenanceMonthDemandSummary summary = _demandSummaryProvider(selectedYear, selectedMonth);
            int budgetHours = Decimal.ToInt32(_numMonthlyBudget.Value);
            int shortageHours = summary.TotalHours - budgetHours;

            if (summary.TotalHours == 0)
            {
                _lblBudgetStatus.Text = "В выбранном месяце работ по графику ТО нет.";
                _lblBudgetStatus.ForeColor = Color.DarkGreen;
                _btnContinue.Enabled = true;
                return;
            }

            if (shortageHours <= 0)
            {
                _lblBudgetStatus.Text = $"Фонд покрывает спрос месяца. Резерв: {Math.Abs(shortageHours)} ч.";
                _lblBudgetStatus.ForeColor = Color.DarkGreen;
                _btnContinue.Enabled = true;
                return;
            }

            _lblBudgetStatus.Text = $"Фонда недостаточно. Нехватка: {shortageHours} ч.";
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
                MaximumSize = new Size(420, 0),
                Margin = new Padding(0, 4, 0, 8)
            };

            layout.Controls.Add(label, 0, rowIndex);
            layout.Controls.Add(valueLabel, 1, rowIndex);
        }
    }
}
