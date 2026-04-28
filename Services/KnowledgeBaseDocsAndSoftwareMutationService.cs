using AsutpKnowledgeBase.Models;

namespace AsutpKnowledgeBase.Services
{
    public sealed class KnowledgeBaseDocumentLinkMutationResult
    {
        public bool IsSuccess { get; init; }

        public string ErrorMessage { get; init; } = string.Empty;

        public List<KbDocumentLink> DocumentLinks { get; init; } = new();
    }

    public sealed class KnowledgeBaseSoftwareRecordMutationResult
    {
        public bool IsSuccess { get; init; }

        public string ErrorMessage { get; init; } = string.Empty;

        public List<KbSoftwareRecord> SoftwareRecords { get; init; } = new();
    }

    public class KnowledgeBaseDocsAndSoftwareMutationService
    {
        public KnowledgeBaseDocumentLinkMutationResult UpsertDocumentLink(
            KbNode? ownerNode,
            IReadOnlyList<KbDocumentLink>? documentLinks,
            KbDocumentLink? draftLink)
        {
            if (!TryValidateOwnerNode(ownerNode, out var ownerNodeId, out var errorMessage))
                return FailureDocument(errorMessage);

            if (draftLink == null)
                return FailureDocument("Черновик ссылки не был передан.");

            if (!Enum.IsDefined(typeof(KbDocumentKind), draftLink.Kind))
                return FailureDocument("Тип ссылки указан неверно.");

            string title = draftLink.Title?.Trim() ?? string.Empty;
            string path = draftLink.Path?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(title))
                return FailureDocument("Укажите наименование ссылки.");

            if (string.IsNullOrWhiteSpace(path))
                return FailureDocument("Укажите путь или ссылку.");

            var updatedLinks = CloneDocumentLinks(documentLinks);
            int existingIndex = !string.IsNullOrWhiteSpace(draftLink.DocumentId)
                ? updatedLinks.FindIndex(link => string.Equals(link.DocumentId, draftLink.DocumentId, StringComparison.Ordinal))
                : -1;

            if (existingIndex >= 0 &&
                !string.Equals(updatedLinks[existingIndex].OwnerNodeId, ownerNodeId, StringComparison.Ordinal))
            {
                return FailureDocument("Нельзя перенести ссылку на другой узел через редактирование.");
            }

            var normalizedDraft = new KbDocumentLink
            {
                DocumentId = draftLink.DocumentId?.Trim() ?? string.Empty,
                OwnerNodeId = ownerNodeId,
                Kind = draftLink.Kind,
                Title = title,
                Path = path,
                UpdatedAt = draftLink.UpdatedAt?.Date
            };

            if (existingIndex >= 0)
                updatedLinks[existingIndex] = normalizedDraft;
            else
                updatedLinks.Add(normalizedDraft);

            return SuccessDocument(updatedLinks);
        }

        public KnowledgeBaseDocumentLinkMutationResult DeleteDocumentLink(
            KbNode? ownerNode,
            IReadOnlyList<KbDocumentLink>? documentLinks,
            string? documentId)
        {
            if (!TryValidateOwnerNode(ownerNode, out var ownerNodeId, out var errorMessage))
                return FailureDocument(errorMessage);

            string normalizedDocumentId = documentId?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(normalizedDocumentId))
                return FailureDocument("Ссылка не выбрана.");

            var updatedLinks = CloneDocumentLinks(documentLinks);
            int removedCount = updatedLinks.RemoveAll(link =>
                string.Equals(link.DocumentId, normalizedDocumentId, StringComparison.Ordinal) &&
                string.Equals(link.OwnerNodeId, ownerNodeId, StringComparison.Ordinal));

