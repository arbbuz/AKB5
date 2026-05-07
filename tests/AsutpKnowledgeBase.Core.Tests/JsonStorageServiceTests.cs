using System.Text.Json;
using AsutpKnowledgeBase.Models;
using AsutpKnowledgeBase.Services;

namespace AsutpKnowledgeBase.Core.Tests;

public class JsonStorageServiceTests
{
    [Fact]
    public void SaveAndLoad_RoundTripsData()
    {
        string tempDirectory = CreateTempDirectory();

        try
        {
            string path = Path.Combine(tempDirectory, "kb.json");
            var service = new JsonStorageService(path);
            var data = CreateSampleData(lastWorkshop: "Цех 2");

            bool saved = service.Save(data, out var errorMessage);
            var loaded = service.Load();

            Assert.True(saved);
            Assert.Null(errorMessage);
            Assert.True(loaded.IsSuccess);
            Assert.Equal(path, loaded.SourcePath);
            Assert.Equal("Цех 2", loaded.Data!.LastWorkshop);
            Assert.Single(loaded.Data.Workshops["Цех 1"]);
            Assert.Equal("Схема 1", loaded.Data.Workshops["Цех 1"][0].Details.Description);
            Assert.Equal(string.Empty, loaded.Data.Workshops["Цех 1"][0].Details.Location);
            Assert.Equal(string.Empty, loaded.Data.Workshops["Цех 1"][0].Children[0].Details.PhotoPath);
            Assert.Equal(string.Empty, loaded.Data.Workshops["Цех 1"][0].Children[0].Details.IpAddress);
            Assert.Equal(string.Empty, loaded.Data.Workshops["Цех 1"][0].Children[0].Details.SchemaLink);
        }
        finally
        {
            Directory.Delete(tempDirectory, recursive: true);
        }
    }

    [Fact]
    public void Save_WhenFileAlreadyExists_CreatesBackupOfPreviousVersion()
    {
        string tempDirectory = CreateTempDirectory();

        try
        {
            string path = Path.Combine(tempDirectory, "kb.json");
            string backupPath = $"{path}.bak";
            var service = new JsonStorageService(path);

            Assert.True(service.Save(CreateSampleData(lastWorkshop: "Первый"), out _));
            Assert.True(service.Save(CreateSampleData(lastWorkshop: "Второй"), out _));

            var backupJson = File.ReadAllText(backupPath);
            var backupData = JsonSerializer.Deserialize<SavedData>(backupJson);

            Assert.NotNull(backupData);
            Assert.Equal("Первый", backupData!.LastWorkshop);
        }
        finally
        {
            Directory.Delete(tempDirectory, recursive: true);
        }
    }

    [Fact]
    public void Save_WhenFileAlreadyExists_CreatesTimestampedSnapshotBeforeOverwrite()
    {
        string tempDirectory = CreateTempDirectory();

        try
        {
            string path = Path.Combine(tempDirectory, "kb.json");
            var snapshotService = new KnowledgeBaseSnapshotService(
                () => new DateTimeOffset(2026, 5, 6, 12, 30, 0, 0, TimeSpan.Zero));
            var service = new JsonStorageService(path, snapshotService: snapshotService);

            Assert.True(service.Save(CreateSampleData(lastWorkshop: "Первый"), out _));
            Assert.True(service.Save(CreateSampleData(lastWorkshop: "Второй"), out _));

            string snapshotDirectory = Path.Combine(tempDirectory, KnowledgeBaseSnapshotService.SnapshotDirectoryName);
            string snapshotPath = Assert.Single(Directory.GetFiles(snapshotDirectory, "*.json"));
            SavedData snapshotData = JsonSerializer.Deserialize<SavedData>(File.ReadAllText(snapshotPath))!;
            SavedData currentData = JsonSerializer.Deserialize<SavedData>(File.ReadAllText(path))!;

            Assert.Equal("Первый", snapshotData.LastWorkshop);
            Assert.Equal("Второй", currentData.LastWorkshop);
            Assert.Equal("kb.20260506-123000-000Z.before-save.json", Path.GetFileName(snapshotPath));
        }
        finally
        {
            Directory.Delete(tempDirectory, recursive: true);
        }
    }

