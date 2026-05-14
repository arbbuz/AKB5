using AsutpKnowledgeBase.Models;

namespace AsutpKnowledgeBase.Services
{
    public sealed class KnowledgeBaseMaintenanceAnnualWorkbookModelBuildResult
    {
        public bool IsSuccess { get; init; }

        public string ErrorMessage { get; init; } = string.Empty;

        public KbMaintenanceAnnualWorkbookModel? WorkbookModel { get; init; }
    }

    public sealed class KnowledgeBaseMaintenanceAnnualWorkbookModelBuilderService
    {
        private readonly KnowledgeBaseMaintenanceMonthWorkResolverService _workResolverService;

        public KnowledgeBaseMaintenanceAnnualWorkbookModelBuilderService(
            KnowledgeBaseMaintenanceMonthWorkResolverService? workResolverService = null)
        {
            _workResolverService = workResolverService ?? new KnowledgeBaseMaintenanceMonthWorkResolverService();
        }

        public KnowledgeBaseMaintenanceAnnualWorkbookModelBuildResult Build(
            int year,
            string workshopName,
            IReadOnlyList<KbNode>? roots,
            IReadOnlyList<KbMaintenanceScheduleProfile>? maintenanceScheduleProfiles)
        {
            if (year < 1)
                return Failure("Год годового графика ТО должен быть положительным.");

            IReadOnlyList<KbNode> normalizedRoots = roots ?? Array.Empty<KbNode>();
            var nodeIndex = BuildNodeIndex(normalizedRoots);
            var systemBuilders = new Dictionary<string, SystemGroupBuilder>(StringComparer.Ordinal);
            AddIncludedProfileRows(systemBuilders, nodeIndex, maintenanceScheduleProfiles);

            for (int month = 1; month <= 12; month++)
            {
                IReadOnlyList<KbMaintenanceMonthWorkItem> workItems =
                    _workResolverService.ResolveMonthWorkItems(year, month, normalizedRoots, maintenanceScheduleProfiles);
                foreach (KbMaintenanceMonthWorkItem workItem in workItems)
                {
                    string ownerNodeId = workItem.OwnerNodeId?.Trim() ?? string.Empty;
                    if (string.IsNullOrWhiteSpace(ownerNodeId) || !nodeIndex.TryGetValue(ownerNodeId, out IndexedNode? indexedNode))
                    {
                        return Failure($"Не удалось сопоставить строку годового графика ТО с узлом дерева: '{workItem.NodeName}'.");
                    }

                    if (indexedNode.VisibleLevel < 2 || indexedNode.Level2Ancestor == null)
                    {
                        return Failure(
                            $"Узел '{indexedNode.Node.Name}' не находится под видимым уровнем Lvl2 и не может быть выгружен в годовую форму графика ТО.");
                    }

                    string systemNodeId = indexedNode.Level2Ancestor.NodeId?.Trim() ?? string.Empty;
                    if (!systemBuilders.TryGetValue(systemNodeId, out SystemGroupBuilder? systemBuilder))
                    {
                        systemBuilder = new SystemGroupBuilder(indexedNode.Level2Ancestor, indexedNode.Level2AncestorPreorderIndex);
                        systemBuilders.Add(systemNodeId, systemBuilder);
                    }

                    systemBuilder.AddWorkItem(month, indexedNode, workItem);
                }
            }

            List<KbMaintenanceAnnualSystemGroup> groups = systemBuilders.Values
                .OrderBy(static builder => builder.SystemPreorderIndex)
                .Select(static (builder, index) => builder.Build(index + 1))
                .Where(static group => group.DetailRows.Count > 0)
                .ToList();

            return Success(
                new KbMaintenanceAnnualWorkbookModel
                {
                    Year = year,
                    WorkshopName = workshopName?.Trim() ?? string.Empty,
                    TotalHours = groups.Sum(static group => group.DetailRows.Sum(static row => row.TotalHours)),
                    SystemGroups = groups
                });
        }

