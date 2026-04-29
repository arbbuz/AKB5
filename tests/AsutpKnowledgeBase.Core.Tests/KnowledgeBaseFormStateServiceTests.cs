using AsutpKnowledgeBase.Models;
using AsutpKnowledgeBase.Services;

namespace AsutpKnowledgeBase.Core.Tests;

public class KnowledgeBaseFormStateServiceTests
{
    private readonly KnowledgeBaseFormStateService _service = new();

    [Fact]
    public void ShouldEnableSave_ReturnsTrueForDirtyState()
    {
        Assert.True(_service.ShouldEnableSave(
            isDirty: true,
            requiresSave: false,
            fileExists: true,
            currentWorkshop: "Цех 1",
            lastSavedWorkshop: "Цех 1"));
    }

    [Fact]
    public void ShouldEnableSave_ReturnsTrueWhenBackupRequiresResave()
    {
        Assert.True(_service.ShouldEnableSave(
            isDirty: false,
            requiresSave: true,
            fileExists: true,
            currentWorkshop: "Цех 1",
            lastSavedWorkshop: "Цех 1"));
    }

    [Fact]
    public void ShouldEnableSave_ReturnsTrueWhenCurrentFileDoesNotExist()
    {
        Assert.True(_service.ShouldEnableSave(
            isDirty: false,
            requiresSave: false,
            fileExists: false,
            currentWorkshop: "Цех 1",
            lastSavedWorkshop: "Цех 1"));
    }

    [Fact]
    public void ShouldEnableSave_ReturnsTrueWhenLastWorkshopChanged()
    {
        Assert.True(_service.ShouldEnableSave(
            isDirty: false,
            requiresSave: false,
            fileExists: true,
            currentWorkshop: "Цех 2",
            lastSavedWorkshop: "Цех 1"));
    }

    [Fact]
    public void Build_ReturnsSelectedNodeStatusAndDirtyTitle()
    {
        var selectedNode = new KbNode
        {
            Name = "Насос",
            LevelIndex = 2,
            NodeType = KbNodeType.Device,
            Details = new KbNodeDetails
            {
                Description = "Основной насос",
                Location = "Участок 4",
                PhotoPath = @"\\server\photos\pump.jpg",
                IpAddress = "10.0.0.12",
                SchemaLink = "https://intra/schemes/pump"
            }
        };

        var state = _service.Build(
            isDirty: true,
            requiresSave: false,
            currentDataPath: "/tmp/knowledge.json",
            currentWorkshop: "Цех 7",
            lastSavedWorkshop: "Цех 7",
            totalNodes: 5,
            currentRoots: new List<KbNode> { selectedNode },
            selectedNode: selectedNode);

        Assert.True(state.CanSave);
        Assert.Equal("Сохранить базу данных (/tmp/knowledge.json)", state.SaveToolTip);
        Assert.Equal("* База знаний АСУТП [knowledge.json]", state.WindowTitle);
        Assert.Equal("Есть несохраненные изменения", state.SessionStatusText);
        Assert.Equal(string.Empty, state.SelectionStatusText);
        Assert.Equal("Есть несохраненные изменения", state.SaveStateText);
        Assert.Equal("Насос", state.SelectedNode.FullPath);
        Assert.Equal("Основной насос", state.SelectedNode.Description);
        Assert.Equal("10.0.0.12", state.SelectedNode.IpAddress);
        Assert.True(state.SelectedNode.ShowTechnicalFields);
        Assert.True(state.SelectedNode.Workspace.UseTabHost);
    }

    [Fact]
    public void Build_UsesNodePathEvenWhenLegacyLevelNamesAreIncomplete()
    {
        var node = new KbNode { Name = "Узел", LevelIndex = 4 };
        var state = _service.Build(
            isDirty: false,
            requiresSave: false,
            currentDataPath: "/tmp/missing-knowledge.json",
            currentWorkshop: "Цех 2",
            lastSavedWorkshop: "Цех 2",
            totalNodes: 3,
            currentRoots: new List<KbNode> { node },
            selectedNode: node);

        Assert.Equal(string.Empty, state.SelectionStatusText);
        Assert.Equal("Узел", state.SelectedNode.FullPath);
    }

    [Fact]
    public void Build_ForNoSelectionReturnsSessionAndEmptySelectionState()
    {
        var state = _service.Build(
            isDirty: false,
            requiresSave: false,
            currentDataPath: "/tmp/missing-knowledge.json",
            currentWorkshop: "Цех 3",
            lastSavedWorkshop: "Цех 3",
            totalNodes: 8,
            currentRoots: new List<KbNode>(),
            selectedNode: null);

        Assert.Equal("Файл: missing-knowledge.json | Файл отсутствует на диске | Цех: Цех 3 | Узлов: 8", state.SessionStatusText);
        Assert.Equal(string.Empty, state.SelectionStatusText);
        Assert.Equal("Ничего не выбрано. Выберите узел в дереве слева.", state.SelectedNode.EmptyStateText);
        Assert.Equal("Файл отсутствует на диске", state.SaveStateText);
        Assert.False(state.SelectedNode.ShowTechnicalFields);
        Assert.False(state.SelectedNode.Workspace.UseTabHost);
        Assert.Equal(string.Empty, state.SelectedNode.Description);
    }

