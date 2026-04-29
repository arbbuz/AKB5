using AsutpKnowledgeBase.Models;
using AsutpKnowledgeBase.Services;

namespace AsutpKnowledgeBase.Core.Tests;

public class KnowledgeBaseNetworkMutationServiceTests
{
    private readonly KnowledgeBaseNetworkMutationService _service = new();

    [Fact]
    public void UpsertNetworkFileReference_AddsNewImageReference_ForSupportedNode()
    {
        var ownerNode = new KbNode
        {
            NodeId = "cabinet-1",
            Name = "Cabinet 1",
            NodeType = KbNodeType.Cabinet
        };

        var result = _service.UpsertNetworkFileReference(
            ownerNode,
            Array.Empty<KbNetworkFileReference>(),
            new KbNetworkFileReference
            {
                Title = " Network scheme ",
                Path = " \\\\srv\\network\\scheme.png "
            });

        Assert.True(result.IsSuccess);
        var reference = Assert.Single(result.NetworkFileReferences);
        Assert.Equal("cabinet-1", reference.OwnerNodeId);
        Assert.Equal("Network scheme", reference.Title);
        Assert.Equal("\\\\srv\\network\\scheme.png", reference.Path);
        Assert.Equal(KbNetworkPreviewKind.Image, reference.PreviewKind);
    }

    [Fact]
    public void UpsertNetworkFileReference_UpdatesExistingReference_ForSameOwner()
    {
        var ownerNode = new KbNode
        {
            NodeId = "controller-1",
            Name = "Controller 1",
            NodeType = KbNodeType.Controller
        };

        var result = _service.UpsertNetworkFileReference(
            ownerNode,
            new List<KbNetworkFileReference>
            {
                new()
                {
                    NetworkAssetId = "network-1",
                    OwnerNodeId = "controller-1",
                    Title = "Old topology",
                    Path = "\\\\srv\\network\\topology.pdf",
                    PreviewKind = KbNetworkPreviewKind.MetadataOnly
                }
            },
            new KbNetworkFileReference
            {
                NetworkAssetId = "network-1",
                Title = "Updated topology",
                Path = "\\\\srv\\network\\topology.jpg"
            });

        Assert.True(result.IsSuccess);
        var reference = Assert.Single(result.NetworkFileReferences);
        Assert.Equal("network-1", reference.NetworkAssetId);
        Assert.Equal("Updated topology", reference.Title);
        Assert.Equal("\\\\srv\\network\\topology.jpg", reference.Path);
        Assert.Equal(KbNetworkPreviewKind.Image, reference.PreviewKind);
    }

    [Fact]
    public void DeleteNetworkFileReference_RemovesOnlySelectedReference_ForSameOwner()
    {
        var ownerNode = new KbNode
        {
            NodeId = "cabinet-1",
            Name = "Cabinet 1",
            NodeType = KbNodeType.Cabinet
        };

        var result = _service.DeleteNetworkFileReference(
            ownerNode,
            new List<KbNetworkFileReference>
            {
                new()
                {
                    NetworkAssetId = "network-1",
                    OwnerNodeId = "cabinet-1",
                    Title = "Main scheme",
                    Path = "\\\\srv\\network\\main.png",
                    PreviewKind = KbNetworkPreviewKind.Image
                },
                new()
                {
                    NetworkAssetId = "network-2",
                    OwnerNodeId = "cabinet-2",
                    Title = "Other scheme",
                    Path = "\\\\srv\\network\\other.png",
                    PreviewKind = KbNetworkPreviewKind.Image
                }
            },
            "network-1");

        Assert.True(result.IsSuccess);
        var remaining = Assert.Single(result.NetworkFileReferences);
        Assert.Equal("network-2", remaining.NetworkAssetId);
    }

    [Fact]
    public void UpsertNetworkFileReference_ForUnsupportedNode_ReturnsFailure()
    {
        var ownerNode = new KbNode
        {
            NodeId = "system-1",
            Name = "System 1",
            NodeType = KbNodeType.System
        };

        var result = _service.UpsertNetworkFileReference(
            ownerNode,
            Array.Empty<KbNetworkFileReference>(),
            new KbNetworkFileReference
            {
                Title = "Network scheme",
                Path = "\\\\srv\\network\\scheme.png"
            });

        Assert.False(result.IsSuccess);
        Assert.Contains("Сеть", result.ErrorMessage, StringComparison.Ordinal);
    }
    [Fact]
    public void UpsertNetworkFileReference_ForVisibleLevel3System_ReturnsSuccess()
    {
        var ownerNode = new KbNode
        {
            NodeId = "legacy-cabinet-1",
            Name = "Шкаф 1",
            NodeType = KbNodeType.System
        };

        var result = _service.UpsertNetworkFileReference(
            ownerNode,
            Array.Empty<KbNetworkFileReference>(),
            new KbNetworkFileReference
            {
                Title = "Network scheme",
                Path = "\\\\srv\\network\\scheme.png"
            },
            visibleLevel: 3);

        Assert.True(result.IsSuccess);
        Assert.Single(result.NetworkFileReferences);
    }
}
