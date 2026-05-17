using System.Globalization;

namespace AsutpKnowledgeBase.Services
{
    public enum KnowledgeBaseCompositionSlotAdvisorySeverity
    {
        None,
        Hint,
        Warning
    }

    public sealed class KnowledgeBaseCompositionSlotAdvisory
    {
        public KnowledgeBaseCompositionSlotAdvisorySeverity Severity { get; init; }

        public string Text { get; init; } = string.Empty;

        public bool HasWarning => Severity == KnowledgeBaseCompositionSlotAdvisorySeverity.Warning;

        public bool HasHint => Severity == KnowledgeBaseCompositionSlotAdvisorySeverity.Hint;

        public static KnowledgeBaseCompositionSlotAdvisory None { get; } = new();
    }

    public static class KnowledgeBaseCompositionRackSlotRulesService
    {
        public const string DefaultProfileId = "simatic-s7-300";
        public const string DefaultProfileDisplayName = "SIMATIC S7-300";
        public const int DefaultRackNumber = 0;
        public const int DefaultSlotCount = 11;

        public static int NormalizeRackNumber(int rackNumber) =>
            rackNumber >= 0 ? rackNumber : DefaultRackNumber;

        public static string FormatRackTitle(int rackNumber) =>
            $"({NormalizeRackNumber(rackNumber).ToString(CultureInfo.InvariantCulture)}) UR / Rack{NormalizeRackNumber(rackNumber).ToString(CultureInfo.InvariantCulture)}";

        public static string FormatRackTitle(int rackNumber, string? rackType, string? label)
        {
            int normalizedRackNumber = NormalizeRackNumber(rackNumber);
            string normalizedRackType = string.IsNullOrWhiteSpace(rackType) ? "UR" : rackType.Trim();
            string title = $"({normalizedRackNumber.ToString(CultureInfo.InvariantCulture)}) {normalizedRackType} / Rack{normalizedRackNumber.ToString(CultureInfo.InvariantCulture)}";
            string normalizedLabel = label?.Trim() ?? string.Empty;
            return string.IsNullOrWhiteSpace(normalizedLabel)
                ? title
                : $"{title} - {normalizedLabel}";
        }

        public static string FormatRackText(int rackNumber) =>
            $"Rack{NormalizeRackNumber(rackNumber).ToString(CultureInfo.InvariantCulture)}";

        public static string GetSlotRoleText(int rackNumber, int slotNumber)
        {
            if (slotNumber <= 0)
                return "-";

            int normalizedRackNumber = NormalizeRackNumber(rackNumber);
            return slotNumber switch
            {
                1 => "PS",
                2 => normalizedRackNumber == DefaultRackNumber ? "CPU" : "Свободен",
                3 => "IM",
                >= 4 and <= DefaultSlotCount => "SM/FM/CP",
                _ => "Расширение"
            };
        }

        public static KnowledgeBaseCompositionSlotAdvisory GetSlotAdvisory(
            int rackNumber,
            int slotNumber,
            string? componentType,
            string? model,
            bool isPlaceholder = false,
            bool hasExpansionRacks = false)
        {
            if (slotNumber <= 0)
                return KnowledgeBaseCompositionSlotAdvisory.None;

            int normalizedRackNumber = NormalizeRackNumber(rackNumber);
            string searchText = NormalizeSearchText(componentType, model);

            if (isPlaceholder)
            {
                if (slotNumber == 3 && normalizedRackNumber == DefaultRackNumber && hasExpansionRacks)
                    return Hint("Rack0 при расширении обычно содержит send IM 360 или IM 365 в слоте 3.");

                if (slotNumber == 3 && normalizedRackNumber > DefaultRackNumber)
                    return Hint("Расширительный Rack обычно содержит receive IM 361 или IM 365 в слоте 3.");

                return KnowledgeBaseCompositionSlotAdvisory.None;
            }

            if (slotNumber > DefaultSlotCount)
                return Warning("Слот вне базовой S7-300 компоновки 1-11; проверьте фактический Rack.");

            return slotNumber switch
            {
                1 => LooksLikePowerSupply(searchText)
                    ? KnowledgeBaseCompositionSlotAdvisory.None
                    : Warning("Для S7-300 slot 1 обычно PS / блок питания."),
                2 when normalizedRackNumber == DefaultRackNumber => LooksLikeCpu(searchText)
                    ? KnowledgeBaseCompositionSlotAdvisory.None
                    : Warning("Для Rack0 slot 2 обычно CPU."),
                2 => Warning("Для расширительного S7-300 Rack slot 2 обычно свободен."),
                3 => GetInterfaceModuleAdvisory(normalizedRackNumber, searchText, hasExpansionRacks),
                >= 4 and <= DefaultSlotCount => LooksLikePowerSupply(searchText) ||
                    LooksLikeCpu(searchText) ||
                    LooksLikeInterfaceModule(searchText)
                        ? Warning("Slots 4-11 обычно предназначены для SM/FM/CP; проверьте тип модуля.")
                        : KnowledgeBaseCompositionSlotAdvisory.None,
                _ => KnowledgeBaseCompositionSlotAdvisory.None
            };
        }

