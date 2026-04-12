using System.Text.Json;
using AsutpKnowledgeBase.Models;
using AsutpKnowledgeBase.Services;

namespace AsutpKnowledgeBase.Core.Tests;

public class KnowledgeBaseFileWorkflowServiceTests
{
    [Fact]
    public void Load_WhenFileMissing_CreatesDefaultDataAndSavesIt()
    {
        string tempDirectory = CreateTempDirectory();

        try
        {
            string path = Path.Combine(tempDirectory, "kb.json");
            var session = new KnowledgeBaseSessionService();
            var workflow = new KnowledgeBaseFileWorkflowService(session, new JsonStorageService(path));

            var result = workflow.Load();

            Assert.Equal(KnowledgeBaseFileLoadOutcome.CreatedDefaultAndSaved, result.Outcome);
            Assert.NotNull(result.ViewState);
            Assert.Equal("Новый цех", result.ViewState!.CurrentWorkshop);
            Assert.Equal(new[] { "Новый цех" }, result.ViewState.WorkshopNames);
            Assert.Empty(result.ViewState.CurrentRoots);
            Assert.True(File.Exists(path));
            Assert.Equal("Новый цех", session.CurrentWorkshop);
            Assert.Equal("Новый цех", session.LastSavedWorkshop);
            Assert.False(session.IsDirty);
            Assert.False(session.RequiresSave);
        }
        finally
        {
            Directory.Delete(tempDirectory, recursive: true);
        }
    }

    [Fact]
    public void Load_WhenExistingFileLoaded_ReturnsViewStateWithCurrentWorkshopAndRoots()
    {
        string tempDirectory = CreateTempDirectory();

        try
        {
            string path = Path.Combine(tempDirectory, "kb.json");
            var storage = new JsonStorageService(path);
            var sample = CreateSampleData(lastWorkshop: "Цех 1");

            Assert.True(storage.Save(sample, out _));

            var session = new KnowledgeBaseSessionService();
            var workflow = new KnowledgeBaseFileWorkflowService(session, storage);

            var result = workflow.Load(createDefaultIfMissing: false, fallbackToDefaultOnError: false);

            Assert.Equal(KnowledgeBaseFileLoadOutcome.LoadedExisting, result.Outcome);
            Assert.NotNull(result.ViewState);
            Assert.Equal("Цех 1", result.ViewState!.CurrentWorkshop);
            Assert.Equal(new[] { "Цех 1", "Цех 2" }, result.ViewState.WorkshopNames);
            Assert.Single(result.ViewState.CurrentRoots);
            Assert.Equal("Линия 1", result.ViewState.CurrentRoots[0].Name);
        }
        finally
        {
            Directory.Delete(tempDirectory, recursive: true);
        }
    }

    [Fact]
    public void Load_WhenPrimaryFileIsBrokenAndFallbackDisabled_ReturnsLoadError()
    {
        string tempDirectory = CreateTempDirectory();

        try
        {
            string path = Path.Combine(tempDirectory, "kb.json");
            File.WriteAllText(path, "{ broken json");

            var session = new KnowledgeBaseSessionService();
            var workflow = new KnowledgeBaseFileWorkflowService(session, new JsonStorageService(path));

            var result = workflow.Load(createDefaultIfMissing: false, fallbackToDefaultOnError: false);

            Assert.Equal(KnowledgeBaseFileLoadOutcome.LoadError, result.Outcome);
            Assert.False(result.IsSuccess);
            Assert.Empty(session.Workshops);
            Assert.Equal(string.Empty, session.CurrentWorkshop);
        }
        finally
        {
            Directory.Delete(tempDirectory, recursive: true);
        }
    }

    [Fact]
    public void Load_WhenPrimaryFileIsBrokenButBackupExists_LoadsBackupAndMarksSessionForSave()
    {
        string tempDirectory = CreateTempDirectory();

        try
        {
            string path = Path.Combine(tempDirectory, "kb.json");
            string backupPath = $"{path}.bak";
            var logger = new InMemoryAppLogger();
            var storage = new JsonStorageService(path, logger);
            var sample = CreateSampleData(lastWorkshop: "Цех 2");

            Assert.True(storage.Save(sample, out _));
            File.Copy(path, backupPath, overwrite: true);
            File.WriteAllText(path, "{ broken json");

            var session = new KnowledgeBaseSessionService();
            var workflow = new KnowledgeBaseFileWorkflowService(session, storage, logger);

            var result = workflow.Load();

            Assert.Equal(KnowledgeBaseFileLoadOutcome.LoadedBackup, result.Outcome);
            Assert.Equal(backupPath, result.SourcePath);
            Assert.True(session.RequiresSave);
            Assert.Equal("Цех 2", session.CurrentWorkshop);
            Assert.True(session.Workshops.ContainsKey("Цех 1"));

            var logEntry = Assert.Single(logger.Entries.Where(entry => entry.EventName == "FileWorkflowLoadBackup"));
            Assert.Equal(AppLogLevel.Warning, logEntry.Level);
            Assert.Equal(path, logEntry.Properties["savePath"]);
            Assert.Equal(backupPath, logEntry.Properties["sourcePath"]);
        }
        finally
        {
            Directory.Delete(tempDirectory, recursive: true);
        }
    }

