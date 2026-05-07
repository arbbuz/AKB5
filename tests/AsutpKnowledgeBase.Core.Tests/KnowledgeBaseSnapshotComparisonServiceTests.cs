using AsutpKnowledgeBase.Models;
using AsutpKnowledgeBase.Services;

namespace AsutpKnowledgeBase.Core.Tests;

public class KnowledgeBaseSnapshotComparisonServiceTests
{
    [Fact]
    public void Compare_ReportsAddedRemovedAndChangedSummaryByArea()
    {
        var service = new KnowledgeBaseSnapshotComparisonService();
        SavedData left = CreateData();
        SavedData right = CreateData();
        right.Workshops.Remove("Цех Б");
        right.Workshops["Цех В"] = new List<KbNode>();
        right.Workshops["Цех А"][0].Name = "Линия 1 изменена";
        right.Workshops["Цех А"].Add(new KbNode
        {
            NodeId = "node-2",
            Name = "Линия 2",
            LevelIndex = 0,
            NodeType = KbNodeType.System
        });
        right.DocumentLinks[0].Title = "Схема изменена";
        right.DocumentLinks.Add(new KbDocumentLink
        {
            DocumentId = "doc-2",
            OwnerNodeId = "node-1",
            Title = "Инструкция",
            Path = @"C:\docs\manual.pdf"
        });
        right.SoftwareRecords.Clear();
        right.Config.ProductionCalendarYears[0].AdditionalNonWorkingDays.Add(new DateOnly(2026, 5, 11));
        right.EquipmentCatalogItems[0].Model = "CPU 1215C";

        KnowledgeBaseSnapshotComparisonResult result = service.Compare(left, right);

        Assert.True(result.HasChanges);
        AssertSection(result, "Цеха", added: 1, removed: 1, changed: 0);
        AssertSection(result, "Узлы дерева", added: 1, removed: 0, changed: 1);
        AssertSection(result, "Документы", added: 1, removed: 0, changed: 1);
        AssertSection(result, "ПО", added: 0, removed: 1, changed: 0);
        AssertSection(result, "Производственный календарь", added: 0, removed: 0, changed: 1);
        AssertSection(result, "Каталог оборудования", added: 0, removed: 0, changed: 1);
    }

    [Fact]
    public void BuildDisplayText_WhenSnapshotsMatch_ReturnsNoChangesMessage()
    {
        var service = new KnowledgeBaseSnapshotComparisonService();

        KnowledgeBaseSnapshotComparisonResult result = service.Compare(CreateData(), CreateData());
        string text = service.BuildDisplayText(result, "Снимок 1", "Снимок 2");

        Assert.False(result.HasChanges);
        Assert.Contains("Отличий", text);
    }

    private static void AssertSection(
        KnowledgeBaseSnapshotComparisonResult result,
        string areaName,
        int added,
        int removed,
        int changed)
    {
        KnowledgeBaseSnapshotComparisonSection section =
            result.Sections.Single(section => section.AreaName == areaName);
        Assert.Equal(added, section.AddedCount);
        Assert.Equal(removed, section.RemovedCount);
        Assert.Equal(changed, section.ChangedCount);
    }

    private static SavedData CreateData() =>
        new()
        {
            SchemaVersion = SavedData.CurrentSchemaVersion,
            Config = new KbConfig
            {
                MaxLevels = 3,
                LevelNames = new List<string> { "Цех", "Линия", "Шкаф" },
                ProductionCalendarYears = new List<KbProductionCalendarYear>
                {
                    new()
                    {
                        Year = 2026,
                        AdditionalNonWorkingDays = new List<DateOnly>(),
                        AdditionalWorkingDays = new List<DateOnly>()
                    }
                }
            },
            Workshops = new Dictionary<string, List<KbNode>>
            {
                ["Цех А"] = new List<KbNode>
                {
                    new()
                    {
                        NodeId = "node-1",
                        Name = "Линия 1",
                        LevelIndex = 0,
                        NodeType = KbNodeType.System
                    }
                },
                ["Цех Б"] = new List<KbNode>()
            },
            DocumentLinks = new List<KbDocumentLink>
            {
                new()
                {
                    DocumentId = "doc-1",
                    OwnerNodeId = "node-1",
                    Title = "Схема",
                    Path = @"C:\docs\scheme.pdf"
                }
            },
            SoftwareRecords = new List<KbSoftwareRecord>
            {
                new()
                {
                    SoftwareId = "soft-1",
                    OwnerNodeId = "node-1",
                    Title = "Проект ПЛК",
                    Path = @"C:\software\plc"
                }
            },
            EquipmentCatalogItems = new List<KbEquipmentCatalogItem>
            {
                new()
                {
                    CatalogItemId = "catalog-1",
                    EquipmentKind = "ПЛК",
                    Manufacturer = "Siemens",
                    Model = "CPU 1214C",
                    DefaultNodeType = KbNodeType.Controller
                }
            },
            LastWorkshop = "Цех А"
        };
}