    [Fact]
    public void Build_WhenRequiresResave_DoesNotEmitSessionStatusMessage()
    {
        var state = _service.Build(
            isDirty: false,
            requiresSave: true,
            currentDataPath: "/tmp/backup.json",
            currentWorkshop: "Цех 9",
            lastSavedWorkshop: "Цех 9",
            totalNodes: 4,
            currentRoots: new List<KbNode>(),
            selectedNode: null);

        Assert.Equal(string.Empty, state.SessionStatusText);
        Assert.Equal(string.Empty, state.SaveStateText);
    }

    [Fact]
    public void Build_HidesTechnicalFieldsForUpperLevels()
    {
        var selectedNode = new KbNode
        {
            Name = "Отделение 1",
            LevelIndex = 1,
            NodeType = KbNodeType.Department,
            Details = new KbNodeDetails
            {
                Description = "Верхний уровень дерева",
                IpAddress = "10.10.10.10",
                SchemaLink = "https://intra/line-1"
            }
        };

        var state = _service.Build(
            isDirty: false,
            requiresSave: false,
            currentDataPath: "/tmp/line.json",
            currentWorkshop: "Цех 1",
            lastSavedWorkshop: "Цех 1",
            totalNodes: 1,
            currentRoots: new List<KbNode> { selectedNode },
            selectedNode: selectedNode);

        Assert.False(state.SelectedNode.ShowTechnicalFields);
        Assert.Equal(string.Empty, state.SelectedNode.IpAddress);
        Assert.Equal(string.Empty, state.SelectedNode.SchemaLink);
        Assert.Equal("Верхний уровень дерева", state.SelectedNode.Description);
        Assert.False(state.SelectedNode.Workspace.UseTabHost);
    }

    [Fact]
    public void Build_ShowsInventoryNumberOnlyForLevel2Nodes()
    {
        var selectedNode = new KbNode
        {
            Name = "Линия 1",
            LevelIndex = 2,
            NodeType = KbNodeType.Department,
            Details = new KbNodeDetails
            {
                InventoryNumber = "INV-100"
            }
        };

        var state = _service.Build(
            isDirty: false,
            requiresSave: false,
            currentDataPath: "/tmp/system.json",
            currentWorkshop: "Цех 1",
            lastSavedWorkshop: "Цех 1",
            totalNodes: 1,
            currentRoots: new List<KbNode> { selectedNode },
            selectedNode: selectedNode);

        Assert.True(state.SelectedNode.ShowInventoryNumber);
        Assert.Equal("INV-100", state.SelectedNode.InventoryNumber);

        selectedNode.LevelIndex = 3;
        selectedNode.NodeType = KbNodeType.System;
        var cabinetState = _service.Build(
            isDirty: false,
            requiresSave: false,
            currentDataPath: "/tmp/cabinet.json",
            currentWorkshop: "Цех 1",
            lastSavedWorkshop: "Цех 1",
            totalNodes: 1,
            currentRoots: new List<KbNode> { selectedNode },
            selectedNode: selectedNode);

        Assert.False(cabinetState.SelectedNode.ShowInventoryNumber);
        Assert.Equal(string.Empty, cabinetState.SelectedNode.InventoryNumber);
    }

    [Fact]
    public void Build_ExposesMaintenanceScheduleStateForEngineeringNodes()
    {
        var selectedNode = new KbNode
        {
            NodeId = "device-1",
            Name = "Насос 1",
            LevelIndex = 3,
            NodeType = KbNodeType.Device
        };

        var state = _service.Build(
            isDirty: false,
            requiresSave: false,
            currentDataPath: "/tmp/device.json",
            currentWorkshop: "Цех 1",
            lastSavedWorkshop: "Цех 1",
            totalNodes: 1,
            currentRoots: new List<KbNode> { selectedNode },
            selectedNode: selectedNode,
            maintenanceScheduleProfiles: new[]
            {
                new KbMaintenanceScheduleProfile
                {
                    MaintenanceProfileId = "maintenance-1",
                    OwnerNodeId = "device-1",
                    IsIncludedInSchedule = true,
                    To1Hours = 2,
                    To2Hours = 5,
                    To3Hours = 12
                }
            });

        Assert.True(state.SelectedNode.Workspace.UseTabHost);
        Assert.True(state.SelectedNode.MaintenanceSchedule.SupportsEditing);
        Assert.True(state.SelectedNode.MaintenanceSchedule.HasProfile);
        Assert.Equal("Да", state.SelectedNode.MaintenanceSchedule.InclusionText);
        Assert.Equal("12 ч", state.SelectedNode.MaintenanceSchedule.To3HoursText);
    }

    [Fact]
    public void CloseDecisions_DistinguishPromptAndSilentSave()
    {
        Assert.True(_service.RequiresSavePromptBeforeContinue(isDirty: true, requiresSave: false));
        Assert.True(_service.RequiresSavePromptOnClose(isDirty: true, requiresSave: false));
        Assert.True(_service.RequiresSavePromptBeforeContinue(isDirty: false, requiresSave: true));
        Assert.True(_service.RequiresSavePromptOnClose(isDirty: false, requiresSave: true));
        Assert.False(_service.RequiresSavePromptBeforeContinue(isDirty: false, requiresSave: false));
        Assert.False(_service.RequiresSavePromptOnClose(isDirty: false, requiresSave: false));
        Assert.True(_service.ShouldSaveSilentlyOnClose("Цех 2", "Цех 1"));
        Assert.False(_service.ShouldSaveSilentlyOnClose("Цех 1", "Цех 1"));
    }
}
