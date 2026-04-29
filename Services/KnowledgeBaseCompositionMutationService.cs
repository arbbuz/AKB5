using AsutpKnowledgeBase.Models;

namespace AsutpKnowledgeBase.Services
{
    public sealed class KnowledgeBaseCompositionMutationResult
    {
        public bool IsSuccess { get; init; }

        public string ErrorMessage { get; init; } = string.Empty;

        public List<KbCompositionEntry> CompositionEntries { get; init; } = new();
    }

    public class KnowledgeBaseCompositionMutationService
    {
        public KnowledgeBaseCompositionMutationResult UpsertEntry(
            KbNode? parentNode,
            IReadOnlyList<KbCompositionEntry>? compositionEntries,
            KbCompositionEntry? draftEntry,
            int visibleLevel = 0)
        {
            if (!TryValidateParentNode(parentNode, visibleLevel, out var parentNodeId, out var errorMessage))
                return Failure(errorMessage);

            if (draftEntry == null)
                return Failure("Запись состава не задана.");

            string componentType = draftEntry.ComponentType?.Trim() ?? string.Empty;
            string model = draftEntry.Model?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(componentType) && string.IsNullOrWhiteSpace(model))
                return Failure("Нужно указать хотя бы тип компонента или модель.");

            int? slotNumber = draftEntry.SlotNumber;
            if (slotNumber.HasValue && slotNumber.Value <= 0)
                return Failure("Номер слота должен быть положительным.");

            var updatedEntries = CloneEntries(compositionEntries);
            int existingIndex = !string.IsNullOrWhiteSpace(draftEntry.EntryId)
                ? updatedEntries.FindIndex(entry => string.Equals(entry.EntryId, draftEntry.EntryId, StringComparison.Ordinal))
                : -1;

            if (existingIndex >= 0 &&
                !string.Equals(updatedEntries[existingIndex].ParentNodeId, parentNodeId, StringComparison.Ordinal))
            {
                return Failure("Нельзя переназначить запись состава другому родителю.");
            }

            var normalizedDraft = new KbCompositionEntry
            {
                EntryId = draftEntry.EntryId?.Trim() ?? string.Empty,
                ParentNodeId = parentNodeId,
                SlotNumber = slotNumber,
                PositionOrder = draftEntry.PositionOrder < 0 ? 0 : draftEntry.PositionOrder,
                ComponentType = componentType,
                Model = model,
                IpAddress = draftEntry.IpAddress?.Trim() ?? string.Empty,
                LastCalibrationAt = draftEntry.LastCalibrationAt,
                NextCalibrationAt = draftEntry.NextCalibrationAt,
                Notes = draftEntry.Notes?.Trim() ?? string.Empty
            };

            if (existingIndex >= 0)
                updatedEntries[existingIndex] = normalizedDraft;
            else
                updatedEntries.Add(normalizedDraft);

            return Success(updatedEntries);
        }

        public KnowledgeBaseCompositionMutationResult DeleteEntry(
            KbNode? parentNode,
            IReadOnlyList<KbCompositionEntry>? compositionEntries,
            string? entryId,
            int visibleLevel = 0)
        {
            if (!TryValidateParentNode(parentNode, visibleLevel, out var parentNodeId, out var errorMessage))
                return Failure(errorMessage);

            string normalizedEntryId = entryId?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(normalizedEntryId))
                return Failure("Не выбрана запись состава для удаления.");

            var updatedEntries = CloneEntries(compositionEntries);
            int removedCount = updatedEntries.RemoveAll(entry =>
                string.Equals(entry.EntryId, normalizedEntryId, StringComparison.Ordinal) &&
                string.Equals(entry.ParentNodeId, parentNodeId, StringComparison.Ordinal));

            return removedCount == 0
                ? Failure("Не удалось найти выбранную запись состава.")
                : Success(updatedEntries);
        }

        private static bool TryValidateParentNode(
            KbNode? parentNode,
            int visibleLevel,
            out string parentNodeId,
            out string errorMessage)
        {
            if (parentNode == null)
            {
                parentNodeId = string.Empty;
                errorMessage = "Не выбран узел для редактирования состава.";
                return false;
            }

            if (!KnowledgeBaseCompositionStateService.SupportsComposition(parentNode.NodeType, visibleLevel))
            {
                parentNodeId = string.Empty;
                errorMessage = "Вкладка \"Состав\" недоступна для выбранного узла.";
                return false;
            }

            parentNodeId = parentNode.NodeId?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(parentNodeId))
            {
                errorMessage = "У выбранного узла отсутствует идентификатор.";
                return false;
            }

            errorMessage = string.Empty;
            return true;
        }

        private static List<KbCompositionEntry> CloneEntries(IReadOnlyList<KbCompositionEntry>? compositionEntries)
        {
            var clones = new List<KbCompositionEntry>();
            if (compositionEntries == null)
                return clones;

            foreach (var entry in compositionEntries)
            {
                clones.Add(new KbCompositionEntry
                {
                    EntryId = entry.EntryId,
                    ParentNodeId = entry.ParentNodeId,
                    SlotNumber = entry.SlotNumber,
                    PositionOrder = entry.PositionOrder,
                    ComponentType = entry.ComponentType,
                    Model = entry.Model,
                    IpAddress = entry.IpAddress,
                    LastCalibrationAt = entry.LastCalibrationAt,
                    NextCalibrationAt = entry.NextCalibrationAt,
                    Notes = entry.Notes
                });
            }

            return clones;
        }

        private static KnowledgeBaseCompositionMutationResult Success(List<KbCompositionEntry> compositionEntries) =>
            new()
            {
                IsSuccess = true,
                CompositionEntries = compositionEntries
            };

        private static KnowledgeBaseCompositionMutationResult Failure(string errorMessage) =>
            new()
            {
                IsSuccess = false,
                ErrorMessage = errorMessage
            };
    }
}
