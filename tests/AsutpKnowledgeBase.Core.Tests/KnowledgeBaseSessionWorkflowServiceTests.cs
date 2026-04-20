using System.Text.Json;
using AsutpKnowledgeBase.Models;
using AsutpKnowledgeBase.Services;

namespace AsutpKnowledgeBase.Core.Tests;

public class KnowledgeBaseSessionWorkflowServiceTests
{
    [Fact]
    public void BuildViewState_ReturnsCurrentWorkshopAndRoots()
    {
        var session = new KnowledgeBaseSessionService();
        session.ApplyLoadedData(CreateSampleData(lastWorkshop: "Цех 2"), recordAsSavedState: true);
        var workflow = new KnowledgeBaseSessionWorkflowService(session);

        var state = workflow.BuildViewState();

        Assert.Equal("Цех 2", state.CurrentWorkshop);
        Assert.Equal(new[] { "Цех 1", "Цех 2" }, state.WorkshopNames);
        Assert.Single(state.CurrentRoots);
        Assert.Equal("Линия 2", state.CurrentRoots[0].Name);
    }

    [Fact]
    public void RestoreSnapshot_WhenJsonIsValid_AppliesLoadedDataAndReturnsViewState()
    {
        var session = new KnowledgeBaseSessionService();
        var workflow = new KnowledgeBaseSessionWorkflowService(session);
        string json = JsonSerializer.Serialize(CreateSampleData(lastWorkshop: "Цех 2"));

        var result = workflow.RestoreSnapshot(json);

        Assert.True(result.IsSuccess);
        Assert.Equal("Цех 2", session.CurrentWorkshop);
        Assert.Equal("Цех 2", result.ViewState.CurrentWorkshop);
        Assert.Single(result.ViewState.CurrentRoots);
        Assert.Equal("Линия 2", result.ViewState.CurrentRoots[0].Name);
    }

    [Fact]
    public void RestoreSnapshot_WhenJsonIsInvalid_ReturnsReadableError()
    {
        var session = new KnowledgeBaseSessionService();
        var workflow = new KnowledgeBaseSessionWorkflowService(session);

        var result = workflow.RestoreSnapshot("{ invalid json");

        Assert.False(result.IsSuccess);
        Assert.Equal(KnowledgeBaseSessionTransitionFailure.InvalidSnapshot, result.Failure);
        Assert.Contains("Ошибка восстановления состояния", result.ErrorMessage);
    }

    [Fact]
    public void SelectWorkshop_SavesCurrentRootsBeforeSwitchAndReturnsNewView()
    {
        var session = new KnowledgeBaseSessionService();
        session.ApplyLoadedData(CreateSampleData(lastWorkshop: "Цех 1"), recordAsSavedState: true);
        var workflow = new KnowledgeBaseSessionWorkflowService(session);

        var result = workflow.SelectWorkshop(
            "Цех 2",
            new List<KbNode> { new() { Name = "Новая линия", LevelIndex = 0 } });

        Assert.True(result.IsSuccess);
        Assert.Equal("Цех 2", session.CurrentWorkshop);
        Assert.Equal("Новая линия", session.Workshops["Цех 1"][0].Name);
        Assert.Equal("Цех 2", result.ViewState.CurrentWorkshop);
    }

    [Fact]
    public void AddWorkshop_RejectsDuplicateNames()
    {
        var session = new KnowledgeBaseSessionService();
        session.ApplyLoadedData(CreateSampleData(lastWorkshop: "Цех 1"), recordAsSavedState: true);
        var workflow = new KnowledgeBaseSessionWorkflowService(session);

        var result = workflow.AddWorkshop(" цЕх 1 ", new List<KbNode>());

        Assert.False(result.IsSuccess);
        Assert.Equal(KnowledgeBaseSessionTransitionFailure.DuplicateWorkshopName, result.Failure);
        Assert.Equal("Цех с таким названием уже существует.", result.ErrorMessage);
    }

    [Fact]
    public void AddWorkshop_AddsTrimmedWorkshopAndReturnsViewState()
    {
        var session = new KnowledgeBaseSessionService();
        session.ApplyLoadedData(CreateSampleData(lastWorkshop: "Цех 1"), recordAsSavedState: true);
        var workflow = new KnowledgeBaseSessionWorkflowService(session);

        var result = workflow.AddWorkshop("  Новый цех  ", new List<KbNode>());

        Assert.True(result.IsSuccess);
        Assert.Equal("Новый цех", session.CurrentWorkshop);
        Assert.Contains("Новый цех", result.ViewState.WorkshopNames);
        Assert.Equal("Новый цех", result.ViewState.CurrentWorkshop);
        var root = Assert.Single(session.Workshops["Новый цех"]);
        Assert.Equal("Новый цех", root.Name);
        Assert.Equal(0, root.LevelIndex);
        Assert.Same(root, Assert.Single(result.ViewState.CurrentRoots));
    }

