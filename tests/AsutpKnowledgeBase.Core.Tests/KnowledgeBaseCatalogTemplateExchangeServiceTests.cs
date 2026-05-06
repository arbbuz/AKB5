using System.Text;
using System.Text.Json;
using AsutpKnowledgeBase.Models;
using AsutpKnowledgeBase.Services;

namespace AsutpKnowledgeBase.Core.Tests;

public class KnowledgeBaseCatalogTemplateExchangeServiceTests
{
    private readonly KnowledgeBaseCatalogTemplateExchangeService _service = new();

    [Fact]
    public void ExportJson_WritesDedicatedCatalogTemplateDocument()
    {
        KnowledgeBaseCatalogTemplateExportResult result = _service.ExportJson(
            new[]
            {
                new KbEquipmentCatalogItem
                {
                    CatalogItemId = " catalog-plc ",
                    EquipmentKind = " ПЛК ",
                    Manufacturer = " Siemens ",
                    Model = " CPU 1214C "
                }
            },
            new[]
            {
                CreateTemplate("template-cabinet", "Шкаф управления", KbNodeType.Cabinet)
            });

        Assert.True(result.IsSuccess, result.ErrorMessage);
        Assert.Equal(1, result.ExportedCatalogItemCount);
        Assert.Equal(1, result.ExportedTemplateCount);

        string json = Encoding.UTF8.GetString(result.JsonBytes);
        using JsonDocument document = JsonDocument.Parse(json);
        Assert.Equal(1, document.RootElement.GetProperty("ExchangeSchemaVersion").GetInt32());
        Assert.Equal(JsonValueKind.Array, document.RootElement.GetProperty("EquipmentCatalogItems").ValueKind);
        Assert.Equal(JsonValueKind.Array, document.RootElement.GetProperty("ObjectTemplates").ValueKind);
        Assert.False(document.RootElement.TryGetProperty("Workshops", out _));
        Assert.Contains("CPU 1214C", json, StringComparison.Ordinal);
        Assert.Contains("Шкаф управления", json, StringComparison.Ordinal);
    }

    [Fact]
    public void ImportJson_MergesWithoutOverwritingExistingCatalogItemsOrTemplates()
    {
        var currentCatalog = new[]
        {
            new KbEquipmentCatalogItem
            {
                CatalogItemId = "catalog-plc",
                EquipmentKind = "ПЛК",
                Manufacturer = "Siemens",
                Model = "CPU 1214C",
                Description = "Текущая запись"
            }
        };
        var currentTemplates = new[]
        {
            CreateTemplate("template-plc", "ПЛК", KbNodeType.Controller)
        };
        KnowledgeBaseCatalogTemplateExportResult exportResult = _service.ExportJson(
            new[]
            {
                new KbEquipmentCatalogItem
                {
                    CatalogItemId = "catalog-plc-imported",
                    EquipmentKind = "плк",
                    Manufacturer = "siemens",
                    Model = "cpu 1214c",
                    Description = "Импортированная копия"
                },
                new KbEquipmentCatalogItem
                {
                    CatalogItemId = "catalog-ups",
                    EquipmentKind = "ИБП",
                    Manufacturer = "Phoenix Contact",
                    Model = "QUINT UPS"
                }
            },
            new[]
            {
                CreateTemplate("template-plc", "ПЛК из импорта", KbNodeType.Controller),
                CreateTemplate("template-hmi", "Панель оператора", KbNodeType.Device)
            });
        Assert.True(exportResult.IsSuccess, exportResult.ErrorMessage);

        KnowledgeBaseCatalogTemplateImportResult result = _service.ImportJson(
            exportResult.JsonBytes,
            currentCatalog,
            currentTemplates);

        Assert.True(result.IsSuccess, result.ErrorMessage);
        Assert.Equal(2, result.ImportedCatalogItemCount);
        Assert.Equal(2, result.ImportedTemplateCount);
        Assert.Equal(1, result.AddedCatalogItemCount);
        Assert.Equal(1, result.SkippedCatalogItemCount);
        Assert.Equal(1, result.AddedTemplateCount);
        Assert.Equal(1, result.SkippedTemplateCount);

        Assert.Equal(2, result.EquipmentCatalogItems.Count);
        Assert.Contains(result.EquipmentCatalogItems, item => item.CatalogItemId == "catalog-ups");
        KbEquipmentCatalogItem currentItem = Assert.Single(
            result.EquipmentCatalogItems,
            item => item.CatalogItemId == "catalog-plc");
        Assert.Equal("Текущая запись", currentItem.Description);

        Assert.Equal(2, result.ObjectTemplates.Count);
        Assert.Contains(result.ObjectTemplates, template => template.TemplateId == "template-hmi");
        KbObjectTemplate currentTemplate = Assert.Single(
            result.ObjectTemplates,
            template => template.TemplateId == "template-plc");
        Assert.Equal("ПЛК", currentTemplate.DisplayName);
    }

    [Fact]
    public void ImportJson_WhenExchangeSectionsAreMissing_ReturnsFailure()
    {
        byte[] jsonBytes = Encoding.UTF8.GetBytes("{\"Workshops\":{}}");

        KnowledgeBaseCatalogTemplateImportResult result = _service.ImportJson(
            jsonBytes,
            Array.Empty<KbEquipmentCatalogItem>(),
            Array.Empty<KbObjectTemplate>());

        Assert.False(result.IsSuccess);
        Assert.Contains("EquipmentCatalogItems", result.ErrorMessage, StringComparison.Ordinal);
        Assert.Contains("ObjectTemplates", result.ErrorMessage, StringComparison.Ordinal);
    }

    private static KbObjectTemplate CreateTemplate(
        string templateId,
        string displayName,
        KbNodeType nodeType) =>
        new()
        {
            TemplateId = templateId,
            DisplayName = displayName,
            RootNode = new KbObjectTemplateNode
            {
                TemplateNodeId = $"{templateId}-root",
                Name = displayName,
                NodeType = nodeType
            }
        };
}
