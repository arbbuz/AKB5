using System.Text.Encodings.Web;
using System.Text.Json;
using AsutpKnowledgeBase.Models;

namespace AsutpKnowledgeBase.Services
{
    public sealed class KnowledgeBaseSnapshotComparisonSection
    {
        public string AreaName { get; init; } = string.Empty;

        public int AddedCount { get; init; }

        public int RemovedCount { get; init; }

        public int ChangedCount { get; init; }

        public bool HasChanges => AddedCount > 0 || RemovedCount > 0 || ChangedCount > 0;
    }

    public sealed class KnowledgeBaseSnapshotComparisonResult
    {
        public IReadOnlyList<KnowledgeBaseSnapshotComparisonSection> Sections { get; init; } =
            Array.Empty<KnowledgeBaseSnapshotComparisonSection>();

        public bool HasChanges => Sections.Any(static section => section.HasChanges);
    }

    public sealed class KnowledgeBaseSnapshotComparisonService
    {
        private static readonly JsonSerializerOptions SerializerOptions = new()
        {
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            WriteIndented = false
        };

        public KnowledgeBaseSnapshotComparisonResult Compare(SavedData left, SavedData right)
        {
            SavedData normalizedLeft = KnowledgeBaseDataService.NormalizeSavedData(left);
            SavedData normalizedRight = KnowledgeBaseDataService.NormalizeSavedData(right);

            var sections = new List<KnowledgeBaseSnapshotComparisonSection>
            {
                CompareKeys(
                    "Цеха",
                    normalizedLeft.Workshops.Keys,
                    normalizedRight.Workshops.Keys),
                CompareKeyed(
                    "Узлы дерева",
                    FlattenNodes(normalizedLeft.Workshops),
                    FlattenNodes(normalizedRight.Workshops),
                    static node => node.NodeId),
                CompareKeyed(
                    "Состав",
                    normalizedLeft.CompositionEntries,
                    normalizedRight.CompositionEntries,
                    static entry => entry.EntryId),
                CompareKeyed(
                    "Rack состава",
                    normalizedLeft.CompositionRacks,
                    normalizedRight.CompositionRacks,
                    static rack => rack.RackId),
                CompareKeyed(
                    "Документы",
                    normalizedLeft.DocumentLinks,
                    normalizedRight.DocumentLinks,
                    static link => link.DocumentId),
                CompareKeyed(
                    "ПО",
                    normalizedLeft.SoftwareRecords,
                    normalizedRight.SoftwareRecords,
                    static record => record.SoftwareId),
                CompareKeyed(
                    "Сетевые файлы",
                    normalizedLeft.NetworkFileReferences,
                    normalizedRight.NetworkFileReferences,
                    static reference => reference.NetworkAssetId),
                CompareKeyed(
                    "Графики ТО",
                    normalizedLeft.MaintenanceScheduleProfiles,
                    normalizedRight.MaintenanceScheduleProfiles,
                    static profile => profile.MaintenanceProfileId),
                CompareKeyed(
                    "Производственный календарь",
                    normalizedLeft.Config.ProductionCalendarYears,
                    normalizedRight.Config.ProductionCalendarYears,
                    static year => year.Year.ToString(System.Globalization.CultureInfo.InvariantCulture)),
                CompareKeyed(
                    "Каталог оборудования",
                    normalizedLeft.EquipmentCatalogItems,
                    normalizedRight.EquipmentCatalogItems,
                    static item => item.CatalogItemId),
                CompareKeyed(
                    "Шаблоны объектов",
                    normalizedLeft.ObjectTemplates,
                    normalizedRight.ObjectTemplates,
                    static template => template.TemplateId)
            };

            return new KnowledgeBaseSnapshotComparisonResult
            {
                Sections = sections
            };
        }

