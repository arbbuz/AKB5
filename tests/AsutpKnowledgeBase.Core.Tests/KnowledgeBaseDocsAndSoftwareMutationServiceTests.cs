using AsutpKnowledgeBase.Models;
using AsutpKnowledgeBase.Services;

namespace AsutpKnowledgeBase.Core.Tests;

public class KnowledgeBaseDocsAndSoftwareMutationServiceTests
{
    private readonly KnowledgeBaseDocsAndSoftwareMutationService _service = new();

    [Fact]
    public void UpsertDocumentLink_AddsNewLink_ForSupportedNode()
    {
        var ownerNode = new KbNode
        {
            NodeId = "cabinet-1",
            Name = "Cabinet 1",
            NodeType = KbNodeType.Cabinet
        };

        var result = _service.UpsertDocumentLink(
            ownerNode,
            Array.Empty<KbDocumentLink>(),
            new KbDocumentLink
            {
                Kind = KbDocumentKind.SchemeLink,
                Title = " Wiring Diagram ",
                Path = " \\\\srv\\docs\\wiring.pdf ",
                UpdatedAt = new DateTime(2026, 4, 1, 12, 0, 0)
            });

        Assert.True(result.IsSuccess);
        var link = Assert.Single(result.DocumentLinks);
        Assert.Equal("cabinet-1", link.OwnerNodeId);
        Assert.Equal(KbDocumentKind.SchemeLink, link.Kind);
        Assert.Equal("Wiring Diagram", link.Title);
        Assert.Equal("\\\\srv\\docs\\wiring.pdf", link.Path);
        Assert.Equal(new DateTime(2026, 4, 1), link.UpdatedAt);
    }

    [Fact]
    public void UpsertSoftwareRecord_UpdatesExistingRecord_ForSameOwner()
    {
        var ownerNode = new KbNode
        {
            NodeId = "controller-1",
            Name = "Controller 1",
            NodeType = KbNodeType.Controller
        };

        var result = _service.UpsertSoftwareRecord(
            ownerNode,
            new List<KbSoftwareRecord>
            {
                new()
                {
                    SoftwareId = "software-1",
                    OwnerNodeId = "controller-1",
                    Title = "PLC Backup",
                    Path = "\\\\srv\\backup\\plc-old.zip",
                    AddedAt = new DateTime(2026, 4, 1),
                    LastChangedAt = new DateTime(2026, 4, 10),
                    LastBackupAt = new DateTime(2026, 4, 11),
                    Notes = "nightly"
                }
            },
            new KbSoftwareRecord
            {
                SoftwareId = "software-1",
                Title = "PLC Backup v2",
                Path = "\\\\srv\\backup\\plc-new.zip",
                AddedAt = new DateTime(2026, 4, 25),
                LastChangedAt = new DateTime(2026, 4, 20),
                LastBackupAt = new DateTime(2026, 4, 21),
                Notes = "weekly"
            });

        Assert.True(result.IsSuccess);
        var record = Assert.Single(result.SoftwareRecords);
        Assert.Equal("software-1", record.SoftwareId);
        Assert.Equal("PLC Backup v2", record.Title);
        Assert.Equal("\\\\srv\\backup\\plc-new.zip", record.Path);
        Assert.Equal(new DateTime(2026, 4, 1), record.AddedAt);
        Assert.Equal(new DateTime(2026, 4, 10), record.LastChangedAt);
        Assert.Equal(new DateTime(2026, 4, 11), record.LastBackupAt);
        Assert.Equal("nightly", record.Notes);
    }

    [Fact]
    public void UpsertSoftwareRecord_NewRecordWithoutDate_AssignsToday()
    {
        var ownerNode = new KbNode
        {
            NodeId = "controller-1",
            Name = "Controller 1",
            NodeType = KbNodeType.Controller
        };

        var result = _service.UpsertSoftwareRecord(
            ownerNode,
            Array.Empty<KbSoftwareRecord>(),
            new KbSoftwareRecord
            {
                Title = "PLC Backup",
                Path = "\\\\srv\\backup\\plc.zip"
            });

        Assert.True(result.IsSuccess);
        var record = Assert.Single(result.SoftwareRecords);
        Assert.Equal(DateTime.Today, record.AddedAt);
    }

    [Fact]
    public void DeleteDocumentLink_RemovesOnlySelectedLink_ForSameOwner()
    {
        var ownerNode = new KbNode
        {
            NodeId = "cabinet-1",
            Name = "Cabinet 1",
            NodeType = KbNodeType.Cabinet
        };

        var result = _service.DeleteDocumentLink(
            ownerNode,
            new List<KbDocumentLink>
            {
                new()
                {
                    DocumentId = "doc-1",
                    OwnerNodeId = "cabinet-1",
                    Kind = KbDocumentKind.Manual,
                    Title = "Manual",
                    Path = "\\\\srv\\manual.pdf"
                },
                new()
                {
                    DocumentId = "doc-2",
                    OwnerNodeId = "cabinet-2",
                    Kind = KbDocumentKind.Manual,
                    Title = "Other",
                    Path = "\\\\srv\\other.pdf"
                }
            },
            "doc-1");

        Assert.True(result.IsSuccess);
        var remaining = Assert.Single(result.DocumentLinks);
        Assert.Equal("doc-2", remaining.DocumentId);
    }

    [Fact]
    public void UpsertDocumentLink_ForUnsupportedNode_ReturnsFailure()
    {
        var ownerNode = new KbNode
        {
            NodeId = "system-1",
            Name = "System 1",
            NodeType = KbNodeType.System
        };

        var result = _service.UpsertDocumentLink(
            ownerNode,
            Array.Empty<KbDocumentLink>(),
            new KbDocumentLink
            {
                Kind = KbDocumentKind.Manual,
                Title = "Manual",
                Path = "\\\\srv\\manual.pdf"
            });

        Assert.False(result.IsSuccess);
        Assert.Contains("Документация и ПО", result.ErrorMessage, StringComparison.Ordinal);
    }
    [Fact]
    public void UpsertDocumentLink_ForVisibleLevel3System_ReturnsSuccess()
    {
        var ownerNode = new KbNode
        {
            NodeId = "legacy-cabinet-1",
            Name = "Шкаф 1",
            NodeType = KbNodeType.System
        };

        var result = _service.UpsertDocumentLink(
            ownerNode,
            Array.Empty<KbDocumentLink>(),
            new KbDocumentLink
            {
                Kind = KbDocumentKind.Manual,
                Title = "Manual",
                Path = "\\\\srv\\manual.pdf"
            },
            visibleLevel: 3);

        Assert.True(result.IsSuccess);
        Assert.Single(result.DocumentLinks);
    }
}