            return removedCount == 0
                ? FailureDocument("Не удалось найти выбранную ссылку.")
                : SuccessDocument(updatedLinks);
        }

        public KnowledgeBaseSoftwareRecordMutationResult UpsertSoftwareRecord(
            KbNode? ownerNode,
            IReadOnlyList<KbSoftwareRecord>? softwareRecords,
            KbSoftwareRecord? draftRecord)
        {
            if (!TryValidateOwnerNode(ownerNode, out var ownerNodeId, out var errorMessage))
                return FailureSoftware(errorMessage);

            if (draftRecord == null)
                return FailureSoftware("Черновик ссылки на ПО не был передан.");

            string title = draftRecord.Title?.Trim() ?? string.Empty;
            string path = draftRecord.Path?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(title))
                return FailureSoftware("Укажите наименование ссылки на ПО.");

            if (string.IsNullOrWhiteSpace(path))
                return FailureSoftware("Укажите путь или ссылку на папку с ПО.");

            var updatedRecords = CloneSoftwareRecords(softwareRecords);
            int existingIndex = !string.IsNullOrWhiteSpace(draftRecord.SoftwareId)
                ? updatedRecords.FindIndex(record => string.Equals(record.SoftwareId, draftRecord.SoftwareId, StringComparison.Ordinal))
                : -1;

            if (existingIndex >= 0 &&
                !string.Equals(updatedRecords[existingIndex].OwnerNodeId, ownerNodeId, StringComparison.Ordinal))
            {
                return FailureSoftware("Нельзя перенести ссылку на ПО на другой узел через редактирование.");
            }

            KbSoftwareRecord? existingRecord = existingIndex >= 0 ? updatedRecords[existingIndex] : null;
            var normalizedDraft = new KbSoftwareRecord
            {
                SoftwareId = draftRecord.SoftwareId?.Trim() ?? string.Empty,
                OwnerNodeId = ownerNodeId,
                Title = title,
                Path = path,
                AddedAt = existingRecord?.AddedAt?.Date ?? draftRecord.AddedAt?.Date ?? DateTime.Today.Date,
                LastChangedAt = existingRecord?.LastChangedAt?.Date ?? draftRecord.LastChangedAt?.Date,
                LastBackupAt = existingRecord?.LastBackupAt?.Date ?? draftRecord.LastBackupAt?.Date,
                Notes = existingRecord?.Notes ?? draftRecord.Notes?.Trim() ?? string.Empty
            };

            if (existingIndex >= 0)
                updatedRecords[existingIndex] = normalizedDraft;
            else
                updatedRecords.Add(normalizedDraft);

            return SuccessSoftware(updatedRecords);
        }

        public KnowledgeBaseSoftwareRecordMutationResult DeleteSoftwareRecord(
            KbNode? ownerNode,
            IReadOnlyList<KbSoftwareRecord>? softwareRecords,
            string? softwareId)
        {
            if (!TryValidateOwnerNode(ownerNode, out var ownerNodeId, out var errorMessage))
                return FailureSoftware(errorMessage);

            string normalizedSoftwareId = softwareId?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(normalizedSoftwareId))
                return FailureSoftware("Ссылка на ПО не выбрана.");

            var updatedRecords = CloneSoftwareRecords(softwareRecords);
            int removedCount = updatedRecords.RemoveAll(record =>
                string.Equals(record.SoftwareId, normalizedSoftwareId, StringComparison.Ordinal) &&
                string.Equals(record.OwnerNodeId, ownerNodeId, StringComparison.Ordinal));

            return removedCount == 0
                ? FailureSoftware("Не удалось найти выбранную ссылку на ПО.")
                : SuccessSoftware(updatedRecords);
        }

        private static bool TryValidateOwnerNode(
            KbNode? ownerNode,
            out string ownerNodeId,
            out string errorMessage)
        {
            if (ownerNode == null)
            {
                ownerNodeId = string.Empty;
                errorMessage = "Не выбран узел для редактирования ссылок.";
                return false;
            }

            if (!KnowledgeBaseDocsAndSoftwareStateService.SupportsRecords(ownerNode.NodeType))
            {
                ownerNodeId = string.Empty;
                errorMessage = "Для выбранного узла вкладка \"Документация и ПО\" недоступна.";
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

        private static List<KbDocumentLink> CloneDocumentLinks(IReadOnlyList<KbDocumentLink>? documentLinks)
        {
            var clones = new List<KbDocumentLink>();
            if (documentLinks == null)
                return clones;

            foreach (var link in documentLinks)
            {
                clones.Add(new KbDocumentLink
                {
                    DocumentId = link.DocumentId,
                    OwnerNodeId = link.OwnerNodeId,
                    Kind = link.Kind,
                    Title = link.Title,
                    Path = link.Path,
                    UpdatedAt = link.UpdatedAt
                });
            }

            return clones;
        }

        private static List<KbSoftwareRecord> CloneSoftwareRecords(IReadOnlyList<KbSoftwareRecord>? softwareRecords)
        {
            var clones = new List<KbSoftwareRecord>();
            if (softwareRecords == null)
                return clones;

            foreach (var record in softwareRecords)
            {
                clones.Add(new KbSoftwareRecord
                {
                    SoftwareId = record.SoftwareId,
                    OwnerNodeId = record.OwnerNodeId,
                    Title = record.Title,
                    Path = record.Path,
                    AddedAt = record.AddedAt,
                    LastChangedAt = record.LastChangedAt,
                    LastBackupAt = record.LastBackupAt,
                    Notes = record.Notes
                });
            }

            return clones;
        }

        private static KnowledgeBaseDocumentLinkMutationResult SuccessDocument(List<KbDocumentLink> documentLinks) =>
            new()
            {
                IsSuccess = true,
                DocumentLinks = documentLinks
            };

        private static KnowledgeBaseSoftwareRecordMutationResult SuccessSoftware(List<KbSoftwareRecord> softwareRecords) =>
            new()
            {
                IsSuccess = true,
                SoftwareRecords = softwareRecords
            };

        private static KnowledgeBaseDocumentLinkMutationResult FailureDocument(string errorMessage) =>
            new()
            {
                IsSuccess = false,
                ErrorMessage = errorMessage
            };

        private static KnowledgeBaseSoftwareRecordMutationResult FailureSoftware(string errorMessage) =>
            new()
            {
                IsSuccess = false,
                ErrorMessage = errorMessage
            };
    }
}
