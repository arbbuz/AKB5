using AsutpKnowledgeBase.Models;
using AsutpKnowledgeBase.Services;

namespace AsutpKnowledgeBase.Core.Tests;

public class KnowledgeBaseActJournalServiceTests
{
    [Fact]
    public void BuildRows_FormatsRowsAndSortsNewestFirst()
    {
        var service = new KnowledgeBaseActJournalService();

        IReadOnlyList<KnowledgeBaseActJournalRow> rows = service.BuildRows(
            new[]
            {
                CreateAct("act-old", new DateTime(2026, 1, 10), string.Empty),
                CreateAct("act-new", new DateTime(2026, 6, 26), "2026-0002")
            },
            new[]
            {
                new KbActDocument
                {
                    ActId = "act-new",
                    VersionNumber = 1,
                    Path = "Documents\\Acts\\2026-0002.docx",
                    IsLatest = true
                }
            });

        Assert.Equal("act-new", rows[0].ActId);
        Assert.Equal("2026-0002", rows[0].ActNumberText);
        Assert.Equal("Documents\\Acts\\2026-0002.docx", rows[0].DocumentPath);
        Assert.False(rows[0].CanDeletePhysically);
        Assert.True(rows[0].CanGenerateDocument);
        Assert.False(rows[0].CanOpenDocument);
        Assert.Equal("act-old", rows[1].ActId);
        Assert.Equal("без номера", rows[1].ActNumberText);
        Assert.True(rows[1].CanDeletePhysically);
    }

    [Fact]
    public void BuildRows_WhenDocumentFileExists_AllowsOpenDocument()
    {
        string baseDirectory = Path.Combine(Path.GetTempPath(), $"akb5-act-journal-{Guid.NewGuid():N}");
        string documentPath = Path.Combine(baseDirectory, "Documents", "Acts", "2026-0002.docx");
        var service = new KnowledgeBaseActJournalService();

        KnowledgeBaseActJournalRow row = Assert.Single(service.BuildRows(
            new[] { CreateAct("act-new", new DateTime(2026, 6, 26), "2026-0002") },
            new[]
            {
                new KbActDocument
                {
                    ActId = "act-new",
                    VersionNumber = 1,
                    Path = "Documents\\Acts\\2026-0002.docx",
                    IsLatest = true
                }
            },
            baseDirectory,
            path => string.Equals(path, documentPath, StringComparison.OrdinalIgnoreCase)));

        Assert.True(row.CanOpenDocument);
        Assert.Equal(documentPath, row.AbsoluteDocumentPath);
    }

    [Fact]
    public void CanDeletePhysically_AllowsOnlyDraftWithoutNumberAndDocument()
    {
        var service = new KnowledgeBaseActJournalService();
        KbAct safeDraft = CreateAct("act-safe", new DateTime(2026, 6, 26), string.Empty);
        KbAct numberedDraft = CreateAct("act-numbered", new DateTime(2026, 6, 26), "2026-0001");
        KbAct generatedDraft = CreateAct("act-doc", new DateTime(2026, 6, 26), string.Empty);
        KbAct generatedAct = CreateAct("act-generated", new DateTime(2026, 6, 26), string.Empty);
        generatedAct.Status = KbActStatus.Generated;

        Assert.True(service.CanDeletePhysically(safeDraft, Array.Empty<KbActDocument>()));
        Assert.False(service.CanDeletePhysically(numberedDraft, Array.Empty<KbActDocument>()));
        Assert.False(service.CanDeletePhysically(
            generatedDraft,
            new[]
            {
                new KbActDocument
                {
                    ActId = "act-doc",
                    Path = "Documents\\Acts\\draft.docx"
                }
            }));
        Assert.False(service.CanDeletePhysically(generatedAct, Array.Empty<KbActDocument>()));
    }

    [Fact]
    public void BuildRows_DisablesDocumentAndStatusActionsForAnnulledAct()
    {
        var service = new KnowledgeBaseActJournalService();
        KbAct annulledAct = CreateAct("act-annulled", new DateTime(2026, 6, 26), "2026-0003");
        annulledAct.Status = KbActStatus.Annulled;

        KnowledgeBaseActJournalRow row = Assert.Single(service.BuildRows(
            new[] { annulledAct },
            Array.Empty<KbActDocument>()));

        Assert.Equal("Аннулирован", row.StatusText);
        Assert.False(row.CanDeletePhysically);
        Assert.False(row.CanChangeStatus);
        Assert.False(row.CanGenerateDocument);
    }

    private static KbAct CreateAct(string actId, DateTime actDate, string actNumber) =>
        new()
        {
            ActId = actId,
            ActDate = actDate,
            ActYear = actDate.Year,
            ActNumber = actNumber,
            ActType = KbActType.EquipmentFailure,
            Status = KbActStatus.Draft,
            WorkshopName = "Workshop",
            ObjectNameSnapshot = "Object",
            EquipmentName = "Equipment",
            CompositionEntryId = "entry-1",
            EquipmentSnapshot = new KbActEquipmentSnapshot
            {
                CompositionEntryId = "entry-1",
                OrderNumber = "6ES7"
            }
        };
}
