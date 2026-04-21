using System.Drawing;
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
    public void SaveAndLoad_RoundTripsMainWindowPlacement()
    {
        string tempDirectory = CreateTempDirectory();

        try
        {
            string path = Path.Combine(tempDirectory, "window-layout-state.json");
            var service = new KnowledgeBaseWindowLayoutStateService(path);

            service.SaveWindowPlacement(
                new KnowledgeBaseWindowPlacement
                {
                    Left = 120,
                    Top = 80,
                    Width = 1440,
                    Height = 900,
                    IsMaximized = true
                });

            var loaded = service.LoadWindowPlacement();

            Assert.NotNull(loaded);
            Assert.Equal(120, loaded!.Left);
            Assert.Equal(80, loaded.Top);
            Assert.Equal(1440, loaded.Width);
            Assert.Equal(900, loaded.Height);
            Assert.True(loaded.IsMaximized);
        }
        finally
        {
            Directory.Delete(tempDirectory, recursive: true);
        }
    }

    [Fact]
    public void SaveSplitterDistance_PreservesExistingWindowPlacement()
    {
        string tempDirectory = CreateTempDirectory();

        try
        {
            string path = Path.Combine(tempDirectory, "window-layout-state.json");
            var service = new KnowledgeBaseWindowLayoutStateService(path);

            service.SaveWindowPlacement(
                new KnowledgeBaseWindowPlacement
                {
                    Left = 40,
                    Top = 50,
                    Width = 1280,
                    Height = 720
                });

            service.SaveSplitterDistance(360);

            var loadedPlacement = service.LoadWindowPlacement();

            Assert.NotNull(loadedPlacement);
            Assert.Equal(40, loadedPlacement!.Left);
            Assert.Equal(50, loadedPlacement.Top);
            Assert.Equal(1280, loadedPlacement.Width);
            Assert.Equal(720, loadedPlacement.Height);
            Assert.Equal(360, service.LoadSplitterDistance());
        }
        finally
        {
            Directory.Delete(tempDirectory, recursive: true);
        }
    }

    [Fact]
    public void SaveWindowPlacement_PreservesExistingSplitterDistance()
    {
        string tempDirectory = CreateTempDirectory();

        try
        {
            string path = Path.Combine(tempDirectory, "window-layout-state.json");
            var service = new KnowledgeBaseWindowLayoutStateService(path);

            service.SaveSplitterDistance(410);
            service.SaveWindowPlacement(
                new KnowledgeBaseWindowPlacement
                {
                    Left = 10,
                    Top = 20,
                    Width = 1024,
                    Height = 768
                });

            Assert.Equal(410, service.LoadSplitterDistance());

            var loadedPlacement = service.LoadWindowPlacement();
            Assert.NotNull(loadedPlacement);
            Assert.Equal(10, loadedPlacement!.Left);
            Assert.Equal(20, loadedPlacement.Top);
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
                    "Р¦РµС… 1": 410,
                    "Р¦РµС… 2": 320
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

            int? loadedSplitter = service.LoadSplitterDistance();
            var loadedPlacement = service.LoadWindowPlacement();

            Assert.Null(loadedSplitter);
            Assert.Null(loadedPlacement);
            Assert.Equal(2, logger.Entries.Count);
            Assert.All(logger.Entries, entry =>
            {
                Assert.Equal("WindowLayoutStateLoadFailed", entry.EventName);
                Assert.Equal(AppLogLevel.Warning, entry.Level);
                Assert.Equal(path, entry.Properties["path"]);
            });
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

    [Fact]
    public void LoadWindowPlacement_IgnoresStateWithNonPositiveSize()
    {
        string tempDirectory = CreateTempDirectory();

        try
        {
            string path = Path.Combine(tempDirectory, "window-layout-state.json");
            File.WriteAllText(
                path,
                """
                {
                  "MainWindowPlacement": {
                    "Left": 20,
                    "Top": 30,
                    "Width": 0,
                    "Height": 800,
                    "IsMaximized": false
                  }
                }
                """);
            var service = new KnowledgeBaseWindowLayoutStateService(path);

            Assert.Null(service.LoadWindowPlacement());
        }
        finally
        {
            Directory.Delete(tempDirectory, recursive: true);
        }
    }

    [Fact]
    public void FitWindowBounds_ClampsBoundsIntoWorkingArea()
    {
        Rectangle fitted = KnowledgeBaseWindowLayoutStateService.FitWindowBounds(
            new Rectangle(-5000, 1800, 2600, 1600),
            new Rectangle(0, 0, 1920, 1080),
            new Size(1080, 640));

        Assert.Equal(new Rectangle(0, 0, 1920, 1080), fitted);
    }

    [Fact]
    public void FitWindowBounds_UsesMinimumSizeWhenRequestedBoundsAreTooSmall()
    {
        Rectangle fitted = KnowledgeBaseWindowLayoutStateService.FitWindowBounds(
            new Rectangle(50, 40, 10, 20),
            new Rectangle(0, 0, 1600, 900),
            new Size(1080, 640));

        Assert.Equal(new Rectangle(50, 40, 1080, 640), fitted);
    }

    private static string CreateTempDirectory()
    {
        string path = Path.Combine(Path.GetTempPath(), $"asutp-window-layout-tests-{Guid.NewGuid():N}");
        Directory.CreateDirectory(path);
        return path;
    }
}
