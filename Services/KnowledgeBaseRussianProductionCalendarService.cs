namespace AsutpKnowledgeBase.Services
{
    public sealed class KnowledgeBaseRussianProductionCalendarService
    {
        private static readonly (int Month, int Day)[] FixedNonWorkingHolidays =
        {
            (1, 1),
            (1, 2),
            (1, 3),
            (1, 4),
            (1, 5),
            (1, 6),
            (1, 7),
            (1, 8),
            (2, 23),
            (3, 8),
            (5, 1),
            (5, 9),
            (6, 12),
            (11, 4)
        };

        private readonly IReadOnlyDictionary<int, HashSet<DateOnly>> _additionalNonWorkingDaysByYear;

        public KnowledgeBaseRussianProductionCalendarService(
            IReadOnlyDictionary<int, IReadOnlyCollection<DateOnly>>? additionalNonWorkingDaysByYear = null)
        {
            _additionalNonWorkingDaysByYear = BuildYearConfiguration(additionalNonWorkingDaysByYear);
        }

        public bool HasConfiguredYear(int year) => _additionalNonWorkingDaysByYear.ContainsKey(year);

        public IReadOnlyList<int> GetConfiguredYears() =>
            _additionalNonWorkingDaysByYear.Keys
                .OrderBy(static year => year)
                .ToArray();

        public bool IsWorkingDay(DateOnly date)
        {
            EnsureConfiguredYear(date.Year);

            return !IsWeekend(date) &&
                   !IsFixedNonWorkingHoliday(date) &&
                   !_additionalNonWorkingDaysByYear[date.Year].Contains(date);
        }

        public IReadOnlyList<DateOnly> GetWorkingDays(int year, int month)
        {
            if (month < 1 || month > 12)
                throw new ArgumentOutOfRangeException(nameof(month), month, "Месяц должен быть в диапазоне от 1 до 12.");

            EnsureConfiguredYear(year);

            int daysInMonth = DateTime.DaysInMonth(year, month);
            var workingDays = new List<DateOnly>(daysInMonth);

            for (int day = 1; day <= daysInMonth; day++)
            {
                var date = new DateOnly(year, month, day);
                if (IsWorkingDay(date))
                    workingDays.Add(date);
            }

            return workingDays;
        }

        public int CountWorkingDays(int year, int month) => GetWorkingDays(year, month).Count;

        private static IReadOnlyDictionary<int, HashSet<DateOnly>> BuildYearConfiguration(
            IReadOnlyDictionary<int, IReadOnlyCollection<DateOnly>>? overrides)
        {
            var configuredYears = CreateDefaultYearConfiguration();

            if (overrides == null)
                return configuredYears;

            foreach (var pair in overrides)
                configuredYears[pair.Key] = NormalizeYearDates(pair.Key, pair.Value);

            return configuredYears;
        }

        private static Dictionary<int, HashSet<DateOnly>> CreateDefaultYearConfiguration()
        {
            // Transfer days are stored as data per year so the monthly planner can
            // switch to a new official calendar without changing allocation logic.
            return new Dictionary<int, HashSet<DateOnly>>
            {
                [2025] = new HashSet<DateOnly>
                {
                    new(2025, 5, 2),
                    new(2025, 5, 8),
                    new(2025, 6, 13),
                    new(2025, 11, 3),
                    new(2025, 12, 31)
                },
                [2026] = new HashSet<DateOnly>
                {
                    new(2026, 1, 9),
                    new(2026, 3, 9),
                    new(2026, 5, 11),
                    new(2026, 12, 31)
                }
            };
        }

        private static HashSet<DateOnly> NormalizeYearDates(int year, IReadOnlyCollection<DateOnly>? dates)
        {
            var normalized = new HashSet<DateOnly>();
            if (dates == null)
                return normalized;

            foreach (var date in dates)
            {
                if (date.Year != year)
                {
                    throw new ArgumentException(
                        $"Дата {date:yyyy-MM-dd} не относится к {year} году.",
                        nameof(dates));
                }

                normalized.Add(date);
            }

            return normalized;
        }

        private static bool IsWeekend(DateOnly date) =>
            date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday;

        private static bool IsFixedNonWorkingHoliday(DateOnly date)
        {
            foreach (var holiday in FixedNonWorkingHolidays)
            {
                if (date.Month == holiday.Month && date.Day == holiday.Day)
                    return true;
            }

            return false;
        }

        private void EnsureConfiguredYear(int year)
        {
            if (!HasConfiguredYear(year))
            {
                throw new InvalidOperationException(
                    $"Производственный календарь для {year} года ещё не настроен.");
            }
        }
    }
}
