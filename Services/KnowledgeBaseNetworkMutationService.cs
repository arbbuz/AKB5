using AsutpKnowledgeBase.Models;

namespace AsutpKnowledgeBase.Services
{
    public sealed class KnowledgeBaseNetworkFileReferenceMutationResult
    {
        public bool IsSuccess { get; init; }

        public string ErrorMessage { get; init; } = string.Empty;

        public List<KbNetworkFileReference> NetworkFileReferences { get; init; } = new();
    }

    public class KnowledgeBaseNetworkMutationService
    {
        public KnowledgeBaseNetworkFileReferenceMutationResult UpsertNetworkFileReference(
            KbNode? ownerNode,
            IReadOnlyList<KbNetworkFileReference>? networkFileReferences,
            KbNetworkFileReference? draftReference,
            int visibleLevel = 0)
        {
            if (!TryValidateOwnerNode(ownerNode, visibleLevel, out var ownerNodeId, out var errorMessage))
                return Failure(errorMessage);

            if (draftReference == null)
                return Failure("Черновик файла сети не был передан.");

            string title = draftReference.Title?.Trim() ?? string.Empty;
            string path = draftReference.Path?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(title))
                return Failure("Укажите наименование файла сети.");

            if (string.IsNullOrWhiteSpace(path))
                return Failure("Укажите путь или ссылку на файл сети.");

            var updatedReferences = CloneNetworkFileReferences(networkFileReferences);
            int existingIndex = !string.IsNullOrWhiteSpace(draftReference.NetworkAssetId)
                ? updatedReferences.FindIndex(reference =>
                    string.Equals(reference.NetworkAssetId, draftReference.NetworkAssetId, StringComparison.Ordinal))
                : -1;

            if (existingIndex >= 0 &&
                !string.Equals(updatedReferences[existingIndex].OwnerNodeId, ownerNodeId, StringComparison.Ordinal))
            {
                return Failure("Нельзя перенести файл сети на другой узел через редактирование.");
            }

            var normalizedDraft = new KbNetworkFileReference
            {
                NetworkAssetId = draftReference.NetworkAssetId?.Trim() ?? string.Empty,
                OwnerNodeId = ownerNodeId,
                Title = title,
                Path = path,
                PreviewKind = KnowledgeBaseNetworkPreviewService.ResolvePreviewKind(path)
            };

            if (existingIndex >= 0)
                updatedReferences[existingIndex] = normalizedDraft;
            else
                updatedReferences.Add(normalizedDraft);

            return Success(updatedReferences);
        }

        public KnowledgeBaseNetworkFileReferenceMutationResult DeleteNetworkFileReference(
            KbNode? ownerNode,
            IReadOnlyList<KbNetworkFileReference>? networkFileReferences,
            string? networkAssetId,
            int visibleLevel = 0)
        {
            if (!TryValidateOwnerNode(ownerNode, visibleLevel, out var ownerNodeId, out var errorMessage))
                return Failure(errorMessage);

            string normalizedNetworkAssetId = networkAssetId?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(normalizedNetworkAssetId))
                return Failure("Файл сети не выбран.");

            var updatedReferences = CloneNetworkFileReferences(networkFileReferences);
            int removedCount = updatedReferences.RemoveAll(reference =>
                string.Equals(reference.NetworkAssetId, normalizedNetworkAssetId, StringComparison.Ordinal) &&
                string.Equals(reference.OwnerNodeId, ownerNodeId, StringComparison.Ordinal));

            return removedCount == 0
                ? Failure("Не удалось найти выбранный файл сети.")
                : Success(updatedReferences);
        }

        private static bool TryValidateOwnerNode(
            KbNode? ownerNode,
            int visibleLevel,
            out string ownerNodeId,
            out string errorMessage)
        {
            if (ownerNode == null)
            {
                ownerNodeId = string.Empty;
                errorMessage = "Не выбран узел для редактирования файлов сети.";
                return false;
            }

            if (!KnowledgeBaseNetworkStateService.SupportsRecords(ownerNode.NodeType, visibleLevel))
            {
                ownerNodeId = string.Empty;
                errorMessage = "Для выбранного узла вкладка \"Сеть\" недоступна.";
                return false;
            }

            ownerNodeId = ownerNode.NodeId?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(ownerNodeId))
            {
                errorMessage = "У выбранного узла отсутствует NodeId.";
                return false;
            }

            errorMessage = string.Empty;
            return true;
        }

        private static List<KbNetworkFileReference> CloneNetworkFileReferences(
            IReadOnlyList<KbNetworkFileReference>? networkFileReferences)
        {
            var clones = new List<KbNetworkFileReference>();
            if (networkFileReferences == null)
                return clones;

            foreach (var reference in networkFileReferences)
            {
                clones.Add(new KbNetworkFileReference
                {
                    NetworkAssetId = reference.NetworkAssetId,
                    OwnerNodeId = reference.OwnerNodeId,
                    Title = reference.Title,
                    Path = reference.Path,
                    PreviewKind = reference.PreviewKind
                });
            }

            return clones;
        }

        private static KnowledgeBaseNetworkFileReferenceMutationResult Success(
            List<KbNetworkFileReference> networkFileReferences) =>
            new()
            {
                IsSuccess = true,
                NetworkFileReferences = networkFileReferences
            };

        private static KnowledgeBaseNetworkFileReferenceMutationResult Failure(string errorMessage) =>
            new()
            {
                IsSuccess = false,
                ErrorMessage = errorMessage
            };
    }
}
