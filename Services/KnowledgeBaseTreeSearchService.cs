using System.Globalization;
using System.Runtime.CompilerServices;
using AsutpKnowledgeBase.Models;

namespace AsutpKnowledgeBase.Services
{
    public enum KnowledgeBaseSearchScope
    {
        All = 0,
        Tree = 1,
        Card = 2,
        Composition = 3,
        AdditionalEquipment = 4,
        DocsAndSoftware = 5,
        Network = 6,
        Maintenance = 7
    }

    public enum KnowledgeBaseSearchDomain
    {
        Tree = 0,
        Card = 1,
        Composition = 2,
        AdditionalEquipment = 3,
        DocsAndSoftware = 4,
        Network = 5,
        Maintenance = 6
    }

    public enum KnowledgeBaseSearchTargetKind
    {
        Node = 0,
        Card = 1,
        CompositionEntry = 2,
        AdditionalEquipmentEntry = 3,
        Document = 4,
        Software = 5,
        NetworkElement = 6,
        NetworkLink = 7,
        MaintenanceProfile = 8,
        MaintenanceYearScheduleEntry = 9
    }

    public sealed class KnowledgeBaseSearchTarget
    {
        public KnowledgeBaseSearchTargetKind Kind { get; init; } = KnowledgeBaseSearchTargetKind.Node;

        public string OwnerNodeId { get; init; } = string.Empty;

        public string EntityId { get; init; } = string.Empty;

        public string FieldKey { get; init; } = string.Empty;

        public string RowKey { get; init; } = string.Empty;
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

        public KnowledgeBaseSearchTarget Target { get; init; } = new();
    }

    public class KnowledgeBaseTreeSearchService
    {
        private static readonly KnowledgeBaseNodeWorkspaceResolverService WorkspaceResolver = new();

        private SearchIndexCache? _searchIndexCache;

        public void InvalidateIndexCache() => _searchIndexCache = null;

        public IReadOnlyList<KnowledgeBaseTreeSearchMatch> FindMatches(
            IReadOnlyList<KbNode> roots,
            KbConfig config,
            string searchText,
            KnowledgeBaseSearchScope scope = KnowledgeBaseSearchScope.All,
            IReadOnlyList<KbCompositionEntry>? compositionEntries = null,
            IReadOnlyList<KbDocumentLink>? documentLinks = null,
            IReadOnlyList<KbSoftwareRecord>? softwareRecords = null,
            IReadOnlyList<KbMaintenanceScheduleProfile>? maintenanceScheduleProfiles = null)
        {
            _ = config;

            string normalizedSearch = searchText.Trim();
            if (string.IsNullOrWhiteSpace(normalizedSearch))
                return Array.Empty<KnowledgeBaseTreeSearchMatch>();

            IReadOnlyList<SearchIndexItem> index = GetOrBuildSearchIndex(
                roots,
                compositionEntries,
                documentLinks,
                softwareRecords,
                maintenanceScheduleProfiles);
            var matches = new List<KnowledgeBaseTreeSearchMatch>();

            foreach (SearchIndexItem item in index)
            {
                if (!IncludesScope(scope, item.Domain))
                    continue;

                if (!Contains(item.Value, normalizedSearch))
                    continue;

                matches.Add(BuildMatch(item, normalizedSearch));
            }

            return matches;
        }

        private IReadOnlyList<SearchIndexItem> GetOrBuildSearchIndex(
            IReadOnlyList<KbNode> roots,
            IReadOnlyList<KbCompositionEntry>? compositionEntries,
            IReadOnlyList<KbDocumentLink>? documentLinks,
            IReadOnlyList<KbSoftwareRecord>? softwareRecords,
            IReadOnlyList<KbMaintenanceScheduleProfile>? maintenanceScheduleProfiles)
        {
            SearchIndexCacheKey cacheKey = SearchIndexCacheKey.Create(
                roots,
                compositionEntries,
                documentLinks,
                softwareRecords,
                maintenanceScheduleProfiles);
            if (_searchIndexCache is { } cache && cache.Key.Equals(cacheKey))
                return cache.Items;

            var items = new List<SearchIndexItem>();
            var pathSegments = new List<string>();
            var searchData = SearchData.Create(
                compositionEntries,
                documentLinks,
                softwareRecords,
                maintenanceScheduleProfiles);

            foreach (KbNode root in EnumerateDisplaySortedNodes(roots))
                CollectIndexItems(root, visibleLevel: 1, searchData, pathSegments, items);

            _searchIndexCache = new SearchIndexCache(cacheKey, items);
            return items;
        }

