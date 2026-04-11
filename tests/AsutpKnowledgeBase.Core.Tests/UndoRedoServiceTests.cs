using AsutpKnowledgeBase.Services;

namespace AsutpKnowledgeBase.Core.Tests;

public class UndoRedoServiceTests
{
    [Fact]
    public void UndoAndRedo_RoundTripCurrentState()
    {
        var history = new UndoRedoService();
        history.SaveState("A");
        history.SaveState("B");

        var undone = history.Undo("C");
        var redone = history.Redo(undone!);

        Assert.Equal("B", undone);
        Assert.Equal("C", redone);
    }

    [Fact]
    public void SaveState_TrimsOldestUndoSnapshots()
    {
        var history = new UndoRedoService(maxUndoSteps: 2);
        history.SaveState("A");
        history.SaveState("B");
        history.SaveState("C");

        Assert.Equal("C", history.Undo("D"));
        Assert.Equal("B", history.Undo("C"));
        Assert.Null(history.Undo("B"));
    }

    [Fact]
    public void Clear_RemovesUndoAndRedoStacks()
    {
        var history = new UndoRedoService();
        history.SaveState("A");
        _ = history.Undo("B");

        history.Clear();

        Assert.False(history.CanUndo);
        Assert.False(history.CanRedo);
    }
}
