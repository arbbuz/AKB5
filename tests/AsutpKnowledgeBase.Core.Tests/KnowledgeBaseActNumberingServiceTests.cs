using AsutpKnowledgeBase.Models;
using AsutpKnowledgeBase.Services;

namespace AsutpKnowledgeBase.Core.Tests;

public class KnowledgeBaseActNumberingServiceTests
{
    [Fact]
    public void EnsureActNumber_AllocatesNextNumberFromYearSequence()
    {
        var service = new KnowledgeBaseActNumberingService();
        KbAct draft = CreateDraft("act-2", new DateTime(2026, 6, 26));

        KnowledgeBaseActNumberingResult result = service.EnsureActNumber(
            draft,
            new[]
            {
                CreateNumberedAct("act-1", "2026-0001")
            },
            new[]
            {
                new KbActNumberSequence
                {
                    Year = 2026,
                    NextNumber = 2
                }
            });

        Assert.True(result.IsSuccess, result.ErrorMessage);
        Assert.Equal("2026-0002", result.Act!.ActNumber);
        Assert.Equal(2026, result.Act.ActYear);
        KbActNumberSequence sequence = Assert.Single(result.NumberSequences);
        Assert.Equal(2026, sequence.Year);
        Assert.Equal(3, sequence.NextNumber);
    }

    [Fact]
    public void EnsureActNumber_UsesExistingActsWhenSequenceIsBehind()
    {
        var service = new KnowledgeBaseActNumberingService();

        KnowledgeBaseActNumberingResult result = service.EnsureActNumber(
            CreateDraft("act-4", new DateTime(2026, 6, 26)),
            new[]
            {
                CreateNumberedAct("act-1", "2026-0001"),
                CreateNumberedAct("act-2", "2026-0007")
            },
            new[]
            {
                new KbActNumberSequence
                {
                    Year = 2026,
                    NextNumber = 3
                }
            });

        Assert.True(result.IsSuccess, result.ErrorMessage);
        Assert.Equal("2026-0008", result.Act!.ActNumber);
        Assert.Equal(9, Assert.Single(result.NumberSequences).NextNumber);
    }

    [Fact]
    public void EnsureActNumber_PreservesExistingNumberAndDoesNotAdvanceSequence()
    {
        var service = new KnowledgeBaseActNumberingService();
        KbAct draft = CreateDraft("act-2", new DateTime(2026, 6, 26));
        draft.ActNumber = "2026-0010";

        KnowledgeBaseActNumberingResult result = service.EnsureActNumber(
            draft,
            new[] { CreateNumberedAct("act-1", "2026-0001") },
            new[]
            {
                new KbActNumberSequence
                {
                    Year = 2026,
                    NextNumber = 2
                }
            });

        Assert.True(result.IsSuccess, result.ErrorMessage);
        Assert.Equal("2026-0010", result.Act!.ActNumber);
        Assert.Equal(2, Assert.Single(result.NumberSequences).NextNumber);
    }

    [Fact]
    public void EnsureActNumber_RejectsDuplicateExistingNumber()
    {
        var service = new KnowledgeBaseActNumberingService();
        KbAct draft = CreateDraft("act-2", new DateTime(2026, 6, 26));
        draft.ActNumber = "2026-0001";

        KnowledgeBaseActNumberingResult result = service.EnsureActNumber(
            draft,
            new[] { CreateNumberedAct("act-1", "2026-0001") },
            Array.Empty<KbActNumberSequence>());

        Assert.False(result.IsSuccess);
        Assert.Contains("2026-0001", result.ErrorMessage, StringComparison.Ordinal);
    }

    private static KbAct CreateDraft(string actId, DateTime actDate) =>
        new()
        {
            ActId = actId,
            ActDate = actDate,
            ActType = KbActType.EquipmentFailure,
            EquipmentSnapshot = new KbActEquipmentSnapshot()
        };

    private static KbAct CreateNumberedAct(string actId, string actNumber) =>
        new()
        {
            ActId = actId,
            ActYear = 2026,
            ActDate = new DateTime(2026, 1, 1),
            ActNumber = actNumber,
            EquipmentSnapshot = new KbActEquipmentSnapshot()
        };
}
