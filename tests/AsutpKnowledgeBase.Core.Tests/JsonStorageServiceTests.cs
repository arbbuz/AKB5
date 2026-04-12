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

    private static SavedData CreateSampleData(string lastWorkshop) =>
        new()
        {
            SchemaVersion = SavedData.CurrentSchemaVersion,
            Config = new KbConfig
            {
                MaxLevels = 3,
                LevelNames = new List<string> { "Цех", "Линия", "Щит" }
            },
            Workshops = new Dictionary<string, List<KbNode>>
            {
                ["Цех 1"] = new List<KbNode>
                {
                    new()
                    {
                        Name = "Линия 1",
                        LevelIndex = 0,
                        Children =
                        {
                            new KbNode
                            {
                                Name = "Щит 1",
                                LevelIndex = 1
                            }
                        }
                    }
                }
            },
            LastWorkshop = lastWorkshop
        };

    private static string CreateTempDirectory()
    {
        string path = Path.Combine(Path.GetTempPath(), $"asutp-tests-{Guid.NewGuid():N}");
        Directory.CreateDirectory(path);
        return path;
    }
}
