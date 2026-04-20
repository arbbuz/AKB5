using System.Text.Json;
using AsutpKnowledgeBase.Services;

namespace AsutpKnowledgeBase.Core.Tests;

public class KnowledgeBaseWindowLayoutStateServiceTests
{
    [Fact]
    public void SaveAndLoad_RoundTripsSplitterDistancesByWorkshop()
    {
        string tempDirectory = CreateTempDirectory();

        try
        {
            string path = Path.Combine(tempDirectory, "window-layout-state.json");
            var service = new KnowledgeBaseWindowLayoutStateService(path);

            service.SaveSplitterDistancesByWorkshop(new Dictionary<string, int>
            {
                ["Цех 1"] = 320,
                ["Цех 2"] = 410
            });

            var loaded = service.LoadSplitterDistancesByWorkshop();

            Assert.Equal(2, loaded.Count);
            Assert.Equal(320, loaded["Цех 1"]);
            Assert.Equal(410, loaded["Цех 2"]);

            using JsonDocument document = JsonDocument.Parse(File.ReadAllText(path));
            Assert.Equal(320, document.RootElement
                .GetProperty("SplitterDistancesByWorkshop")
                .GetProperty("Цех 1")
                .GetInt32());
        }
        finally
        {
            Directory.Delete(tempDirectory, recursive: true);
        }
    }

    [Fact]
    public void Load_WhenStateFileIsBroken_ReturnsEmptyStateAndLogsWarning()
    {
        string tempDirectory = CreateTempDirectory();

        try
        {
            string path = Path.Combine(tempDirectory, "window-layout-state.json");
            File.WriteAllText(path, "{ broken json");
            var logger = new InMemoryAppLogger();
            var service = new KnowledgeBaseWindowLayoutStateService(path, logger);

            var loaded = service.LoadSplitterDistancesByWorkshop();

            Assert.Empty(loaded);

            var entry = Assert.Single(logger.Entries);
            Assert.Equal("WindowLayoutStateLoadFailed", entry.EventName);
            Assert.Equal(AppLogLevel.Warning, entry.Level);
            Assert.Equal(path, entry.Properties["path"]);
        }
        finally
        {
            Directory.Delete(tempDirectory, recursive: true);
        }
    }

    [Fact]
    public void Save_IgnoresInvalidWorkshopEntries()
    {
        string tempDirectory = CreateTempDirectory();

        try
        {
            string path = Path.Combine(tempDirectory, "window-layout-state.json");
            var service = new KnowledgeBaseWindowLayoutStateService(path);

            service.SaveSplitterDistancesByWorkshop(new Dictionary<string, int>
            {
                ["Цех 1"] = 320,
                ["   "] = 410,
                ["Цех 2"] = 0
            });

            var loaded = service.LoadSplitterDistancesByWorkshop();

            Assert.Single(loaded);
            Assert.Equal(320, loaded["Цех 1"]);
        }
        finally
        {
            Directory.Delete(tempDirectory, recursive: true);
        }
    }

    private static string CreateTempDirectory()
    {
        string path = Path.Combine(Path.GetTempPath(), $"asutp-window-layout-tests-{Guid.NewGuid():N}");
        Directory.CreateDirectory(path);
        return path;
    }
}
