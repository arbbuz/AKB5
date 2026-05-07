using System.Text.Json;
using AsutpKnowledgeBase.Models;
using AsutpKnowledgeBase.Services;
using Microsoft.Data.Sqlite;

namespace AsutpKnowledgeBase.Core.Tests;

public class KnowledgeBaseFirstLaunchMigrationServiceTests
{
    private static readonly DateTime FixedNow = new(2026, 5, 7, 8, 30, 0, DateTimeKind.Utc);

    [Fact]
    public void CreatePlan_WhenSqliteMissingAndLegacyJsonExists_OffersMigrationWithoutWritingFiles()
    {
        string tempDirectory = CreateTempDirectory();

        try
        {
            string sqlitePath = Path.Combine(tempDirectory, "knowledge-base.akb");
            string jsonPath = Path.Combine(tempDirectory, "ASUTP_KnowledgeBase.json");
            File.WriteAllText(jsonPath, "{}");

            var service = new KnowledgeBaseFirstLaunchMigrationService(utcNow: () => FixedNow);
            KnowledgeBaseFirstLaunchMigrationPlan plan = service.CreatePlan(sqlitePath, jsonPath);

            Assert.True(plan.ShouldOfferMigration);
            Assert.Equal(sqlitePath, plan.TargetSqlitePath);
            Assert.Equal(jsonPath, plan.LegacyJsonPath);
            Assert.False(File.Exists(sqlitePath));
            Assert.Single(Directory.GetFiles(tempDirectory));
        }
        finally
        {
            Directory.Delete(tempDirectory, recursive: true);
        }
    }

    [Fact]
    public void CreatePlan_WhenSqliteAlreadyExists_DoesNotOfferMigration()
    {
        string tempDirectory = CreateTempDirectory();

        try
        {
            string sqlitePath = Path.Combine(tempDirectory, "knowledge-base.akb");
            string jsonPath = Path.Combine(tempDirectory, "ASUTP_KnowledgeBase.json");
            File.WriteAllText(sqlitePath, "existing");
            File.WriteAllText(jsonPath, "{}");

            var service = new KnowledgeBaseFirstLaunchMigrationService();
            KnowledgeBaseFirstLaunchMigrationPlan plan = service.CreatePlan(sqlitePath, jsonPath);

            Assert.False(plan.ShouldOfferMigration);
        }
        finally
        {
            Directory.Delete(tempDirectory, recursive: true);
        }
    }

    [Fact]
    public void Migrate_LoadsLegacyJsonIntoSqliteAndWritesSafetyExportWithoutChangingJson()
    {
        string tempDirectory = CreateTempDirectory();

        try
        {
            string sqlitePath = Path.Combine(tempDirectory, "knowledge-base.akb");
            string jsonPath = Path.Combine(tempDirectory, "ASUTP_KnowledgeBase.json");
            SavedData source = CreateSampleData();
            var jsonStorage = new JsonStorageService(jsonPath);
            Assert.True(jsonStorage.Save(source, out string? saveError), saveError);
            string originalJson = File.ReadAllText(jsonPath);

            var logger = new InMemoryAppLogger();
            var service = new KnowledgeBaseFirstLaunchMigrationService(logger, () => FixedNow);
            KnowledgeBaseFirstLaunchMigrationPlan plan = service.CreatePlan(sqlitePath, jsonPath);

            KnowledgeBaseFirstLaunchMigrationResult result = service.Migrate(plan);

            Assert.True(result.IsSuccess, result.ErrorMessage);
            Assert.Equal(sqlitePath, result.TargetSqlitePath);
            Assert.Equal(jsonPath, result.SourceJsonPath);
            Assert.True(File.Exists(sqlitePath));
            Assert.True(File.Exists(result.SafetyJsonExportPath));
            Assert.Equal(originalJson, File.ReadAllText(jsonPath));

            var sqliteStorage = new SqliteKnowledgeBaseStorageService(sqlitePath);
            KnowledgeBaseStorageLoadResult sqliteLoad = sqliteStorage.Load();
            Assert.True(sqliteLoad.IsSuccess, sqliteLoad.ErrorMessage);
            Assert.NotNull(sqliteLoad.Data);
            SavedData expected = KnowledgeBaseDataService.NormalizeSavedData(source);
            Assert.Equal(Serialize(expected), Serialize(sqliteLoad.Data!));

            SavedData? safetyExport = JsonSerializer.Deserialize<SavedData>(
                File.ReadAllText(result.SafetyJsonExportPath));
            Assert.NotNull(safetyExport);
            Assert.Equal(Serialize(expected), Serialize(KnowledgeBaseDataService.NormalizeSavedData(safetyExport)));

            Dictionary<string, string> metadata = ReadAppMetadata(sqlitePath);
            Assert.Equal("success", metadata["last_migration_status"]);
            Assert.Equal(jsonPath, metadata["last_migration_source_path"]);
            Assert.Equal(result.SafetyJsonExportPath, metadata["last_migration_safety_export_path"]);
            KnowledgeBaseChangeLogListResult history = sqliteStorage.ListChangeLog();
            Assert.True(history.IsSuccess, history.ErrorMessage);
            Assert.Contains(history.Entries, entry => entry.ActionKind == "migration");
            Assert.Contains(logger.Entries, entry => entry.EventName == "FirstLaunchJsonMigrationSucceeded");
        }
        finally
        {
            Directory.Delete(tempDirectory, recursive: true);
        }
    }

