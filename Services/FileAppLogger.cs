using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AsutpKnowledgeBase.Services
{
    public sealed class FileAppLogger : IAppLogger
    {
        private const int DefaultRetentionDays = 14;

        private static readonly JsonSerializerOptions SerializerOptions = new()
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        private static readonly object CleanupSync = new();
        private static readonly HashSet<string> CleanedDirectories = new(StringComparer.OrdinalIgnoreCase);

        private readonly object _writeSync = new();
        private readonly string _logDirectory;
        private readonly int _retentionDays;

        public FileAppLogger(string? logDirectory = null, int retentionDays = DefaultRetentionDays)
        {
            _logDirectory = AppLogPathResolver.ResolveLogDirectory(logDirectory);
            _retentionDays = retentionDays > 0 ? retentionDays : DefaultRetentionDays;
        }

        public void Log(
            string eventName,
            AppLogLevel level,
            string message,
            Exception? exception = null,
            IReadOnlyDictionary<string, object?>? properties = null)
        {
            if (string.IsNullOrWhiteSpace(eventName) || string.IsNullOrWhiteSpace(message))
                return;

            try
            {
                EnsureRetention();
                Directory.CreateDirectory(_logDirectory);

                var entry = new AppLogEntry
                {
                    TsUtc = DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture),
                    Level = level.ToString(),
                    Event = eventName,
                    Message = message,
                    Properties = NormalizeProperties(properties),
                    Exception = exception == null ? null : AppLogExceptionInfo.FromException(exception)
                };

                string line = JsonSerializer.Serialize(entry, SerializerOptions);
                string filePath = Path.Combine(_logDirectory, $"app-{DateTime.Now:yyyy-MM-dd}.ndjson");

                lock (_writeSync)
                {
                    File.AppendAllText(filePath, line + Environment.NewLine);
                }
            }
            catch
            {
            }
        }

        private void EnsureRetention()
        {
            lock (CleanupSync)
            {
                if (!CleanedDirectories.Add(_logDirectory))
                    return;
            }

            try
            {
                if (!Directory.Exists(_logDirectory))
                    return;

                DateTime cutoffDate = DateTime.Today.AddDays(-_retentionDays);
                foreach (string path in Directory.EnumerateFiles(_logDirectory, "app-*.ndjson"))
                {
                    if (!TryParseLogDate(path, out DateTime logDate) || logDate >= cutoffDate)
                        continue;

                    File.Delete(path);
                }
            }
            catch
            {
            }
        }

        private static Dictionary<string, object?> NormalizeProperties(IReadOnlyDictionary<string, object?>? properties)
        {
            var normalized = new Dictionary<string, object?>(StringComparer.Ordinal);
            if (properties == null)
                return normalized;

            foreach (var pair in properties)
            {
                if (string.IsNullOrWhiteSpace(pair.Key))
                    continue;

                normalized[pair.Key] = NormalizeValue(pair.Value);
            }

            return normalized;
        }

        private static object? NormalizeValue(object? value) =>
            value switch
            {
                null => null,
                string or bool or byte or sbyte or short or ushort or int or uint or long or ulong or float or double or decimal => value,
                DateTime dateTime => dateTime.ToString("O", CultureInfo.InvariantCulture),
                DateTimeOffset dateTimeOffset => dateTimeOffset.ToString("O", CultureInfo.InvariantCulture),
                TimeSpan timeSpan => timeSpan.ToString(),
                Guid guid => guid.ToString(),
                Enum enumValue => enumValue.ToString(),
                _ => value.ToString()
            };

        private static bool TryParseLogDate(string path, out DateTime logDate)
        {
            logDate = default;
            string fileName = Path.GetFileNameWithoutExtension(path);
            if (!fileName.StartsWith("app-", StringComparison.OrdinalIgnoreCase))
                return false;

            return DateTime.TryParseExact(
                fileName["app-".Length..],
                "yyyy-MM-dd",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out logDate);
        }
    }

    internal static class AppLogPathResolver
    {
        public static string ResolveLogDirectory(string? overrideDirectory)
        {
            if (!string.IsNullOrWhiteSpace(overrideDirectory))
                return overrideDirectory;

            string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            if (!string.IsNullOrWhiteSpace(localAppData))
                return Path.Combine(localAppData, "AKB5", "logs");

            return Path.Combine(Path.GetTempPath(), "AKB5", "logs");
        }
    }

    internal sealed class AppLogEntry
    {
        [JsonPropertyName("tsUtc")]
        public string TsUtc { get; init; } = string.Empty;

        [JsonPropertyName("level")]
        public string Level { get; init; } = string.Empty;

        [JsonPropertyName("event")]
        public string Event { get; init; } = string.Empty;

        [JsonPropertyName("message")]
        public string Message { get; init; } = string.Empty;

        [JsonPropertyName("properties")]
        public IReadOnlyDictionary<string, object?> Properties { get; init; } =
            new Dictionary<string, object?>(StringComparer.Ordinal);

        [JsonPropertyName("exception")]
        public AppLogExceptionInfo? Exception { get; init; }
    }

    internal sealed class AppLogExceptionInfo
    {
        [JsonPropertyName("type")]
        public string Type { get; init; } = string.Empty;

        [JsonPropertyName("message")]
        public string Message { get; init; } = string.Empty;

        [JsonPropertyName("stackTrace")]
        public string? StackTrace { get; init; }

        public static AppLogExceptionInfo FromException(Exception exception) =>
            new()
            {
                Type = exception.GetType().FullName ?? exception.GetType().Name,
                Message = exception.Message,
                StackTrace = exception.StackTrace
            };
    }
}