        private static void AddIncludedProfileRows(
            IDictionary<string, SystemGroupBuilder> systemBuilders,
            IReadOnlyDictionary<string, IndexedNode> nodeIndex,
            IReadOnlyList<KbMaintenanceScheduleProfile>? maintenanceScheduleProfiles)
        {
            if (maintenanceScheduleProfiles == null || maintenanceScheduleProfiles.Count == 0)
                return;

            foreach (KbMaintenanceScheduleProfile profile in maintenanceScheduleProfiles)
            {
                if (profile == null || !profile.IsIncludedInSchedule)
                    continue;

                string ownerNodeId = profile.OwnerNodeId?.Trim() ?? string.Empty;
                if (string.IsNullOrWhiteSpace(ownerNodeId) ||
                    !nodeIndex.TryGetValue(ownerNodeId, out IndexedNode? indexedNode) ||
                    indexedNode.VisibleLevel < 2 ||
                    indexedNode.Level2Ancestor == null ||
                    !KnowledgeBaseMaintenanceScheduleStateService.SupportsProfile(indexedNode.Node.NodeType, indexedNode.VisibleLevel))
                {
                    continue;
                }

                string systemNodeId = indexedNode.Level2Ancestor.NodeId?.Trim() ?? string.Empty;
                if (!systemBuilders.TryGetValue(systemNodeId, out SystemGroupBuilder? systemBuilder))
                {
                    systemBuilder = new SystemGroupBuilder(indexedNode.Level2Ancestor, indexedNode.Level2AncestorPreorderIndex);
                    systemBuilders.Add(systemNodeId, systemBuilder);
                }

                systemBuilder.AddDetailNode(indexedNode);
            }
        }

        private static Dictionary<string, IndexedNode> BuildNodeIndex(IReadOnlyList<KbNode> roots)
        {
            var index = new Dictionary<string, IndexedNode>(StringComparer.Ordinal);
            int preorderIndex = 0;

            IndexNodes(roots, visibleLevel: 1, level2Ancestor: null, level2AncestorPreorderIndex: -1, index, ref preorderIndex);
            return index;
        }

        private static void IndexNodes(
            IEnumerable<KbNode> nodes,
            int visibleLevel,
            KbNode? level2Ancestor,
            int level2AncestorPreorderIndex,
            IDictionary<string, IndexedNode> index,
            ref int preorderIndex)
        {
            foreach (KbNode node in nodes)
            {
                int currentVisibleLevel = GetEffectiveVisibleLevel(node, visibleLevel);
                KbNode? currentLevel2Ancestor = level2Ancestor;
                int currentLevel2AncestorPreorderIndex = level2AncestorPreorderIndex;
                if (currentVisibleLevel == 2)
                {
                    currentLevel2Ancestor = node;
                    currentLevel2AncestorPreorderIndex = preorderIndex;
                }

                string nodeId = node.NodeId?.Trim() ?? string.Empty;
                if (!string.IsNullOrWhiteSpace(nodeId))
                {
                    index[nodeId] = new IndexedNode(
                        node,
                        currentVisibleLevel,
                        preorderIndex,
                        currentLevel2Ancestor,
                        currentLevel2AncestorPreorderIndex);
                }

                preorderIndex++;
                IndexNodes(
                    node.Children,
                    currentVisibleLevel + 1,
                    currentLevel2Ancestor,
                    currentLevel2AncestorPreorderIndex,
                    index,
                    ref preorderIndex);
            }
        }

        private static int GetEffectiveVisibleLevel(KbNode node, int visibleLevel)
        {
            if (node.NodeType == KbNodeType.WorkshopRoot && node.LevelIndex == 0)
                return Math.Max(0, visibleLevel - 1);

            return visibleLevel;
        }

        private static KnowledgeBaseMaintenanceAnnualWorkbookModelBuildResult Success(KbMaintenanceAnnualWorkbookModel workbookModel) =>
            new()
            {
                IsSuccess = true,
                WorkbookModel = workbookModel
            };

        private static KnowledgeBaseMaintenanceAnnualWorkbookModelBuildResult Failure(string errorMessage) =>
            new()
            {
                IsSuccess = false,
                ErrorMessage = errorMessage
            };

        private static string BuildPlanText(KbMaintenanceWorkKind workKind, int hours) =>
            $"{GetWorkKindText(workKind)}/{hours}";

        private static string GetWorkKindText(KbMaintenanceWorkKind workKind) =>
            workKind switch
            {
                KbMaintenanceWorkKind.To1 => "ТО1",
                KbMaintenanceWorkKind.To2 => "ТО2",
                KbMaintenanceWorkKind.To3 => "ТО3",
                _ => "ТО"
            };

        private sealed record IndexedNode(
            KbNode Node,
            int VisibleLevel,
            int PreorderIndex,
            KbNode? Level2Ancestor,
            int Level2AncestorPreorderIndex);

