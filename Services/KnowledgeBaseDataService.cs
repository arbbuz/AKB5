using System.Text.Json;
using AsutpKnowledgeBase.Models;

namespace AsutpKnowledgeBase.Services
{
    public static class KnowledgeBaseDataService
    {
        private static readonly JsonSerializerOptions SnapshotOptions = new() { WriteIndented = false };

        public static KbConfig CreateDefaultConfig() =>
            new()
            {
                MaxLevels = 7,
                LevelNames = new List<string>
                {
                    "Цех",
                    "Отделение",
                    "Оборудование",
                    "Щит",
                    "Устройства",
                    "Модули",
                    "Примечание"
                }
            };

        public static SavedData CreateDefaultData() =>
            new()
            {
                Config = CreateDefaultConfig(),
                Workshops = new Dictionary<string, List<KbNode>>
                {
                    ["Новый цех"] = new List<KbNode>()
                },
                LastWorkshop = "Новый цех"
            };

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
            var normalized = new Dictionary<string, List<KbNode>>();

            if (workshops != null)
            {
                foreach (var pair in workshops)
                {
                    if (string.IsNullOrWhiteSpace(pair.Key))
                        continue;

                    string workshopName = pair.Key.Trim();
                    if (!normalized.ContainsKey(workshopName))
                        normalized[workshopName] = pair.Value ?? new List<KbNode>();
                }
            }

            if (normalized.Count == 0)
                normalized["Новый цех"] = new List<KbNode>();

            return normalized;
        }

        public static string ResolveWorkshop(Dictionary<string, List<KbNode>> workshops, string? preferredWorkshop)
        {
            if (!string.IsNullOrWhiteSpace(preferredWorkshop))
            {
                string normalizedPreferred = preferredWorkshop.Trim();
                if (workshops.ContainsKey(normalizedPreferred))
                    return normalizedPreferred;
            }

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
    }
}
