using System.Text.Json;
using AsutpKnowledgeBase.Models;

namespace AsutpKnowledgeBase.Services
{
    public sealed class KnowledgeBaseProductionCalendarJsonImportResult
    {
        public bool IsSuccess { get; init; }

        public string ErrorMessage { get; init; } = string.Empty;

        public List<KbProductionCalendarYear> ProductionCalendarYears { get; init; } = new();

        public int ImportedYearCount { get; init; }
    }

    public sealed class KnowledgeBaseProductionCalendarJsonImportService
    {
        private static readonly JsonSerializerOptions SerializerOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public KnowledgeBaseProductionCalendarJsonImportResult ImportJson(byte[]? jsonBytes)
        {
            if (jsonBytes == null || jsonBytes.Length == 0)
                return Failure("Файл JSON с производственным календарём не был передан.");

            try
            {
                string json = System.Text.Encoding.UTF8.GetString(jsonBytes);
                List<KbProductionCalendarYear> importedYears = ReadProductionCalendarYears(json);
                if (importedYears.Count == 0)
                {
                    return Failure(
                        "В JSON не найдены годы производственного календаря. " +
                        "Ожидается объект с полем ProductionCalendarYears или массив годов.");
                }

                List<KbProductionCalendarYear> normalizedYears = NormalizeImportedYears(importedYears);
                return new KnowledgeBaseProductionCalendarJsonImportResult
                {
                    IsSuccess = true,
                    ProductionCalendarYears = normalizedYears,
                    ImportedYearCount = importedYears
                        .Where(static year => year != null && year.Year > 0)
                        .Select(static year => year.Year)
                        .Distinct()
                        .Count()
                };
            }
            catch (Exception ex)
            {
                return Failure($"Не удалось импортировать производственный календарь из JSON: {ex.Message}");
            }
        }

        private static List<KbProductionCalendarYear> ReadProductionCalendarYears(string json)
        {
            using JsonDocument document = JsonDocument.Parse(json);
            return document.RootElement.ValueKind switch
            {
                JsonValueKind.Array => JsonSerializer.Deserialize<List<KbProductionCalendarYear>>(json, SerializerOptions) ??
                                       new List<KbProductionCalendarYear>(),
                JsonValueKind.Object => JsonSerializer.Deserialize<ProductionCalendarImportDocument>(json, SerializerOptions)
                    ?.ProductionCalendarYears ?? new List<KbProductionCalendarYear>(),
                _ => new List<KbProductionCalendarYear>()
            };
        }

        private static List<KbProductionCalendarYear> NormalizeImportedYears(IEnumerable<KbProductionCalendarYear> years)
        {
            var normalizedByYear = new SortedDictionary<int, KbProductionCalendarYear>();
            foreach (KbProductionCalendarYear? yearConfiguration in years)
            {
                if (yearConfiguration == null || yearConfiguration.Year < 1)
                    continue;

                int year = yearConfiguration.Year;
                var dates = new SortedSet<DateOnly>();
                foreach (DateOnly date in yearConfiguration.AdditionalNonWorkingDays ?? Enumerable.Empty<DateOnly>())
                {
                    if (date.Year != year)
                    {
                        throw new InvalidOperationException(
                            $"Дата {date:yyyy-MM-dd} не относится к {year} году производственного календаря.");
                    }

                    dates.Add(date);
                }

                normalizedByYear[year] = new KbProductionCalendarYear
                {
                    Year = year,
                    AdditionalNonWorkingDays = dates.ToList()
                };
            }

            return normalizedByYear.Values.ToList();
        }

        private static KnowledgeBaseProductionCalendarJsonImportResult Failure(string errorMessage) =>
            new()
            {
                IsSuccess = false,
                ErrorMessage = errorMessage
            };

        private sealed class ProductionCalendarImportDocument
        {
            public List<KbProductionCalendarYear> ProductionCalendarYears { get; set; } = new();
        }
    }
}