    [Fact]
    public void Save_WhenSnapshotCannotBeCreated_DoesNotOverwriteExistingFile()
    {
        string tempDirectory = CreateTempDirectory();

        try
        {
            string path = Path.Combine(tempDirectory, "kb.json");
            var service = new JsonStorageService(path);

            Assert.True(service.Save(CreateSampleData(lastWorkshop: "Первый"), out _));
            File.WriteAllText(
                Path.Combine(tempDirectory, KnowledgeBaseSnapshotService.SnapshotDirectoryName),
                "snapshot directory is blocked");

            bool saved = service.Save(CreateSampleData(lastWorkshop: "Второй"), out string? errorMessage);
            SavedData currentData = JsonSerializer.Deserialize<SavedData>(File.ReadAllText(path))!;

            Assert.False(saved);
            Assert.NotNull(errorMessage);
            Assert.Equal("Первый", currentData.LastWorkshop);
        }
        finally
        {
            Directory.Delete(tempDirectory, recursive: true);
        }
    }

    [Fact]
    public void CreateManualSnapshot_WritesCurrentDataWithoutOverwritingSourceFile()
    {
        string tempDirectory = CreateTempDirectory();

        try
        {
            string path = Path.Combine(tempDirectory, "kb.json");
            var snapshotService = new KnowledgeBaseSnapshotService(
                () => new DateTimeOffset(2026, 5, 6, 14, 0, 0, 0, TimeSpan.Zero));
            var service = new JsonStorageService(path, snapshotService: snapshotService);

            Assert.True(service.Save(CreateSampleData(lastWorkshop: "Сохранено"), out _));
            KnowledgeBaseSnapshotCreateResult result =
                service.CreateManualSnapshot(CreateSampleData(lastWorkshop: "В снимке"), "Контрольная точка");

            Assert.True(result.IsSuccess, result.ErrorMessage);
            SavedData currentData = JsonSerializer.Deserialize<SavedData>(File.ReadAllText(path))!;
            SavedData snapshotData = JsonSerializer.Deserialize<SavedData>(File.ReadAllText(result.SnapshotPath))!;
            KnowledgeBaseSnapshotMetadata metadata =
                JsonSerializer.Deserialize<KnowledgeBaseSnapshotMetadata>(File.ReadAllText(result.MetadataPath))!;

            Assert.Equal("Сохранено", currentData.LastWorkshop);
            Assert.Equal("В снимке", snapshotData.LastWorkshop);
            Assert.Equal("Контрольная точка", metadata.Note);
            Assert.Equal("manual", metadata.Kind);
        }
        finally
        {
            Directory.Delete(tempDirectory, recursive: true);
        }
    }

    [Fact]
    public void ListSnapshots_ReturnsEntriesForCurrentSavePath()
    {
        string tempDirectory = CreateTempDirectory();

        try
        {
            string path = Path.Combine(tempDirectory, "kb.json");
            DateTimeOffset snapshotTime = new(2026, 5, 6, 15, 0, 0, 0, TimeSpan.Zero);
            var snapshotService = new KnowledgeBaseSnapshotService(() => snapshotTime);
            var service = new JsonStorageService(path, snapshotService: snapshotService);

            Assert.True(service.Save(CreateSampleData(lastWorkshop: "Первый"), out _));
            snapshotTime = new DateTimeOffset(2026, 5, 6, 15, 30, 0, 0, TimeSpan.Zero);
            Assert.True(service.Save(CreateSampleData(lastWorkshop: "Второй"), out _));
            snapshotTime = new DateTimeOffset(2026, 5, 6, 16, 0, 0, 0, TimeSpan.Zero);
            KnowledgeBaseSnapshotCreateResult manualResult =
                service.CreateManualSnapshot(CreateSampleData(lastWorkshop: "Третий"), "Перед массовой правкой");

            KnowledgeBaseSnapshotListResult result = service.ListSnapshots();

            Assert.True(manualResult.IsSuccess, manualResult.ErrorMessage);
            Assert.True(result.IsSuccess, result.ErrorMessage);
            Assert.Equal(2, result.Snapshots.Count);
            Assert.Contains(result.Snapshots, entry =>
                entry.Kind == "before-save" &&
                entry.SourcePath == Path.GetFullPath(path));
            Assert.Contains(result.Snapshots, entry =>
                entry.Kind == "manual" &&
                entry.Note == "Перед массовой правкой");
        }
        finally
        {
            Directory.Delete(tempDirectory, recursive: true);
        }
    }

