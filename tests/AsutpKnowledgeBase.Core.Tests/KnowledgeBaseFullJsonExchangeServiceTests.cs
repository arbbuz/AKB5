using System.Text.Json;
using AsutpKnowledgeBase.Models;
using AsutpKnowledgeBase.Services;

namespace AsutpKnowledgeBase.Core.Tests;

public class KnowledgeBaseFullJsonExchangeServiceTests
{
    private readonly KnowledgeBaseFullJsonExchangeService _service = new();

    [Fact]
    public void ExportJson_WritesReadableFullSavedDataDocument()
    {
        SavedData source = CreateSampleData();

        KnowledgeBaseFullJsonExportResult result = _service.ExportJson(source);

        Assert.True(result.IsSuccess, result.ErrorMessage);
        string json = System.Text.Encoding.UTF8.GetString(result.JsonBytes);
        Assert.Contains("\"SchemaVersion\"", json);
        Assert.Contains("Цех 1", json);

        SavedData? parsed = JsonSerializer.Deserialize<SavedData>(json);
        Assert.NotNull(parsed);
        Assert.Equal("Цех 1", parsed!.LastWorkshop);
    }

    [Fact]
    public void ImportJson_NormalizesSupportedSavedData()
    {
        SavedData source = CreateSampleData();
        KnowledgeBaseFullJsonExportResult exportResult = _service.ExportJson(source);

        KnowledgeBaseFullJsonImportResult result = _service.ImportJson(exportResult.JsonBytes);

        Assert.True(result.IsSuccess, result.ErrorMessage);
        Assert.NotNull(result.Data);
        Assert.Equal(SavedData.CurrentSchemaVersion, result.Data!.SchemaVersion);
        Assert.Equal("Цех 1", result.Data.LastWorkshop);
        Assert.Single(result.Data.Workshops["Цех 1"]);
        Assert.Equal("node-1", result.Data.Workshops["Цех 1"].Single().NodeId);
    }

    [Fact]
    public void ImportJson_WhenSchemaIsTooNew_ReturnsFailure()
    {
        var data = CreateSampleData();
        data.SchemaVersion = SavedData.CurrentSchemaVersion + 1;
        byte[] json = JsonSerializer.SerializeToUtf8Bytes(data);

        KnowledgeBaseFullJsonImportResult result = _service.ImportJson(json);

        Assert.False(result.IsSuccess);
        Assert.Contains("более новой версией", result.ErrorMessage);
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
                        NodeType = KbNodeType.System
                    }
                }
            },
            LastWorkshop = "Цех 1"
        };
}
