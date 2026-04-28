using System.Globalization;
using AsutpKnowledgeBase.Models;

namespace AsutpKnowledgeBase.Services
{
    public enum KnowledgeBaseSearchScope
    {
        All = 0,
        Tree = 1,
        Card = 2,
        Composition = 3,
        DocsAndSoftware = 4
    }

    public enum KnowledgeBaseSearchDomain
    {
        Tree = 0,
        Card = 1,
        Composition = 2,
        DocsAndSoftware = 3
    }

    public class KnowledgeBaseTreeSearchMatch
    {
        public KbNode Node { get; init; } = null!;

        public KnowledgeBaseSearchDomain Domain { get; init; }

        public KnowledgeBaseNodeWorkspaceTabKind PreferredTabKind { get; init; }

        public string SearchText { get; init; } = string.Empty;

        public string MatchFieldLabel { get; init; } = string.Empty;

        public string MatchValue { get; init; } = string.Empty;

        public string NodePath { get; init; } = string.Empty;
    }

    public class KnowledgeBaseTreeSearchService
    {
        public IReadOnlyList<KnowledgeBaseTreeSearchMatch> FindMatches(
            IReadOnlyList<KbNode> roots,
            KbConfig config,
            string searchText,
            KnowledgeBaseSearchScope scope = KnowledgeBaseSearchScope.All,
            IReadOnlyList<KbCompositionEntry>? compositionEntries = null,
            IReadOnlyList<KbDocumentLink>? documentLinks = null,
            IReadOnlyList<KbSoftwareRecord>? softwareRecords = null)
        {
            string normalizedSearch = searchText.Trim();
            if (string.IsNullOrWhiteSpace(normalizedSearch))
                return Array.Empty<KnowledgeBaseTreeSearchMatch>();

            var matches = new List<KnowledgeBaseTreeSearchMatch>();
            var pathSegments = new List<string>();
            var searchData = SearchData.Create(compositionEntries, documentLinks, softwareRecords);

            foreach (var root in EnumerateDisplaySortedNodes(roots))
            {
                CollectMatches(
                    root,
                    config,
                    normalizedSearch,
                    scope,
                    searchData,
                    pathSegments,
                    matches);
            }

            return matches;
        }

        private void CollectMatches(
            KbNode node,
            KbConfig config,
            string normalizedSearch,
            KnowledgeBaseSearchScope scope,
            SearchData searchData,
            IList<string> pathSegments,
            ICollection<KnowledgeBaseTreeSearchMatch> matches)
        {
            pathSegments.Add(node.Name);
            string nodePath = string.Join(" / ", pathSegments);

            if (IncludesTree(scope))
                AddTreeMatches(node, normalizedSearch, nodePath, matches);

            if (IncludesCard(scope))
                AddCardMatches(node, normalizedSearch, nodePath, matches);

            if (IncludesComposition(scope))
                AddCompositionMatches(node, normalizedSearch, nodePath, searchData, matches);

            if (IncludesDocsAndSoftware(scope))
                AddDocsAndSoftwareMatches(node, normalizedSearch, nodePath, searchData, matches);

            foreach (var child in EnumerateDisplaySortedNodes(node.Children))
            {
                CollectMatches(
                    child,
                    config,
                    normalizedSearch,
                    scope,
                    searchData,
                    pathSegments,
                    matches);
            }

            pathSegments.RemoveAt(pathSegments.Count - 1);
        }

        private static bool IncludesTree(KnowledgeBaseSearchScope scope) =>
            scope is KnowledgeBaseSearchScope.All or KnowledgeBaseSearchScope.Tree;

        private static bool IncludesCard(KnowledgeBaseSearchScope scope) =>
            scope is KnowledgeBaseSearchScope.All or KnowledgeBaseSearchScope.Card;

        private static bool IncludesComposition(KnowledgeBaseSearchScope scope) =>
            scope is KnowledgeBaseSearchScope.All or KnowledgeBaseSearchScope.Composition;

        private static bool IncludesDocsAndSoftware(KnowledgeBaseSearchScope scope) =>
            scope is KnowledgeBaseSearchScope.All or KnowledgeBaseSearchScope.DocsAndSoftware;

