using System.Text.Json;
using AsutpKnowledgeBase.Models;

namespace AsutpKnowledgeBase.Services
{
    public static class KnowledgeBaseDataService
    {
        private static readonly JsonSerializerOptions SnapshotOptions = new() { WriteIndented = false };

        public static StringComparer WorkshopNameComparer { get; } = StringComparer.OrdinalIgnoreCase;

        public static KbConfig CreateDefaultConfig() =>
            new()
            {
                MaxLevels = 10,
                LevelNames = Enumerable
                    .Range(1, 10)
                    .Select(static level => $"Уровень {level}")
                    .ToList()
            };

        public static SavedData CreateDefaultData() =>
            new()
            {
                Config = CreateDefaultConfig(),
                Workshops = new Dictionary<string, List<KbNode>>(WorkshopNameComparer)
                {
                    ["Новый цех"] = new List<KbNode>()
                },
                LastWorkshop = "Новый цех"
            };

        public static string NormalizeWorkshopName(string? workshopName) =>
            workshopName?.Trim() ?? string.Empty;

        public static bool WorkshopNamesEqual(string? left, string? right) =>
            WorkshopNameComparer.Equals(
                NormalizeWorkshopName(left),
                NormalizeWorkshopName(right));

        public static string? FindWorkshopName(IEnumerable<string> workshopNames, string? workshopName)
        {
            string normalizedWorkshop = NormalizeWorkshopName(workshopName);
            if (string.IsNullOrWhiteSpace(normalizedWorkshop))
                return null;

            foreach (string existingWorkshop in workshopNames)
            {
                if (WorkshopNameComparer.Equals(existingWorkshop, normalizedWorkshop))
                    return existingWorkshop;
            }

            return null;
        }

        public static string? ValidateSupportedSchemaVersion(int schemaVersion)
        {
            if (schemaVersion < 1)
                return $"Неподдерживаемая версия схемы: {schemaVersion}.";

            if (schemaVersion > SavedData.CurrentSchemaVersion)
            {
                return
                    $"Файл создан более новой версией приложения: SchemaVersion = {schemaVersion}. " +
                    $"Максимально поддерживаемая версия: {SavedData.CurrentSchemaVersion}.";
            }

            return null;
        }

        public static string? ValidateWorkshopNames(Dictionary<string, List<KbNode>>? workshops)
        {
            if (workshops == null)
                return null;

            var seenWorkshopNames = new Dictionary<string, string>(WorkshopNameComparer);
            foreach (var pair in workshops)
            {
                string normalizedWorkshopName = NormalizeWorkshopName(pair.Key);
                if (string.IsNullOrWhiteSpace(normalizedWorkshopName))
                    continue;

                if (seenWorkshopNames.TryGetValue(normalizedWorkshopName, out var existingWorkshop))
                {
                    return
                        $"Обнаружены конфликтующие названия цехов '{NormalizeWorkshopName(existingWorkshop)}' " +
                        $"и '{normalizedWorkshopName}'. Имена цехов сравниваются без учёта регистра и крайних пробелов.";
                }

                seenWorkshopNames[normalizedWorkshopName] = pair.Key;
            }

            return null;
        }

        public static KbConfig NormalizeConfig(KbConfig? config)
        {
            var defaults = CreateDefaultConfig();
            if (config == null)
                return defaults;

            var normalized = new KbConfig
            {
                MaxLevels = config.MaxLevels > 0 ? config.MaxLevels : defaults.MaxLevels
            };

            foreach (var name in config.LevelNames ?? Enumerable.Empty<string>())
            {
                if (!string.IsNullOrWhiteSpace(name))
                    normalized.LevelNames.Add(name.Trim());
            }

            while (normalized.LevelNames.Count < normalized.MaxLevels)
            {
                int index = normalized.LevelNames.Count;
                if (index < defaults.LevelNames.Count)
                    normalized.LevelNames.Add(defaults.LevelNames[index]);
                else
                    normalized.LevelNames.Add($"Уровень {index + 1}");
            }

            if (normalized.LevelNames.Count > normalized.MaxLevels)
                normalized.LevelNames = normalized.LevelNames.Take(normalized.MaxLevels).ToList();

            return normalized;
        }

        public static Dictionary<string, List<KbNode>> NormalizeWorkshops(Dictionary<string, List<KbNode>>? workshops)
        {
            string? workshopValidationError = ValidateWorkshopNames(workshops);
            if (workshopValidationError != null)
                throw new InvalidOperationException(workshopValidationError);

            var normalized = new Dictionary<string, List<KbNode>>(WorkshopNameComparer);

            if (workshops != null)
            {
                foreach (var pair in workshops)
                {
                    if (string.IsNullOrWhiteSpace(pair.Key))
                        continue;

                    string workshopName = NormalizeWorkshopName(pair.Key);
                    var workshopNodes = pair.Value ?? new List<KbNode>();
                    NormalizeNodes(workshopNodes);
                    normalized.Add(workshopName, workshopNodes);
                }
            }

            if (normalized.Count == 0)
                normalized["Новый цех"] = new List<KbNode>();

            return normalized;
        }

        public static string ResolveWorkshop(Dictionary<string, List<KbNode>> workshops, string? preferredWorkshop)
        {
            string? resolvedWorkshop = FindWorkshopName(workshops.Keys, preferredWorkshop);
            if (!string.IsNullOrWhiteSpace(resolvedWorkshop))
                return resolvedWorkshop;

            return workshops.Keys.FirstOrDefault() ?? string.Empty;
        }

        public static string SerializeSnapshot(
            KbConfig config,
            Dictionary<string, List<KbNode>> workshops,
            string currentWorkshop,
            bool includeCurrentWorkshop)
        {
            var data = new SavedData
            {
                Config = config,
                Workshops = workshops,
                LastWorkshop = includeCurrentWorkshop ? currentWorkshop : string.Empty
            };

            return JsonSerializer.Serialize(data, SnapshotOptions);
        }

        private static void NormalizeNodes(IEnumerable<KbNode> nodes)
        {
            foreach (var node in nodes)
                NormalizeNode(node);
        }

        private static void NormalizeNode(KbNode node)
        {
            node.Name ??= string.Empty;
            node.Details = NormalizeDetails(node.Details, node.LevelIndex);
            node.Children ??= new List<KbNode>();
            NormalizeNodes(node.Children);
        }

        private static KbNodeDetails NormalizeDetails(KbNodeDetails? details, int levelIndex) =>
            new()
            {
                Description = details?.Description ?? string.Empty,
                Location = details?.Location ?? string.Empty,
                PhotoPath = details?.PhotoPath ?? string.Empty,
                IpAddress = levelIndex >= 2 ? details?.IpAddress ?? string.Empty : string.Empty,
                SchemaLink = levelIndex >= 2 ? details?.SchemaLink ?? string.Empty : string.Empty
            };
    }
}
