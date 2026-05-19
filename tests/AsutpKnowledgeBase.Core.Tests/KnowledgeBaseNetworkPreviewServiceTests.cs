using AsutpKnowledgeBase.Models;
using AsutpKnowledgeBase.Services;

namespace AsutpKnowledgeBase.Core.Tests;

public class KnowledgeBaseNetworkPreviewServiceTests
{
    [Theory]
    [InlineData(@"C:\schemes\network.pdf")]
    [InlineData(@"\\srv\net\scheme.PDF")]
    public void ResolvePreviewKind_ForPdf_ReturnsPdf(string path)
    {
        Assert.Equal(KbNetworkPreviewKind.Pdf, KnowledgeBaseNetworkPreviewService.ResolvePreviewKind(path));
        Assert.False(KnowledgeBaseNetworkPreviewService.CanPreviewInForm(KbNetworkPreviewKind.Pdf));
        Assert.Equal("PDF", KnowledgeBaseNetworkPreviewService.GetPreviewKindText(KbNetworkPreviewKind.Pdf));
    }

    [Fact]
    public void ResolvePreviewKind_ForImage_ReturnsImage()
    {
        Assert.Equal(KbNetworkPreviewKind.Image, KnowledgeBaseNetworkPreviewService.ResolvePreviewKind(@"C:\schemes\network.png"));
    }

    [Fact]
    public void ResolvePreviewKind_ForOtherFile_ReturnsMetadataOnly()
    {
        Assert.Equal(KbNetworkPreviewKind.MetadataOnly, KnowledgeBaseNetworkPreviewService.ResolvePreviewKind(@"C:\schemes\network.txt"));
    }
}
