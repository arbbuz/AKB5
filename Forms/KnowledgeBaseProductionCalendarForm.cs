using System.Globalization;
using AsutpKnowledgeBase.Models;
using AsutpKnowledgeBase.Services;

namespace AsutpKnowledgeBase
{
    public sealed class KnowledgeBaseProductionCalendarForm : Form
    {
        private const string RussianDateFormat = "dd.MM.yyyy";
        private const string LegacyIsoDateFormat = "yyyy-MM-dd";
        private static readonly string[] DateFormats = { RussianDateFormat, LegacyIsoDateFormat };
        private static readonly char[] DateSeparators = { '\r', '\n', ',', ';', ' ', '\t' };
        private static readonly HashSet<int> BuiltInYears = KnowledgeBaseDataService
            .CreateDefaultProductionCalendarYears()
            .Select(static year => year.Year)
            .ToHashSet();

        private readonly List<KbProductionCalendarYear> _years;
        private readonly ListBox _lstYears = new();
        private readonly NumericUpDown _numYear = new();
        private readonly TextBox _txtDates = new();
        private readonly Label _lblYearTitle = new();
        private bool _isBinding;
        private int _selectedYearIndex = -1;

        public KnowledgeBaseProductionCalendarForm(IReadOnlyList<KbProductionCalendarYear>? productionCalendarYears)
        {
            _years = KnowledgeBaseDataService.NormalizeProductionCalendarYears(productionCalendarYears)
                .Select(CloneYear)
                .ToList();

            InitializeComponent();
            BindYears();
            if (_years.Count > 0)
                _lstYears.SelectedIndex = 0;
        }

        public List<KbProductionCalendarYear> ResultYears { get; private set; } = new();

