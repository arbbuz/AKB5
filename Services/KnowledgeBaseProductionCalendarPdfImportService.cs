using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using AsutpKnowledgeBase.Models;
using UglyToad.PdfPig;
using UglyToad.PdfPig.DocumentLayoutAnalysis.TextExtractor;

namespace AsutpKnowledgeBase.Services
{
    public sealed class KnowledgeBaseProductionCalendarPdfImportResult
    {
        public bool IsSuccess { get; init; }

        public string ErrorMessage { get; init; } = string.Empty;

        public List<KbProductionCalendarYear> ProductionCalendarYears { get; init; } = new();

        public int ImportedYearCount { get; init; }

        public List<string> Warnings { get; init; } = new();
    }

    public sealed class KnowledgeBaseProductionCalendarPdfImportService
    {
        private static readonly RegexOptions CalendarRegexOptions =
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled;

        private static readonly Regex YearRegex = new(
            @"производственный\s+календарь\s+на\s+(?<year>\d{4})\s+год",
            CalendarRegexOptions);

        private static readonly Regex FallbackYearRegex = new(
            @"\bна\s+(?<year>\d{4})\s+год\b",
            CalendarRegexOptions);

        private static readonly Regex TransferRegex = new(
            @"[сc]\s+(?<sourceWeekday>понедельника|вторника|среды|четверга|пятницы|субботы|воскресенья)\s+(?<sourceDay>\d{1,2})\s+(?<sourceMonth>[а-яё]+)\s+на\s+(?<targetWeekday>понедельник|вторник|среду|четверг|пятницу|субботу|воскресенье)\s+(?<targetDay>\d{1,2})\s+(?<targetMonth>[а-яё]+)",
            CalendarRegexOptions);

        private static readonly Regex CrossYearRestPeriodRegex = new(
            @"[сc]\s+(?<startDay>\d{1,2})\s+(?<startMonth>[а-яё]+)\s+(?<startYear>\d{4})\s+года\s+по\s+(?<endDay>\d{1,2})\s+(?<endMonth>[а-яё]+)\s+(?<endYear>\d{4})\s+года",
            CalendarRegexOptions);

        private static readonly Regex SameMonthRestPeriodRegex = new(
            @"[сc]\s+(?<startDay>\d{1,2})\s+по\s+(?<endDay>\d{1,2})\s+(?<month>[а-яё]+)(?:\s+(?<year>\d{4})\s+года)?",
            CalendarRegexOptions);

        private static readonly Dictionary<string, int> MonthNumbers =
            new(StringComparer.OrdinalIgnoreCase)
            {
                ["январь"] = 1,
                ["января"] = 1,
                ["февраль"] = 2,
                ["февраля"] = 2,
                ["март"] = 3,
                ["марта"] = 3,
                ["апрель"] = 4,
                ["апреля"] = 4,
                ["май"] = 5,
                ["мая"] = 5,
                ["июнь"] = 6,
                ["июня"] = 6,
                ["июль"] = 7,
                ["июля"] = 7,
                ["август"] = 8,
                ["августа"] = 8,
                ["сентябрь"] = 9,
                ["сентября"] = 9,
                ["октябрь"] = 10,
                ["октября"] = 10,
                ["ноябрь"] = 11,
                ["ноября"] = 11,
                ["декабрь"] = 12,
                ["декабря"] = 12
            };

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

        public KnowledgeBaseProductionCalendarPdfImportResult ImportPdf(byte[]? pdfBytes)
        {
            if (pdfBytes == null || pdfBytes.Length == 0)
                return Failure("Файл PDF с производственным календарём не был передан.");

            try
            {
                string text = ExtractText(pdfBytes);
                return ImportText(text);
            }
            catch (Exception ex)
            {
                return Failure($"Не удалось импортировать производственный календарь из PDF: {ex.Message}");
            }
        }

