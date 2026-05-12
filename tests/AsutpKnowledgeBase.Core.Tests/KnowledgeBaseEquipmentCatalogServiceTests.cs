using AsutpKnowledgeBase.Models;
using AsutpKnowledgeBase.Services;

namespace AsutpKnowledgeBase.Core.Tests;

public class KnowledgeBaseEquipmentCatalogServiceTests
{
    private readonly KnowledgeBaseEquipmentCatalogService _service = new();

    [Fact]
    public void UpsertItem_AddsNewCatalogItemWithStableId()
    {
        KnowledgeBaseEquipmentCatalogMutationResult result = _service.UpsertItem(
            Array.Empty<KbEquipmentCatalogItem>(),
            new KbEquipmentCatalogItem
            {
                EquipmentKind = " ПЛК ",
                Manufacturer = " Siemens ",
                Series = " S7-1200 ",
                Model = " CPU 1214C ",
                DefaultNodeType = KbNodeType.Controller,
                Description = " Контроллер ",
                Properties =
                {
                    new KbEquipmentCatalogProperty { Name = " Питание ", Value = " 24 В DC " }
                }
            });

        Assert.True(result.IsSuccess, result.ErrorMessage);
        KbEquipmentCatalogItem item = Assert.Single(result.EquipmentCatalogItems);
        Assert.StartsWith("catalog-", item.CatalogItemId, StringComparison.Ordinal);
        Assert.Equal("ПЛК", item.EquipmentKind);
        Assert.Equal(KbNodeType.Controller, item.DefaultNodeType);
        Assert.Equal("Питание", Assert.Single(item.Properties).Name);
    }

    [Fact]
    public void UpsertItem_RejectsDuplicateSemanticCatalogItem()
    {
        var currentItems = new[]
        {
            new KbEquipmentCatalogItem
            {
                CatalogItemId = "catalog-plc",
                EquipmentKind = "ПЛК",
                Manufacturer = "Siemens",
                Series = "S7-1200",
                Model = "CPU 1214C"
            }
        };

        KnowledgeBaseEquipmentCatalogMutationResult result = _service.UpsertItem(
            currentItems,
            new KbEquipmentCatalogItem
            {
                EquipmentKind = " плк ",
                Manufacturer = "siemens",
                Series = "s7-1200",
                Model = "cpu 1214c"
            });

        Assert.False(result.IsSuccess);
        Assert.Contains("уже есть запись", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void UpsertItem_UpdatesExistingCatalogItem()
    {
        var currentItems = new[]
        {
            new KbEquipmentCatalogItem
            {
                CatalogItemId = "catalog-hmi",
                EquipmentKind = "Панель оператора",
                Manufacturer = "Siemens",
                Model = "KTP700"
            }
        };

        KnowledgeBaseEquipmentCatalogMutationResult result = _service.UpsertItem(
            currentItems,
            new KbEquipmentCatalogItem
            {
                CatalogItemId = " catalog-hmi ",
                EquipmentKind = "Панель оператора",
                Manufacturer = "Siemens",
                Series = "Basic",
                Model = "KTP700",
                DefaultNodeType = KbNodeType.Device
            });

        Assert.True(result.IsSuccess, result.ErrorMessage);
        KbEquipmentCatalogItem item = Assert.Single(result.EquipmentCatalogItems);
        Assert.Equal("catalog-hmi", item.CatalogItemId);
        Assert.Equal("Basic", item.Series);
    }

    [Fact]
    public void DeleteItem_RemovesSelectedCatalogItem()
    {
        var currentItems = new[]
        {
            new KbEquipmentCatalogItem
            {
                CatalogItemId = "catalog-hmi",
                EquipmentKind = "Панель оператора",
                Model = "KTP700"
            },
            new KbEquipmentCatalogItem
            {
                CatalogItemId = "catalog-plc",
                EquipmentKind = "ПЛК",
                Model = "CPU 1214C"
            }
        };

        KnowledgeBaseEquipmentCatalogMutationResult result = _service.DeleteItem(currentItems, "catalog-hmi");

        Assert.True(result.IsSuccess, result.ErrorMessage);
        KbEquipmentCatalogItem item = Assert.Single(result.EquipmentCatalogItems);
        Assert.Equal("catalog-plc", item.CatalogItemId);
    }

    [Fact]
    public void Search_FindsByVisibleCatalogFields()
    {
        var currentItems = new[]
        {
            new KbEquipmentCatalogItem
            {
                CatalogItemId = "catalog-plc",
                EquipmentKind = "ПЛК",
                Manufacturer = "Siemens",
                Model = "6ES7214-1AG40-0XB0",
                Description = "Запасная позиция"
            },
            new KbEquipmentCatalogItem
            {
                CatalogItemId = "catalog-ups",
                EquipmentKind = "ИБП",
                Manufacturer = "Phoenix Contact",
                Model = "QUINT UPS"
            }
        };

        List<KbEquipmentCatalogItem> result = _service.Search(currentItems, "6ES7214");

        KbEquipmentCatalogItem item = Assert.Single(result);
        Assert.Equal("catalog-plc", item.CatalogItemId);
    }

    [Fact]
    public void Search_DoesNotUseHiddenPropertiesOrNodeType()
    {
        var currentItems = new[]
        {
            new KbEquipmentCatalogItem
            {
                CatalogItemId = "catalog-plc",
                EquipmentKind = "ПЛК",
                Model = "CPU 1214C",
                DefaultNodeType = KbNodeType.Controller,
                Properties =
                {
                    new KbEquipmentCatalogProperty { Name = "Интерфейс", Value = "Profinet" }
                }
            },
            new KbEquipmentCatalogItem
            {
                CatalogItemId = "catalog-ups",
                EquipmentKind = "ИБП",
                Model = "QUINT UPS",
                DefaultNodeType = KbNodeType.Device
            }
        };

        List<KbEquipmentCatalogItem> result = _service.Search(currentItems, "profinet");

        Assert.Empty(result);
    }
}
