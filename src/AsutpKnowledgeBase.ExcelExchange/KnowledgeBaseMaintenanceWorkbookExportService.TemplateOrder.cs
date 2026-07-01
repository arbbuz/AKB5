using AsutpKnowledgeBase.Models;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;

namespace AsutpKnowledgeBase.Services
{
    public sealed partial class KnowledgeBaseMaintenanceWorkbookExportService
    {
        internal IReadOnlyList<KbMaintenanceMonthWorkItem> OrderWorkItemsByMonthTemplate(
            int month,
            IReadOnlyList<KbMaintenanceMonthWorkItem> workItems,
            IReadOnlyList<KbNode> roots)
        {
            if (workItems.Count == 0)
                return Array.Empty<KbMaintenanceMonthWorkItem>();

            Dictionary<string, KbNode> nodeIndex = BuildNodeIndex(roots);
            IReadOnlyList<TemplateSystemOrderEntry> templateOrder = ReadMonthTemplateSystemOrder(month);
            if (templateOrder.Count == 0)
                return workItems;

            var orderedItems = workItems
                .Select((item, index) =>
                {
                    TemplateSystemOrderEntry? systemEntry = ResolveTemplateSystemOrderEntry(
                        templateOrder,
                        ResolveNodeName(nodeIndex, item.SystemNodeId),
                        ResolveNodeInventoryNumber(nodeIndex, item.SystemNodeId));
                    TemplateDetailOrderEntry? detailEntry = systemEntry == null
                        ? null
                        : ResolveTemplateDetailOrderEntry(
                            systemEntry.DetailRows,
                            ResolveNodeName(nodeIndex, item.OwnerNodeId, item.NodeName),
                            string.Empty);

                    int systemOrder = systemEntry?.Rank ?? ResolveFallbackOrder(item.SystemPreorderIndex, index);
                    int ownerOrder = detailEntry?.Rank ?? ResolveFallbackOrder(item.OwnerPreorderIndex, index);
                    return new
                    {
                        Item = CloneWithOrder(item, systemOrder, ownerOrder),
                        OriginalIndex = index
                    };
                })
                .OrderBy(static item => item.Item.SystemPreorderIndex)
                .ThenBy(static item => item.Item.OwnerPreorderIndex)
                .ThenBy(static item => GetPlanningWorkKindPriority(item.Item.WorkKind))
                .ThenBy(static item => item.OriginalIndex)
                .Select(static item => item.Item)
                .ToList();

            return orderedItems;
        }

        private IReadOnlyList<TemplateSystemOrderEntry> ReadMonthTemplateSystemOrder(int month)
        {
            using MemoryStream stream = CreateExpandableMemoryStream(_templateService.GetTemplatePackage());
            using SpreadsheetDocument document = SpreadsheetDocument.Open(stream, false);
            WorkbookPart workbookPart = document.WorkbookPart
                ?? throw new InvalidOperationException("Шаблон графика ТО повреждён: отсутствует workbook part.");
            Sheet monthSheet = FindMonthSheet(workbookPart, month);
            WorksheetPart monthWorksheetPart = GetWorksheetPart(workbookPart, monthSheet);
            return ReadMonthTemplateSystemOrder(monthWorksheetPart);
        }

        private static Dictionary<string, KbNode> BuildNodeIndex(IEnumerable<KbNode> roots)
        {
            var result = new Dictionary<string, KbNode>(StringComparer.Ordinal);
            AddNodesToIndex(roots, result);
            return result;
        }

        private static void AddNodesToIndex(IEnumerable<KbNode> nodes, IDictionary<string, KbNode> result)
        {
            foreach (KbNode node in nodes)
            {
                string nodeId = node.NodeId?.Trim() ?? string.Empty;
                if (!string.IsNullOrWhiteSpace(nodeId))
                    result[nodeId] = node;

                AddNodesToIndex(node.Children, result);
            }
        }

        private static string ResolveNodeName(
            IReadOnlyDictionary<string, KbNode> nodeIndex,
            string nodeId,
            string fallback = "")
        {
            string normalizedNodeId = nodeId?.Trim() ?? string.Empty;
            return !string.IsNullOrWhiteSpace(normalizedNodeId) &&
                   nodeIndex.TryGetValue(normalizedNodeId, out KbNode? node)
                ? node.Name?.Trim() ?? string.Empty
                : fallback?.Trim() ?? string.Empty;
        }

        private static string ResolveNodeInventoryNumber(
            IReadOnlyDictionary<string, KbNode> nodeIndex,
            string nodeId)
        {
            string normalizedNodeId = nodeId?.Trim() ?? string.Empty;
            return !string.IsNullOrWhiteSpace(normalizedNodeId) &&
                   nodeIndex.TryGetValue(normalizedNodeId, out KbNode? node)
                ? node.Details?.InventoryNumber?.Trim() ?? string.Empty
                : string.Empty;
        }

        private static int ResolveFallbackOrder(int order, int originalIndex)
        {
            if (order != int.MaxValue)
                return order;

            return (int.MaxValue / 2) + Math.Min(originalIndex, int.MaxValue / 2);
        }

        private static int GetPlanningWorkKindPriority(KbMaintenanceWorkKind workKind) =>
            workKind switch
            {
                KbMaintenanceWorkKind.To3 => 0,
                KbMaintenanceWorkKind.To2 => 1,
                _ => 2
            };

        private static KbMaintenanceMonthWorkItem CloneWithOrder(
            KbMaintenanceMonthWorkItem source,
            int systemOrder,
            int ownerOrder) =>
            new()
            {
                OwnerNodeId = source.OwnerNodeId,
                NodeName = source.NodeName,
                SystemNodeId = source.SystemNodeId,
                SystemPreorderIndex = systemOrder,
                OwnerPreorderIndex = ownerOrder,
                SystemLevel3NodeCount = source.SystemLevel3NodeCount,
                WorkKind = source.WorkKind,
                Hours = source.Hours
            };
    }
}