        public KnowledgeBaseProductionCalendarPdfImportResult ImportText(string? text)
        {
            string normalizedText = NormalizeText(text);
            if (string.IsNullOrWhiteSpace(normalizedText))
                return Failure("В PDF не найден текстовый слой производственного календаря.");

            if (!TryReadYear(normalizedText, out int year))
                return Failure("Не удалось определить год производственного календаря в PDF.");

            var additionalNonWorkingDays = new SortedSet<DateOnly>();
            var additionalWorkingDays = new SortedSet<DateOnly>();
            var warnings = new List<string>();

            ReadTransfers(normalizedText, year, additionalNonWorkingDays, additionalWorkingDays, warnings);
            ReadRestPeriods(normalizedText, year, additionalNonWorkingDays, warnings);
            additionalNonWorkingDays.ExceptWith(additionalWorkingDays);

            if (additionalNonWorkingDays.Count == 0 && additionalWorkingDays.Count == 0)
            {
                return Failure(
                    $"В PDF найден {year} год, но не найдены переносы или дополнительные дни производственного календаря.");
            }

            var importedYear = new KbProductionCalendarYear
            {
                Year = year,
                AdditionalNonWorkingDays = additionalNonWorkingDays.ToList(),
                AdditionalWorkingDays = additionalWorkingDays.ToList()
            };

            ValidateWorkingDayCount(normalizedText, importedYear, warnings);

            return new KnowledgeBaseProductionCalendarPdfImportResult
            {
                IsSuccess = true,
                ProductionCalendarYears = KnowledgeBaseDataService.NormalizeProductionCalendarYears(new[] { importedYear })
                    .Where(item => item.Year == year)
                    .ToList(),
                ImportedYearCount = 1,
                Warnings = warnings
            };
        }

        private static string ExtractText(byte[] pdfBytes)
        {
            using var stream = new MemoryStream(pdfBytes);
            using PdfDocument document = PdfDocument.Open(stream);
            var builder = new StringBuilder();
            foreach (var page in document.GetPages())
            {
                builder.AppendLine(ContentOrderTextExtractor.GetText(page));
            }

            return builder.ToString();
        }

        private static void ReadTransfers(
            string text,
            int calendarYear,
            SortedSet<DateOnly> additionalNonWorkingDays,
            SortedSet<DateOnly> additionalWorkingDays,
            List<string> warnings)
        {
            foreach (Match match in TransferRegex.Matches(text))
            {
                if (!TryCreateDate(calendarYear, match.Groups["sourceDay"].Value, match.Groups["sourceMonth"].Value, out DateOnly sourceDate) ||
                    !TryCreateDate(calendarYear, match.Groups["targetDay"].Value, match.Groups["targetMonth"].Value, out DateOnly targetDate))
                {
                    warnings.Add($"Не удалось разобрать перенос: {match.Value}");
                    continue;
                }

                additionalNonWorkingDays.Add(targetDate);
                if (IsWeekend(sourceDate) && !IsFixedNonWorkingHoliday(sourceDate))
                    additionalWorkingDays.Add(sourceDate);
            }
        }

        private static void ReadRestPeriods(
            string text,
            int calendarYear,
            SortedSet<DateOnly> additionalNonWorkingDays,
            List<string> warnings)
        {
            foreach (Match match in CrossYearRestPeriodRegex.Matches(text))
            {
                if (!TryCreateDate(
                        match.Groups["startYear"].Value,
                        match.Groups["startDay"].Value,
                        match.Groups["startMonth"].Value,
                        out DateOnly startDate) ||
                    !TryCreateDate(
                        match.Groups["endYear"].Value,
                        match.Groups["endDay"].Value,
                        match.Groups["endMonth"].Value,
                        out DateOnly endDate))
                {
                    warnings.Add($"Не удалось разобрать период отдыха: {match.Value}");
                    continue;
                }

                AddAdditionalRestPeriodDates(calendarYear, startDate, endDate, additionalNonWorkingDays);
            }

            foreach (Match match in SameMonthRestPeriodRegex.Matches(text))
            {
                string yearText = match.Groups["year"].Success ? match.Groups["year"].Value : calendarYear.ToString(CultureInfo.InvariantCulture);
                if (!TryCreateDate(yearText, match.Groups["startDay"].Value, match.Groups["month"].Value, out DateOnly startDate) ||
                    !TryCreateDate(yearText, match.Groups["endDay"].Value, match.Groups["month"].Value, out DateOnly endDate))
                {
                    warnings.Add($"Не удалось разобрать период отдыха: {match.Value}");
                    continue;
                }

                AddAdditionalRestPeriodDates(calendarYear, startDate, endDate, additionalNonWorkingDays);
            }
        }

