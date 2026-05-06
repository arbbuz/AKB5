using AsutpKnowledgeBase.Models;
using AsutpKnowledgeBase.Services;

namespace AsutpKnowledgeBase.Core.Tests;

public class KnowledgeBaseObjectTemplateServiceTests
{
    private readonly KnowledgeBaseObjectTemplateService _service = new();

    [Fact]
    public void CreateInstance_GeneratesFreshNodeIdsAndRemapsDefaults()
    {
        KbObjectTemplate template = CreateCabinetTemplate();

        KnowledgeBaseObjectTemplateInstantiationResult first = _service.CreateInstance(template);
        KnowledgeBaseObjectTemplateInstantiationResult second = _service.CreateInstance(template, "Шкаф 2");

        Assert.True(first.IsSuccess, first.ErrorMessage);
        Assert.True(second.IsSuccess, second.ErrorMessage);
        Assert.NotNull(first.RootNode);
        Assert.NotNull(second.RootNode);

        KbNode firstRoot = first.RootNode!;
        KbNode secondRoot = second.RootNode!;
        Assert.NotEqual("cabinet", firstRoot.NodeId);
        Assert.NotEqual(firstRoot.NodeId, secondRoot.NodeId);
        Assert.Equal("Шкаф управления", firstRoot.Name);
        Assert.Equal("Шкаф 2", secondRoot.Name);
        Assert.Equal(KbNodeType.Cabinet, firstRoot.NodeType);
        Assert.Equal("Основной шкаф", firstRoot.Details.Description);

        KbNode firstController = Assert.Single(firstRoot.Children);
        Assert.NotEqual("controller", firstController.NodeId);
        Assert.NotEqual(firstRoot.NodeId, firstController.NodeId);
        Assert.Equal(KbNodeType.Controller, firstController.NodeType);
        Assert.Equal(firstRoot.NodeId, first.NodeIdMap["cabinet"]);
        Assert.Equal(firstController.NodeId, first.NodeIdMap["controller"]);

        KbCompositionEntry compositionEntry = Assert.Single(first.CompositionEntries);
        Assert.Equal(firstRoot.NodeId, compositionEntry.ParentNodeId);
        Assert.Equal(1, compositionEntry.SlotNumber);
        Assert.Equal("CPU", compositionEntry.ComponentType);
        Assert.Equal(string.Empty, compositionEntry.EntryId);

        KbDocumentLink document = Assert.Single(first.DocumentLinks);
        Assert.Equal(firstRoot.NodeId, document.OwnerNodeId);
        Assert.Equal(KbDocumentKind.SchemeLink, document.Kind);
        Assert.Equal(string.Empty, document.DocumentId);

        KbSoftwareRecord software = Assert.Single(first.SoftwareRecords);
        Assert.Equal(firstController.NodeId, software.OwnerNodeId);
        Assert.Equal("Проект PLC", software.Title);

        KbNetworkFileReference network = Assert.Single(first.NetworkFileReferences);
        Assert.Equal(firstRoot.NodeId, network.OwnerNodeId);
        Assert.Equal(KbNetworkPreviewKind.Image, network.PreviewKind);

        KbMaintenanceScheduleProfile maintenance = Assert.Single(first.MaintenanceScheduleProfiles);
        Assert.Equal(firstRoot.NodeId, maintenance.OwnerNodeId);
        Assert.True(maintenance.IsIncludedInSchedule);
        Assert.Equal(2, maintenance.To1Hours);
        Assert.Equal(12, Assert.Single(maintenance.YearScheduleEntries).Month);
    }

    [Fact]
    public void CreateInstance_WhenTemplateIsInvalid_ReturnsFailure()
    {
        KnowledgeBaseObjectTemplateInstantiationResult result = _service.CreateInstance(
            new KbObjectTemplate
            {
                DisplayName = "Пустой шаблон",
                RootNode = null!
            });

        Assert.False(result.IsSuccess);
        Assert.Null(result.RootNode);
        Assert.Contains("некорректно", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    private static KbObjectTemplate CreateCabinetTemplate() =>
        new()
        {
            TemplateId = "template-cabinet",
            DisplayName = "Шкаф управления",
            RootNode = new KbObjectTemplateNode
            {
                TemplateNodeId = "cabinet",
                Name = "Шкаф управления",
                NodeType = KbNodeType.Cabinet,
                Details = new KbNodeDetails
                {
                    Description = "Основной шкаф",
                    Location = "Машзал"
                },
                Children =
                {
                    new KbObjectTemplateNode
                    {
                        TemplateNodeId = "controller",
                        Name = "Контроллер",
                        NodeType = KbNodeType.Controller
                    }
                }
            },
            CompositionEntries =
            {
                new KbObjectTemplateCompositionEntry
                {
                    ParentTemplateNodeId = "cabinet",
                    SlotNumber = 1,
                    ComponentType = "CPU",
                    Model = "S7-1200"
                }
            },
            DocumentLinks =
            {
                new KbObjectTemplateDocumentLink
                {
                    OwnerTemplateNodeId = "cabinet",
                    Kind = KbDocumentKind.SchemeLink,
                    Title = "Схема",
                    Path = "\\\\srv\\schema.pdf"
                }
            },
            SoftwareRecords =
            {
                new KbObjectTemplateSoftwareRecord
                {
                    OwnerTemplateNodeId = "controller",
                    Title = "Проект PLC",
                    Path = "\\\\srv\\plc"
                }
            },
            NetworkFileReferences =
            {
                new KbObjectTemplateNetworkFileReference
                {
                    OwnerTemplateNodeId = "cabinet",
                    Title = "Топология",
                    Path = "\\\\srv\\topology.png"
                }
            },
            MaintenanceScheduleProfiles =
            {
                new KbObjectTemplateMaintenanceScheduleProfile
                {
                    OwnerTemplateNodeId = "cabinet",
                    IsIncludedInSchedule = true,
                    To1Hours = 2,
                    To2Hours = 4,
                    To3Hours = 8,
                    YearScheduleEntries =
                    {
                        new KbMaintenanceYearScheduleEntry
                        {
                            Month = 12,
                            WorkKind = KbMaintenanceWorkKind.To3
                        }
                    }
                }
            }
        };
}
