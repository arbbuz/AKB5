using System.Text.Json;

namespace AsutpKnowledgeBase.Services
{
    public class KnowledgeBaseWindowLayoutStateService
    {
        private static readonly JsonSerializerOptions SerializerOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            WriteIndented = true
        };

        private readonly IAppLogger _logger;

        public string StatePath { get; }

        public KnowledgeBaseWindowLayoutStateService(string? statePath = null, IAppLogger? logger = null)
        {
            StatePath = ResolveStatePath(statePath);
            _logger = logger ?? NullAppLogger.Instance;
        }

        public IReadOnlyDictionary<string, int> LoadSplitterDistancesByWorkshop()
        {
            try
            {
                if (!File.Exists(StatePath))
                    return CreateEmptyState();

                string json = File.ReadAllText(StatePath);
                if (string.IsNullOrWhiteSpace(json))
                    return CreateEmptyState();

                var state = JsonSerializer.Deserialize<KnowledgeBaseWindowLayoutState>(json, SerializerOptions);
                return NormalizeSplitterDistances(state?.SplitterDistancesByWorkshop);
            }
            catch (Exception ex)
            {
                _logger.Log(
                    "WindowLayoutStateLoadFailed",
                    AppLogLevel.Warning,
                    "Failed to load persisted window layout state.",
                    ex,
                    CreateProperties(("path", StatePath)));

                return CreateEmptyState();
            }
        }

        public void SaveSplitterDistancesByWorkshop(IReadOnlyDictionary<string, int> splitterDistancesByWorkshop)
        {
            try
            {
                string? directory = Path.GetDirectoryName(StatePath);
                if (!string.IsNullOrWhiteSpace(directory))
                    Directory.CreateDirectory(directory);

                var state = new KnowledgeBaseWindowLayoutState
                {
                    SplitterDistancesByWorkshop = NormalizeSplitterDistances(splitterDistancesByWorkshop)
                };

                string json = JsonSerializer.Serialize(state, SerializerOptions);
                File.WriteAllText(StatePath, json);
            }
            catch (Exception ex)
            {
                _logger.Log(
                    "WindowLayoutStateSaveFailed",
                    AppLogLevel.Warning,
                    "Failed to save window layout state.",
                    ex,
                    CreateProperties(("path", StatePath)));
            }
        }

        private static Dictionary<string, int> NormalizeSplitterDistances(
            IReadOnlyDictionary<string, int>? splitterDistancesByWorkshop)
        {
            var normalized = CreateEmptyState();
            if (splitterDistancesByWorkshop == null)
                return normalized;

            foreach (var pair in splitterDistancesByWorkshop)
            {
                string workshop = pair.Key?.Trim() ?? string.Empty;
                if (string.IsNullOrWhiteSpace(workshop) || pair.Value <= 0)
                    continue;

                normalized[workshop] = pair.Value;
            }

            return normalized;
        }

        private static Dictionary<string, int> CreateEmptyState() =>
            new(StringComparer.Ordinal);

        private static string ResolveStatePath(string? overridePath)
        {
            if (!string.IsNullOrWhiteSpace(overridePath))
                return overridePath;

            string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            if (!string.IsNullOrWhiteSpace(localAppData))
                return Path.Combine(localAppData, "AKB5", "window-layout-state.json");

            return Path.Combine(Path.GetTempPath(), "AKB5", "window-layout-state.json");
        }

        private static Dictionary<string, object?> CreateProperties(params (string Key, object? Value)[] values)
        {
            var properties = new Dictionary<string, object?>(StringComparer.Ordinal);
            foreach (var (key, value) in values)
            {
                if (string.IsNullOrWhiteSpace(key) || value == null)
                    continue;

                properties[key] = value;
            }

            return properties;
        }
    }

    internal sealed class KnowledgeBaseWindowLayoutState
    {
        public Dictionary<string, int> SplitterDistancesByWorkshop { get; init; } =
            new(StringComparer.Ordinal);
    }
}