        private static void CollectIndexItems(
            KbNode node,
            int visibleLevel,
            SearchData searchData,
            IList<string> pathSegments,
            ICollection<SearchIndexItem> items)
        {
            pathSegments.Add(node.Name);
            string nodePath = string.Join(" / ", pathSegments);
            KnowledgeBaseNodeWorkspaceTabKind defaultTabKind = GetDefaultTabKind(node, visibleLevel);

            AddIndexItem(
                items,
                node,
                KnowledgeBaseSearchDomain.Tree,
                defaultTabKind,
                "имя узла",
                node.Name,
                nodePath,
                targetKind: KnowledgeBaseSearchTargetKind.Node,
                targetEntityId: node.NodeId,
                targetFieldKey: "tree.name");

            AddCardIndexItems(node, visibleLevel, defaultTabKind, nodePath, items);
            AddCompositionIndexItems(node, visibleLevel, nodePath, searchData, items);
            AddDocsAndSoftwareIndexItems(node, visibleLevel, nodePath, searchData, items);
            AddNetworkIndexItems(node, visibleLevel, nodePath, items);
            AddMaintenanceIndexItems(node, visibleLevel, nodePath, searchData, items);

            foreach (KbNode child in EnumerateDisplaySortedNodes(node.Children))
                CollectIndexItems(child, visibleLevel + 1, searchData, pathSegments, items);

            pathSegments.RemoveAt(pathSegments.Count - 1);
        }

        private static void AddCardIndexItems(
            KbNode node,
            int visibleLevel,
            KnowledgeBaseNodeWorkspaceTabKind preferredTabKind,
            string nodePath,
            ICollection<SearchIndexItem> items)
        {
            if (visibleLevel == 3)
                return;

            var details = node.Details ?? new KbNodeDetails();

            AddIndexItem(
                items,
                node,
                KnowledgeBaseSearchDomain.Card,
                preferredTabKind,
                "описание",
                details.Description,
                nodePath,
                targetKind: KnowledgeBaseSearchTargetKind.Card,
                targetEntityId: node.NodeId,
                targetFieldKey: "card.description");

            if (KnowledgeBaseNodeMetadataService.SupportsInventoryNumber(visibleLevel))
            {
                AddIndexItem(
                    items,
                    node,
                    KnowledgeBaseSearchDomain.Card,
                    preferredTabKind,
                    "инвентарный номер",
                    details.InventoryNumber,
                    nodePath,
                    targetKind: KnowledgeBaseSearchTargetKind.Card,
                    targetEntityId: node.NodeId,
                    targetFieldKey: "card.inventoryNumber");
            }
        }

        private static void AddCompositionIndexItems(
            KbNode node,
            int visibleLevel,
            string nodePath,
            SearchData searchData,
            ICollection<SearchIndexItem> items)
        {
            if (!KnowledgeBaseCompositionStateService.SupportsComposition(node.NodeType, visibleLevel))
                return;

            if (!searchData.CompositionEntriesByParentId.TryGetValue(node.NodeId, out List<KbCompositionEntry>? entries))
                return;

            int additionalEquipmentIndex = 0;
            foreach (KbCompositionEntry entry in entries)
            {
                if (entry.SlotNumber.HasValue)
                {
                    AddSlotCompositionIndexItems(node, entry, nodePath, items);
                    continue;
                }

                additionalEquipmentIndex++;
                AddAdditionalEquipmentIndexItems(node, entry, additionalEquipmentIndex, nodePath, items);
            }
        }

