using AsutpKnowledgeBase.Models;

namespace AsutpKnowledgeBase.Services
{
    public sealed class KnowledgeBaseMaintenanceMonthSheetModelBuildResult
    {
        public bool IsSuccess { get; init; }

        public string ErrorMessage { get; init; } = string.Empty;

        public KbMaintenanceMonthSheetModel? SheetModel { get; init; }
    }

    public sealed class KnowledgeBaseMaintenanceMonthSheetModelBuilderService
    {
        public KnowledgeBaseMaintenanceMonthSheetModelBuildResult Build(
            int year,
            int month,
            IReadOnlyList<KbNode> roots,
            KnowledgeBaseMaintenanceMonthPlanResult? planResult)
        {
            if (planResult == null)
                return Failure("Отсутствует результат месячного планирования.");

            if (!planResult.IsSuccess)
            {
                string planError = string.IsNullOrWhiteSpace(planResult.ErrorMessage)
                    ? "Месячный план не был сформирован."
                    : planResult.ErrorMessage;
                return Failure($"Невозможно построить модель листа графика ТО: {planError}");
            }

            var nodeIndex = BuildNodeIndex(roots);
            var systemBuilders = new Dictionary<string, SystemGroupBuilder>(StringComparer.Ordinal);

            foreach (KbMaintenanceMonthPlanDay plannedDay in planResult.PlannedDays)
            {
                foreach (KbMaintenanceMonthPlanAssignment assignment in plannedDay.Assignments)
                {
                    string ownerNodeId = assignment.OwnerNodeId?.Trim() ?? string.Empty;
                    if (string.IsNullOrWhiteSpace(ownerNodeId) || !nodeIndex.TryGetValue(ownerNodeId, out IndexedNode? indexedNode))
                    {
                        return Failure(
                            $"Не удалось сопоставить строку графика ТО с узлом дерева: '{assignment.NodeName}'.");
                    }

                    if (indexedNode.VisibleLevel <= 2 || indexedNode.Level2Ancestor == null)
                    {
                        return Failure(
                            $"Узел '{indexedNode.Node.Name}' не находится под видимым уровнем Lvl2 и не может быть выгружен в форму графика ТО.");
                    }

                    string systemNodeId = indexedNode.Level2Ancestor.NodeId?.Trim() ?? string.Empty;
                    if (!systemBuilders.TryGetValue(systemNodeId, out SystemGroupBuilder? systemBuilder))
                    {
                        systemBuilder = new SystemGroupBuilder(indexedNode.Level2Ancestor, indexedNode.Level2AncestorPreorderIndex);
                        systemBuilders.Add(systemNodeId, systemBuilder);
                    }

                    systemBuilder.AddAssignment(indexedNode, assignment);
                }
            }

            List<KbMaintenanceMonthSheetSystemGroup> groups = systemBuilders.Values
                .OrderBy(static builder => builder.SystemPreorderIndex)
                .Select(static (builder, index) => builder.Build(index + 1))
                .ToList();

            var dailyTotals = planResult.PlannedDays
                .OrderBy(static day => day.Date)
                .Select(static day => new KbMaintenanceMonthSheetDayTotal
                {
                    DayOfMonth = day.Date.Day,
                    TotalHours = day.TotalHours
                })
                .ToList();

            return Success(
                new KbMaintenanceMonthSheetModel
                {
                    Year = year,
                    Month = month,
                    WorkingDayCount = planResult.WorkingDayCount,
                    TotalPlannedHours = planResult.PlannedDays.Sum(static day => day.TotalHours),
                    DailyTotals = dailyTotals,
                    SystemGroups = groups
                });
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
                KbNode? currentLevel2Ancestor = level2Ancestor;
                int currentLevel2AncestorPreorderIndex = level2AncestorPreorderIndex;
                if (visibleLevel == 2)
                {
                    currentLevel2Ancestor = node;
                    currentLevel2AncestorPreorderIndex = preorderIndex;
                }

                string nodeId = node.NodeId?.Trim() ?? string.Empty;
                if (!string.IsNullOrWhiteSpace(nodeId))
                {
                    index[nodeId] = new IndexedNode(
                        node,
                        visibleLevel,
                        preorderIndex,
                        currentLevel2Ancestor,
                        currentLevel2AncestorPreorderIndex);
                }

                preorderIndex++;
                IndexNodes(
                    node.Children,
                    visibleLevel + 1,
                    currentLevel2Ancestor,
                    currentLevel2AncestorPreorderIndex,
                    index,
                    ref preorderIndex);
            }
        }