        public string BuildDisplayText(
            KnowledgeBaseSnapshotComparisonResult result,
            string leftLabel,
            string rightLabel)
        {
            var lines = new List<string>
            {
                "Сравнение снимков базы",
                $"1: {leftLabel}",
                $"2: {rightLabel}",
                string.Empty
            };

            if (!result.HasChanges)
            {
                lines.Add("Отличий в сравниваемых областях не найдено.");
                return string.Join(Environment.NewLine, lines);
            }

            foreach (KnowledgeBaseSnapshotComparisonSection section in result.Sections)
            {
                lines.Add(
                    $"{section.AreaName}: +{section.AddedCount}, -{section.RemovedCount}, изменено {section.ChangedCount}");
            }

            return string.Join(Environment.NewLine, lines);
        }

        private static KnowledgeBaseSnapshotComparisonSection CompareKeys(
            string areaName,
            IEnumerable<string> leftKeys,
            IEnumerable<string> rightKeys)
        {
            var left = new HashSet<string>(
                leftKeys.Select(static key => key.Trim()).Where(static key => key.Length > 0),
                StringComparer.OrdinalIgnoreCase);
            var right = new HashSet<string>(
                rightKeys.Select(static key => key.Trim()).Where(static key => key.Length > 0),
                StringComparer.OrdinalIgnoreCase);

            return new KnowledgeBaseSnapshotComparisonSection
            {
                AreaName = areaName,
                AddedCount = right.Except(left, StringComparer.OrdinalIgnoreCase).Count(),
                RemovedCount = left.Except(right, StringComparer.OrdinalIgnoreCase).Count()
            };
        }

        private static KnowledgeBaseSnapshotComparisonSection CompareKeyed<T>(
            string areaName,
            IEnumerable<T> leftItems,
            IEnumerable<T> rightItems,
            Func<T, string> keySelector)
        {
            Dictionary<string, string> left = BuildComparisonMap(leftItems, keySelector);
            Dictionary<string, string> right = BuildComparisonMap(rightItems, keySelector);

            var leftKeys = new HashSet<string>(left.Keys, StringComparer.Ordinal);
            var rightKeys = new HashSet<string>(right.Keys, StringComparer.Ordinal);
            int changedCount = leftKeys
                .Intersect(rightKeys, StringComparer.Ordinal)
                .Count(key => !StringComparer.Ordinal.Equals(left[key], right[key]));

            return new KnowledgeBaseSnapshotComparisonSection
            {
                AreaName = areaName,
                AddedCount = rightKeys.Except(leftKeys, StringComparer.Ordinal).Count(),
                RemovedCount = leftKeys.Except(rightKeys, StringComparer.Ordinal).Count(),
                ChangedCount = changedCount
            };
        }

        private static Dictionary<string, string> BuildComparisonMap<T>(
            IEnumerable<T> items,
            Func<T, string> keySelector)
        {
            var map = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (T item in items)
            {
                string key = keySelector(item)?.Trim() ?? string.Empty;
                if (string.IsNullOrWhiteSpace(key))
                    continue;

                map[key] = JsonSerializer.Serialize(item, SerializerOptions);
            }

            return map;
        }

        private static IReadOnlyList<NodeComparisonItem> FlattenNodes(
            Dictionary<string, List<KbNode>> workshops)
        {
            var nodes = new List<NodeComparisonItem>();
            foreach (var workshop in workshops.OrderBy(static pair => pair.Key, StringComparer.OrdinalIgnoreCase))
            {
                for (int i = 0; i < workshop.Value.Count; i++)
                    AddNode(nodes, workshop.Key, parentNodeId: string.Empty, workshop.Value[i], i);
            }

            return nodes;
        }

        private static void AddNode(
            List<NodeComparisonItem> nodes,
            string workshopName,
            string parentNodeId,
            KbNode node,
            int positionOrder)
        {
            nodes.Add(new NodeComparisonItem(
                node.NodeId,
                parentNodeId,
                workshopName,
                positionOrder,
                node.Name,
                node.LevelIndex,
                node.NodeType,
                node.Details));

            for (int i = 0; i < node.Children.Count; i++)
                AddNode(nodes, workshopName, node.NodeId, node.Children[i], i);
        }

        private sealed record NodeComparisonItem(
            string NodeId,
            string ParentNodeId,
            string WorkshopName,
            int PositionOrder,
            string Name,
            int LevelIndex,
            KbNodeType NodeType,
            KbNodeDetails Details);
    }
}