        private static void AddTreeMatches(
            KbNode node,
            string searchText,
            string nodePath,
            ICollection<KnowledgeBaseTreeSearchMatch> matches)
        {
            AddMatchIfContains(
                matches,
                node,
                KnowledgeBaseSearchDomain.Tree,
                searchText,
                "имя узла",
                node.Name,
                nodePath);
        }

        private static void AddCardMatches(
            KbNode node,
            string searchText,
            string nodePath,
            ICollection<KnowledgeBaseTreeSearchMatch> matches)
        {
            var details = node.Details ?? new KbNodeDetails();

            AddMatchIfContains(
                matches,
                node,
                KnowledgeBaseSearchDomain.Card,
                searchText,
                "описание",
                details.Description,
                nodePath);
            AddMatchIfContains(
                matches,
                node,
                KnowledgeBaseSearchDomain.Card,
                searchText,
                "расположение",
                details.Location,
                nodePath);
            AddMatchIfContains(
                matches,
                node,
                KnowledgeBaseSearchDomain.Card,
                searchText,
                "фото",
                details.PhotoPath,
                nodePath);
            AddMatchIfContains(
                matches,
                node,
                KnowledgeBaseSearchDomain.Card,
                searchText,
                "IP-адрес",
                details.IpAddress,
                nodePath);
            AddMatchIfContains(
                matches,
                node,
                KnowledgeBaseSearchDomain.Card,
                searchText,
                "ссылка на схему",
                details.SchemaLink,
                nodePath);
        }

        private static void AddCompositionMatches(
            KbNode node,
            string searchText,
            string nodePath,
            SearchData searchData,
            ICollection<KnowledgeBaseTreeSearchMatch> matches)
        {
            if (!searchData.CompositionEntriesByParentId.TryGetValue(node.NodeId, out var entries))
                return;

            foreach (var entry in entries)
            {
                AddMatchIfContains(
                    matches,
                    node,
                    KnowledgeBaseSearchDomain.Composition,
                    searchText,
                    "слот",
                    entry.SlotNumber?.ToString(CultureInfo.InvariantCulture),
                    nodePath);
                AddMatchIfContains(
                    matches,
                    node,
                    KnowledgeBaseSearchDomain.Composition,
                    searchText,
                    "позиция",
                    entry.PositionOrder.ToString(CultureInfo.InvariantCulture),
                    nodePath);
                AddMatchIfContains(
                    matches,
                    node,
                    KnowledgeBaseSearchDomain.Composition,
                    searchText,
                    "тип компонента",
                    entry.ComponentType,
                    nodePath);
                AddMatchIfContains(
                    matches,
                    node,
                    KnowledgeBaseSearchDomain.Composition,
                    searchText,
                    "модель",
                    entry.Model,
                    nodePath);
                AddMatchIfContains(
                    matches,
                    node,
                    KnowledgeBaseSearchDomain.Composition,
                    searchText,
                    "IP-адрес",
                    entry.IpAddress,
                    nodePath);
                AddMatchIfContains(
                    matches,
                    node,
                    KnowledgeBaseSearchDomain.Composition,
                    searchText,
                    "последняя калибровка",
                    FormatDate(entry.LastCalibrationAt),
                    nodePath);
                AddMatchIfContains(
                    matches,
                    node,
                    KnowledgeBaseSearchDomain.Composition,
                    searchText,
                    "следующая калибровка",
                    FormatDate(entry.NextCalibrationAt),
                    nodePath);
                AddMatchIfContains(
                    matches,
                    node,
                    KnowledgeBaseSearchDomain.Composition,
                    searchText,
                    "примечание",
                    entry.Notes,
                    nodePath);
            }
        }