    [Fact]
    public void Save_PersistsCurrentRootsAndClearsDirtyState()
    {
        string tempDirectory = CreateTempDirectory();

        try
        {
            string path = Path.Combine(tempDirectory, "kb.json");
            var session = new KnowledgeBaseSessionService();
            session.ApplyLoadedData(CreateSampleData(lastWorkshop: "Цех 1"), recordAsSavedState: true);

            var currentRoots = new List<KbNode>
            {
                new() { Name = "Новый корень", LevelIndex = 0 }
            };
            session.RefreshDirtyState(currentRoots);

            var workflow = new KnowledgeBaseFileWorkflowService(session, new JsonStorageService(path));
            var result = workflow.Save(currentRoots);

            Assert.True(result.IsSuccess);
            Assert.False(session.IsDirty);
            Assert.False(session.RequiresSave);

            var saved = JsonSerializer.Deserialize<SavedData>(File.ReadAllText(path));
            Assert.NotNull(saved);
            Assert.Equal("Цех 1", saved!.LastWorkshop);
            Assert.Equal("Новый корень", saved.Workshops["Цех 1"].Single().Name);
        }
        finally
        {
            Directory.Delete(tempDirectory, recursive: true);
        }
    }

    [Fact]
    public void ReplaceAllData_PersistsImportedDataAndUpdatesSession()
    {
        string tempDirectory = CreateTempDirectory();

        try
        {
            string path = Path.Combine(tempDirectory, "kb.json");
            var session = new KnowledgeBaseSessionService();
            var workflow = new KnowledgeBaseFileWorkflowService(session, new JsonStorageService(path));

            Assert.Equal(KnowledgeBaseFileLoadOutcome.CreatedDefaultAndSaved, workflow.Load().Outcome);

            var importedData = new SavedData
            {
                SchemaVersion = SavedData.CurrentSchemaVersion,
                Config = new KbConfig
                {
                    MaxLevels = 3,
                    LevelNames = new List<string> { " Цех ", "Линия", "Щит" }
                },
                Workshops = new Dictionary<string, List<KbNode>>
                {
                    ["  Цех 2  "] = new List<KbNode>
                    {
                        new()
                        {
                            Name = "Импортированный корень",
                            LevelIndex = 0
                        }
                    },
                    ["   "] = new List<KbNode>
                    {
                        new()
                        {
                            Name = "Игнорируемый корень",
                            LevelIndex = 0
                        }
                    }
                },
                LastWorkshop = "  Цех 2  "
            };

            var result = workflow.ReplaceAllData(importedData);

            Assert.True(result.IsSuccess);
            Assert.NotNull(result.ViewState);
            Assert.Equal("Цех 2", session.CurrentWorkshop);
            Assert.Equal("Цех 2", session.LastSavedWorkshop);
            Assert.False(session.IsDirty);
            Assert.False(session.RequiresSave);
            Assert.Equal("Цех 2", result.ViewState!.CurrentWorkshop);
            Assert.Equal(new[] { "Цех 2" }, result.ViewState.WorkshopNames);
            Assert.Single(result.ViewState.CurrentRoots);
            Assert.Equal("Импортированный корень", result.ViewState.CurrentRoots[0].Name);
            Assert.Equal("Импортированный корень", session.Workshops["Цех 2"].Single().Name);

            string json = File.ReadAllText(path);
            var saved = JsonSerializer.Deserialize<SavedData>(json);
            Assert.NotNull(saved);
            Assert.Equal("Цех 2", saved!.LastWorkshop);
            Assert.Equal(new[] { "Цех 2" }, saved.Workshops.Keys);
            Assert.Equal("Импортированный корень", saved.Workshops["Цех 2"].Single().Name);
            Assert.True(File.Exists($"{path}.bak"));
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
                        LevelIndex = 0
                    }
                },
                ["Цех 2"] = new List<KbNode>()
            },
            LastWorkshop = lastWorkshop
        };

    private static string CreateTempDirectory()
    {
        string path = Path.Combine(Path.GetTempPath(), $"asutp-file-workflow-tests-{Guid.NewGuid():N}");
        Directory.CreateDirectory(path);
        return path;
    }
}
