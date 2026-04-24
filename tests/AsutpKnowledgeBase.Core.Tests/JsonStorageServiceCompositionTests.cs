using System.Text.Json;
using AsutpKnowledgeBase.Models;
using AsutpKnowledgeBase.Services;

namespace AsutpKnowledgeBase.Core.Tests;

public class JsonStorageServiceCompositionTests
{
    [Fact]
    public void SaveAndLoad_RoundTripsCompositionEntries()
    {
        string tempDirectory = CreateTempDirectory();

        try
        {
            string path = Path.Combine(tempDirectory, "kb.json");
            var service = new JsonStorageService(path);
            var data = new SavedData
            {
                SchemaVersion = SavedData.CurrentSchemaVersion,
                Config = new KbConfig
                {
                    MaxLevels = 2,
                    LevelNames = new List<string> { "Цех", "Шкаф" }
                },
                Workshops = new Dictionary<string, List<KbNode>>
                {
                    ["Цех 1"] = new List<KbNode>
                    {
                        new()
                        {
                            NodeId = "cabinet-1",
                            Name = "Шкаф 1",
                            LevelIndex = 0,
                            NodeType = KbNodeType.Cabinet
                        }
                    }
                },
                CompositionEntries = new List<KbCompositionEntry>
                {
                    new()
                    {
                        EntryId = "entry-1",
                        ParentNodeId = "cabinet-1",
                        SlotNumber = 1,
                        PositionOrder = 0,
                        ComponentType = "CPU",
                        Model = "S7-1500",
                        IpAddress = "10.10.0.15",
                        Notes = "Main PLC"
                    }
                },
                LastWorkshop = "Цех 1"
            };

            Assert.True(service.Save(data, out var errorMessage));
            Assert.Null(errorMessage);

            var loaded = service.Load();

            Assert.True(loaded.IsSuccess);
            var entry = Assert.Single(loaded.Data!.CompositionEntries);
            Assert.Equal("entry-1", entry.EntryId);
            Assert.Equal("cabinet-1", entry.ParentNodeId);
            Assert.Equal(1, entry.SlotNumber);
            Assert.Equal("CPU", entry.ComponentType);
            Assert.Equal("S7-1500", entry.Model);
            Assert.Equal("10.10.0.15", entry.IpAddress);
            Assert.Equal("Main PLC", entry.Notes);
        }
        finally
        {
            Directory.Delete(tempDirectory, recursive: true);
        }
    }

    [Fact]
    public void Save_NormalizesCompositionEntriesBeforeWriting()
    {
        string tempDirectory = CreateTempDirectory();

        try
        {
            string path = Path.Combine(tempDirectory, "kb.json");
            var service = new JsonStorageService(path);

            Assert.True(service.Save(
                new SavedData
                {
                    SchemaVersion = SavedData.CurrentSchemaVersion,
                    Config = new KbConfig
                    {
                        MaxLevels = 2,
                        LevelNames = new List<string> { "Цех", "Шкаф" }
                    },
                    Workshops = new Dictionary<string, List<KbNode>>
                    {
                        ["Цех 1"] = new List<KbNode>
                        {
                            new()
                            {
                                NodeId = "cabinet-1",
                                Name = "Шкаф 1",
                                LevelIndex = 0,
                                NodeType = KbNodeType.Cabinet
                            }
                        }
                    },
                    CompositionEntries = new List<KbCompositionEntry>
                    {
                        new()
                        {
                            ParentNodeId = " cabinet-1 ",
                            PositionOrder = -1,
                            ComponentType = " CPU ",
                            Model = " PLC-1 "
                        }
                    },
                    LastWorkshop = "Цех 1"
                },
                out _));

            var saved = JsonSerializer.Deserialize<SavedData>(File.ReadAllText(path));

            var entry = Assert.Single(saved!.CompositionEntries);
            Assert.Equal("cabinet-1", entry.ParentNodeId);
            Assert.Equal(0, entry.PositionOrder);
            Assert.Equal("CPU", entry.ComponentType);
            Assert.Equal("PLC-1", entry.Model);
            Assert.False(string.IsNullOrWhiteSpace(entry.EntryId));
        }
        finally
        {
            Directory.Delete(tempDirectory, recursive: true);
        }
    }

    private static string CreateTempDirectory()
    {
        string path = Path.Combine(Path.GetTempPath(), $"asutp-composition-tests-{Guid.NewGuid():N}");
        Directory.CreateDirectory(path);
        return path;
    }
}