        private static void AddDocsAndSoftwareMatches(
            KbNode node,
            string searchText,
            string nodePath,
            SearchData searchData,
            ICollection<KnowledgeBaseTreeSearchMatch> matches)
        {
            if (searchData.DocumentLinksByOwnerId.TryGetValue(node.NodeId, out var documentLinks))
            {
                foreach (var link in documentLinks)
                {
                    AddMatchIfContains(
                        matches,
                        node,
                        KnowledgeBaseSearchDomain.DocsAndSoftware,
                        searchText,
                        "название документа",
                        link.Title,
                        nodePath);
                    AddMatchIfContains(
                        matches,
                        node,
                        KnowledgeBaseSearchDomain.DocsAndSoftware,
                        searchText,
                        "путь к документу",
                        link.Path,
                        nodePath);
                    AddMatchIfContains(
                        matches,
                        node,
                        KnowledgeBaseSearchDomain.DocsAndSoftware,
                        searchText,
                        "тип документа",
                        GetDocumentKindSearchText(link.Kind),
                        nodePath);
                }
            }

            if (!searchData.SoftwareRecordsByOwnerId.TryGetValue(node.NodeId, out var softwareRecords))
                return;

            foreach (var record in softwareRecords)
            {
                AddMatchIfContains(
                    matches,
                    node,
                    KnowledgeBaseSearchDomain.DocsAndSoftware,
                    searchText,
                    "название ПО",
                    record.Title,
                    nodePath);
                AddMatchIfContains(
                    matches,
                    node,
                    KnowledgeBaseSearchDomain.DocsAndSoftware,
                    searchText,
                    "путь к ПО",
                    record.Path,
                    nodePath);
                AddMatchIfContains(
                    matches,
                    node,
                    KnowledgeBaseSearchDomain.DocsAndSoftware,
                    searchText,
                    "дата добавления ПО",
                    FormatDate(record.AddedAt),
                    nodePath);
            }
        }

        private static void AddMatchIfContains(
            ICollection<KnowledgeBaseTreeSearchMatch> matches,
            KbNode node,
            KnowledgeBaseSearchDomain domain,
            string searchText,
            string matchFieldLabel,
            string? matchValue,
            string nodePath)
        {
            string normalizedValue = matchValue?.Trim() ?? string.Empty;
            if (!Contains(normalizedValue, searchText))
                return;

            matches.Add(BuildMatch(node, domain, searchText, matchFieldLabel, normalizedValue, nodePath));
        }

        private static bool Contains(string? value, string searchText) =>
            !string.IsNullOrWhiteSpace(value) &&
            value.Contains(searchText, StringComparison.CurrentCultureIgnoreCase);

        private static IEnumerable<KbNode> EnumerateDisplaySortedNodes(IReadOnlyList<KbNode> nodes)
        {
            if (nodes.Count <= 1)
                return nodes;

            var sortedNodes = nodes.ToArray();
            Array.Sort(
                sortedNodes,
                static (left, right) => KnowledgeBaseNaturalStringComparer.Instance.Compare(left.Name, right.Name));
            return sortedNodes;
        }

        private static KnowledgeBaseTreeSearchMatch BuildMatch(
            KbNode node,
            KnowledgeBaseSearchDomain domain,
            string searchText,
            string matchFieldLabel,
            string matchValue,
            string nodePath) =>
            new()
            {
                Node = node,
                Domain = domain,
                PreferredTabKind = GetPreferredTabKind(domain),
                SearchText = searchText,
                MatchFieldLabel = matchFieldLabel,
                MatchValue = matchValue,
                NodePath = nodePath
            };

        private static KnowledgeBaseNodeWorkspaceTabKind GetPreferredTabKind(KnowledgeBaseSearchDomain domain) =>
            domain switch
            {
                KnowledgeBaseSearchDomain.Tree => KnowledgeBaseNodeWorkspaceTabKind.Info,
                KnowledgeBaseSearchDomain.Card => KnowledgeBaseNodeWorkspaceTabKind.Info,
                KnowledgeBaseSearchDomain.Composition => KnowledgeBaseNodeWorkspaceTabKind.Composition,
                KnowledgeBaseSearchDomain.DocsAndSoftware => KnowledgeBaseNodeWorkspaceTabKind.DocsAndSoftware,
                _ => KnowledgeBaseNodeWorkspaceTabKind.Info
            };

