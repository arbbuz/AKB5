using AsutpKnowledgeBase.Models;

namespace AsutpKnowledgeBase.Services
{
    public sealed class KnowledgeBaseNetworkFileReferenceState
    {
        public string NetworkAssetId { get; init; } = string.Empty;

        public string TitleText { get; init; } = string.Empty;

        public string PathText { get; init; } = string.Empty;

        public KbNetworkPreviewKind PreviewKind { get; init; }

        public string PreviewKindText { get; init; } = string.Empty;

        public bool CanPreviewInForm { get; init; }
    }

    public sealed class KnowledgeBaseNetworkState
    {
        public bool SupportsEditing { get; init; }

        public string SourceText { get; init; } = string.Empty;

        public string EmptyStateText { get; init; } = string.Empty;

        public int FileReferencesCount { get; init; }

        public IReadOnlyList<KnowledgeBaseNetworkFileReferenceState> FileReferenceStates { get; init; } =
            Array.Empty<KnowledgeBaseNetworkFileReferenceState>();

        public bool HasEntries => FileReferencesCount > 0;
    }

    public class KnowledgeBaseNetworkStateService
    {
        public KnowledgeBaseNetworkState Build(
            KbNode? selectedNode,
            IReadOnlyList<KbNetworkFileReference>? networkFileReferences,
            int visibleLevel = 0)
        {
            if (selectedNode == null || !SupportsRecords(selectedNode.NodeType, visibleLevel))
            {
                return new KnowledgeBaseNetworkState
                {
                    EmptyStateText = "Вкладка \"Сеть\" недоступна для выбранного узла."
                };
            }

            string ownerNodeId = selectedNode.NodeId?.Trim() ?? string.Empty;
            var nodeFileReferences = GetOwnedFileReferences(ownerNodeId, networkFileReferences);
            var fileReferenceStates = BuildFileReferenceStates(nodeFileReferences);

            return new KnowledgeBaseNetworkState
            {
                SupportsEditing = true,
                SourceText = "Показаны файлы сетевых схем, адресации и других материалов по сети для этого узла.",
                EmptyStateText = "Для этого узла пока нет файлов сети.",
                FileReferencesCount = fileReferenceStates.Count,
                FileReferenceStates = fileReferenceStates
            };
        }

        public static bool SupportsRecords(KbNodeType nodeType, int visibleLevel = 0) =>
            KnowledgeBaseEngineeringNodeSupportService.SupportsEngineeringWorkspace(nodeType, visibleLevel);

        private static List<KbNetworkFileReference> GetOwnedFileReferences(
            string ownerNodeId,
            IReadOnlyList<KbNetworkFileReference>? networkFileReferences)
        {
            if (string.IsNullOrWhiteSpace(ownerNodeId) || networkFileReferences == null)
                return new List<KbNetworkFileReference>();

            return networkFileReferences
                .Where(reference => string.Equals(reference.OwnerNodeId, ownerNodeId, StringComparison.Ordinal))
                .OrderBy(reference => reference.Title, KnowledgeBaseNaturalStringComparer.Instance)
                .ThenBy(reference => reference.Path, KnowledgeBaseNaturalStringComparer.Instance)
                .ThenBy(reference => reference.NetworkAssetId, StringComparer.Ordinal)
                .ToList();
        }

        private static List<KnowledgeBaseNetworkFileReferenceState> BuildFileReferenceStates(
            IEnumerable<KbNetworkFileReference> references) =>
            references.Select(reference => new KnowledgeBaseNetworkFileReferenceState
            {
                NetworkAssetId = reference.NetworkAssetId,
                TitleText = GetDisplayTitle(reference.Title, reference.Path),
                PathText = GetDisplayText(reference.Path),
                PreviewKind = reference.PreviewKind,
                PreviewKindText = KnowledgeBaseNetworkPreviewService.GetPreviewKindText(reference.PreviewKind),
                CanPreviewInForm = KnowledgeBaseNetworkPreviewService.CanPreviewInForm(reference.PreviewKind)
            })
                .ToList();

        private static string GetDisplayTitle(string? title, string? path)
        {
            string normalizedTitle = title?.Trim() ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(normalizedTitle))
                return normalizedTitle;

            string normalizedPath = path?.Trim() ?? string.Empty;
            return string.IsNullOrWhiteSpace(normalizedPath)
                ? "(без названия)"
                : normalizedPath;
        }

        private static string GetDisplayText(string? value) =>
            string.IsNullOrWhiteSpace(value)
                ? "-"
                : value.Trim();
    }
}