        private static void AddSlotCompositionIndexItems(
            KbNode node,
            KbCompositionEntry entry,
            string nodePath,
            ICollection<SearchIndexItem> items)
        {
            const KnowledgeBaseSearchDomain domain = KnowledgeBaseSearchDomain.Composition;
            const KnowledgeBaseNodeWorkspaceTabKind preferredTabKind = KnowledgeBaseNodeWorkspaceTabKind.Composition;
            int slotNumber = entry.SlotNumber.GetValueOrDefault();

            AddIndexItem(
                items,
                node,
                domain,
                preferredTabKind,
                "rack",
                KnowledgeBaseCompositionRackSlotRulesService.FormatRackText(entry.RackNumber),
                nodePath,
                targetKind: KnowledgeBaseSearchTargetKind.CompositionEntry,
                targetEntityId: entry.EntryId,
                targetFieldKey: "composition.rack");
            AddIndexItem(
                items,
                node,
                domain,
                preferredTabKind,
                "слот",
                slotNumber.ToString(CultureInfo.InvariantCulture),
                nodePath,
                targetKind: KnowledgeBaseSearchTargetKind.CompositionEntry,
                targetEntityId: entry.EntryId,
                targetFieldKey: "composition.slot");
            AddIndexItem(
                items,
                node,
                domain,
                preferredTabKind,
                "роль",
                KnowledgeBaseCompositionRackSlotRulesService.GetSlotRoleText(entry.RackNumber, slotNumber),
                nodePath,
                targetKind: KnowledgeBaseSearchTargetKind.CompositionEntry,
                targetEntityId: entry.EntryId,
                targetFieldKey: "composition.role");
            AddIndexItem(
                items,
                node,
                domain,
                preferredTabKind,
                "тип",
                entry.ComponentType,
                nodePath,
                targetKind: KnowledgeBaseSearchTargetKind.CompositionEntry,
                targetEntityId: entry.EntryId,
                targetFieldKey: "composition.componentType");
            AddIndexItem(
                items,
                node,
                domain,
                preferredTabKind,
                "заказной номер",
                entry.OrderNumber,
                nodePath,
                targetKind: KnowledgeBaseSearchTargetKind.CompositionEntry,
                targetEntityId: entry.EntryId,
                targetFieldKey: "composition.orderNumber");
        }

        private static void AddAdditionalEquipmentIndexItems(
            KbNode node,
            KbCompositionEntry entry,
            int rowNumber,
            string nodePath,
            ICollection<SearchIndexItem> items)
        {
            const KnowledgeBaseSearchDomain domain = KnowledgeBaseSearchDomain.AdditionalEquipment;
            const KnowledgeBaseNodeWorkspaceTabKind preferredTabKind =
                KnowledgeBaseNodeWorkspaceTabKind.AdditionalEquipment;

            AddIndexItem(
                items,
                node,
                domain,
                preferredTabKind,
                "№",
                rowNumber.ToString(CultureInfo.InvariantCulture),
                nodePath,
                targetKind: KnowledgeBaseSearchTargetKind.AdditionalEquipmentEntry,
                targetEntityId: entry.EntryId,
                targetFieldKey: "additionalEquipment.rowNumber");
            AddIndexItem(
                items,
                node,
                domain,
                preferredTabKind,
                "тип",
                entry.ComponentType,
                nodePath,
                targetKind: KnowledgeBaseSearchTargetKind.AdditionalEquipmentEntry,
                targetEntityId: entry.EntryId,
                targetFieldKey: "additionalEquipment.componentType");
            AddIndexItem(
                items,
                node,
                domain,
                preferredTabKind,
                "заказной номер",
                entry.OrderNumber,
                nodePath,
                targetKind: KnowledgeBaseSearchTargetKind.AdditionalEquipmentEntry,
                targetEntityId: entry.EntryId,
                targetFieldKey: "additionalEquipment.orderNumber");
            AddIndexItem(
                items,
                node,
                domain,
                preferredTabKind,
                "примечание",
                entry.Notes,
                nodePath,
                targetKind: KnowledgeBaseSearchTargetKind.AdditionalEquipmentEntry,
                targetEntityId: entry.EntryId,
                targetFieldKey: "additionalEquipment.notes");
        }

