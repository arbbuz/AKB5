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

        public int? LoadSplitterDistance()
        {
            try
            {
                if (!File.Exists(StatePath))
                    return null;

                string json = File.ReadAllText(StatePath);
                if (string.IsNullOrWhiteSpace(json))
                    return null;

                var state = JsonSerializer.Deserialize<KnowledgeBaseWindowLayoutState>(json, SerializerOptions);
                return NormalizeSplitterDistance(state);
            }
            catch (Exception ex)
            {
                _logger.Log(
                    "WindowLayoutStateLoadFailed",
                    AppLogLevel.Warning,
                    "Failed to load persisted window layout state.",
                    ex,
                    CreateProperties(("path", StatePath)));

                return null;
            }
        }

        public void SaveSplitterDistance(int splitterDistance)
        {
            try
            {
                string? directory = Path.GetDirectoryName(StatePath);
                if (!string.IsNullOrWhiteSpace(directory))
                    Directory.CreateDirectory(directory);

                var state = new KnowledgeBaseWindowLayoutState
                {
                    SplitterDistance = NormalizeSplitterDistance(splitterDistance)
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

        private static int? NormalizeSplitterDistance(KnowledgeBaseWindowLayoutState? state)
        {
            int? normalizedCurrentValue = NormalizeSplitterDistance(state?.SplitterDistance);
            if (normalizedCurrentValue.HasValue)
                return normalizedCurrentValue;

            if (state?.SplitterDistancesByWorkshop == null)
                return null;

            foreach (var pair in state.SplitterDistancesByWorkshop)
            {
                int? normalizedLegacyValue = NormalizeSplitterDistance(pair.Value);
                if (normalizedLegacyValue.HasValue)
                    return normalizedLegacyValue;
            }

            return null;
        }

        private static int? NormalizeSplitterDistance(int? splitterDistance)
        {
            if (!splitterDistance.HasValue || splitterDistance.Value <= 0)
                return null;

            return splitterDistance.Value;
        }

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
        public int? SplitterDistance { get; init; }

        public Dictionary<string, int>? SplitterDistancesByWorkshop { get; init; }
    }
}