        private static KnowledgeBaseMaintenanceMonthSheetModelBuildResult Success(KbMaintenanceMonthSheetModel sheetModel) =>
            new()
            {
                IsSuccess = true,
                SheetModel = sheetModel
            };

        private static KnowledgeBaseMaintenanceMonthSheetModelBuildResult Failure(string errorMessage) =>
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

            public void AddAssignment(IndexedNode indexedNode, KbMaintenanceMonthPlanAssignment assignment)
            {
                string ownerNodeId = indexedNode.Node.NodeId?.Trim() ?? string.Empty;
                if (!_detailBuilders.TryGetValue(ownerNodeId, out DetailRowBuilder? detailBuilder))
                {
                    detailBuilder = new DetailRowBuilder(indexedNode.Node, indexedNode.PreorderIndex);
                    _detailBuilders.Add(ownerNodeId, detailBuilder);
                }

                detailBuilder.AddAssignment(assignment);
            }

            public KbMaintenanceMonthSheetSystemGroup Build(int sequenceNumber) =>
                new()
                {
                    SequenceNumber = sequenceNumber,
                    SystemNodeId = _systemNode.NodeId?.Trim() ?? string.Empty,
                    SystemName = _systemNode.Name?.Trim() ?? string.Empty,
                    InventoryNumber = _systemNode.Details?.InventoryNumber?.Trim() ?? string.Empty,
                    DetailRows = _detailBuilders.Values
                        .OrderBy(static builder => builder.NodePreorderIndex)
                        .Select(static builder => builder.Build())
                        .ToList()
                };
        }

        private sealed class DetailRowBuilder
        {
            private readonly KbNode _node;
            private readonly Dictionary<int, DayCellBuilder> _dayCellBuilders = new();

            public DetailRowBuilder(KbNode node, int nodePreorderIndex)
            {
                _node = node;
                NodePreorderIndex = nodePreorderIndex;
            }

            public int NodePreorderIndex { get; }

            public void AddAssignment(KbMaintenanceMonthPlanAssignment assignment)
            {
                int dayOfMonth = assignment.Date.Day;
                if (!_dayCellBuilders.TryGetValue(dayOfMonth, out DayCellBuilder? dayCellBuilder))
                {
                    dayCellBuilder = new DayCellBuilder(dayOfMonth);
                    _dayCellBuilders.Add(dayOfMonth, dayCellBuilder);
                }

                dayCellBuilder.WorkEntries.Add(new KbMaintenanceMonthSheetWorkEntry
                {
                    WorkKind = assignment.WorkKind,
                    Hours = assignment.Hours,
                    PlanText = BuildPlanText(assignment.WorkKind, assignment.Hours)
                });
                dayCellBuilder.TotalHours += assignment.Hours;
            }

            public KbMaintenanceMonthSheetDetailRow Build() =>
                new()
                {
                    OwnerNodeId = _node.NodeId?.Trim() ?? string.Empty,
                    NodeName = _node.Name?.Trim() ?? string.Empty,
                    NodeType = _node.NodeType,
                    TotalHours = _dayCellBuilders.Values.Sum(static builder => builder.TotalHours),
                    DayCells = _dayCellBuilders.Values
                        .OrderBy(static builder => builder.DayOfMonth)
                        .Select(static builder => builder.Build())
                        .ToList()
                };
        }

        private sealed class DayCellBuilder
        {
            public DayCellBuilder(int dayOfMonth)
            {
                DayOfMonth = dayOfMonth;
            }

            public int DayOfMonth { get; }

            public int TotalHours { get; set; }

            public List<KbMaintenanceMonthSheetWorkEntry> WorkEntries { get; } = new();

            public KbMaintenanceMonthSheetDayCell Build() =>
                new()
                {
                    DayOfMonth = DayOfMonth,
                    TotalHours = TotalHours,
                    WorkEntries = WorkEntries.ToList()
                };
        }
    }
}