        public static string GetSlotHintText(int rackNumber, int slotNumber, bool hasExpansionRacks = false)
        {
            var advisory = GetSlotAdvisory(
                rackNumber,
                slotNumber,
                componentType: string.Empty,
                model: string.Empty,
                isPlaceholder: true,
                hasExpansionRacks);
            return advisory.Text;
        }

        private static KnowledgeBaseCompositionSlotAdvisory GetInterfaceModuleAdvisory(
            int normalizedRackNumber,
            string searchText,
            bool hasExpansionRacks)
        {
            if (!LooksLikeInterfaceModule(searchText))
                return Warning("Для S7-300 slot 3 обычно IM / интерфейсный модуль.");

            if (normalizedRackNumber == DefaultRackNumber &&
                hasExpansionRacks &&
                !ContainsAny(searchText, "im 360", "im360", "6es7 360", "6es7360", "im 365", "im365", "6es7 365", "6es7365"))
            {
                return Hint("Для send IM в Rack0 обычно используют IM 360 или IM 365.");
            }

            if (normalizedRackNumber > DefaultRackNumber &&
                !ContainsAny(searchText, "im 361", "im361", "6es7 361", "6es7361", "im 365", "im365", "6es7 365", "6es7365"))
            {
                return Hint("Для receive IM в Rack1+ обычно используют IM 361 или IM 365.");
            }

            return KnowledgeBaseCompositionSlotAdvisory.None;
        }

        private static bool LooksLikePowerSupply(string searchText) =>
            ContainsToken(searchText, "ps") ||
            ContainsAny(
                searchText,
                "power supply",
                "блок питания",
                "питание",
                "источник питания",
                "ps 307",
                "ps307",
                "6es7 307",
                "6es7307",
                "6es7 305",
                "6es7305");

        private static bool LooksLikeCpu(string searchText) =>
            ContainsAny(
                searchText,
                "cpu",
                "цпу",
                "контроллер",
                "6es7 312",
                "6es7312",
                "6es7 313",
                "6es7313",
                "6es7 314",
                "6es7314",
                "6es7 315",
                "6es7315",
                "6es7 317",
                "6es7317",
                "6es7 318",
                "6es7318",
                "6es7 319",
                "6es7319");

        private static bool LooksLikeInterfaceModule(string searchText) =>
            ContainsToken(searchText, "im") ||
            ContainsAny(
                searchText,
                "interface module",
                "интерфейсный",
                "интерфейс",
                "6es7 360",
                "6es7360",
                "6es7 361",
                "6es7361",
                "6es7 365",
                "6es7365");

        private static bool ContainsAny(string searchText, params string[] tokens)
        {
            if (string.IsNullOrWhiteSpace(searchText))
                return false;

            return tokens.Any(token => searchText.Contains(token, StringComparison.OrdinalIgnoreCase));
        }

        private static bool ContainsToken(string searchText, string expectedToken)
        {
            if (string.IsNullOrWhiteSpace(searchText))
                return false;

            var tokens = searchText
                .Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(static token => token.Trim('.', ',', ';', ':', '/', '\\', '-', '_', '(', ')', '[', ']'));
            return tokens.Any(token => string.Equals(token, expectedToken, StringComparison.OrdinalIgnoreCase));
        }

        private static string NormalizeSearchText(string? componentType, string? model) =>
            string.Join(
                    " ",
                    new[] { componentType, model }
                        .Where(static value => !string.IsNullOrWhiteSpace(value))
                        .Select(static value => value!.Trim()))
                .ToLowerInvariant();

        private static KnowledgeBaseCompositionSlotAdvisory Hint(string text) =>
            new()
            {
                Severity = KnowledgeBaseCompositionSlotAdvisorySeverity.Hint,
                Text = text
            };

        private static KnowledgeBaseCompositionSlotAdvisory Warning(string text) =>
            new()
            {
                Severity = KnowledgeBaseCompositionSlotAdvisorySeverity.Warning,
                Text = text
            };
    }
}
