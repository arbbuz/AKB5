using AsutpKnowledgeBase.Models;

namespace AsutpKnowledgeBase.Services
{
    public sealed class KnowledgeBaseActDraftRequest
    {
        public KbNode? Lvl3Node { get; init; }

        public IReadOnlyList<KbNode>? WorkshopRoots { get; init; }

        public string WorkshopName { get; init; } = string.Empty;

        public int VisibleLevel { get; init; } = 3;

        public KbCompositionRack? Rack { get; init; }

        public KbCompositionEntry? CompositionEntry { get; init; }

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

            KbNode? lvl3Node = request.Lvl3Node;
            if (lvl3Node == null)
                return Failure("Не выбран объект Lvl3.");

            if (!KnowledgeBaseCompositionStateService.SupportsComposition(lvl3Node.NodeType, request.VisibleLevel))
                return Failure("Черновик акта можно создать только из вкладки \"Состав\" объекта Lvl3.");

            KbCompositionEntry? entry = request.CompositionEntry;
            if (entry == null)
                return Failure("Не выбрана строка состава.");

            string entryId = entry.EntryId?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(entryId))
                return Failure("Нельзя создать акт по пустой строке Rack.");

            string lvl3NodeId = lvl3Node.NodeId?.Trim() ?? string.Empty;
            string entryParentNodeId = entry.ParentNodeId?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(lvl3NodeId) ||
                !string.Equals(entryParentNodeId, lvl3NodeId, StringComparison.Ordinal))
            {
                return Failure("Выбранная строка состава не принадлежит выбранному объекту Lvl3.");
            }

            KbCompositionRack? rack = request.Rack;
            int entryRackNumber = KnowledgeBaseCompositionRackSlotRulesService.NormalizeRackNumber(entry.RackNumber);
            if (rack != null)
            {
                string rackParentNodeId = rack.ParentNodeId?.Trim() ?? string.Empty;
                if (!string.IsNullOrWhiteSpace(rackParentNodeId) &&
                    !string.Equals(rackParentNodeId, lvl3NodeId, StringComparison.Ordinal))
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
                ? lvl3Node.Name
                : _nodePresentationService.BuildNodePath(request.WorkshopRoots, lvl3Node);
            string objectName = ResolveParentNodeName(request.WorkshopRoots, lvl3Node);
            string rackName = rack == null
                ? KnowledgeBaseCompositionRackSlotRulesService.FormatRackTitle(entryRackNumber)
                : KnowledgeBaseCompositionRackSlotRulesService.FormatRackTitle(
                    rack.RackNumber,
                    rack.RackType,
                    rack.Label);

            var equipmentSnapshot = new KbActEquipmentSnapshot
            {
                Lvl3Name = lvl3Node.Name,
                ObjectPath = objectPath,
                RackId = rack?.RackId ?? string.Empty,
                RackNumber = entryRackNumber,
                RackName = rackName,
                CompositionEntryId = entryId,
                ComponentType = entry.ComponentType,
                Model = entry.Model,
                OrderNumber = entry.OrderNumber,
                SerialNumber = string.Empty,
                Notes = entry.Notes
            };
            string equipmentName = BuildDefaultEquipmentName(
                entry.ComponentType,
                entry.Model,
                entry.OrderNumber);

            var act = new KbAct
            {
                ActId = _actIdFactory(),
                ActYear = actDate.Year,
                ActNumber = string.Empty,
                ActType = request.ActType,
                Status = KbActStatus.Draft,
                ActDate = actDate,
                WorkshopName = workshopName,
                Lvl3NodeId = lvl3NodeId,
                Lvl3NameSnapshot = lvl3Node.Name,
                ObjectNameSnapshot = objectName,
                ObjectPathSnapshot = objectPath,
                RackId = rack?.RackId ?? string.Empty,
                RackNumberSnapshot = entryRackNumber,
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