        private static string GetDocumentKindSearchText(KbDocumentKind kind) =>
            kind switch
            {
                KbDocumentKind.SchemeLink => "схема",
                KbDocumentKind.Manual => "инструкция",
                KbDocumentKind.Instruction => "инструкция",
                _ => "документ"
            };

        private static string FormatDate(DateTime? value) =>
            value.HasValue
                ? value.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
                : string.Empty;

        private sealed class SearchData
        {
            public Dictionary<string, List<KbCompositionEntry>> CompositionEntriesByParentId { get; init; } =
                new(StringComparer.Ordinal);

            public Dictionary<string, List<KbDocumentLink>> DocumentLinksByOwnerId { get; init; } =
                new(StringComparer.Ordinal);

            public Dictionary<string, List<KbSoftwareRecord>> SoftwareRecordsByOwnerId { get; init; } =
                new(StringComparer.Ordinal);

            public static SearchData Create(
                IReadOnlyList<KbCompositionEntry>? compositionEntries,
                IReadOnlyList<KbDocumentLink>? documentLinks,
                IReadOnlyList<KbSoftwareRecord>? softwareRecords) =>
                new()
                {
                    CompositionEntriesByParentId = GroupCompositionEntries(compositionEntries),
                    DocumentLinksByOwnerId = GroupDocumentLinks(documentLinks),
                    SoftwareRecordsByOwnerId = GroupSoftwareRecords(softwareRecords)
                };

            private static Dictionary<string, List<KbCompositionEntry>> GroupCompositionEntries(
                IReadOnlyList<KbCompositionEntry>? entries)
            {
                if (entries == null || entries.Count == 0)
                    return new Dictionary<string, List<KbCompositionEntry>>(StringComparer.Ordinal);

                return entries
                    .Where(static entry => !string.IsNullOrWhiteSpace(entry.ParentNodeId))
                    .OrderBy(static entry => entry.SlotNumber.HasValue ? 0 : 1)
                    .ThenBy(static entry => entry.SlotNumber ?? int.MaxValue)
                    .ThenBy(static entry => entry.PositionOrder)
                    .ThenBy(static entry => entry.EntryId, StringComparer.Ordinal)
                    .GroupBy(static entry => entry.ParentNodeId.Trim(), StringComparer.Ordinal)
                    .ToDictionary(static group => group.Key, static group => group.ToList(), StringComparer.Ordinal);
            }

            private static Dictionary<string, List<KbDocumentLink>> GroupDocumentLinks(
                IReadOnlyList<KbDocumentLink>? links)
            {
                if (links == null || links.Count == 0)
                    return new Dictionary<string, List<KbDocumentLink>>(StringComparer.Ordinal);

                return links
                    .Where(static link => !string.IsNullOrWhiteSpace(link.OwnerNodeId))
                    .OrderBy(static link => link.Title, KnowledgeBaseNaturalStringComparer.Instance)
                    .ThenBy(static link => link.Path, KnowledgeBaseNaturalStringComparer.Instance)
                    .ThenBy(static link => link.DocumentId, StringComparer.Ordinal)
                    .GroupBy(static link => link.OwnerNodeId.Trim(), StringComparer.Ordinal)
                    .ToDictionary(static group => group.Key, static group => group.ToList(), StringComparer.Ordinal);
            }

            private static Dictionary<string, List<KbSoftwareRecord>> GroupSoftwareRecords(
                IReadOnlyList<KbSoftwareRecord>? records)
            {
                if (records == null || records.Count == 0)
                    return new Dictionary<string, List<KbSoftwareRecord>>(StringComparer.Ordinal);

                return records
                    .Where(static record => !string.IsNullOrWhiteSpace(record.OwnerNodeId))
                    .OrderBy(static record => record.Title, KnowledgeBaseNaturalStringComparer.Instance)
                    .ThenBy(static record => record.Path, KnowledgeBaseNaturalStringComparer.Instance)
                    .ThenBy(static record => record.SoftwareId, StringComparer.Ordinal)
                    .GroupBy(static record => record.OwnerNodeId.Trim(), StringComparer.Ordinal)
                    .ToDictionary(static group => group.Key, static group => group.ToList(), StringComparer.Ordinal);
            }
        }
    }
}