        private static void AddDocsAndSoftwareIndexItems(
            KbNode node,
            int visibleLevel,
            string nodePath,
            SearchData searchData,
            ICollection<SearchIndexItem> items)
        {
            if (!KnowledgeBaseDocsAndSoftwareStateService.SupportsRecords(node.NodeType, visibleLevel))
                return;

            if (searchData.DocumentLinksByOwnerId.TryGetValue(node.NodeId, out List<KbDocumentLink>? documentLinks))
            {
                foreach (KbDocumentLink link in documentLinks)
                {
                    AddIndexItem(
                        items,
                        node,
                        KnowledgeBaseSearchDomain.DocsAndSoftware,
                        KnowledgeBaseNodeWorkspaceTabKind.DocsAndSoftware,
                        "название документа",
                        link.Title,
                        nodePath,
                        targetKind: KnowledgeBaseSearchTargetKind.Document,
                        targetEntityId: link.DocumentId,
                        targetFieldKey: "document.title");
                    AddIndexItem(
                        items,
                        node,
                        KnowledgeBaseSearchDomain.DocsAndSoftware,
                        KnowledgeBaseNodeWorkspaceTabKind.DocsAndSoftware,
                        "путь к документу",
                        link.Path,
                        nodePath,
                        targetKind: KnowledgeBaseSearchTargetKind.Document,
                        targetEntityId: link.DocumentId,
                        targetFieldKey: "document.path");
                    AddIndexItem(
                        items,
                        node,
                        KnowledgeBaseSearchDomain.DocsAndSoftware,
                        KnowledgeBaseNodeWorkspaceTabKind.DocsAndSoftware,
                        "дата обновления документа",
                        FormatDate(link.UpdatedAt),
                        nodePath,
                        targetKind: KnowledgeBaseSearchTargetKind.Document,
                        targetEntityId: link.DocumentId,
                        targetFieldKey: "document.updatedAt");
                }
            }

            if (!searchData.SoftwareRecordsByOwnerId.TryGetValue(node.NodeId, out List<KbSoftwareRecord>? softwareRecords))
                return;

            foreach (KbSoftwareRecord record in softwareRecords)
            {
                AddIndexItem(
                    items,
                    node,
                    KnowledgeBaseSearchDomain.DocsAndSoftware,
                    KnowledgeBaseNodeWorkspaceTabKind.DocsAndSoftware,
                    "название ПО",
                    record.Title,
                    nodePath,
                    targetKind: KnowledgeBaseSearchTargetKind.Software,
                    targetEntityId: record.SoftwareId,
                    targetFieldKey: "software.title");
                AddIndexItem(
                    items,
                    node,
                    KnowledgeBaseSearchDomain.DocsAndSoftware,
                    KnowledgeBaseNodeWorkspaceTabKind.DocsAndSoftware,
                    "путь к ПО",
                    record.Path,
                    nodePath,
                    targetKind: KnowledgeBaseSearchTargetKind.Software,
                    targetEntityId: record.SoftwareId,
                    targetFieldKey: "software.path");
                AddIndexItem(
                    items,
                    node,
                    KnowledgeBaseSearchDomain.DocsAndSoftware,
                    KnowledgeBaseNodeWorkspaceTabKind.DocsAndSoftware,
                    "дата добавления ПО",
                    FormatDate(record.AddedAt),
                    nodePath,
                    targetKind: KnowledgeBaseSearchTargetKind.Software,
                    targetEntityId: record.SoftwareId,
                    targetFieldKey: "software.addedAt");
            }
        }

        private static void AddNetworkIndexItems(
            KbNode node,
            int visibleLevel,
            string nodePath,
            ICollection<SearchIndexItem> items)
        {
            if (!KnowledgeBaseNodeMetadataService.SupportsNetworkTopology(visibleLevel))
                return;

            KbNetworkTopology? topology = node.Details?.NetworkTopology;
            if (topology == null)
                return;

            foreach (KbNetworkElement element in topology.Elements)
            {
                AddIndexItem(
                    items,
                    node,
                    KnowledgeBaseSearchDomain.Network,
                    KnowledgeBaseNodeWorkspaceTabKind.Network,
                    element.Kind == KbNetworkElementKind.ExternalConnection ? "текст внешней связи" : "имя объекта сети",
                    element.Name,
                    nodePath,
                    targetKind: KnowledgeBaseSearchTargetKind.NetworkElement,
                    targetEntityId: element.ElementId,
                    targetFieldKey: "networkElement.name");
                AddIndexItem(
                    items,
                    node,
                    KnowledgeBaseSearchDomain.Network,
                    KnowledgeBaseNodeWorkspaceTabKind.Network,
                    "IP-адрес",
                    element.IpAddress,
                    nodePath,
                    targetKind: KnowledgeBaseSearchTargetKind.NetworkElement,
                    targetEntityId: element.ElementId,
                    targetFieldKey: "networkElement.ipAddress");

                foreach (string additionalIpAddress in element.AdditionalIpAddresses)
                {
                    AddIndexItem(
                        items,
                        node,
                        KnowledgeBaseSearchDomain.Network,
                        KnowledgeBaseNodeWorkspaceTabKind.Network,
                        "доп. IP",
                        additionalIpAddress,
                        nodePath,
                        targetKind: KnowledgeBaseSearchTargetKind.NetworkElement,
                        targetEntityId: element.ElementId,
                        targetFieldKey: "networkElement.additionalIpAddress");
                }
            }
        }

