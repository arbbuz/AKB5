using AsutpKnowledgeBase.Models;

namespace AsutpKnowledgeBase.Services
{
    public sealed class KnowledgeBaseCompositionRackMutationResult
    {
        public bool IsSuccess { get; init; }

        public string ErrorMessage { get; init; } = string.Empty;

        public List<KbCompositionRack> CompositionRacks { get; init; } = new();

        public KbCompositionRack? Rack { get; init; }
    }

    public sealed class KnowledgeBaseCompositionRackMutationService
    {
        public KbCompositionRack CreateAddDraft(
            KbNode? parentNode,
            IReadOnlyList<KbCompositionRack>? compositionRacks,
            IReadOnlyList<KbCompositionEntry>? compositionEntries)
        {
            string parentNodeId = parentNode?.NodeId?.Trim() ?? string.Empty;
            int rackNumber = ResolveNextRackNumber(parentNodeId, compositionRacks, compositionEntries);
            return new KbCompositionRack
            {
                ParentNodeId = parentNodeId,
                RackNumber = rackNumber,
                SortOrder = rackNumber,
                RackType = "UR"
            };
        }

        public KnowledgeBaseCompositionRackMutationResult UpsertRack(
            KbNode? parentNode,
            IReadOnlyList<KbCompositionRack>? compositionRacks,
            KbCompositionRack? draftRack,
            int visibleLevel = 0)
        {
            if (!TryValidateParentNode(parentNode, visibleLevel, out string parentNodeId, out string errorMessage))
                return Failure(errorMessage);

            if (draftRack == null)
                return Failure("Rack не задан.");

            int rackNumber = KnowledgeBaseCompositionRackSlotRulesService.NormalizeRackNumber(draftRack.RackNumber);
            string rackType = draftRack.RackType?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(rackType))
                rackType = "UR";

            var updatedRacks = CloneRacks(compositionRacks);
            int existingIndex = ResolveExistingRackIndex(updatedRacks, parentNodeId, draftRack.RackId, rackNumber);
            int sortOrder = draftRack.SortOrder >= 0
                ? draftRack.SortOrder
                : ResolveNextSortOrder(parentNodeId, updatedRacks);

            var normalizedDraft = new KbCompositionRack
            {
                RackId = draftRack.RackId?.Trim() ?? string.Empty,
                ParentNodeId = parentNodeId,
                RackNumber = rackNumber,
                SortOrder = sortOrder,
                RackType = rackType,
                Label = draftRack.Label?.Trim() ?? string.Empty,
                Notes = draftRack.Notes?.Trim() ?? string.Empty,
                Properties = CloneProperties(draftRack.Properties)
            };

            if (existingIndex >= 0)
                updatedRacks[existingIndex] = normalizedDraft;
            else
                updatedRacks.Add(normalizedDraft);

            List<KbCompositionRack> normalizedRacks = KnowledgeBaseDataService.NormalizeCompositionRacks(updatedRacks);
            var savedRack = normalizedRacks.FirstOrDefault(rack =>
                string.Equals(rack.ParentNodeId, parentNodeId, StringComparison.Ordinal) &&
                rack.RackNumber == rackNumber);

            return new KnowledgeBaseCompositionRackMutationResult
            {
                IsSuccess = true,
                CompositionRacks = normalizedRacks,
                Rack = savedRack
            };
        }

        public KnowledgeBaseCompositionRackMutationResult DeleteRack(
            KbNode? parentNode,
            IReadOnlyList<KbCompositionRack>? compositionRacks,
            IReadOnlyList<KbCompositionEntry>? compositionEntries,
            int rackNumber,
            int visibleLevel = 0)
        {
            if (!TryValidateParentNode(parentNode, visibleLevel, out string parentNodeId, out string errorMessage))
                return Failure(errorMessage);

            int normalizedRackNumber = KnowledgeBaseCompositionRackSlotRulesService.NormalizeRackNumber(rackNumber);
            bool hasEntries = (compositionEntries ?? Array.Empty<KbCompositionEntry>())
                .Any(entry =>
                    string.Equals(entry.ParentNodeId, parentNodeId, StringComparison.Ordinal) &&
                    entry.SlotNumber.HasValue &&
                    KnowledgeBaseCompositionRackSlotRulesService.NormalizeRackNumber(entry.RackNumber) == normalizedRackNumber);
            if (hasEntries)
                return Failure("Нельзя удалить Rack, пока в нём есть слотные записи состава.");

            var updatedRacks = CloneRacks(compositionRacks);
            int removedCount = updatedRacks.RemoveAll(rack =>
                string.Equals(rack.ParentNodeId, parentNodeId, StringComparison.Ordinal) &&
                KnowledgeBaseCompositionRackSlotRulesService.NormalizeRackNumber(rack.RackNumber) == normalizedRackNumber);

            if (removedCount == 0)
                return Failure("У выбранного Rack нет сохранённой записи. Для Rack0 без метаданных удаление не требуется.");

            return new KnowledgeBaseCompositionRackMutationResult
            {
                IsSuccess = true,
                CompositionRacks = KnowledgeBaseDataService.NormalizeCompositionRacks(updatedRacks)
            };
        }

