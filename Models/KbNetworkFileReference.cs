namespace AsutpKnowledgeBase.Models
{
    public class KbNetworkFileReference
    {
        public string NetworkAssetId { get; set; } = string.Empty;

        public string OwnerNodeId { get; set; } = string.Empty;

        public string Title { get; set; } = string.Empty;

        public string Path { get; set; } = string.Empty;

        public KbNetworkPreviewKind PreviewKind { get; set; } = KbNetworkPreviewKind.MetadataOnly;
    }
}
