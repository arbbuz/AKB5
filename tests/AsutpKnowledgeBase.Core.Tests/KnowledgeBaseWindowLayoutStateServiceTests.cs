using System.Text.Json;
using AsutpKnowledgeBase.Services;

namespace AsutpKnowledgeBase.Core.Tests;

public class KnowledgeBaseWindowLayoutStateServiceTests
{
    [Fact]
    public void SaveAndLoad_RoundTripsSingleSplitterDistance()
    {
        string tempDirectory = CreateTempDirectory();

        try
        {
            string path = Path.Combine(tempDirectory, "window-layout-state.json");
            var service = new KnowledgeBaseWindowLayoutStateService(path);

            service.SaveSplitterDistance(320);

            int? loaded = service.LoadSplitterDistance();

            Assert.Equal(320, loaded);

            using JsonDocument document = JsonDocument.Parse(File.ReadAllText(path));
            Assert.Equal(320, document.RootElement
                .GetProperty("SplitterDistance")
                .GetInt32());
        }
        finally
        {
            Directory.Delete(tempDirectory, recursive: true);
        }
    }

    [Fact]
    public void Load_WhenLegacyWorkshopMapExists_UsesFirstValidLegacyValue()
    {
        string tempDirectory = CreateTempDirectory();

        try
        {
            string path = Path.Combine(tempDirectory, "window-layout-state.json");
            File.WriteAllText(
                path,
                """
                {
                  "SplitterDistancesByWorkshop": {
                    "Цех 1": 410,
                    "Цех 2": 320
                  }
                }
                """);
            var service = new KnowledgeBaseWindowLayoutStateService(path);

            int? loaded = service.LoadSplitterDistance();

            Assert.Equal(410, loaded);
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

            int? loaded = service.LoadSplitterDistance();

            Assert.Null(loaded);

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
    public void Save_IgnoresNonPositiveSplitterDistance()
    {
        string tempDirectory = CreateTempDirectory();

        try
        {
            string path = Path.Combine(tempDirectory, "window-layout-state.json");
            var service = new KnowledgeBaseWindowLayoutStateService(path);

            service.SaveSplitterDistance(0);

            int? loaded = service.LoadSplitterDistance();

            Assert.Null(loaded);
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