        private static void AddMaintenanceIndexItems(
            KbNode node,
            int visibleLevel,
            string nodePath,
            SearchData searchData,
            ICollection<SearchIndexItem> items)
        {
            if (!KnowledgeBaseMaintenanceScheduleStateService.SupportsProfile(node.NodeType, visibleLevel))
                return;

            if (!searchData.MaintenanceProfilesByOwnerId.TryGetValue(
                    node.NodeId,
                    out List<KbMaintenanceScheduleProfile>? profiles))
            {
                return;
            }

            foreach (KbMaintenanceScheduleProfile profile in profiles)
            {
                AddIndexItem(
                    items,
                    node,
                    KnowledgeBaseSearchDomain.Maintenance,
                    KnowledgeBaseNodeWorkspaceTabKind.Maintenance,
                    "участие в графике ТО",
                    profile.IsIncludedInSchedule ? "Да" : "Нет",
                    nodePath,
                    targetKind: KnowledgeBaseSearchTargetKind.MaintenanceProfile,
                    targetEntityId: profile.MaintenanceProfileId,
                    targetFieldKey: "maintenance.isIncludedInSchedule");
                AddIndexItem(
                    items,
                    node,
                    KnowledgeBaseSearchDomain.Maintenance,
                    KnowledgeBaseNodeWorkspaceTabKind.Maintenance,
                    "норма часов ТО1",
                    FormatHours(profile.To1Hours),
                    nodePath,
                    targetKind: KnowledgeBaseSearchTargetKind.MaintenanceProfile,
                    targetEntityId: profile.MaintenanceProfileId,
                    targetFieldKey: "maintenance.to1Hours");
                AddIndexItem(
                    items,
                    node,
                    KnowledgeBaseSearchDomain.Maintenance,
                    KnowledgeBaseNodeWorkspaceTabKind.Maintenance,
                    "норма часов ТО2",
                    FormatHours(profile.To2Hours),
                    nodePath,
                    targetKind: KnowledgeBaseSearchTargetKind.MaintenanceProfile,
                    targetEntityId: profile.MaintenanceProfileId,
                    targetFieldKey: "maintenance.to2Hours");
                AddIndexItem(
                    items,
                    node,
                    KnowledgeBaseSearchDomain.Maintenance,
                    KnowledgeBaseNodeWorkspaceTabKind.Maintenance,
                    "норма часов ТО3",
                    FormatHours(profile.To3Hours),
                    nodePath,
                    targetKind: KnowledgeBaseSearchTargetKind.MaintenanceProfile,
                    targetEntityId: profile.MaintenanceProfileId,
                    targetFieldKey: "maintenance.to3Hours");
                AddIndexItem(
                    items,
                    node,
                    KnowledgeBaseSearchDomain.Maintenance,
                    KnowledgeBaseNodeWorkspaceTabKind.Maintenance,
                    "годовое размещение",
                    KnowledgeBaseMaintenanceScheduleStateService.FormatYearSchedule(profile.YearScheduleEntries),
                    nodePath,
                    targetKind: KnowledgeBaseSearchTargetKind.MaintenanceProfile,
                    targetEntityId: profile.MaintenanceProfileId,
                    targetFieldKey: "maintenance.yearSchedule.summary");
            }
        }

        private static void AddIndexItem(
            ICollection<SearchIndexItem> items,
            KbNode node,
            KnowledgeBaseSearchDomain domain,
            KnowledgeBaseNodeWorkspaceTabKind preferredTabKind,
            string fieldLabel,
            string? value,
            string nodePath,
            KnowledgeBaseSearchTargetKind? targetKind = null,
            string? targetEntityId = null,
            string? targetFieldKey = null,
            string? targetRowKey = null)
        {
            string normalizedValue = value?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(normalizedValue))
                return;

