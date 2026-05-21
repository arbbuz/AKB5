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

    [Fact]
    public void CreateTemplateFromExistingObject_RemovesRealIdsAndRemapsTypedRecords()
    {
        var controller = new KbNode
        {
            NodeId = "controller-1",
            Name = "PLC",
            NodeType = KbNodeType.Controller,
            Details = new KbNodeDetails
            {
                IpAddress = "10.10.10.20"
            }
        };
        var cabinet = new KbNode
        {
            NodeId = "cabinet-1",
            Name = "Cabinet A",
            NodeType = KbNodeType.Cabinet,
            Details = new KbNodeDetails
            {
                Description = "Filled cabinet",
                Location = "Panel room"
            },
            Children = { controller }
        };

        KnowledgeBaseObjectTemplateBuildResult result = _service.CreateTemplateFromExistingObject(
            cabinet,
            "Reusable cabinet",
            "Cabinets",
            "Template description",
            compositionRacks: null,
            new[]
            {
                new KbCompositionEntry
                {
                    EntryId = "composition-real-id",
                    ParentNodeId = "cabinet-1",
                    SlotNumber = 1,
                    PositionOrder = 2,
                    ComponentType = "CPU",
                    Model = "S7",
                    OrderNumber = "6ES7",
                    Firmware = "V2.6",
                    MpiDpPnAddress = "3",
                    InputAddress = "I 0.0",
                    OutputAddress = "Q 4.0",
                    Comment = "Template CPU",
                    InterfaceRows = "X1, Port 1"
                },
                new KbCompositionEntry { ParentNodeId = "outside-node", ComponentType = "Skip" }
            },
            new[]
            {
                new KbDocumentLink
                {
                    DocumentId = "document-real-id",
                    OwnerNodeId = "controller-1",
                    Kind = KbDocumentKind.SchemeLink,
                    Title = "Scheme",
                    Path = "\\\\srv\\scheme.pdf"
                }
            },
            new[]
            {
                new KbSoftwareRecord
                {
                    SoftwareId = "software-real-id",
                    OwnerNodeId = "controller-1",
                    Title = "PLC backup",
                    Path = "\\\\srv\\backup.zip"
                }
            },
            new[]
            {
                new KbMaintenanceScheduleProfile
                {
                    MaintenanceProfileId = "maintenance-real-id",
                    OwnerNodeId = "controller-1",
                    IsIncludedInSchedule = true,
                    To1Hours = 1,
                    To2Hours = 3,
                    To3Hours = 8,
                    YearScheduleEntries =
                    {
                        new KbMaintenanceYearScheduleEntry
                        {
                            Month = 4,
                            WorkKind = KbMaintenanceWorkKind.To2
                        }
                    }
                }
            });

        Assert.True(result.IsSuccess, result.ErrorMessage);
        Assert.NotNull(result.Template);
        KbObjectTemplate template = result.Template!;
        Assert.Equal(string.Empty, template.TemplateId);
        Assert.Equal("Reusable cabinet", template.DisplayName);
        Assert.Equal("Cabinets", template.Category);
        Assert.Equal("Template description", template.Description);
        Assert.Equal("Cabinet A", template.RootNode.Name);
        Assert.Equal(KbNodeType.Cabinet, template.RootNode.NodeType);
        Assert.Equal("Filled cabinet", template.RootNode.Details.Description);

        string cabinetTemplateNodeId = result.NodeIdMap["cabinet-1"];
        string controllerTemplateNodeId = result.NodeIdMap["controller-1"];
        Assert.Equal(cabinetTemplateNodeId, template.RootNode.TemplateNodeId);
        Assert.NotEqual("cabinet-1", cabinetTemplateNodeId);
        Assert.DoesNotContain("cabinet-1", CollectTemplateNodeIds(template.RootNode));

        KbObjectTemplateNode templateController = Assert.Single(template.RootNode.Children);
        Assert.Equal(controllerTemplateNodeId, templateController.TemplateNodeId);
        Assert.NotEqual("controller-1", controllerTemplateNodeId);

        KbObjectTemplateCompositionEntry composition = Assert.Single(template.CompositionEntries);
        Assert.Equal(cabinetTemplateNodeId, composition.ParentTemplateNodeId);
        Assert.Equal("CPU", composition.ComponentType);
        Assert.Equal("6ES7", composition.OrderNumber);
        Assert.Equal("V2.6", composition.Firmware);
        Assert.Equal("3", composition.MpiDpPnAddress);
        Assert.Equal("I 0.0", composition.InputAddress);
        Assert.Equal("Q 4.0", composition.OutputAddress);
        Assert.Equal("Template CPU", composition.Comment);
        Assert.Equal("X1, Port 1", composition.InterfaceRows);

        KbObjectTemplateDocumentLink document = Assert.Single(template.DocumentLinks);
        Assert.Equal(controllerTemplateNodeId, document.OwnerTemplateNodeId);
        Assert.Equal("Scheme", document.Title);

        KbObjectTemplateSoftwareRecord software = Assert.Single(template.SoftwareRecords);
        Assert.Equal(controllerTemplateNodeId, software.OwnerTemplateNodeId);

        KbObjectTemplateMaintenanceScheduleProfile maintenance = Assert.Single(template.MaintenanceScheduleProfiles);
        Assert.Equal(controllerTemplateNodeId, maintenance.OwnerTemplateNodeId);
        Assert.Equal(4, Assert.Single(maintenance.YearScheduleEntries).Month);
    }

    [Fact]
    public void BuildApplyToExistingObjectPlan_AddsMissingDataAndDoesNotOverwriteExistingFields()
    {
        KbObjectTemplate template = CreateCabinetTemplate();
        var target = new KbNode
        {
            NodeId = "target-cabinet",
            Name = "Шкаф существующий",
            LevelIndex = 2,
            NodeType = KbNodeType.Cabinet,
            Details = new KbNodeDetails
            {
                Description = "Описание пользователя"
            }
        };

        KnowledgeBaseObjectTemplateApplicationPlan plan = _service.BuildApplyToExistingObjectPlan(
            template,
            target,
            maxLevels: 4,
            existingCompositionRacks: Array.Empty<KbCompositionRack>(),
            existingCompositionEntries: Array.Empty<KbCompositionEntry>(),
            existingDocumentLinks: Array.Empty<KbDocumentLink>(),
            existingSoftwareRecords: Array.Empty<KbSoftwareRecord>(),
            existingMaintenanceScheduleProfiles: new[]
            {
                new KbMaintenanceScheduleProfile
                {
                    OwnerNodeId = "target-cabinet",
                    To1Hours = 99
                }
            });

        Assert.True(plan.IsSuccess, plan.ErrorMessage);
        Assert.True(plan.HasChanges);

        KnowledgeBaseObjectTemplateDetailUpdate detailUpdate = Assert.Single(plan.DetailUpdates);
        Assert.Same(target, detailUpdate.TargetNode);
        Assert.Equal("location", detailUpdate.FieldKey);
        Assert.Equal("Машзал", detailUpdate.Value);

        KnowledgeBaseObjectTemplateNodeAddition addition = Assert.Single(plan.NodeAdditions);
        Assert.Same(target, addition.ParentNode);
        Assert.Equal("Контроллер", addition.Node.Name);
        Assert.NotEqual("controller", addition.Node.NodeId);

        KbCompositionEntry composition = Assert.Single(plan.CompositionEntries);
        Assert.Equal("target-cabinet", composition.ParentNodeId);

        KbDocumentLink document = Assert.Single(plan.DocumentLinks);
        Assert.Equal("target-cabinet", document.OwnerNodeId);

        KbSoftwareRecord software = Assert.Single(plan.SoftwareRecords);
        Assert.Equal(addition.Node.NodeId, software.OwnerNodeId);

        Assert.Empty(plan.MaintenanceScheduleProfiles);
        Assert.Contains(
            plan.PreviewItems,
            item => item.Action == KnowledgeBaseObjectTemplateApplicationAction.Skipped &&
                    item.Area == "Карточка" &&
                    item.Description.Contains("не будет перезаписано", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(
            plan.PreviewItems,
            item => item.Action == KnowledgeBaseObjectTemplateApplicationAction.Skipped &&
                    item.Area == "График ТО");
    }

    [Fact]
    public void BuildApplyToExistingObjectPlan_WhenTargetTypeDiffers_ReturnsFailure()
    {
        KnowledgeBaseObjectTemplateApplicationPlan plan = _service.BuildApplyToExistingObjectPlan(
            CreateCabinetTemplate(),
            new KbNode
            {
                NodeId = "controller-1",
                Name = "PLC",
                NodeType = KbNodeType.Controller
            },
            maxLevels: 4,
            existingCompositionRacks: null,
            existingCompositionEntries: null,
            existingDocumentLinks: null,
            existingSoftwareRecords: null,
            existingMaintenanceScheduleProfiles: null);

        Assert.False(plan.IsSuccess);
        Assert.False(plan.HasChanges);
        Assert.Contains("другого типа", plan.ErrorMessage, StringComparison.OrdinalIgnoreCase);
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
    private static List<string> CollectTemplateNodeIds(KbObjectTemplateNode node)
    {
        var nodeIds = new List<string> { node.TemplateNodeId };
        foreach (KbObjectTemplateNode child in node.Children)
            nodeIds.AddRange(CollectTemplateNodeIds(child));

        return nodeIds;
    }
}
