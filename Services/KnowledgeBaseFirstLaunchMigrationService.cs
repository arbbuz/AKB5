using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using AsutpKnowledgeBase.Models;

namespace AsutpKnowledgeBase.Services
{
    public sealed class KnowledgeBaseFirstLaunchMigrationPlan
    {
        public bool ShouldOfferMigration { get; init; }

        public string TargetSqlitePath { get; init; } = string.Empty;

        public string LegacyJsonPath { get; init; } = string.Empty;
    }

    public sealed class KnowledgeBaseFirstLaunchMigrationResult
    {
        public bool IsSuccess { get; init; }

        public string ErrorMessage { get; init; } = string.Empty;

        public string TargetSqlitePath { get; init; } = string.Empty;

        public string SourceJsonPath { get; init; } = string.Empty;

        public string SafetyJsonExportPath { get; init; } = string.Empty;

        public bool LoadedFromBackup { get; init; }
    }

    public sealed class KnowledgeBaseFirstLaunchMigrationService
    {
        private static readonly JsonSerializerOptions SafetyExportOptions = new()
        {
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            PropertyNameCaseInsensitive = true,
            WriteIndented = true
        };

        private readonly IAppLogger _logger;
        private readonly Func<DateTime> _utcNow;

        public KnowledgeBaseFirstLaunchMigrationService(
            IAppLogger? logger = null,
            Func<DateTime>? utcNow = null)
        {
            _logger = logger ?? NullAppLogger.Instance;
            _utcNow = utcNow ?? (() => DateTime.UtcNow);
        }

        public KnowledgeBaseFirstLaunchMigrationPlan CreatePlan(
            string targetSqlitePath,
            string legacyJsonPath)
        {
            bool shouldOfferMigration =
                !File.Exists(targetSqlitePath) &&
                File.Exists(legacyJsonPath);

            return new KnowledgeBaseFirstLaunchMigrationPlan
            {
                ShouldOfferMigration = shouldOfferMigration,
                TargetSqlitePath = targetSqlitePath,
                LegacyJsonPath = legacyJsonPath
            };
        }

