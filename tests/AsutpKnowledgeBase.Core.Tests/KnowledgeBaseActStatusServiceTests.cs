using AsutpKnowledgeBase.Models;
using AsutpKnowledgeBase.Services;

namespace AsutpKnowledgeBase.Core.Tests;

public class KnowledgeBaseActStatusServiceTests
{
    [Fact]
    public void PrepareStatusChange_GeneratesDraftAndRecordsHistoryWithoutAuthor()
    {
        var service = new KnowledgeBaseActStatusService();
        DateTime changedAt = new(2026, 7, 15, 10, 30, 0);

        KnowledgeBaseActStatusChangeResult result = service.PrepareStatusChange(
            new KnowledgeBaseActStatusChangeRequest
            {
                Act = CreateAct(KbActStatus.Draft),
                NewStatus = KbActStatus.Generated,
                ChangedAt = changedAt
            });

        Assert.True(result.IsSuccess, result.ErrorMessage);
        KbAct act = Assert.IsType<KbAct>(result.Act);
        Assert.Equal(KbActStatus.Generated, act.Status);
        KbActStatusChange history = Assert.Single(act.StatusHistory);
        Assert.Equal(KbActStatus.Draft, history.PreviousStatus);
        Assert.Equal(KbActStatus.Generated, history.NewStatus);
        Assert.Equal(changedAt, history.ChangedAt);
        Assert.Equal(string.Empty, history.ChangedBy);
    }

    [Fact]
    public void PrepareStatusChange_SignsGeneratedActAndStoresSigningDate()
    {
        var service = new KnowledgeBaseActStatusService();

        KnowledgeBaseActStatusChangeResult result = service.PrepareStatusChange(
            new KnowledgeBaseActStatusChangeRequest
            {
                Act = CreateAct(KbActStatus.Generated),
                NewStatus = KbActStatus.Signed,
                ChangedAt = new DateTime(2026, 7, 15, 11, 0, 0),
                SignedAt = new DateTime(2026, 7, 14)
            });

        Assert.True(result.IsSuccess, result.ErrorMessage);
        KbAct act = Assert.IsType<KbAct>(result.Act);
        Assert.Equal(KbActStatus.Signed, act.Status);
        Assert.Equal(new DateTime(2026, 7, 14), act.SignedAt);
        Assert.Equal(string.Empty, Assert.Single(act.StatusHistory).ChangedBy);
        Assert.False(KnowledgeBaseActStatusService.CanEdit(act.Status));
        Assert.False(KnowledgeBaseActStatusService.CanGenerateDocument(act.Status));
        Assert.False(KnowledgeBaseActStatusService.CanCancel(act.Status));
    }

    [Fact]
    public void PrepareStatusChange_CancelsGeneratedActAndRejectsSignedActCancellation()
    {
        var service = new KnowledgeBaseActStatusService();

        KnowledgeBaseActStatusChangeResult cancelledAct = service.PrepareStatusChange(
            new KnowledgeBaseActStatusChangeRequest
            {
                Act = CreateAct(KbActStatus.Generated),
                NewStatus = KbActStatus.Cancelled,
                ChangedAt = new DateTime(2026, 7, 15, 12, 0, 0)
            });
        KnowledgeBaseActStatusChangeResult signedAct = service.PrepareStatusChange(
            new KnowledgeBaseActStatusChangeRequest
            {
                Act = CreateAct(KbActStatus.Signed),
                NewStatus = KbActStatus.Cancelled,
                ChangedAt = new DateTime(2026, 7, 15, 12, 0, 0)
            });

        Assert.True(cancelledAct.IsSuccess, cancelledAct.ErrorMessage);
        Assert.Equal(KbActStatus.Cancelled, cancelledAct.Act?.Status);
        Assert.False(signedAct.IsSuccess);
        Assert.Equal("Для текущего статуса это действие недоступно.", signedAct.ErrorMessage);
    }

    private static KbAct CreateAct(KbActStatus status) =>
        new()
        {
            ActId = "act-1",
            Status = status,
            StatusHistory = new List<KbActStatusChange>()
        };
}