        private void InitializeComponent()
        {
            Text = "Производственный календарь";
            StartPosition = FormStartPosition.CenterParent;
            MinimumSize = new Size(760, 520);
            Size = new Size(900, 620);

            var root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 2,
                Padding = new Padding(12)
            };
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 250));
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            var leftPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 5,
                Margin = new Padding(0, 0, 12, 0)
            };
            leftPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            leftPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            leftPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            leftPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            leftPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            leftPanel.Controls.Add(new Label
            {
                Text = "Годы",
                AutoSize = true,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                Margin = new Padding(0, 0, 0, 6)
            }, 0, 0);

            _lstYears.Dock = DockStyle.Fill;
            _lstYears.IntegralHeight = false;
            _lstYears.SelectedIndexChanged += LstYears_SelectedIndexChanged;
            leftPanel.Controls.Add(_lstYears, 0, 1);

            _numYear.Minimum = 1;
            _numYear.Maximum = 9999;
            _numYear.Value = DateTime.Now.Year + 1;
            _numYear.Dock = DockStyle.Top;
            _numYear.Margin = new Padding(0, 8, 0, 4);
            leftPanel.Controls.Add(_numYear, 0, 2);

            var btnAddYear = new Button
            {
                Text = "Добавить год",
                Dock = DockStyle.Top,
                Height = 32,
                Margin = new Padding(0, 0, 0, 4)
            };
            btnAddYear.Click += (_, _) => AddYear();
            leftPanel.Controls.Add(btnAddYear, 0, 3);

            var btnDeleteYear = new Button
            {
                Text = "Удалить год",
                Dock = DockStyle.Top,
                Height = 32
            };
            btnDeleteYear.Click += (_, _) => DeleteSelectedYear();
            leftPanel.Controls.Add(btnDeleteYear, 0, 4);

            var rightPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3
            };
            rightPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            rightPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            rightPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            _lblYearTitle.AutoSize = true;
            _lblYearTitle.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            _lblYearTitle.Margin = new Padding(0, 0, 0, 6);
            rightPanel.Controls.Add(_lblYearTitle, 0, 0);

            rightPanel.Controls.Add(new Label
            {
                Text = "Дополнительные нерабочие дни: одна дата в строке или через пробел/запятую. Формат: дд.мм.гггг.",
                AutoSize = true,
                Margin = new Padding(0, 0, 0, 8)
            }, 0, 1);

            _txtDates.Dock = DockStyle.Fill;
            _txtDates.Multiline = true;
            _txtDates.ScrollBars = ScrollBars.Vertical;
            _txtDates.AcceptsReturn = true;
            _txtDates.AcceptsTab = true;
            _txtDates.Font = new Font("Consolas", 10F);
            rightPanel.Controls.Add(_txtDates, 0, 2);

            var buttonPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.RightToLeft,
                AutoSize = true
            };

            var btnOk = new Button
            {
                Text = "OK",
                DialogResult = DialogResult.None,
                Width = 100,
                Height = 32
            };
            btnOk.Click += (_, _) => AcceptChanges();

            var btnCancel = new Button
            {
                Text = "Отмена",
                DialogResult = DialogResult.Cancel,
                Width = 100,
                Height = 32
            };

            buttonPanel.Controls.Add(btnOk);
            buttonPanel.Controls.Add(btnCancel);

            root.Controls.Add(leftPanel, 0, 0);
            root.Controls.Add(rightPanel, 1, 0);
            root.Controls.Add(buttonPanel, 0, 1);
            root.SetColumnSpan(buttonPanel, 2);

            Controls.Add(root);
            AcceptButton = btnOk;
            CancelButton = btnCancel;
        }

        private void BindYears()
        {
            _isBinding = true;
            try
            {
                _lstYears.Items.Clear();
                foreach (KbProductionCalendarYear year in _years.OrderBy(static year => year.Year))
                    _lstYears.Items.Add(year.Year.ToString(CultureInfo.InvariantCulture));
            }
            finally
            {
                _isBinding = false;
            }
        }

        private void LstYears_SelectedIndexChanged(object? sender, EventArgs e)
        {
            if (_isBinding)
                return;

            int nextIndex = _lstYears.SelectedIndex;
            if (_selectedYearIndex >= 0 && !TrySaveSelectedYear(showErrors: true))
            {
                _isBinding = true;
                _lstYears.SelectedIndex = _selectedYearIndex;
                _isBinding = false;
                return;
            }

            _selectedYearIndex = nextIndex;
            LoadSelectedYear();
        }

        private void LoadSelectedYear()
        {
            if (_selectedYearIndex < 0 || _selectedYearIndex >= _years.Count)
            {
                _lblYearTitle.Text = "Год не выбран";
                _txtDates.Text = string.Empty;
                _txtDates.Enabled = false;
                return;
            }

            KbProductionCalendarYear year = _years[_selectedYearIndex];
            _lblYearTitle.Text = $"Календарь {year.Year}";
            _txtDates.Enabled = true;
            _txtDates.Text = string.Join(
                Environment.NewLine,
                year.AdditionalNonWorkingDays
                    .OrderBy(static date => date)
                    .Select(static date => date.ToString(RussianDateFormat, CultureInfo.InvariantCulture)));
        }

        private void AddYear()
        {
            if (_selectedYearIndex >= 0 && !TrySaveSelectedYear(showErrors: true))
                return;

            int year = (int)_numYear.Value;
            int existingIndex = _years.FindIndex(item => item.Year == year);
            if (existingIndex < 0)
            {
                _years.Add(new KbProductionCalendarYear { Year = year });
                _years.Sort(static (left, right) => left.Year.CompareTo(right.Year));
                existingIndex = _years.FindIndex(item => item.Year == year);
                BindYears();
            }

            _lstYears.SelectedIndex = existingIndex;
        }

        private void DeleteSelectedYear()
        {
            if (_selectedYearIndex < 0 || _selectedYearIndex >= _years.Count)
                return;

            int selectedYear = _years[_selectedYearIndex].Year;
            if (BuiltInYears.Contains(selectedYear))
            {
                MessageBox.Show(
                    this,
                    $"Встроенный календарь для {selectedYear} года нельзя удалить. Можно изменить список дополнительных нерабочих дней.",
                    "Производственный календарь",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            int removedIndex = _selectedYearIndex;
            _years.RemoveAt(removedIndex);
            _selectedYearIndex = -1;
            BindYears();
            if (_years.Count > 0)
                _lstYears.SelectedIndex = Math.Min(removedIndex, _years.Count - 1);
            else
                LoadSelectedYear();
        }

        private void AcceptChanges()
        {
            if (_selectedYearIndex >= 0 && !TrySaveSelectedYear(showErrors: true))
                return;

            try
            {
                ResultYears = KnowledgeBaseDataService.NormalizeProductionCalendarYears(_years);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    this,
                    ex.Message,
                    "Производственный календарь",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            DialogResult = DialogResult.OK;
            Close();
        }

        private bool TrySaveSelectedYear(bool showErrors)
        {
            if (_selectedYearIndex < 0 || _selectedYearIndex >= _years.Count)
                return true;

            int year = _years[_selectedYearIndex].Year;
            if (!TryParseDates(year, _txtDates.Text, out List<DateOnly> dates, out string errorMessage))
            {
                if (showErrors)
                {
                    MessageBox.Show(
                        this,
                        errorMessage,
                        "Производственный календарь",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                }

                return false;
            }

            _years[_selectedYearIndex].AdditionalNonWorkingDays = dates;
            return true;
        }

        private static bool TryParseDates(
            int year,
            string text,
            out List<DateOnly> dates,
            out string errorMessage)
        {
            dates = new List<DateOnly>();
            errorMessage = string.Empty;
            var normalizedDates = new SortedSet<DateOnly>();
            string[] parts = (text ?? string.Empty)
                .Split(DateSeparators, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            foreach (string part in parts)
            {
                if (!DateOnly.TryParseExact(
                        part,
                        DateFormats,
                        CultureInfo.InvariantCulture,
                        DateTimeStyles.None,
                        out DateOnly date))
                {
                    errorMessage = $"Дата '{part}' указана в неверном формате. Используйте формат дд.мм.гггг.";
                    return false;
                }

                if (date.Year != year)
                {
                    errorMessage = $"Дата {date:dd.MM.yyyy} не относится к {year} году.";
                    return false;
                }

                normalizedDates.Add(date);
            }

            dates = normalizedDates.ToList();
            return true;
        }

        private static KbProductionCalendarYear CloneYear(KbProductionCalendarYear year) =>
            new()
            {
                Year = year.Year,
                AdditionalNonWorkingDays = (year.AdditionalNonWorkingDays ?? new List<DateOnly>())
                    .OrderBy(static date => date)
                    .ToList()
            };
    }
}
