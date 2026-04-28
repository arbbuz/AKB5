using AsutpKnowledgeBase.Models;

namespace AsutpKnowledgeBase.Services
{
    public static class KnowledgeBaseNetworkPreviewService
    {
        public static KbNetworkPreviewKind ResolvePreviewKind(string? path) =>
            IsPreviewableImagePath(path)
                ? KbNetworkPreviewKind.Image
                : KbNetworkPreviewKind.MetadataOnly;

        public static bool CanPreviewInForm(KbNetworkPreviewKind previewKind) =>
            previewKind == KbNetworkPreviewKind.Image;

        public static string GetPreviewKindText(KbNetworkPreviewKind previewKind) => previewKind switch
        {
            KbNetworkPreviewKind.Image => "Изображение",
            _ => "Без встроенного предпросмотра"
        };

        public static bool IsPreviewableImagePath(string? path)
        {
            string normalizedPath = path?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(normalizedPath))
                return false;

            string extension = Path.GetExtension(normalizedPath);
            return extension.Equals(".jpg", StringComparison.OrdinalIgnoreCase) ||
                   extension.Equals(".jpeg", StringComparison.OrdinalIgnoreCase) ||
                   extension.Equals(".png", StringComparison.OrdinalIgnoreCase) ||
                   extension.Equals(".bmp", StringComparison.OrdinalIgnoreCase) ||
                   extension.Equals(".gif", StringComparison.OrdinalIgnoreCase);
        }
    }
}
