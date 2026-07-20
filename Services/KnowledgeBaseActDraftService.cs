using AsutpKnowledgeBase.Models;

namespace AsutpKnowledgeBase.Services
{
    public enum KnowledgeBaseActDraftSource
    {
        Composition = 0,
        AdditionalEquipment = 1,
        Lvl2Object = 2
    }

    public sealed class KnowledgeBaseActDraftRequest
    {
        public KbNode? Lvl3Node { get; init; }

        public KbNode? ObjectNode { get; init; }

        public IReadOnlyList<KbNode>? WorkshopRoots { get; init; }

        public string WorkshopName { get; init; } = string.Empty;

        public int VisibleLevel { get; init; } = 3;

        public KbCompositionRack? Rack { get; init; }

        public KbCompositionEntry? CompositionEntry { get; init; }

        public KnowledgeBaseActDraftSource Source { get; init; } = KnowledgeBaseActDraftSource.Composition;

        public KbActType ActType { get; init; } = KbActType.EquipmentFailure;

        public DateTime? ActDate { get; init; }

        public string CreatedBy { get; init; } = string.Empty;
    }

    public sealed class KnowledgeBaseActDraftResult
    {
        public bool IsSuccess { get; init; }

        public string ErrorMessage { get; init; } = string.Empty;

        public KbAct? Act { get; init; }
    }

    public sealed class KnowledgeBaseActDraftService
    {
        private readonly Func<DateTime> _clock;
        private readonly Func<string> _actIdFactory;
        private readonly KnowledgeBaseNodePresentationService _nodePresentationService;

        public KnowledgeBaseActDraftService(
            Func<DateTime>? clock = null,
            Func<string>? actIdFactory = null,
            KnowledgeBaseNodePresentationService? nodePresentationService = null)
        {
            _clock = clock ?? (() => DateTime.Now);
            _actIdFactory = actIdFactory ?? (() => $"act-draft-{Guid.NewGuid():N}");
            _nodePresentationService = nodePresentationService ?? new KnowledgeBaseNodePresentationService();
        }