        private static int ResolveExistingRackIndex(
            List<KbCompositionRack> racks,
            string parentNodeId,
            string? rackId,
            int rackNumber)
        {
            string normalizedRackId = rackId?.Trim() ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(normalizedRackId))
            {
                int byId = racks.FindIndex(rack => string.Equals(rack.RackId, normalizedRackId, StringComparison.Ordinal));
                if (byId >= 0)
                    return byId;
            }

            return racks.FindIndex(rack =>
                string.Equals(rack.ParentNodeId, parentNodeId, StringComparison.Ordinal) &&
                KnowledgeBaseCompositionRackSlotRulesService.NormalizeRackNumber(rack.RackNumber) == rackNumber);
        }

        private static int ResolveNextRackNumber(
            string parentNodeId,
            IReadOnlyList<KbCompositionRack>? compositionRacks,
            IReadOnlyList<KbCompositionEntry>? compositionEntries)
        {
            int maxRack = 0;
            foreach (KbCompositionRack rack in compositionRacks ?? Array.Empty<KbCompositionRack>())
            {
                if (string.Equals(rack.ParentNodeId, parentNodeId, StringComparison.Ordinal))
                    maxRack = Math.Max(maxRack, KnowledgeBaseCompositionRackSlotRulesService.NormalizeRackNumber(rack.RackNumber));
            }

            foreach (KbCompositionEntry entry in compositionEntries ?? Array.Empty<KbCompositionEntry>())
            {
                if (string.Equals(entry.ParentNodeId, parentNodeId, StringComparison.Ordinal) && entry.SlotNumber.HasValue)
                    maxRack = Math.Max(maxRack, KnowledgeBaseCompositionRackSlotRulesService.NormalizeRackNumber(entry.RackNumber));
            }

            return maxRack + 1;
        }

        private static int ResolveNextSortOrder(string parentNodeId, IReadOnlyList<KbCompositionRack> racks)
        {
            int? maxSortOrder = racks
                .Where(rack => string.Equals(rack.ParentNodeId, parentNodeId, StringComparison.Ordinal))
                .Select(rack => (int?)rack.SortOrder)
                .Max();

            return (maxSortOrder ?? -1) + 1;
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
                errorMessage = "Не выбран узел для редактирования Rack.";
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

        private static List<KbCompositionRack> CloneRacks(IReadOnlyList<KbCompositionRack>? racks)
        {
            var clones = new List<KbCompositionRack>();
            if (racks == null)
                return clones;

            foreach (KbCompositionRack rack in racks)
            {
                clones.Add(new KbCompositionRack
                {
                    RackId = rack.RackId,
                    ParentNodeId = rack.ParentNodeId,
                    RackNumber = rack.RackNumber,
                    SortOrder = rack.SortOrder,
                    RackType = rack.RackType,
                    Label = rack.Label,
                    Notes = rack.Notes,
                    Properties = CloneProperties(rack.Properties)
                });
            }

            return clones;
        }

        private static List<KbCompositionRackProperty> CloneProperties(
            IEnumerable<KbCompositionRackProperty>? properties) =>
            (properties ?? Array.Empty<KbCompositionRackProperty>())
                .Select(static property => new KbCompositionRackProperty
                {
                    Name = property.Name,
                    Value = property.Value
                })
                .ToList();

        private static KnowledgeBaseCompositionRackMutationResult Failure(string errorMessage) =>
            new()
            {
                IsSuccess = false,
                ErrorMessage = errorMessage
            };
    }
}
