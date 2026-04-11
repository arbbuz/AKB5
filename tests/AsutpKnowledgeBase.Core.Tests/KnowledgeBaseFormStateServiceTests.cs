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

        var state = _service.Build(
            isDirty: true,
            requiresSave: false,
            currentDataPath: "/tmp/knowledge.json",
            currentWorkshop: "Цех 7",
            lastSavedWorkshop: "Цех 7",
            totalNodes: 5,
            config: config,
            selectedNode: new KbNode { Name = "Насос", LevelIndex = 2 });

        Assert.True(state.CanSave);
        Assert.Equal("Сохранить базу данных (/tmp/knowledge.json)", state.SaveToolTip);
        Assert.Equal("* База знаний АСУТП [knowledge.json]", state.WindowTitle);
        Assert.Equal("Цех: Цех 7 | Всего: 5 | Выбрано: Насос (Оборудование)", state.StatusText);
    }

    [Fact]
    public void BuildStatusText_UsesFallbackLevelNameWhenConfigDoesNotContainIt()
    {
        var status = _service.BuildStatusText(
            currentWorkshop: "Цех 2",
            totalNodes: 3,
            config: new KbConfig { MaxLevels = 1, LevelNames = new List<string> { "Цех" } },
            selectedNode: new KbNode { Name = "Узел", LevelIndex = 4 });

        Assert.Equal("Цех: Цех 2 | Всего: 3 | Выбрано: Узел (Ур. 5)", status);
    }

    [Fact]
    public void BuildStatusText_ForNoSelectionShowsTotals()
    {
        var status = _service.BuildStatusText(
            currentWorkshop: "Цех 3",
            totalNodes: 8,
            config: new KbConfig { MaxLevels = 6, LevelNames = new List<string>() },
            selectedNode: null);

        Assert.Equal("Цех: Цех 3 | Всего узлов: 8 | Уровней: 6", status);
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