        public KnowledgeBaseFirstLaunchMigrationResult Migrate(KnowledgeBaseFirstLaunchMigrationPlan plan)
        {
            if (!plan.ShouldOfferMigration)
            {
                return Failure(
                    plan,
                    "Миграция не требуется: новая база уже существует или старый JSON-файл не найден.");
            }

            DateTime startedAt = _utcNow();
            string targetDirectory = Path.GetDirectoryName(plan.TargetSqlitePath) ?? string.Empty;
            string tempSqlitePath = $"{plan.TargetSqlitePath}.migration.tmp";
            string safetyExportPath = GetAvailableSafetyExportPath(plan.TargetSqlitePath, startedAt);
            string tempSafetyExportPath = $"{safetyExportPath}.tmp";
            bool targetSqliteCreated = false;

            try
            {
                if (!string.IsNullOrWhiteSpace(targetDirectory))
                    Directory.CreateDirectory(targetDirectory);
                DeleteIfExists(tempSqlitePath);
                DeleteIfExists(tempSafetyExportPath);

                var legacyStorage = new JsonStorageService(plan.LegacyJsonPath, _logger);
                KnowledgeBaseStorageLoadResult loadResult = legacyStorage.Load();
                if (!loadResult.IsSuccess || loadResult.Data == null)
                {
                    return Failure(
                        plan,
                        $"Не удалось прочитать старую JSON-базу: {loadResult.ErrorMessage ?? loadResult.PrimaryErrorMessage ?? "неизвестная ошибка"}");
                }

                SavedData normalizedData = KnowledgeBaseDataService.NormalizeSavedData(loadResult.Data);
                var sqliteStorage = new SqliteKnowledgeBaseStorageService(tempSqlitePath, _logger);
                if (!sqliteStorage.Save(normalizedData, out string? saveErrorMessage))
                {
                    return Failure(
                        plan,
                        $"Не удалось создать новую SQLite-базу: {saveErrorMessage}");
                }

                WriteSafetyExport(tempSafetyExportPath, normalizedData);

                if (File.Exists(plan.TargetSqlitePath))
                {
                    return Failure(
                        plan,
                        "Новая SQLite-база уже появилась во время миграции. Старый JSON-файл не изменён.");
                }

                File.Move(tempSqlitePath, plan.TargetSqlitePath);
                targetSqliteCreated = true;
                File.Move(tempSafetyExportPath, safetyExportPath);

                var finalSqliteStorage = new SqliteKnowledgeBaseStorageService(plan.TargetSqlitePath, _logger);
                finalSqliteStorage.WriteAppMetadata(
                    new Dictionary<string, string>(StringComparer.Ordinal)
                    {
                        ["last_migration_status"] = "success",
                        ["last_migration_source_path"] = loadResult.SourcePath,
                        ["last_migration_legacy_json_path"] = plan.LegacyJsonPath,
                        ["last_migration_safety_export_path"] = safetyExportPath,
                        ["last_migration_completed_at"] = startedAt.ToString("O")
                    });

                var result = new KnowledgeBaseFirstLaunchMigrationResult
                {
                    IsSuccess = true,
                    TargetSqlitePath = plan.TargetSqlitePath,
                    SourceJsonPath = loadResult.SourcePath,
                    SafetyJsonExportPath = safetyExportPath,
                    LoadedFromBackup = loadResult.LoadedFromBackup
                };

                _logger.Log(
                    "FirstLaunchJsonMigrationSucceeded",
                    AppLogLevel.Information,
                    "Legacy JSON database was migrated to SQLite.",
                    properties: CreateProperties(
                        ("targetSqlitePath", result.TargetSqlitePath),
                        ("sourceJsonPath", result.SourceJsonPath),
                        ("safetyJsonExportPath", result.SafetyJsonExportPath),
                        ("loadedFromBackup", result.LoadedFromBackup)));

                return result;
            }
            catch (Exception ex)
            {
                if (targetSqliteCreated)
                    DeleteIfExists(plan.TargetSqlitePath);

                _logger.Log(
                    "FirstLaunchJsonMigrationFailed",
                    AppLogLevel.Error,
                    "Legacy JSON database migration failed.",
                    ex,
                    CreateProperties(
                        ("targetSqlitePath", plan.TargetSqlitePath),
                        ("legacyJsonPath", plan.LegacyJsonPath),
                        ("safetyJsonExportPath", safetyExportPath)));

                return Failure(plan, ex.Message);
            }
            finally
            {
                DeleteIfExists(tempSqlitePath);
                DeleteIfExists(tempSafetyExportPath);
            }
        }

        private static string GetAvailableSafetyExportPath(string sqlitePath, DateTime createdAt)
        {
            string basePath = KnowledgeBaseStoragePaths.BuildPostMigrationJsonExportPath(sqlitePath, createdAt);
            if (!File.Exists(basePath))
                return basePath;

            string directory = Path.GetDirectoryName(basePath) ?? string.Empty;
            string name = Path.GetFileNameWithoutExtension(basePath);
            string extension = Path.GetExtension(basePath);
            for (int index = 2; ; index++)
            {
                string candidate = Path.Combine(directory, $"{name}-{index}{extension}");
                if (!File.Exists(candidate))
                    return candidate;
            }
        }

        private static void WriteSafetyExport(string path, SavedData data)
        {
            string json = JsonSerializer.Serialize(data, SafetyExportOptions);
            File.WriteAllText(path, json, Encoding.UTF8);
        }

        private static void DeleteIfExists(string path)
        {
            try
            {
                if (File.Exists(path))
                    File.Delete(path);
            }
            catch
            {
            }
        }

        private KnowledgeBaseFirstLaunchMigrationResult Failure(
            KnowledgeBaseFirstLaunchMigrationPlan plan,
            string errorMessage)
        {
            var result = new KnowledgeBaseFirstLaunchMigrationResult
            {
                IsSuccess = false,
                ErrorMessage = errorMessage,
                TargetSqlitePath = plan.TargetSqlitePath,
                SourceJsonPath = plan.LegacyJsonPath
            };

            _logger.Log(
                "FirstLaunchJsonMigrationFailed",
                AppLogLevel.Error,
                "Legacy JSON database migration failed.",
                properties: CreateProperties(
                    ("targetSqlitePath", plan.TargetSqlitePath),
                    ("legacyJsonPath", plan.LegacyJsonPath),
                    ("errorMessage", errorMessage)));

            return result;
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
}