        private static void AddAdditionalRestPeriodDates(
            int calendarYear,
            DateOnly startDate,
            DateOnly endDate,
            SortedSet<DateOnly> additionalNonWorkingDays)
        {
            if (endDate < startDate)
                return;

            for (DateOnly date = startDate; date <= endDate; date = date.AddDays(1))
            {
                if (date.Year == calendarYear &&
                    !IsWeekend(date) &&
                    !IsFixedNonWorkingHoliday(date))
                {
                    additionalNonWorkingDays.Add(date);
                }
            }
        }

        private static void ValidateWorkingDayCount(
            string text,
            KbProductionCalendarYear importedYear,
            List<string> warnings)
        {
            int? expectedWorkingDays = TryReadExpectedYearWorkingDays(text, importedYear.Year);
            if (expectedWorkingDays == null)
                return;

            var calendar = new KnowledgeBaseRussianProductionCalendarService(new[] { importedYear });
            int actualWorkingDays = Enumerable.Range(1, 12)
                .Sum(month => calendar.CountWorkingDays(importedYear.Year, month));

            if (actualWorkingDays != expectedWorkingDays.Value)
            {
                warnings.Add(
                    $"Количество рабочих дней после импорта: {actualWorkingDays}; в PDF указано: {expectedWorkingDays.Value}. Проверьте календарь перед применением.");
            }
        }

        private static int? TryReadExpectedYearWorkingDays(string text, int year)
        {
            var regex = new Regex(
                $@"\b{year}\s+год\s+(?:365|366)\s+(?<workingDays>\d{{1,3}})\s+\d{{1,3}}\b",
                CalendarRegexOptions);
            Match match = regex.Match(text);
            return match.Success && int.TryParse(match.Groups["workingDays"].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int workingDays)
                ? workingDays
                : null;
        }

        private static bool TryReadYear(string text, out int year)
        {
            year = 0;
            Match match = YearRegex.Match(text);
            if (!match.Success)
                match = FallbackYearRegex.Match(text);

            return match.Success &&
                   int.TryParse(match.Groups["year"].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out year) &&
                   year is >= 1900 and <= 9999;
        }

        private static bool TryCreateDate(
            string yearText,
            string dayText,
            string monthText,
            out DateOnly date)
        {
            date = default;
            if (!int.TryParse(yearText, NumberStyles.Integer, CultureInfo.InvariantCulture, out int year))
                return false;

            return TryCreateDate(year, dayText, monthText, out date);
        }

        private static bool TryCreateDate(
            int year,
            string dayText,
            string monthText,
            out DateOnly date)
        {
            date = default;
            if (!int.TryParse(dayText, NumberStyles.Integer, CultureInfo.InvariantCulture, out int day) ||
                !MonthNumbers.TryGetValue(monthText.Replace('ё', 'е'), out int month))
            {
                return false;
            }

            try
            {
                date = new DateOnly(year, month, day);
                return true;
            }
            catch (ArgumentOutOfRangeException)
            {
                return false;
            }
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

        private static string NormalizeText(string? text) =>
            Regex.Replace(
                    text ?? string.Empty,
                    @"\s+",
                    " ",
                    CalendarRegexOptions)
                .Trim();

        private static KnowledgeBaseProductionCalendarPdfImportResult Failure(string errorMessage) =>
            new()
            {
                IsSuccess = false,
                ErrorMessage = errorMessage
            };
    }
}
