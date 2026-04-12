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
        var config = new KbConfig
        {
            MaxLevels = 3,
            LevelNames = new List<string> { "Цех", "Отделение", "Оборудование" }
        };
        var selectedNode = new KbNode { Name = "Насос", LevelIndex = 2 };

        var state = _service.Build(
            isDirty: true,
            requiresSave: false,
            currentDataPath: "/tmp/knowledge.json",
            currentWorkshop: "Цех 7",
            lastSavedWorkshop: "Цех 7",
            totalNodes: 5,
            config: config,
            currentRoots: new List<KbNode> { selectedNode },
            selectedNode: selectedNode);

        Assert.True(state.CanSave);
        Assert.Equal("Сохранить базу данных (/tmp/knowledge.json)", state.SaveToolTip);
        Assert.Equal("* База знаний АСУТП [knowledge.json]", state.WindowTitle);
        Assert.Equal("Файл: knowledge.json | Есть несохранённые изменения | Цех: Цех 7 | Узлов: 5", state.SessionStatusText);
        Assert.Equal("Выбор: Насос | Уровень: Оборудование | Дочерних: 0", state.SelectionStatusText);
        Assert.Equal("Есть несохранённые изменения", state.SaveStateText);
        Assert.Equal("Насос", state.SelectedNode.FullPath);
    }

    [Fact]
    public void Build_UsesFallbackLevelNameWhenConfigDoesNotContainIt()
    {
        var node = new KbNode { Name = "Узел", LevelIndex = 4 };
        var state = _service.Build(
            isDirty: false,
            requiresSave: false,
            currentDataPath: "/tmp/missing-knowledge.json",
            currentWorkshop: "Цех 2",
            lastSavedWorkshop: "Цех 2",
            totalNodes: 3,
            config: new KbConfig { MaxLevels = 1, LevelNames = new List<string> { "Цех" } },
            currentRoots: new List<KbNode> { node },
            selectedNode: node);

        Assert.Equal("Выбор: Узел | Уровень: Ур. 5 | Дочерних: 0", state.SelectionStatusText);
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
            config: new KbConfig { MaxLevels = 6, LevelNames = new List<string>() },
            currentRoots: new List<KbNode>(),
            selectedNode: null);

        Assert.Equal("Файл: missing-knowledge.json | Файл отсутствует на диске | Цех: Цех 3 | Узлов: 8", state.SessionStatusText);
        Assert.Equal("Выбранный узел: нет", state.SelectionStatusText);
        Assert.Equal("Ничего не выбрано. Выберите узел в дереве слева.", state.SelectedNode.EmptyStateText);
        Assert.Equal("Файл отсутствует на диске", state.SaveStateText);
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