    [Fact]
    public void RenameCurrentWorkshop_RenamesSelectedWorkshopAndReturnsUpdatedViewState()
    {
        var session = new KnowledgeBaseSessionService();
        session.ApplyLoadedData(
            new SavedData
            {
                SchemaVersion = SavedData.CurrentSchemaVersion,
                Config = new KbConfig
                {
                    MaxLevels = 3,
                    LevelNames = new List<string> { "Цех", "Отделение", "Оборудование" }
                },
                Workshops = new Dictionary<string, List<KbNode>>
                {
                    ["Цех 1"] = new List<KbNode>
                    {
                        new()
                        {
                            Name = "Цех 1",
                            LevelIndex = 0,
                            Children = { new KbNode { Name = "Отделение 1", LevelIndex = 1 } }
                        }
                    },
                    ["Цех 2"] = new List<KbNode>()
                },
                LastWorkshop = "Цех 1"
            },
            recordAsSavedState: true);
        var workflow = new KnowledgeBaseSessionWorkflowService(session);

        var result = workflow.RenameCurrentWorkshop("Переименованный цех", session.GetCurrentWorkshopNodes());

        Assert.True(result.IsSuccess);
        Assert.Equal("Переименованный цех", session.CurrentWorkshop);
        Assert.Equal("Переименованный цех", result.ViewState.CurrentWorkshop);
        Assert.Contains("Переименованный цех", result.ViewState.WorkshopNames);
        var root = Assert.Single(result.ViewState.CurrentRoots);
        Assert.Equal("Переименованный цех", root.Name);
    }

    [Fact]
    public void DeleteCurrentWorkshop_RemovesSelectedWorkshopAndReturnsNextWorkshop()
    {
        var session = new KnowledgeBaseSessionService();
        session.ApplyLoadedData(CreateSampleData(lastWorkshop: "Цех 1"), recordAsSavedState: true);
        var workflow = new KnowledgeBaseSessionWorkflowService(session);

        var result = workflow.DeleteCurrentWorkshop(session.GetCurrentWorkshopNodes());

        Assert.True(result.IsSuccess);
        Assert.Equal("Цех 2", session.CurrentWorkshop);
        Assert.Equal("Цех 2", result.ViewState.CurrentWorkshop);
        Assert.DoesNotContain("Цех 1", result.ViewState.WorkshopNames);
    }

    [Fact]
    public void DeleteCurrentWorkshop_RejectsDeletingLastWorkshop()
    {
        var session = new KnowledgeBaseSessionService();
        session.ApplyLoadedData(
            new SavedData
            {
                SchemaVersion = SavedData.CurrentSchemaVersion,
                Config = new KbConfig
                {
                    MaxLevels = 2,
                    LevelNames = new List<string> { "Цех", "Отделение" }
                },
                Workshops = new Dictionary<string, List<KbNode>>
                {
                    ["Цех 1"] = new List<KbNode>()
                },
                LastWorkshop = "Цех 1"
            },
            recordAsSavedState: true);
        var workflow = new KnowledgeBaseSessionWorkflowService(session);

        var result = workflow.DeleteCurrentWorkshop(session.GetCurrentWorkshopNodes());

        Assert.False(result.IsSuccess);
        Assert.Equal(KnowledgeBaseSessionTransitionFailure.TransitionRejected, result.Failure);
        Assert.Equal("Нельзя удалить последний цех.", result.ErrorMessage);
    }

    private static SavedData CreateSampleData(string lastWorkshop) =>
        new()
        {
            SchemaVersion = SavedData.CurrentSchemaVersion,
            Config = new KbConfig
            {
                MaxLevels = 3,
                LevelNames = new List<string> { "Цех", "Линия", "Щит" }
            },
            Workshops = new Dictionary<string, List<KbNode>>
            {
                ["Цех 1"] = new List<KbNode>
                {
                    new() { Name = "Линия 1", LevelIndex = 0 }
                },
                ["Цех 2"] = new List<KbNode>
                {
                    new() { Name = "Линия 2", LevelIndex = 0 }
                }
            },
            LastWorkshop = lastWorkshop
        };
}
