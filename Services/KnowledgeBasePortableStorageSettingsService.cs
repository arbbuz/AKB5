using System.Text.Encodings.Web;
using System.Text.Json;

namespace AsutpKnowledgeBase.Services
{
    public sealed class KnowledgeBasePortableStorageSettings
    {
        public int SchemaVersion { get; set; } = 1;

        public string DatabasePath { get; set; } = string.Empty;
    }

    public sealed class KnowledgeBasePortableStorageSettingsLoadResult
    {
        public bool IsSuccess { get; init; }

        public bool FileMissing { get; init; }

        public KnowledgeBasePortableStorageSettings? Settings { get; init; }

        public string? ErrorMessage { get; init; }
    }

    public sealed class KnowledgeBasePortableStorageSettingsService
    {
        public const string SettingsFileName = "akb5.settings.json";
        public const string DatabaseDirectoryName = "database";

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            PropertyNameCaseInsensitive = true,
            WriteIndented = true
        };

        public KnowledgeBasePortableStorageSettingsService(string applicationDirectory)
        {
            ApplicationDirectory = Path.GetFullPath(applicationDirectory);
            SettingsPath = Path.Combine(ApplicationDirectory, SettingsFileName);
        }

        public string ApplicationDirectory { get; }

        public string SettingsPath { get; }

        public string DefaultDatabasePath =>
            Path.Combine(
                ApplicationDirectory,
                DatabaseDirectoryName,
                KnowledgeBaseStoragePaths.DefaultSqliteFileName);

        public KnowledgeBasePortableStorageSettingsLoadResult Load()
        {
            if (!File.Exists(SettingsPath))
            {
                return new KnowledgeBasePortableStorageSettingsLoadResult
                {
                    FileMissing = true
                };
            }

            try
            {
                var settings = JsonSerializer.Deserialize<KnowledgeBasePortableStorageSettings>(
                    File.ReadAllText(SettingsPath),
                    JsonOptions);

                if (settings == null)
                {
                    return new KnowledgeBasePortableStorageSettingsLoadResult
                    {
                        ErrorMessage = "Файл настроек хранения пустой или повреждён."
                    };
                }

                return new KnowledgeBasePortableStorageSettingsLoadResult
                {
                    IsSuccess = true,
                    Settings = settings
                };
            }
            catch (Exception ex)
            {
                return new KnowledgeBasePortableStorageSettingsLoadResult
                {
                    ErrorMessage = ex.Message
                };
            }
        }

        public string ResolveDatabasePath(KnowledgeBasePortableStorageSettings settings)
        {
            string configuredPath = settings.DatabasePath?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(configuredPath))
                return DefaultDatabasePath;

            return Path.IsPathRooted(configuredPath)
                ? Path.GetFullPath(configuredPath)
                : Path.GetFullPath(Path.Combine(ApplicationDirectory, configuredPath));
        }

        public bool SaveDatabasePath(string databasePath, out string? errorMessage)
        {
            errorMessage = null;

            try
            {
                string? settingsDirectory = Path.GetDirectoryName(SettingsPath);
                if (!string.IsNullOrWhiteSpace(settingsDirectory))
                    Directory.CreateDirectory(settingsDirectory);

                var settings = new KnowledgeBasePortableStorageSettings
                {
                    DatabasePath = FormatStoredDatabasePath(databasePath)
                };

                File.WriteAllText(SettingsPath, JsonSerializer.Serialize(settings, JsonOptions));
                return true;
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message;
                return false;
            }
        }

        private string FormatStoredDatabasePath(string databasePath)
        {
            string fullDatabasePath = Path.GetFullPath(databasePath);
            string fullApplicationDirectory = EnsureTrailingSeparator(ApplicationDirectory);

            if (fullDatabasePath.StartsWith(fullApplicationDirectory, StringComparison.OrdinalIgnoreCase))
                return Path.GetRelativePath(ApplicationDirectory, fullDatabasePath);

            return fullDatabasePath;
        }

        private static string EnsureTrailingSeparator(string path)
        {
            string fullPath = Path.GetFullPath(path);
            return fullPath.EndsWith(Path.DirectorySeparatorChar)
                ? fullPath
                : fullPath + Path.DirectorySeparatorChar;
        }
    }
}