            var target = new KnowledgeBaseSearchTarget
            {
                Kind = targetKind ?? GetDefaultTargetKind(domain),
                OwnerNodeId = node.NodeId,
                EntityId = string.IsNullOrWhiteSpace(targetEntityId) ? node.NodeId : targetEntityId.Trim(),
                FieldKey = string.IsNullOrWhiteSpace(targetFieldKey) ? fieldLabel : targetFieldKey.Trim(),
                RowKey = targetRowKey?.Trim() ?? string.Empty
            };

            items.Add(new SearchIndexItem(
                node,
                domain,
                preferredTabKind,
                fieldLabel,
                normalizedValue,
                nodePath,
                target));
        }

        private static bool IncludesScope(KnowledgeBaseSearchScope scope, KnowledgeBaseSearchDomain domain) =>
            scope == KnowledgeBaseSearchScope.All ||
            scope switch
            {
                KnowledgeBaseSearchScope.Tree => domain == KnowledgeBaseSearchDomain.Tree,
                KnowledgeBaseSearchScope.Card => domain == KnowledgeBaseSearchDomain.Card,
                KnowledgeBaseSearchScope.Composition => domain == KnowledgeBaseSearchDomain.Composition,
                KnowledgeBaseSearchScope.AdditionalEquipment => domain == KnowledgeBaseSearchDomain.AdditionalEquipment,
                KnowledgeBaseSearchScope.DocsAndSoftware => domain == KnowledgeBaseSearchDomain.DocsAndSoftware,
                KnowledgeBaseSearchScope.Network => domain == KnowledgeBaseSearchDomain.Network,
                KnowledgeBaseSearchScope.Maintenance => domain == KnowledgeBaseSearchDomain.Maintenance,
                _ => false
            };

        private static bool Contains(string value, string searchText) =>
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

        private static KnowledgeBaseNodeWorkspaceTabKind GetDefaultTabKind(KbNode node, int visibleLevel)
        {
            KnowledgeBaseNodeWorkspaceState workspace = WorkspaceResolver.Resolve(node.NodeType, visibleLevel);
            KnowledgeBaseNodeWorkspaceTabState? infoTab = workspace.Tabs.FirstOrDefault(
                static tab => tab.Kind == KnowledgeBaseNodeWorkspaceTabKind.Info);

            return infoTab?.Kind ??
                workspace.Tabs.FirstOrDefault()?.Kind ??
                KnowledgeBaseNodeWorkspaceTabKind.Info;
        }

        private static KnowledgeBaseTreeSearchMatch BuildMatch(SearchIndexItem item, string searchText) =>
            new()
            {
                Node = item.Node,
                Domain = item.Domain,
                PreferredTabKind = item.PreferredTabKind,
                SearchText = searchText,
                MatchFieldLabel = item.FieldLabel,
                MatchValue = item.Value,
                NodePath = item.NodePath,
                Target = item.Target
            };

        private static KnowledgeBaseSearchTargetKind GetDefaultTargetKind(KnowledgeBaseSearchDomain domain) =>
            domain switch
            {
                KnowledgeBaseSearchDomain.Card => KnowledgeBaseSearchTargetKind.Card,
                KnowledgeBaseSearchDomain.Composition => KnowledgeBaseSearchTargetKind.CompositionEntry,
                KnowledgeBaseSearchDomain.AdditionalEquipment => KnowledgeBaseSearchTargetKind.AdditionalEquipmentEntry,
                KnowledgeBaseSearchDomain.DocsAndSoftware => KnowledgeBaseSearchTargetKind.Document,
                KnowledgeBaseSearchDomain.Network => KnowledgeBaseSearchTargetKind.NetworkElement,
                KnowledgeBaseSearchDomain.Maintenance => KnowledgeBaseSearchTargetKind.MaintenanceProfile,
                _ => KnowledgeBaseSearchTargetKind.Node
            };

        private static string FormatHours(int hours) =>
            $"{Math.Max(0, hours)} ч";

