using AsutpKnowledgeBase.Models;

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

        private readonly IReadOnlyDictionary<int, ProductionCalendarYearConfiguration> _yearConfigurations;

        public KnowledgeBaseRussianProductionCalendarService(
            IReadOnlyDictionary<int, IReadOnlyCollection<DateOnly>>? additionalNonWorkingDaysByYear = null)
        {
            _yearConfigurations = BuildYearConfiguration(additionalNonWorkingDaysByYear);
        }

        public KnowledgeBaseRussianProductionCalendarService(
            IEnumerable<KbProductionCalendarYear>? productionCalendarYears)
        {
            _yearConfigurations = BuildYearConfiguration(productionCalendarYears);
        }

        public bool HasConfiguredYear(int year) => _yearConfigurations.ContainsKey(year);

        public IReadOnlyList<int> GetConfiguredYears() =>
            _yearConfigurations.Keys
                .OrderBy(static year => year)
                .ToArray();

        public bool IsWorkingDay(DateOnly date)
        {
            EnsureConfiguredYear(date.Year);

            ProductionCalendarYearConfiguration configuration = _yearConfigurations[date.Year];
            if (configuration.AdditionalWorkingDays.Contains(date))
                return true;

            return !IsWeekend(date) &&
                   !IsFixedNonWorkingHoliday(date) &&
                   !configuration.AdditionalNonWorkingDays.Contains(date);
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

        private static IReadOnlyDictionary<int, ProductionCalendarYearConfiguration> BuildYearConfiguration(
            IReadOnlyDictionary<int, IReadOnlyCollection<DateOnly>>? overrides)
        {
            var configuredYears = CreateDefaultYearConfiguration();

            if (overrides == null)
                return configuredYears;

            foreach (var pair in overrides)
                configuredYears[pair.Key] = new ProductionCalendarYearConfiguration(
                    NormalizeYearDates(pair.Key, pair.Value),
                    new HashSet<DateOnly>());

            return configuredYears;
        }

        private static IReadOnlyDictionary<int, ProductionCalendarYearConfiguration> BuildYearConfiguration(
            IEnumerable<KbProductionCalendarYear>? productionCalendarYears)
        {
            var configuredYears = CreateDefaultYearConfiguration();
            if (productionCalendarYears == null)
                return configuredYears;

            foreach (KbProductionCalendarYear year in KnowledgeBaseDataService.NormalizeProductionCalendarYears(productionCalendarYears))
            {
                configuredYears[year.Year] = new ProductionCalendarYearConfiguration(
                    NormalizeYearDates(year.Year, year.AdditionalNonWorkingDays),
                    NormalizeYearDates(year.Year, year.AdditionalWorkingDays));
            }

            return configuredYears;
        }

        private static Dictionary<int, ProductionCalendarYearConfiguration> CreateDefaultYearConfiguration()
        {
            return KnowledgeBaseDataService.CreateDefaultProductionCalendarYears()
                .ToDictionary(
                    static year => year.Year,
                    static year => new ProductionCalendarYearConfiguration(
                        new HashSet<DateOnly>(year.AdditionalNonWorkingDays),
                        new HashSet<DateOnly>(year.AdditionalWorkingDays)));
        }

        public static IReadOnlyDictionary<int, IReadOnlyCollection<DateOnly>> ToAdditionalNonWorkingDaysByYear(
            IReadOnlyList<KbProductionCalendarYear>? productionCalendarYears)
        {
            if (productionCalendarYears == null)
                return new Dictionary<int, IReadOnlyCollection<DateOnly>>();

            return KnowledgeBaseDataService.NormalizeProductionCalendarYears(productionCalendarYears)
                .ToDictionary(
                    static year => year.Year,
                    static year => (IReadOnlyCollection<DateOnly>)year.AdditionalNonWorkingDays);
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
                        $"Дата {date:dd.MM.yyyy} не относится к {year} году.",
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

        private sealed class ProductionCalendarYearConfiguration
        {
            public ProductionCalendarYearConfiguration(
                HashSet<DateOnly> additionalNonWorkingDays,
                HashSet<DateOnly> additionalWorkingDays)
            {
                AdditionalNonWorkingDays = additionalNonWorkingDays;
                AdditionalWorkingDays = additionalWorkingDays;
            }

            public HashSet<DateOnly> AdditionalNonWorkingDays { get; }

            public HashSet<DateOnly> AdditionalWorkingDays { get; }
        }

        private void EnsureConfiguredYear(int year)
        {
            if (!HasConfiguredYear(year))
            {
                throw new InvalidOperationException(
                    $"Производственный календарь для {year} года ещё не настроен. " +
                    "Настройте его через меню \"Файл -> Производственный календарь...\" " +
                    "или импортируйте производственный календарь из PDF/JSON.");
            }
        }
    }
}
