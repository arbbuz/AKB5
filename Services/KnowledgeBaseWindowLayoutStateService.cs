using System.Drawing;
using System.Text.Json;

namespace AsutpKnowledgeBase.Services
{
    public sealed class KnowledgeBaseWindowPlacement
    {
        public int Left { get; init; }

        public int Top { get; init; }

        public int Width { get; init; }

        public int Height { get; init; }

        public bool IsMaximized { get; init; }
    }

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
                return NormalizeSplitterDistance(LoadStateCore());
            }
            catch (Exception ex)
            {
                LogLoadFailure(ex);
                return null;
            }
        }

        public KnowledgeBaseWindowPlacement? LoadWindowPlacement()
        {
            try
            {
                return NormalizeWindowPlacement(LoadStateCore()?.MainWindowPlacement);
            }
            catch (Exception ex)
            {
                LogLoadFailure(ex);
                return null;
            }
        }

        public void SaveSplitterDistance(int splitterDistance)
        {
            try
            {
                var existingState = LoadStateForWrite();
                SaveStateCore(
                    new KnowledgeBaseWindowLayoutState
                    {
                        SplitterDistance = NormalizeSplitterDistance(splitterDistance),
                        SplitterDistancesByWorkshop = existingState?.SplitterDistancesByWorkshop,
                        MainWindowPlacement = NormalizeWindowPlacement(existingState?.MainWindowPlacement)
                    });
            }
            catch (Exception ex)
            {
                LogSaveFailure(ex);
            }
        }

        public void SaveWindowPlacement(KnowledgeBaseWindowPlacement placement)
        {
            try
            {
                var normalizedPlacement = NormalizeWindowPlacement(placement);
                if (normalizedPlacement == null)
                    return;

                var existingState = LoadStateForWrite();
                SaveStateCore(
                    new KnowledgeBaseWindowLayoutState
                    {
                        SplitterDistance = NormalizeSplitterDistance(existingState),
                        SplitterDistancesByWorkshop = existingState?.SplitterDistancesByWorkshop,
                        MainWindowPlacement = normalizedPlacement
                    });
            }
            catch (Exception ex)
            {
                LogSaveFailure(ex);
            }
        }

        public static Rectangle FitWindowBounds(Rectangle requestedBounds, Rectangle workingArea, Size minimumSize)
        {
            if (workingArea.Width <= 0 || workingArea.Height <= 0)
                return requestedBounds;

            int minWidth = Math.Max(1, minimumSize.Width);
            int minHeight = Math.Max(1, minimumSize.Height);
            minWidth = Math.Min(minWidth, workingArea.Width);
            minHeight = Math.Min(minHeight, workingArea.Height);

            int width = requestedBounds.Width > 0 ? requestedBounds.Width : minWidth;
            int height = requestedBounds.Height > 0 ? requestedBounds.Height : minHeight;

            width = Math.Clamp(width, minWidth, workingArea.Width);
            height = Math.Clamp(height, minHeight, workingArea.Height);

            int maxLeft = workingArea.Right - width;
            int maxTop = workingArea.Bottom - height;

            int left = Math.Clamp(requestedBounds.Left, workingArea.Left, maxLeft);
            int top = Math.Clamp(requestedBounds.Top, workingArea.Top, maxTop);

            return new Rectangle(left, top, width, height);
        }

        private KnowledgeBaseWindowLayoutState? LoadStateForWrite()
        {
            try
            {
                return LoadStateCore();
            }
            catch (Exception ex)
            {
                LogLoadFailure(ex);
                return null;
            }
        }

        private KnowledgeBaseWindowLayoutState? LoadStateCore()
        {
            if (!File.Exists(StatePath))
                return null;

            string json = File.ReadAllText(StatePath);
            if (string.IsNullOrWhiteSpace(json))
                return null;

            return JsonSerializer.Deserialize<KnowledgeBaseWindowLayoutState>(json, SerializerOptions);
        }

        private void SaveStateCore(KnowledgeBaseWindowLayoutState state)
        {
            string? directory = Path.GetDirectoryName(StatePath);
            if (!string.IsNullOrWhiteSpace(directory))
                Directory.CreateDirectory(directory);

            string json = JsonSerializer.Serialize(state, SerializerOptions);
            File.WriteAllText(StatePath, json);
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

        private static KnowledgeBaseWindowPlacement? NormalizeWindowPlacement(KnowledgeBaseWindowPlacement? placement)
        {
            if (placement == null || placement.Width <= 0 || placement.Height <= 0)
                return null;

            return new KnowledgeBaseWindowPlacement
            {
                Left = placement.Left,
                Top = placement.Top,
                Width = placement.Width,
                Height = placement.Height,
                IsMaximized = placement.IsMaximized
            };
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

        private void LogLoadFailure(Exception ex)
        {
            _logger.Log(
                "WindowLayoutStateLoadFailed",
                AppLogLevel.Warning,
                "Failed to load persisted window layout state.",
                ex,
                CreateProperties(("path", StatePath)));
        }

        private void LogSaveFailure(Exception ex)
        {
            _logger.Log(
                "WindowLayoutStateSaveFailed",
                AppLogLevel.Warning,
                "Failed to save window layout state.",
                ex,
                CreateProperties(("path", StatePath)));
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

        public KnowledgeBaseWindowPlacement? MainWindowPlacement { get; init; }
    }
}