        private sealed class SystemGroupBuilder
        {
            private readonly KbNode _systemNode;
            private readonly Dictionary<string, DetailRowBuilder> _detailBuilders = new(StringComparer.Ordinal);

            public SystemGroupBuilder(KbNode systemNode, int systemPreorderIndex)
            {
                _systemNode = systemNode;
                SystemPreorderIndex = systemPreorderIndex;
            }

            public int SystemPreorderIndex { get; }

            public string SystemName => _systemNode.Name?.Trim() ?? string.Empty;

            public void AddDetailNode(IndexedNode indexedNode)
            {
                string ownerNodeId = indexedNode.Node.NodeId?.Trim() ?? string.Empty;
                if (string.IsNullOrWhiteSpace(ownerNodeId) || _detailBuilders.ContainsKey(ownerNodeId))
                    return;

                _detailBuilders.Add(ownerNodeId, new DetailRowBuilder(indexedNode.Node, indexedNode.PreorderIndex));
            }

            public void AddWorkItem(int month, IndexedNode indexedNode, KbMaintenanceMonthWorkItem workItem)
            {
                string ownerNodeId = indexedNode.Node.NodeId?.Trim() ?? string.Empty;
                if (!_detailBuilders.TryGetValue(ownerNodeId, out DetailRowBuilder? detailBuilder))
                {
                    detailBuilder = new DetailRowBuilder(indexedNode.Node, indexedNode.PreorderIndex);
                    _detailBuilders.Add(ownerNodeId, detailBuilder);
                }

                detailBuilder.AddWorkItem(month, workItem);
            }

            public KbMaintenanceAnnualSystemGroup Build(int sequenceNumber) =>
                new()
                {
                    SequenceNumber = sequenceNumber,
                    SystemNodeId = _systemNode.NodeId?.Trim() ?? string.Empty,
                    SystemName = _systemNode.Name?.Trim() ?? string.Empty,
                    InventoryNumber = _systemNode.Details?.InventoryNumber?.Trim() ?? string.Empty,
                    DetailRows = _detailBuilders.Values
                        .OrderBy(static builder => builder.NodePreorderIndex)
                        .Select(static builder => builder.Build())
                        .Where(static row => row.TotalHours > 0)
                        .ToList()
                };
        }

        private sealed class DetailRowBuilder
        {
            private readonly KbNode _node;
            private readonly Dictionary<int, MonthCellBuilder> _monthCellBuilders = new();

            public DetailRowBuilder(KbNode node, int nodePreorderIndex)
            {
                _node = node;
                NodePreorderIndex = nodePreorderIndex;
            }

            public int NodePreorderIndex { get; }

            public void AddWorkItem(int month, KbMaintenanceMonthWorkItem workItem)
            {
                if (!_monthCellBuilders.TryGetValue(month, out MonthCellBuilder? monthCellBuilder))
                {
                    monthCellBuilder = new MonthCellBuilder(month);
                    _monthCellBuilders.Add(month, monthCellBuilder);
                }

                monthCellBuilder.Add(workItem.WorkKind, Math.Max(0, workItem.Hours));
            }

            public KbMaintenanceAnnualDetailRow Build() =>
                new()
                {
                    OwnerNodeId = _node.NodeId?.Trim() ?? string.Empty,
                    NodeName = _node.Name?.Trim() ?? string.Empty,
                    InventoryNumber = _node.Details?.InventoryNumber?.Trim() ?? string.Empty,
                    TotalHours = _monthCellBuilders.Values.Sum(static builder => builder.Hours),
                    MonthCells = _monthCellBuilders.Values
                        .OrderBy(static builder => builder.Month)
                        .Select(static builder => builder.Build())
                        .ToList()
                };
        }

        private sealed class MonthCellBuilder
        {
            public MonthCellBuilder(int month)
            {
                Month = month;
            }

            public int Month { get; }

            public KbMaintenanceWorkKind WorkKind { get; private set; }

            public int Hours { get; private set; }

            public void Add(KbMaintenanceWorkKind workKind, int hours)
            {
                if (hours <= 0)
                    return;

                if (Hours == 0 || workKind > WorkKind)
                    WorkKind = workKind;

                Hours += hours;
            }

            public KbMaintenanceAnnualMonthCell Build() =>
                new()
                {
                    Month = Month,
                    WorkKind = WorkKind,
                    Hours = Hours,
                    PlanText = BuildPlanText(WorkKind, Hours)
                };
        }
    }
}
