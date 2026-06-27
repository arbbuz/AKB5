using System.Globalization;
using AsutpKnowledgeBase.Models;

namespace AsutpKnowledgeBase.Services
{
    public sealed class KnowledgeBaseActNumberingResult
    {
        public bool IsSuccess { get; init; }

        public string ErrorMessage { get; init; } = string.Empty;

        public KbAct? Act { get; init; }

        public List<KbActNumberSequence> NumberSequences { get; init; } = new();
    }

    public sealed class KnowledgeBaseActNumberingService
    {
        public KnowledgeBaseActNumberingResult EnsureActNumber(
            KbAct? act,
            IEnumerable<KbAct>? existingActs,
            IEnumerable<KbActNumberSequence>? numberSequences)
        {
            if (act == null)
                return Failure("Не переданы данные акта.");

            if (!act.ActDate.HasValue)
                return Failure("Укажите дату акта для присвоения номера.");

            DateTime actDate = act.ActDate.Value.Date;
            KbAct candidate = KnowledgeBaseActEditorService.CloneAct(act);
            candidate.ActDate = actDate;
            int year = actDate.Year;
            candidate.ActYear = year;

            List<KbAct> normalizedExistingActs = KnowledgeBaseDataService.NormalizeActs(existingActs);
            List<KbActNumberSequence> normalizedSequences =
                KnowledgeBaseDataService.NormalizeActNumberSequences(numberSequences);

            if (!string.IsNullOrWhiteSpace(candidate.ActNumber))
            {
                candidate.ActNumber = candidate.ActNumber.Trim();
                if (HasDuplicateNumber(candidate, normalizedExistingActs))
                    return Failure($"Номер акта {candidate.ActNumber} уже используется.");

                return Success(candidate, normalizedSequences);
            }

            int nextNumber = ResolveNextNumber(year, normalizedExistingActs, normalizedSequences);
            candidate.ActNumber = FormatActNumber(year, nextNumber);

            List<KbActNumberSequence> updatedSequences = normalizedSequences
                .Where(sequence => sequence.Year != year)
                .ToList();
            updatedSequences.Add(new KbActNumberSequence
            {
                Year = year,
                NextNumber = nextNumber + 1
            });

            return Success(
                candidate,
                KnowledgeBaseDataService.NormalizeActNumberSequences(updatedSequences));
        }

        public static string FormatActNumber(int year, int number) =>
            $"{year.ToString(CultureInfo.InvariantCulture)}-{number.ToString("D4", CultureInfo.InvariantCulture)}";

        private static int ResolveNextNumber(
            int year,
            IEnumerable<KbAct> existingActs,
            IEnumerable<KbActNumberSequence> numberSequences)
        {
            int nextFromSequence = numberSequences
                .Where(sequence => sequence.Year == year)
                .Select(sequence => Math.Max(1, sequence.NextNumber))
                .DefaultIfEmpty(1)
                .Max();
            int nextFromExistingActs = existingActs
                .Select(static act => act.ActNumber)
                .Select(TryParseActNumber)
                .Where(parsed => parsed.IsSuccess && parsed.Year == year)
                .Select(parsed => parsed.Number + 1)
                .DefaultIfEmpty(1)
                .Max();

            return Math.Max(nextFromSequence, nextFromExistingActs);
        }

        private static bool HasDuplicateNumber(KbAct candidate, IEnumerable<KbAct> existingActs) =>
            existingActs.Any(existingAct =>
                !string.Equals(existingAct.ActId, candidate.ActId, StringComparison.Ordinal) &&
                string.Equals(existingAct.ActNumber, candidate.ActNumber, StringComparison.OrdinalIgnoreCase));

        private static (bool IsSuccess, int Year, int Number) TryParseActNumber(string? actNumber)
        {
            string value = actNumber?.Trim() ?? string.Empty;
            string[] parts = value.Split('-', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (parts.Length != 2 ||
                !int.TryParse(parts[0], NumberStyles.None, CultureInfo.InvariantCulture, out int year) ||
                !int.TryParse(parts[1], NumberStyles.None, CultureInfo.InvariantCulture, out int number))
            {
                return (false, 0, 0);
            }

            return year > 0 && number > 0
                ? (true, year, number)
                : (false, 0, 0);
        }

        private static KnowledgeBaseActNumberingResult Success(
            KbAct act,
            List<KbActNumberSequence> numberSequences) =>
            new()
            {
                IsSuccess = true,
                Act = act,
                NumberSequences = numberSequences
            };

        private static KnowledgeBaseActNumberingResult Failure(string errorMessage) =>
            new()
            {
                IsSuccess = false,
                ErrorMessage = errorMessage
            };
    }
}