    [Fact]
    public void Migrate_WhenLegacyJsonIsInvalid_DoesNotCreateSqliteOrChangeJson()
    {
        string tempDirectory = CreateTempDirectory();

        try
        {
            string sqlitePath = Path.Combine(tempDirectory, "knowledge-base.akb");
            string jsonPath = Path.Combine(tempDirectory, "ASUTP_KnowledgeBase.json");
            File.WriteAllText(jsonPath, "{ broken json");
            string originalJson = File.ReadAllText(jsonPath);

            var service = new KnowledgeBaseFirstLaunchMigrationService(utcNow: () => FixedNow);
            KnowledgeBaseFirstLaunchMigrationPlan plan = service.CreatePlan(sqlitePath, jsonPath);

            KnowledgeBaseFirstLaunchMigrationResult result = service.Migrate(plan);

            Assert.False(result.IsSuccess);
            Assert.NotEmpty(result.ErrorMessage);
            Assert.False(File.Exists(sqlitePath));
            Assert.Equal(originalJson, File.ReadAllText(jsonPath));
            Assert.Empty(Directory.GetFiles(tempDirectory, "*.tmp"));
        }
        finally
        {
            Directory.Delete(tempDirectory, recursive: true);
        }
    }

    [Fact]
    public void RoutedStorage_UsesJsonForJsonPathAndSqliteForAkbPath()
    {
        string tempDirectory = CreateTempDirectory();

        try
        {
            SavedData source = CreateSampleData();
            string jsonPath = Path.Combine(tempDirectory, "kb.json");
            string sqlitePath = Path.Combine(tempDirectory, "kb.akb");

            var routedJson = new KnowledgeBaseRoutedStorageService(jsonPath);
            Assert.True(routedJson.Save(source, out string? jsonError), jsonError);
            Assert.Equal(KnowledgeBaseStorageKind.LegacyJson, routedJson.CurrentKind);
            Assert.Contains("\"LastWorkshop\"", File.ReadAllText(jsonPath));

            var routedSqlite = new KnowledgeBaseRoutedStorageService(sqlitePath);
            Assert.True(routedSqlite.Save(source, out string? sqliteError), sqliteError);
            Assert.Equal(KnowledgeBaseStorageKind.Sqlite, routedSqlite.CurrentKind);

            KnowledgeBaseStorageLoadResult sqliteLoad = routedSqlite.Load();
            Assert.True(sqliteLoad.IsSuccess, sqliteLoad.ErrorMessage);
            Assert.Equal("Цех 1", sqliteLoad.Data!.LastWorkshop);
        }
        finally
        {
            Directory.Delete(tempDirectory, recursive: true);
        }
    }

    private static SavedData CreateSampleData() =>
        new()
        {
            SchemaVersion = SavedData.CurrentSchemaVersion,
            Config = new KbConfig
            {
                MaxLevels = 3,
                LevelNames = new List<string> { "Цех", "Линия", "Шкаф" }
            },
            Workshops = new Dictionary<string, List<KbNode>>
            {
                ["Цех 1"] = new List<KbNode>
                {
                    new()
                    {
                        NodeId = "node-1",
                        Name = "Линия 1",
                        LevelIndex = 0,
                        NodeType = KbNodeType.System,
                        Children = new List<KbNode>
                        {
                            new()
                            {
                                NodeId = "node-2",
                                Name = "Шкаф 1",
                                LevelIndex = 1,
                                NodeType = KbNodeType.Cabinet
                            }
                        }
                    }
                }
            },
            CompositionEntries = new List<KbCompositionEntry>
            {
                new()
                {
                    EntryId = "entry-1",
                    ParentNodeId = "node-2",
                    ComponentType = "ПЛК",
                    Model = "CPU 1214C"
                }
            },
            LastWorkshop = "Цех 1"
        };

    private static Dictionary<string, string> ReadAppMetadata(string sqlitePath)
    {
        using var connection = new SqliteConnection($"Data Source={sqlitePath};Pooling=False");
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = "SELECT key, value FROM app_metadata;";
        using var reader = command.ExecuteReader();

        var result = new Dictionary<string, string>(StringComparer.Ordinal);
        while (reader.Read())
            result[reader.GetString(0)] = reader.GetString(1);

        return result;
    }

    private static string Serialize(SavedData data) =>
        JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = false });

    private static string CreateTempDirectory()
    {
        string path = Path.Combine(Path.GetTempPath(), $"asutp-first-launch-migration-tests-{Guid.NewGuid():N}");
        Directory.CreateDirectory(path);
        return path;
    }
}