    [Fact]
    public void Load_WhenPrimaryFileIsBroken_FallsBackToBackup()
    {
        string tempDirectory = CreateTempDirectory();

        try
        {
            string path = Path.Combine(tempDirectory, "kb.json");
            string backupPath = $"{path}.bak";
            var logger = new InMemoryAppLogger();
            var service = new JsonStorageService(path, logger);

            Assert.True(service.Save(CreateSampleData(lastWorkshop: "Из backup"), out _));
            File.Copy(path, backupPath, overwrite: true);
            File.WriteAllText(path, "{ broken json");

            var result = service.Load();

            Assert.True(result.IsSuccess);
            Assert.True(result.LoadedFromBackup);
            Assert.Equal(backupPath, result.SourcePath);
            Assert.Equal("Из backup", result.Data!.LastWorkshop);
            Assert.NotNull(result.PrimaryErrorMessage);

            var fallbackEntry = Assert.Single(logger.Entries.Where(entry => entry.EventName == "JsonLoadFallbackToBackup"));
            Assert.Equal(AppLogLevel.Warning, fallbackEntry.Level);
            Assert.Equal(backupPath, fallbackEntry.Properties["backupPath"]);

            var successEntry = Assert.Single(logger.Entries.Where(entry =>
                entry.EventName == "JsonLoadSucceeded" &&
                entry.Properties.TryGetValue("usedBackup", out var usedBackup) &&
                Equals(usedBackup, true)));

            Assert.Equal(AppLogLevel.Information, successEntry.Level);
        }
        finally
        {
            Directory.Delete(tempDirectory, recursive: true);
        }
    }

    [Fact]
    public void Load_WhenJsonStructureIsInvalid_ReturnsValidationError()
    {
        string tempDirectory = CreateTempDirectory();

        try
        {
            string path = Path.Combine(tempDirectory, "kb.json");
            var service = new JsonStorageService(path);
            File.WriteAllText(path, """
            {
              "SchemaVersion": 1,
              "Config": null,
              "Workshops": {}
            }
            """);

            var result = service.Load();

            Assert.False(result.IsSuccess);
            Assert.NotNull(result.ErrorMessage);
            Assert.Contains("Config", result.ErrorMessage);
        }
        finally
        {
            Directory.Delete(tempDirectory, recursive: true);
        }
    }

    [Fact]
    public void Load_WhenSchema1JsonDoesNotContainDetails_NormalizesEmptyNodeDetails()
    {
        string tempDirectory = CreateTempDirectory();

        try
        {
            string path = Path.Combine(tempDirectory, "kb.json");
            var service = new JsonStorageService(path);
            File.WriteAllText(path, """
            {
              "SchemaVersion": 1,
              "Config": {
                "MaxLevels": 2,
                "LevelNames": ["Цех", "Щит"]
              },
              "Workshops": {
                "Цех 1": [
                  {
                    "Name": "Щит 1",
                    "LevelIndex": 0,
                    "Children": []
                  }
                ]
              },
              "LastWorkshop": "Цех 1"
            }
            """);

            var result = service.Load();

            Assert.True(result.IsSuccess);
            var node = Assert.Single(result.Data!.Workshops["Цех 1"]);
            Assert.NotNull(node.Details);
            Assert.Equal(string.Empty, node.Details.Description);
            Assert.Equal(string.Empty, node.Details.Location);
            Assert.Equal(string.Empty, node.Details.PhotoPath);
            Assert.Equal(string.Empty, node.Details.IpAddress);
            Assert.Equal(string.Empty, node.Details.SchemaLink);
        }
        finally
        {
            Directory.Delete(tempDirectory, recursive: true);
        }
    }