        private static string FormatDate(DateTime? value) =>
            value.HasValue
                ? value.Value.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture)
                : string.Empty;

        private sealed record SearchIndexItem(
            KbNode Node,
            KnowledgeBaseSearchDomain Domain,
            KnowledgeBaseNodeWorkspaceTabKind PreferredTabKind,
            string FieldLabel,
            string Value,
            string NodePath,
            KnowledgeBaseSearchTarget Target);

        private sealed record SearchIndexCache(SearchIndexCacheKey Key, IReadOnlyList<SearchIndexItem> Items);

        private readonly record struct SearchIndexCacheKey(
            int RootsReference,
            int RootsCount,
            int CompositionReference,
            int CompositionCount,
            int DocumentReference,
            int DocumentCount,
            int SoftwareReference,
            int SoftwareCount,
            int MaintenanceReference,
            int MaintenanceCount)
        {
            public static SearchIndexCacheKey Create(
                IReadOnlyList<KbNode> roots,
                IReadOnlyList<KbCompositionEntry>? compositionEntries,
                IReadOnlyList<KbDocumentLink>? documentLinks,
                IReadOnlyList<KbSoftwareRecord>? softwareRecords,
                IReadOnlyList<KbMaintenanceScheduleProfile>? maintenanceScheduleProfiles) =>
                new(
                    GetReferenceHash(roots),
                    roots.Count,
                    GetReferenceHash(compositionEntries),
                    GetCount(compositionEntries),
                    GetReferenceHash(documentLinks),
                    GetCount(documentLinks),
                    GetReferenceHash(softwareRecords),
                    GetCount(softwareRecords),
                    GetReferenceHash(maintenanceScheduleProfiles),
                    GetCount(maintenanceScheduleProfiles));

            private static int GetReferenceHash<T>(IReadOnlyList<T>? items) =>
                items == null ? 0 : RuntimeHelpers.GetHashCode(items);

            private static int GetCount<T>(IReadOnlyList<T>? items) =>
                items?.Count ?? 0;
        }

        private sealed class SearchData
        {
            public Dictionary<string, List<KbCompositionEntry>> CompositionEntriesByParentId { get; init; } =
                new(StringComparer.Ordinal);

            public Dictionary<string, List<KbDocumentLink>> DocumentLinksByOwnerId { get; init; } =
                new(StringComparer.Ordinal);

            public Dictionary<string, List<KbSoftwareRecord>> SoftwareRecordsByOwnerId { get; init; } =
                new(StringComparer.Ordinal);

            public Dictionary<string, List<KbMaintenanceScheduleProfile>> MaintenanceProfilesByOwnerId { get; init; } =
                new(StringComparer.Ordinal);

            public static SearchData Create(
                IReadOnlyList<KbCompositionEntry>? compositionEntries,
                IReadOnlyList<KbDocumentLink>? documentLinks,
                IReadOnlyList<KbSoftwareRecord>? softwareRecords,
                IReadOnlyList<KbMaintenanceScheduleProfile>? maintenanceScheduleProfiles) =>
                new()
                {
                    CompositionEntriesByParentId = GroupCompositionEntries(compositionEntries),
                    DocumentLinksByOwnerId = GroupDocumentLinks(documentLinks),
                    SoftwareRecordsByOwnerId = GroupSoftwareRecords(softwareRecords),
                    MaintenanceProfilesByOwnerId = GroupMaintenanceProfiles(maintenanceScheduleProfiles)
                };

            private static Dictionary<string, List<KbCompositionEntry>> GroupCompositionEntries(
                IReadOnlyList<KbCompositionEntry>? entries)
            {
                if (entries == null || entries.Count == 0)
                    return new Dictionary<string, List<KbCompositionEntry>>(StringComparer.Ordinal);

                return entries
                    .Where(static entry => !string.IsNullOrWhiteSpace(entry.ParentNodeId))
                    .OrderBy(static entry => entry.SlotNumber.HasValue ? 0 : 1)
                    .ThenBy(static entry => entry.RackNumber)
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

            private static Dictionary<string, List<KbMaintenanceScheduleProfile>> GroupMaintenanceProfiles(
                IReadOnlyList<KbMaintenanceScheduleProfile>? profiles)
            {
                if (profiles == null || profiles.Count == 0)
                    return new Dictionary<string, List<KbMaintenanceScheduleProfile>>(StringComparer.Ordinal);

                return profiles
                    .Where(static profile => !string.IsNullOrWhiteSpace(profile.OwnerNodeId))
                    .OrderBy(static profile => profile.MaintenanceProfileId, StringComparer.Ordinal)
                    .GroupBy(static profile => profile.OwnerNodeId.Trim(), StringComparer.Ordinal)
                    .ToDictionary(static group => group.Key, static group => group.ToList(), StringComparer.Ordinal);
            }
        }
    }
}