        public KnowledgeBaseActDraftResult CreateDraft(KnowledgeBaseActDraftRequest request)
        {
            if (request == null)
                return Failure("Не переданы данные для создания черновика акта.");

            bool requiresEquipment = request.ActType == KbActType.EquipmentFailure;
            KbNode? sourceNode = requiresEquipment ? request.Lvl3Node : request.ObjectNode;
            if (sourceNode == null)
            {
                return requiresEquipment
                    ? Failure("Не выбран объект Lvl3.")
                    : Failure("Не выбран объект Lvl2.");
            }

            if (requiresEquipment &&
                !KnowledgeBaseCompositionStateService.SupportsComposition(sourceNode.NodeType, request.VisibleLevel))
            {
                return Failure("Акт выхода из строя можно создать только для оборудования объекта Lvl3.");
            }

            if (!requiresEquipment && request.VisibleLevel != 2)
                return Failure("Акт выполненных работ можно создать только для объекта Lvl2.");

            KbCompositionEntry? entry = request.CompositionEntry;
            if (requiresEquipment && entry == null)
                return Failure("Не выбрана строка оборудования.");

            string entryId = requiresEquipment ? entry!.EntryId?.Trim() ?? string.Empty : string.Empty;
            if (requiresEquipment && string.IsNullOrWhiteSpace(entryId))
                return Failure("Нельзя создать акт по пустой строке оборудования.");

            string sourceNodeId = sourceNode.NodeId?.Trim() ?? string.Empty;
            string entryParentNodeId = requiresEquipment ? entry!.ParentNodeId?.Trim() ?? string.Empty : string.Empty;
            if (string.IsNullOrWhiteSpace(sourceNodeId))
                return Failure("У выбранного объекта отсутствует идентификатор.");

            if (requiresEquipment &&
                !string.Equals(entryParentNodeId, sourceNodeId, StringComparison.Ordinal))
            {
                return Failure("Выбранная строка состава не принадлежит выбранному объекту Lvl3.");
            }

            bool useRackSnapshot = requiresEquipment && request.Source == KnowledgeBaseActDraftSource.Composition;
            KbCompositionRack? rack = useRackSnapshot ? request.Rack : null;
            int entryRackNumber = requiresEquipment
                ? KnowledgeBaseCompositionRackSlotRulesService.NormalizeRackNumber(entry!.RackNumber)
                : 0;
            if (rack != null)
            {
                string rackParentNodeId = rack.ParentNodeId?.Trim() ?? string.Empty;
                if (!string.IsNullOrWhiteSpace(rackParentNodeId) &&
                    !string.Equals(rackParentNodeId, sourceNodeId, StringComparison.Ordinal))
                {
                    return Failure("Выбранный Rack не принадлежит выбранному объекту Lvl3.");
                }

                int rackNumber = KnowledgeBaseCompositionRackSlotRulesService.NormalizeRackNumber(rack.RackNumber);
                if (rackNumber != entryRackNumber)
                    return Failure("Выбранная строка состава не принадлежит выбранному Rack.");
            }

            DateTime now = _clock();
            DateTime actDate = (request.ActDate ?? now).Date;
            string workshopName = request.WorkshopName?.Trim() ?? string.Empty;
            string objectPath = request.WorkshopRoots == null || request.WorkshopRoots.Count == 0
                ? sourceNode.Name
                : _nodePresentationService.BuildNodePath(request.WorkshopRoots, sourceNode);
            string objectName = requiresEquipment
                ? ResolveParentNodeName(request.WorkshopRoots, sourceNode)
                : sourceNode.Name?.Trim() ?? string.Empty;
            int rackNumberSnapshot = useRackSnapshot ? entryRackNumber : 0;
            string rackName = !useRackSnapshot
                ? string.Empty
                : rack == null
                ? KnowledgeBaseCompositionRackSlotRulesService.FormatRackTitle(entryRackNumber)
                : KnowledgeBaseCompositionRackSlotRulesService.FormatRackTitle(
                    rack.RackNumber,
                    rack.RackType,
                    rack.Label);

            var equipmentSnapshot = new KbActEquipmentSnapshot
            {
                Lvl3Name = requiresEquipment ? sourceNode.Name ?? string.Empty : string.Empty,
                ObjectPath = objectPath,
                RackId = rack?.RackId ?? string.Empty,
                RackNumber = rackNumberSnapshot,
                RackName = rackName,
                CompositionEntryId = entryId,
                ComponentType = requiresEquipment ? entry!.ComponentType : string.Empty,
                Model = requiresEquipment ? entry!.Model : string.Empty,
                OrderNumber = requiresEquipment ? entry!.OrderNumber : string.Empty,
                SerialNumber = string.Empty,
                Notes = requiresEquipment ? entry!.Notes : string.Empty
            };
            string equipmentName = requiresEquipment
                ? BuildDefaultEquipmentName(entry!.ComponentType, entry.Model, entry.OrderNumber)
                : string.Empty;

            var act = new KbAct
            {
                ActId = _actIdFactory(),
                ActYear = actDate.Year,
                ActNumber = string.Empty,
                ActType = request.ActType,
                Status = KbActStatus.Draft,
                ActDate = actDate,
                WorkshopName = workshopName,
                Lvl3NodeId = requiresEquipment ? sourceNodeId : string.Empty,
                Lvl3NameSnapshot = requiresEquipment ? sourceNode.Name ?? string.Empty : string.Empty,
                ObjectNameSnapshot = objectName,
                ObjectPathSnapshot = objectPath,
                RackId = rack?.RackId ?? string.Empty,
                RackNumberSnapshot = rackNumberSnapshot,
                RackNameSnapshot = rackName,
                CompositionEntryId = entryId,
                EquipmentName = equipmentName,
                EquipmentSnapshot = equipmentSnapshot,
                FailureDate = null,
                FaultDescription = string.Empty,
                FailureReason = string.Empty,
                InspectionResult = string.Empty,
                FaultCriterion = string.Empty,
                RequestDocument = string.Empty,
                ActualLaborHours = string.Empty,
                CustomerName = string.Empty,
                CustomerPosition = string.Empty,
                CreatedBy = request.CreatedBy,
                CreatedAt = now,
                UpdatedAt = now
            };

            return new KnowledgeBaseActDraftResult
            {
                IsSuccess = true,
                Act = KnowledgeBaseDataService.NormalizeActs(new[] { act }).Single()
            };
        }

        private static string ResolveParentNodeName(
            IReadOnlyList<KbNode>? roots,
            KbNode lvl3Node)
        {
            if (roots == null || roots.Count == 0)
                return string.Empty;

            KbNode? parent = FindParentNode(roots, lvl3Node);
            return parent?.Name?.Trim() ?? string.Empty;
        }

        private static KbNode? FindParentNode(
            IEnumerable<KbNode> nodes,
            KbNode targetNode)
        {
            foreach (KbNode node in nodes)
            {
                foreach (KbNode child in node.Children)
                {
                    if (ReferenceEquals(child, targetNode) ||
                        string.Equals(child.NodeId, targetNode.NodeId, StringComparison.Ordinal))
                    {
                        return node;
                    }
                }

                KbNode? nestedParent = FindParentNode(node.Children, targetNode);
                if (nestedParent != null)
                    return nestedParent;
            }

            return null;
        }

        public static string BuildDefaultEquipmentName(
            string? componentType,
            string? model,
            string? orderNumber)
        {
            string orderNumberText = orderNumber?.Trim() ?? string.Empty;
            string source = componentType?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(source))
            {
                string modelText = model?.Trim() ?? string.Empty;
                source = string.Equals(modelText, orderNumberText, StringComparison.OrdinalIgnoreCase)
                    ? string.Empty
                    : modelText;
            }

            if (string.IsNullOrWhiteSpace(source))
                return string.Empty;

            var segments = source
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Where(static segment => !string.IsNullOrWhiteSpace(segment))
                .Take(3)
                .ToList();

            return segments.Count == 0
                ? source
                : string.Join(", ", segments);
        }

        private static KnowledgeBaseActDraftResult Failure(string errorMessage) =>
            new()
            {
                IsSuccess = false,
                ErrorMessage = errorMessage
            };
    }
}