    [Fact]
    public void Load_WhenSchemaVersionIsFromFutureVersion_ReturnsValidationError()
    {
        string tempDirectory = CreateTempDirectory();

        try
        {
            string path = Path.Combine(tempDirectory, "kb.json");
            var service = new JsonStorageService(path);
            File.WriteAllText(path, """
            {
              "SchemaVersion": 4,
              "Config": {
                "MaxLevels": 1,
                "LevelNames": ["Цех"]
              },
              "Workshops": {
                "Цех 1": []
              },
              "LastWorkshop": "Цех 1"
            }
            """);

            var result = service.Load();

            Assert.False(result.IsSuccess);
            Assert.NotNull(result.ErrorMessage);
            Assert.Contains("более новой версией приложения", result.ErrorMessage);
        }
        finally
        {
            Directory.Delete(tempDirectory, recursive: true);
        }
    }

    [Fact]
    public void Load_WhenWorkshopNamesConflictAfterTrim_ReturnsValidationError()
    {
        string tempDirectory = CreateTempDirectory();

        try
        {
            string path = Path.Combine(tempDirectory, "kb.json");
            var service = new JsonStorageService(path);
            File.WriteAllText(path, """
            {
              "SchemaVersion": 2,
              "Config": {
                "MaxLevels": 1,
                "LevelNames": ["Цех"]
              },
              "Workshops": {
                "Цех 1": [],
                " Цех 1 ": []
              },
              "LastWorkshop": "Цех 1"
            }
            """);

            var result = service.Load();

            Assert.False(result.IsSuccess);
            Assert.NotNull(result.ErrorMessage);
            Assert.Contains("конфликтующие названия цехов", result.ErrorMessage);
        }
        finally
        {
            Directory.Delete(tempDirectory, recursive: true);
        }
    }

    [Fact]
    public void Load_WhenWorkshopNamesConflictOnlyByCase_ReturnsValidationError()
    {
        string tempDirectory = CreateTempDirectory();

        try
        {
            string path = Path.Combine(tempDirectory, "kb.json");
            var service = new JsonStorageService(path);
            File.WriteAllText(path, """
            {
              "SchemaVersion": 2,
              "Config": {
                "MaxLevels": 1,
                "LevelNames": ["Цех"]
              },
              "Workshops": {
                "Цех 1": [],
                "цех 1": []
              },
              "LastWorkshop": "Цех 1"
            }
            """);

            var result = service.Load();

            Assert.False(result.IsSuccess);
            Assert.NotNull(result.ErrorMessage);
            Assert.Contains("конфликтующие названия цехов", result.ErrorMessage);
        }
        finally
        {
            Directory.Delete(tempDirectory, recursive: true);
        }
    }

    private static SavedData CreateSampleData(string lastWorkshop)
    {
        var workshops = new Dictionary<string, List<KbNode>>
        {
            ["Цех 1"] = new List<KbNode>
            {
                new()
                {
                    Name = "Линия 1",
                    LevelIndex = 0,
                    Details = new KbNodeDetails
                    {
                        Description = "Схема 1"
                    },
                    Children =
                    {
                        new KbNode
                        {
                            Name = "Щит 1",
                            LevelIndex = 1,
                            Details = new KbNodeDetails
                            {
                                Description = string.Empty
                            }
                        }
                    }
                }
            }
        };

        if (!string.IsNullOrWhiteSpace(lastWorkshop) && !workshops.ContainsKey(lastWorkshop))
            workshops[lastWorkshop] = new List<KbNode>();

        return new SavedData
        {
            SchemaVersion = SavedData.CurrentSchemaVersion,
            Config = new KbConfig
            {
                MaxLevels = 3,
                LevelNames = new List<string> { "Цех", "Линия", "Щит" }
            },
            Workshops = workshops,
            LastWorkshop = lastWorkshop
        };
    }

    private static string CreateTempDirectory()
    {
        string path = Path.Combine(Path.GetTempPath(), $"asutp-tests-{Guid.NewGuid():N}");
        Directory.CreateDirectory(path);
        return path;
    }
}
