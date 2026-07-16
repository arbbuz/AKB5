using AsutpKnowledgeBase.Models;
using AsutpKnowledgeBase.Services;

namespace AsutpKnowledgeBase.Core.Tests;

public class KnowledgeBaseActInputHistoryPersistenceTests
{
    [Fact]
    public void SessionSaveData_PreservesNormalizedActInputHistory()
    {
        var session = new KnowledgeBaseSessionService();
        session.ApplyLoadedData(
            new SavedData
            {
                Workshops = new Dictionary<string, List<KbNode>>
                {
                    ["Цех 1"] = new List<KbNode>()
                },
                LastWorkshop = "Цех 1",
                ActInputHistory = new List<KbActInputHistoryEntry>
                {
                    new()
                    {
                        WorkshopName = " Цех 1 ",
                        Field = KbActInputHistoryField.CustomerPosition,
                        DisplayValue = " Начальник   участка ",
                        UseOrder = 7
                    }
                }
            },
            recordAsSavedState: true);

        SavedData savedData = session.CreateSaveData(session.GetCurrentWorkshopNodes());

        KbActInputHistoryEntry entry = Assert.Single(savedData.ActInputHistory);
        Assert.Equal("Цех 1", entry.WorkshopName);
        Assert.Equal("Начальник участка", entry.DisplayValue);
        Assert.Equal("НАЧАЛЬНИК УЧАСТКА", entry.NormalizedValue);
        Assert.Equal(7, entry.UseOrder);
    }

    [Fact]
    public void ReplaceActInputHistory_AffectsDirtyStateSnapshot()
    {
        var session = new KnowledgeBaseSessionService();
        session.InitializeDefaultData(recordAsSavedState: true);

        session.ReplaceActInputHistory(
        [
            new KbActInputHistoryEntry
            {
                WorkshopName = session.CurrentWorkshop,
                Field = KbActInputHistoryField.ApproverName,
                DisplayValue = "Иванов И.И.",
                UseOrder = 1
            }
        ]);
        session.RefreshDirtyState(session.GetCurrentWorkshopNodes());

        Assert.True(session.IsDirty);
    }
}
