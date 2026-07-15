using System.Globalization;
using System.Text.Json;
using AsutpKnowledgeBase.Models;

namespace AsutpKnowledgeBase.Services
{
    public static class KnowledgeBaseDataService
    {
        private static readonly JsonSerializerOptions SnapshotOptions = new() { WriteIndented = false };

        public static StringComparer WorkshopNameComparer { get; } = StringComparer.OrdinalIgnoreCase;

        private readonly record struct NodeOwnershipState(int VisibleLevel, string Level2NodeId);

        public static KbConfig CreateDefaultConfig() =>
            new()
            {
                MaxLevels = 10,
                LevelNames = Enumerable
                    .Range(1, 10)
                    .Select(static level => $"Уровень {level}")
                    .ToList(),
                ProductionCalendarYears = CreateDefaultProductionCalendarYears(),
                ActDocumentsDirectoryPath = KbConfig.DefaultActDocumentsDirectoryPath
            };

        public static SavedData CreateDefaultData() =>
            new()
            {
                SchemaVersion = SavedData.CurrentSchemaVersion,
                Config = CreateDefaultConfig(),
                Workshops = new Dictionary<string, List<KbNode>>(WorkshopNameComparer)
                {
                    ["Новый цех"] = new List<KbNode>()
                },
                CompositionRacks = new List<KbCompositionRack>(),
                CompositionEntries = new List<KbCompositionEntry>(),
                DocumentLinks = new List<KbDocumentLink>(),
                SoftwareRecords = new List<KbSoftwareRecord>(),
                MaintenanceScheduleProfiles = new List<KbMaintenanceScheduleProfile>(),
                EquipmentCatalogItems = new List<KbEquipmentCatalogItem>(),
                ObjectTemplates = new List<KbObjectTemplate>(),
                Acts = new List<KbAct>(),
                ActExecutors = new List<KbActExecutor>(),
                ActDocuments = new List<KbActDocument>(),
                ActNumberSequences = new List<KbActNumberSequence>(),
                LastWorkshop = "Новый цех"
            };

        public static SavedData NormalizeSavedData(SavedData? data)
        {
            var source = data ?? CreateDefaultData();
            var normalizedConfig = NormalizeConfig(source.Config);
            var normalizedWorkshops = NormalizeWorkshops(source.Workshops);
            var nodeOwnershipIndex = BuildNodeOwnershipIndex(normalizedWorkshops);
            var normalizedCompositionRacks = NormalizeCompositionRacks(source.CompositionRacks);
            var normalizedCompositionEntries = NormalizeCompositionEntries(source.CompositionEntries);
            var normalizedDocumentLinks = NormalizeDocumentLinks(source.DocumentLinks, nodeOwnershipIndex);
            var normalizedSoftwareRecords = NormalizeSoftwareRecords(source.SoftwareRecords, nodeOwnershipIndex);
            var normalizedMaintenanceScheduleProfiles = NormalizeMaintenanceScheduleProfiles(source.MaintenanceScheduleProfiles);
            var normalizedEquipmentCatalogItems = NormalizeEquipmentCatalogItems(source.EquipmentCatalogItems);
            var normalizedObjectTemplates = NormalizeObjectTemplates(source.ObjectTemplates);
            var normalizedActs = NormalizeActs(source.Acts);
            var knownActIds = normalizedActs.Select(static act => act.ActId).ToList();
            var normalizedActExecutors = NormalizeActExecutors(source.ActExecutors, knownActIds);
            var normalizedActDocuments = NormalizeActDocuments(source.ActDocuments, knownActIds);
            var normalizedActNumberSequences = NormalizeActNumberSequences(source.ActNumberSequences);
            var reindexService = new KnowledgeBaseService(normalizedConfig, normalizedWorkshops);

            foreach (var roots in normalizedWorkshops.Values)
            {
                foreach (var root in roots)
                    reindexService.ReindexSubtree(root, 0);
            }

            return new SavedData
            {
                SchemaVersion = SavedData.CurrentSchemaVersion,
                Config = normalizedConfig,
                Workshops = normalizedWorkshops,
                CompositionRacks = normalizedCompositionRacks,
                CompositionEntries = normalizedCompositionEntries,
                DocumentLinks = normalizedDocumentLinks,
                SoftwareRecords = normalizedSoftwareRecords,
                MaintenanceScheduleProfiles = normalizedMaintenanceScheduleProfiles,
                EquipmentCatalogItems = normalizedEquipmentCatalogItems,
                ObjectTemplates = normalizedObjectTemplates,
                Acts = normalizedActs,
                ActExecutors = normalizedActExecutors,
                ActDocuments = normalizedActDocuments,
                ActNumberSequences = normalizedActNumberSequences,
                LastWorkshop = ResolveWorkshop(normalizedWorkshops, source.LastWorkshop)
            };
        }

        public static string NormalizeWorkshopName(string? workshopName) =>
            workshopName?.Trim() ?? string.Empty;

        public static bool WorkshopNamesEqual(string? left, string? right) =>
            WorkshopNameComparer.Equals(
                NormalizeWorkshopName(left),
                NormalizeWorkshopName(right));

        public static string? FindWorkshopName(IEnumerable<string> workshopNames, string? workshopName)
        {
            string normalizedWorkshop = NormalizeWorkshopName(workshopName);
            if (string.IsNullOrWhiteSpace(normalizedWorkshop))
                return null;

            foreach (string existingWorkshop in workshopNames)
            {
                if (WorkshopNameComparer.Equals(existingWorkshop, normalizedWorkshop))
                    return existingWorkshop;
            }

            return null;
        }

        public static string? ValidateSupportedSchemaVersion(int schemaVersion)
        {
            if (schemaVersion < 1)
                return $"Неподдерживаемая версия схемы: {schemaVersion}.";

            if (schemaVersion > SavedData.CurrentSchemaVersion)
            {
                return
                    $"Файл создан более новой версией приложения: SchemaVersion = {schemaVersion}. " +
                    $"Максимально поддерживаемая версия: {SavedData.CurrentSchemaVersion}.";
            }

            return null;
        }

        public static string? ValidateWorkshopNames(Dictionary<string, List<KbNode>>? workshops)
        {
            if (workshops == null)
                return null;

            var seenWorkshopNames = new Dictionary<string, string>(WorkshopNameComparer);
            foreach (var pair in workshops)
            {
                string normalizedWorkshopName = NormalizeWorkshopName(pair.Key);
                if (string.IsNullOrWhiteSpace(normalizedWorkshopName))
                    continue;

                if (seenWorkshopNames.TryGetValue(normalizedWorkshopName, out var existingWorkshop))
                {
                    return
                        $"Обнаружены конфликтующие названия цехов '{NormalizeWorkshopName(existingWorkshop)}' " +
                        $"и '{normalizedWorkshopName}'. Имена цехов сравниваются без учёта регистра и крайних пробелов.";
                }

                seenWorkshopNames[normalizedWorkshopName] = pair.Key;
            }

            return null;
        }

        public static KbConfig NormalizeConfig(KbConfig? config)
        {
            var defaults = CreateDefaultConfig();
            if (config == null)
                return defaults;

            var normalized = new KbConfig
            {
                MaxLevels = config.MaxLevels > 0 ? config.MaxLevels : defaults.MaxLevels,
                ActDocumentsDirectoryPath = string.IsNullOrWhiteSpace(config.ActDocumentsDirectoryPath)
                    ? defaults.ActDocumentsDirectoryPath
                    : config.ActDocumentsDirectoryPath.Trim()
            };

            foreach (var name in config.LevelNames ?? Enumerable.Empty<string>())
            {
                if (!string.IsNullOrWhiteSpace(name))
                    normalized.LevelNames.Add(name.Trim());
            }

            while (normalized.LevelNames.Count < normalized.MaxLevels)
            {
                int index = normalized.LevelNames.Count;
                if (index < defaults.LevelNames.Count)
                    normalized.LevelNames.Add(defaults.LevelNames[index]);
                else
                    normalized.LevelNames.Add($"Уровень {index + 1}");
            }

            if (normalized.LevelNames.Count > normalized.MaxLevels)
                normalized.LevelNames = normalized.LevelNames.Take(normalized.MaxLevels).ToList();

            normalized.ProductionCalendarYears = NormalizeProductionCalendarYears(config.ProductionCalendarYears);

            return normalized;
        }

        public static List<KbProductionCalendarYear> CreateDefaultProductionCalendarYears() =>
        [
            new()
            {
                Year = 2025,
                AdditionalNonWorkingDays =
                [
                    new DateOnly(2025, 5, 2),
                    new DateOnly(2025, 5, 8),
                    new DateOnly(2025, 6, 13),
                    new DateOnly(2025, 11, 3),
                    new DateOnly(2025, 12, 31)
                ],
                AdditionalWorkingDays = []
            },
            new()
            {
                Year = 2026,
                AdditionalNonWorkingDays =
                [
                    new DateOnly(2026, 1, 9),
                    new DateOnly(2026, 3, 9),
                    new DateOnly(2026, 5, 11),
                    new DateOnly(2026, 12, 31)
                ],
                AdditionalWorkingDays = []
            }
        ];

        public static List<KbProductionCalendarYear> NormalizeProductionCalendarYears(
            IEnumerable<KbProductionCalendarYear>? years)
        {
            Dictionary<int, KbProductionCalendarYear> normalizedByYear = CreateDefaultProductionCalendarYears()
                .ToDictionary(static year => year.Year);

            foreach (KbProductionCalendarYear? yearConfiguration in years ?? Enumerable.Empty<KbProductionCalendarYear>())
            {
                if (yearConfiguration == null || yearConfiguration.Year < 1)
                    continue;

                int year = yearConfiguration.Year;
                var normalizedNonWorkingDates = new SortedSet<DateOnly>();
                foreach (DateOnly date in yearConfiguration.AdditionalNonWorkingDays ?? Enumerable.Empty<DateOnly>())
                {
                    if (date.Year != year)
                    {
                        throw new InvalidOperationException(
                            $"Дата {date:dd.MM.yyyy} не относится к {year} году производственного календаря.");
                    }

                    normalizedNonWorkingDates.Add(date);
                }

                var normalizedWorkingDates = new SortedSet<DateOnly>();
                foreach (DateOnly date in yearConfiguration.AdditionalWorkingDays ?? Enumerable.Empty<DateOnly>())
                {
                    if (date.Year != year)
                    {
                        throw new InvalidOperationException(
                            $"Дата {date:dd.MM.yyyy} не относится к {year} году производственного календаря.");
                    }

                    if (normalizedNonWorkingDates.Contains(date))
                    {
                        throw new InvalidOperationException(
                            $"Дата {date:dd.MM.yyyy} указана одновременно как рабочая и нерабочая для {year} года.");
                    }

                    normalizedWorkingDates.Add(date);
                }

                normalizedByYear[year] = new KbProductionCalendarYear
                {
                    Year = year,
                    AdditionalNonWorkingDays = normalizedNonWorkingDates.ToList(),
                    AdditionalWorkingDays = normalizedWorkingDates.ToList()
                };
            }

            return normalizedByYear.Values
                .OrderBy(static year => year.Year)
                .ToList();
        }

        public static Dictionary<string, List<KbNode>> NormalizeWorkshops(Dictionary<string, List<KbNode>>? workshops)
        {
            string? workshopValidationError = ValidateWorkshopNames(workshops);
            if (workshopValidationError != null)
                throw new InvalidOperationException(workshopValidationError);

            var normalized = new Dictionary<string, List<KbNode>>(WorkshopNameComparer);
            var usedNodeIds = new HashSet<string>(StringComparer.Ordinal);

            if (workshops != null)
            {
                foreach (var pair in workshops)
                {
                    if (string.IsNullOrWhiteSpace(pair.Key))
                        continue;

                    string workshopName = NormalizeWorkshopName(pair.Key);
                    var workshopNodes = pair.Value ?? new List<KbNode>();
                    NormalizeNodes(workshopName, workshopNodes, usedNodeIds);
                    normalized.Add(workshopName, workshopNodes);
                }
            }

            if (normalized.Count == 0)
                normalized["Новый цех"] = new List<KbNode>();

            return normalized;
        }

        public static string ResolveWorkshop(Dictionary<string, List<KbNode>> workshops, string? preferredWorkshop)
        {
            string? resolvedWorkshop = FindWorkshopName(workshops.Keys, preferredWorkshop);
            if (!string.IsNullOrWhiteSpace(resolvedWorkshop))
                return resolvedWorkshop;

            return workshops.Keys.FirstOrDefault() ?? string.Empty;
        }

        public static string SerializeSnapshot(
            KbConfig config,
            Dictionary<string, List<KbNode>> workshops,
            string currentWorkshop,
            bool includeCurrentWorkshop) =>
            SerializeSnapshot(
                config,
                workshops,
                compositionRacks: null,
                compositionEntries: null,
                documentLinks: null,
                softwareRecords: null,
                maintenanceScheduleProfiles: null,
                equipmentCatalogItems: null,
                objectTemplates: null,
                currentWorkshop,
                includeCurrentWorkshop);

        public static string SerializeSnapshot(
            KbConfig config,
            Dictionary<string, List<KbNode>> workshops,
            IReadOnlyList<KbCompositionRack>? compositionRacks,
            IReadOnlyList<KbCompositionEntry>? compositionEntries,
            IReadOnlyList<KbDocumentLink>? documentLinks,
            IReadOnlyList<KbSoftwareRecord>? softwareRecords,
            IReadOnlyList<KbMaintenanceScheduleProfile>? maintenanceScheduleProfiles,
            IReadOnlyList<KbEquipmentCatalogItem>? equipmentCatalogItems,
            string currentWorkshop,
            bool includeCurrentWorkshop) =>
            SerializeSnapshot(
                config,
                workshops,
                compositionRacks,
                compositionEntries,
                documentLinks,
                softwareRecords,
                maintenanceScheduleProfiles,
                equipmentCatalogItems,
                objectTemplates: null,
                currentWorkshop,
                includeCurrentWorkshop);

        public static string SerializeSnapshot(
            KbConfig config,
            Dictionary<string, List<KbNode>> workshops,
            IReadOnlyList<KbCompositionEntry>? compositionEntries,
            IReadOnlyList<KbDocumentLink>? documentLinks,
            IReadOnlyList<KbSoftwareRecord>? softwareRecords,
            IReadOnlyList<KbMaintenanceScheduleProfile>? maintenanceScheduleProfiles,
            IReadOnlyList<KbEquipmentCatalogItem>? equipmentCatalogItems,
            string currentWorkshop,
            bool includeCurrentWorkshop) =>
            SerializeSnapshot(
                config,
                workshops,
                compositionRacks: null,
                compositionEntries,
                documentLinks,
                softwareRecords,
                maintenanceScheduleProfiles,
                equipmentCatalogItems,
                objectTemplates: null,
                currentWorkshop,
                includeCurrentWorkshop);

        public static string SerializeSnapshot(
            KbConfig config,
            Dictionary<string, List<KbNode>> workshops,
            IReadOnlyList<KbCompositionRack>? compositionRacks,
            IReadOnlyList<KbCompositionEntry>? compositionEntries,
            IReadOnlyList<KbDocumentLink>? documentLinks,
            IReadOnlyList<KbSoftwareRecord>? softwareRecords,
            IReadOnlyList<KbMaintenanceScheduleProfile>? maintenanceScheduleProfiles,
            IReadOnlyList<KbEquipmentCatalogItem>? equipmentCatalogItems,
            IReadOnlyList<KbObjectTemplate>? objectTemplates,
            string currentWorkshop,
            bool includeCurrentWorkshop) =>
            SerializeSnapshot(
                config,
                workshops,
                compositionRacks,
                compositionEntries,
                documentLinks,
                softwareRecords,
                maintenanceScheduleProfiles,
                equipmentCatalogItems,
                objectTemplates,
                acts: null,
                actExecutors: null,
                actDocuments: null,
                actNumberSequences: null,
                currentWorkshop,
                includeCurrentWorkshop);

        public static string SerializeSnapshot(
            KbConfig config,
            Dictionary<string, List<KbNode>> workshops,
            IReadOnlyList<KbCompositionRack>? compositionRacks,
            IReadOnlyList<KbCompositionEntry>? compositionEntries,
            IReadOnlyList<KbDocumentLink>? documentLinks,
            IReadOnlyList<KbSoftwareRecord>? softwareRecords,
            IReadOnlyList<KbMaintenanceScheduleProfile>? maintenanceScheduleProfiles,
            IReadOnlyList<KbEquipmentCatalogItem>? equipmentCatalogItems,
            IReadOnlyList<KbObjectTemplate>? objectTemplates,
            IReadOnlyList<KbAct>? acts,
            IReadOnlyList<KbActExecutor>? actExecutors,
            IReadOnlyList<KbActDocument>? actDocuments,
            IReadOnlyList<KbActNumberSequence>? actNumberSequences,
            string currentWorkshop,
            bool includeCurrentWorkshop)
        {
            var data = new SavedData
            {
                SchemaVersion = SavedData.CurrentSchemaVersion,
                Config = config,
                Workshops = workshops,
                CompositionRacks = compositionRacks?.ToList() ?? new List<KbCompositionRack>(),
                CompositionEntries = compositionEntries?.ToList() ?? new List<KbCompositionEntry>(),
                DocumentLinks = documentLinks?.ToList() ?? new List<KbDocumentLink>(),
                SoftwareRecords = softwareRecords?.ToList() ?? new List<KbSoftwareRecord>(),
                MaintenanceScheduleProfiles = maintenanceScheduleProfiles?.ToList() ?? new List<KbMaintenanceScheduleProfile>(),
                EquipmentCatalogItems = equipmentCatalogItems?.ToList() ?? new List<KbEquipmentCatalogItem>(),
                ObjectTemplates = objectTemplates?.ToList() ?? new List<KbObjectTemplate>(),
                Acts = acts?.ToList() ?? new List<KbAct>(),
                ActExecutors = actExecutors?.ToList() ?? new List<KbActExecutor>(),
                ActDocuments = actDocuments?.ToList() ?? new List<KbActDocument>(),
                ActNumberSequences = actNumberSequences?.ToList() ?? new List<KbActNumberSequence>(),
                LastWorkshop = includeCurrentWorkshop ? currentWorkshop : string.Empty
            };

            return JsonSerializer.Serialize(data, SnapshotOptions);
        }

        private static void NormalizeNodes(string workshopName, IList<KbNode> nodes, ISet<string> usedNodeIds)
        {
            KnowledgeBaseNodeMetadataService.NormalizePersistentWorkshopNodes(workshopName, nodes, usedNodeIds);

            bool hasHiddenWrapperRoot =
                nodes.Count == 1 &&
                nodes[0].LevelIndex == 0 &&
                nodes[0].NodeType == KbNodeType.WorkshopRoot;

            foreach (var node in nodes)
                NormalizeNodeDetailsRecursive(node, hasHiddenWrapperRoot && ReferenceEquals(node, nodes[0]) ? 0 : 1);
        }

        private static void NormalizeNodeDetailsRecursive(KbNode node, int visibleLevel)
        {
            node.Name ??= string.Empty;
            node.Details = NormalizeDetails(node.Details, node.NodeType, visibleLevel);
            node.Children ??= new List<KbNode>();

            foreach (var child in node.Children)
                NormalizeNodeDetailsRecursive(child, visibleLevel + 1);
        }

        private static KbNodeDetails NormalizeDetails(KbNodeDetails? details, KbNodeType nodeType, int visibleLevel) =>
            new()
            {
                Description = details?.Description ?? string.Empty,
                Location = KnowledgeBaseNodeMetadataService.SupportsLocation(visibleLevel)
                    ? details?.Location ?? string.Empty
                    : string.Empty,
                InventoryNumber = KnowledgeBaseNodeMetadataService.SupportsInventoryNumber(visibleLevel)
                    ? details?.InventoryNumber ?? string.Empty
                    : string.Empty,
                PhotoPath = KnowledgeBaseNodeMetadataService.SupportsPhoto(visibleLevel)
                    ? details?.PhotoPath ?? string.Empty
                    : string.Empty,
                IpAddress = KnowledgeBaseNodeMetadataService.SupportsTechnicalFields(nodeType, visibleLevel)
                    ? details?.IpAddress ?? string.Empty
                    : string.Empty,
                SchemaLink = KnowledgeBaseNodeMetadataService.SupportsTechnicalFields(nodeType, visibleLevel)
                    ? details?.SchemaLink ?? string.Empty
                    : string.Empty,
                NetworkTopology = KnowledgeBaseNodeMetadataService.SupportsNetworkTopology(visibleLevel)
                    ? NormalizeNetworkTopology(details?.NetworkTopology)
                    : new KbNetworkTopology()
            };

        private static Dictionary<string, NodeOwnershipState> BuildNodeOwnershipIndex(
            IReadOnlyDictionary<string, List<KbNode>> workshops)
        {
            var index = new Dictionary<string, NodeOwnershipState>(StringComparer.Ordinal);
            foreach (List<KbNode> roots in workshops.Values)
            {
                bool hasHiddenWrapperRoot =
                    roots.Count == 1 &&
                    roots[0].LevelIndex == 0 &&
                    roots[0].NodeType == KbNodeType.WorkshopRoot;

                foreach (KbNode root in roots)
                {
                    int visibleLevel = hasHiddenWrapperRoot && ReferenceEquals(root, roots[0]) ? 0 : 1;
                    CollectNodeOwnership(root, visibleLevel, level2NodeId: string.Empty, index);
                }
            }

            return index;
        }

        private static void CollectNodeOwnership(
            KbNode node,
            int visibleLevel,
            string level2NodeId,
            IDictionary<string, NodeOwnershipState> index)
        {
            string nodeId = node.NodeId?.Trim() ?? string.Empty;
            string currentLevel2NodeId = visibleLevel == 2 && !string.IsNullOrWhiteSpace(nodeId)
                ? nodeId
                : level2NodeId;

            if (!string.IsNullOrWhiteSpace(nodeId))
            {
                index[nodeId] = new NodeOwnershipState(
                    visibleLevel,
                    currentLevel2NodeId);
            }

            foreach (KbNode child in node.Children ?? new List<KbNode>())
                CollectNodeOwnership(child, visibleLevel + 1, currentLevel2NodeId, index);
        }

        private static string ResolveLevel2EngineeringOwnerNodeId(
            string ownerNodeId,
            IReadOnlyDictionary<string, NodeOwnershipState>? nodeOwnershipIndex)
        {
            if (string.IsNullOrWhiteSpace(ownerNodeId) ||
                nodeOwnershipIndex == null ||
                !nodeOwnershipIndex.TryGetValue(ownerNodeId, out NodeOwnershipState ownership))
            {
                return ownerNodeId;
            }

            return ownership.VisibleLevel >= 3 && !string.IsNullOrWhiteSpace(ownership.Level2NodeId)
                ? ownership.Level2NodeId
                : ownerNodeId;
        }

        public static List<KbCompositionRack> NormalizeCompositionRacks(IEnumerable<KbCompositionRack>? racks)
        {
            var normalized = new List<KbCompositionRack>();
            if (racks == null)
                return normalized;

            var usedRackIds = new HashSet<string>(StringComparer.Ordinal);
            var usedParentRackKeys = new HashSet<string>(StringComparer.Ordinal);
            int normalizedIndex = 0;

            foreach (var rack in racks)
            {
                if (rack == null)
                    continue;

                string parentNodeId = rack.ParentNodeId?.Trim() ?? string.Empty;
                if (string.IsNullOrWhiteSpace(parentNodeId))
                    continue;

                int rackNumber = KnowledgeBaseCompositionRackSlotRulesService.NormalizeRackNumber(rack.RackNumber);
                string parentRackKey = $"{parentNodeId}|{rackNumber}";
                if (!usedParentRackKeys.Add(parentRackKey))
                    continue;

                int sortOrder = rack.SortOrder >= 0 ? rack.SortOrder : normalizedIndex;
                normalized.Add(new KbCompositionRack
                {
                    RackId = NormalizeOwnedRecordId(
                        rack.RackId,
                        "rack",
                        parentNodeId,
                        $"rack{rackNumber}",
                        usedRackIds),
                    ParentNodeId = parentNodeId,
                    RackNumber = rackNumber,
                    SortOrder = sortOrder,
                    RackType = NormalizeRackType(rack.RackType),
                    Label = rack.Label?.Trim() ?? string.Empty,
                    Notes = rack.Notes?.Trim() ?? string.Empty,
                    Properties = NormalizeCompositionRackProperties(rack.Properties)
                });

                normalizedIndex++;
            }

            return normalized
                .OrderBy(static rack => rack.ParentNodeId, StringComparer.Ordinal)
                .ThenBy(static rack => rack.SortOrder)
                .ThenBy(static rack => rack.RackNumber)
                .ThenBy(static rack => rack.RackId, StringComparer.Ordinal)
                .ToList();
        }

        private static List<KbCompositionRackProperty> NormalizeCompositionRackProperties(
            IEnumerable<KbCompositionRackProperty>? properties)
        {
            var normalized = new List<KbCompositionRackProperty>();
            if (properties == null)
                return normalized;

            var usedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (KbCompositionRackProperty? property in properties)
            {
                if (property == null)
                    continue;

                string name = property.Name?.Trim() ?? string.Empty;
                string value = property.Value?.Trim() ?? string.Empty;
                if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(value))
                    continue;

                if (!usedNames.Add(name))
                    continue;

                normalized.Add(new KbCompositionRackProperty
                {
                    Name = name,
                    Value = value
                });
            }

            return normalized;
        }

        private static string NormalizeRackType(string? rackType)
        {
            string normalizedRackType = rackType?.Trim() ?? string.Empty;
            return string.IsNullOrWhiteSpace(normalizedRackType) ? "UR" : normalizedRackType;
        }

        private static List<KbCompositionEntry> NormalizeCompositionEntries(IEnumerable<KbCompositionEntry>? entries)
        {
            var normalized = new List<KbCompositionEntry>();
            if (entries == null)
                return normalized;

            var usedEntryIds = new HashSet<string>(StringComparer.Ordinal);
            int normalizedIndex = 0;

            foreach (var entry in entries)
            {
                if (entry == null)
                    continue;

                string parentNodeId = entry.ParentNodeId?.Trim() ?? string.Empty;
                if (string.IsNullOrWhiteSpace(parentNodeId))
                    continue;

                int positionOrder = entry.PositionOrder >= 0
                    ? entry.PositionOrder
                    : normalizedIndex;

                normalized.Add(new KbCompositionEntry
                {
                    EntryId = NormalizeCompositionEntryId(
                        entry.EntryId,
                        parentNodeId,
                        KnowledgeBaseCompositionRackSlotRulesService.NormalizeRackNumber(entry.RackNumber),
                        entry.SlotNumber,
                        positionOrder,
                        usedEntryIds),
                    ParentNodeId = parentNodeId,
                    RackNumber = KnowledgeBaseCompositionRackSlotRulesService.NormalizeRackNumber(entry.RackNumber),
                    SlotNumber = entry.SlotNumber is > 0 ? entry.SlotNumber : null,
                    PositionOrder = positionOrder,
                    ComponentType = entry.ComponentType?.Trim() ?? string.Empty,
                    Model = entry.Model?.Trim() ?? string.Empty,
                    OrderNumber = entry.OrderNumber?.Trim() ?? string.Empty,
                    Firmware = entry.Firmware?.Trim() ?? string.Empty,
                    MpiDpPnAddress = entry.MpiDpPnAddress?.Trim() ?? string.Empty,
                    InputAddress = entry.InputAddress?.Trim() ?? string.Empty,
                    OutputAddress = entry.OutputAddress?.Trim() ?? string.Empty,
                    Comment = entry.Comment?.Trim() ?? string.Empty,
                    InterfaceRows = entry.InterfaceRows?.Trim() ?? string.Empty,
                    IpAddress = entry.IpAddress?.Trim() ?? string.Empty,
                    LastCalibrationAt = entry.LastCalibrationAt,
                    NextCalibrationAt = entry.NextCalibrationAt,
                    Notes = entry.Notes?.Trim() ?? string.Empty
                });

                normalizedIndex++;
            }

            return normalized;
        }

        public static List<KbAct> NormalizeActs(IEnumerable<KbAct>? acts)
        {
            var normalized = new List<KbAct>();
            if (acts == null)
                return normalized;

            var usedActIds = new HashSet<string>(StringComparer.Ordinal);
            int normalizedIndex = 0;

            foreach (KbAct? act in acts)
            {
                if (act == null)
                    continue;

                string actNumber = act.ActNumber?.Trim() ?? string.Empty;
                int actYear = act.ActYear > 0
                    ? act.ActYear
                    : act.ActDate?.Year ?? 0;
                string actId = NormalizeActId(act.ActId, actYear, actNumber, normalizedIndex, usedActIds);
                var equipmentSnapshot = NormalizeActEquipmentSnapshot(act.EquipmentSnapshot);
                normalized.Add(new KbAct
                {
                    ActId = actId,
                    ActYear = actYear,
                    ActNumber = actNumber,
                    ActType = Enum.IsDefined(typeof(KbActType), act.ActType)
                        ? act.ActType
                        : KbActType.EquipmentFailure,
                    Status = Enum.IsDefined(typeof(KbActStatus), act.Status)
                        ? act.Status
                        : KbActStatus.Draft,
                    SignedAt = act.SignedAt?.Date,
                    StatusHistory = NormalizeActStatusHistory(act.StatusHistory, actId),
                    ActDate = act.ActDate,
                    WorkshopName = act.WorkshopName?.Trim() ?? string.Empty,
                    Lvl3NodeId = act.Lvl3NodeId?.Trim() ?? string.Empty,
                    Lvl3NameSnapshot = act.Lvl3NameSnapshot?.Trim() ?? string.Empty,
                    ObjectNameSnapshot = act.ObjectNameSnapshot?.Trim() ?? string.Empty,
                    ObjectPathSnapshot = act.ObjectPathSnapshot?.Trim() ?? string.Empty,
                    RackId = act.RackId?.Trim() ?? string.Empty,
                    RackNumberSnapshot = NormalizeNullableNonNegativeInt(act.RackNumberSnapshot),
                    RackNameSnapshot = act.RackNameSnapshot?.Trim() ?? string.Empty,
                    CompositionEntryId = act.CompositionEntryId?.Trim() ?? string.Empty,
                    EquipmentName = act.EquipmentName?.Trim() ?? string.Empty,
                    EquipmentSnapshot = equipmentSnapshot,
                    FailureDate = act.FailureDate,
                    FaultDescription = act.FaultDescription?.Trim() ?? string.Empty,
                    FailureReason = act.FailureReason?.Trim() ?? string.Empty,
                    InspectionResult = act.InspectionResult?.Trim() ?? string.Empty,
                    FaultCriterion = act.FaultCriterion?.Trim() ?? string.Empty,
                    RequestDocument = act.RequestDocument?.Trim() ?? string.Empty,
                    ActualLaborHours = act.ActualLaborHours?.Trim() ?? string.Empty,
                    CustomerName = act.CustomerName?.Trim() ?? string.Empty,
                    CustomerPosition = act.CustomerPosition?.Trim() ?? string.Empty,
                    ApproverName = act.ApproverName?.Trim() ?? string.Empty,
                    ApproverPosition = act.ApproverPosition?.Trim() ?? string.Empty,
                    CreatedBy = act.CreatedBy?.Trim() ?? string.Empty,
                    CreatedAt = act.CreatedAt,
                    UpdatedAt = act.UpdatedAt
                });

                normalizedIndex++;
            }

            return normalized
                .OrderBy(static act => act.ActYear)
                .ThenBy(static act => act.ActNumber, StringComparer.OrdinalIgnoreCase)
                .ThenBy(static act => act.ActId, StringComparer.Ordinal)
                .ToList();
        }

        public static List<KbActStatusChange> NormalizeActStatusHistory(
            IEnumerable<KbActStatusChange>? statusHistory,
            string actId)
        {
            var normalized = new List<KbActStatusChange>();
            if (statusHistory == null)
                return normalized;

            var usedChangeIds = new HashSet<string>(StringComparer.Ordinal);
            int normalizedIndex = 0;
            foreach (KbActStatusChange? change in statusHistory)
            {
                if (change == null)
                    continue;

                string changedBy = change.ChangedBy?.Trim() ?? string.Empty;
                if (change.ChangedAt == default)
                    continue;

                normalized.Add(new KbActStatusChange
                {
                    ChangeId = NormalizeOwnedRecordId(
                        change.ChangeId,
                        "act-status-change",
                        actId,
                        normalizedIndex.ToString(CultureInfo.InvariantCulture),
                        usedChangeIds),
                    PreviousStatus = Enum.IsDefined(typeof(KbActStatus), change.PreviousStatus)
                        ? change.PreviousStatus
                        : KbActStatus.Draft,
                    NewStatus = Enum.IsDefined(typeof(KbActStatus), change.NewStatus)
                        ? change.NewStatus
                        : KbActStatus.Draft,
                    ChangedAt = change.ChangedAt,
                    ChangedBy = changedBy
                });
                normalizedIndex++;
            }

            return normalized
                .OrderBy(static change => change.ChangedAt)
                .ThenBy(static change => change.ChangeId, StringComparer.Ordinal)
                .ToList();
        }

        public static List<KbActExecutor> NormalizeActExecutors(
            IEnumerable<KbActExecutor>? executors,
            IEnumerable<string>? knownActIds = null)
        {
            var normalized = new List<KbActExecutor>();
            if (executors == null)
                return normalized;

            HashSet<string>? knownActIdSet = knownActIds == null
                ? null
                : new HashSet<string>(
                    knownActIds
                        .Select(static id => id?.Trim() ?? string.Empty)
                        .Where(static id => !string.IsNullOrWhiteSpace(id)),
                    StringComparer.Ordinal);
            var usedExecutorIds = new HashSet<string>(StringComparer.Ordinal);
            int normalizedIndex = 0;

            foreach (KbActExecutor? executor in executors)
            {
                if (executor == null)
                    continue;

                string actId = executor.ActId?.Trim() ?? string.Empty;
                if (string.IsNullOrWhiteSpace(actId) ||
                    (knownActIdSet != null && !knownActIdSet.Contains(actId)))
                {
                    continue;
                }

                string lastName = executor.LastName?.Trim() ?? string.Empty;
                string firstName = executor.FirstName?.Trim() ?? string.Empty;
                string middleName = executor.MiddleName?.Trim() ?? string.Empty;
                string position = executor.Position?.Trim() ?? string.Empty;
                if (string.IsNullOrWhiteSpace(lastName) &&
                    string.IsNullOrWhiteSpace(firstName) &&
                    string.IsNullOrWhiteSpace(middleName) &&
                    string.IsNullOrWhiteSpace(position))
                {
                    continue;
                }

                int sortOrder = executor.SortOrder >= 0
                    ? executor.SortOrder
                    : normalizedIndex;
                normalized.Add(new KbActExecutor
                {
                    ExecutorId = NormalizeOwnedRecordId(
                        executor.ExecutorId,
                        "act-executor",
                        actId,
                        sortOrder.ToString(CultureInfo.InvariantCulture),
                        usedExecutorIds),
                    ActId = actId,
                    SortOrder = sortOrder,
                    LastName = lastName,
                    FirstName = firstName,
                    MiddleName = middleName,
                    Position = position
                });

                normalizedIndex++;
            }

            return normalized
                .OrderBy(static executor => executor.ActId, StringComparer.Ordinal)
                .ThenBy(static executor => executor.SortOrder)
                .ThenBy(static executor => executor.ExecutorId, StringComparer.Ordinal)
                .ToList();
        }

        public static List<KbActDocument> NormalizeActDocuments(
            IEnumerable<KbActDocument>? documents,
            IEnumerable<string>? knownActIds = null)
        {
            var normalized = new List<KbActDocument>();
            if (documents == null)
                return normalized;

            HashSet<string>? knownActIdSet = knownActIds == null
                ? null
                : new HashSet<string>(
                    knownActIds
                        .Select(static id => id?.Trim() ?? string.Empty)
                        .Where(static id => !string.IsNullOrWhiteSpace(id)),
                    StringComparer.Ordinal);
            var usedDocumentIds = new HashSet<string>(StringComparer.Ordinal);
            int normalizedIndex = 0;

            foreach (KbActDocument? document in documents)
            {
                if (document == null)
                    continue;

                string actId = document.ActId?.Trim() ?? string.Empty;
                if (string.IsNullOrWhiteSpace(actId) ||
                    (knownActIdSet != null && !knownActIdSet.Contains(actId)))
                {
                    continue;
                }

                string path = document.Path?.Trim() ?? string.Empty;
                if (string.IsNullOrWhiteSpace(path))
                    continue;

                int versionNumber = document.VersionNumber > 0 ? document.VersionNumber : 1;
                normalized.Add(new KbActDocument
                {
                    DocumentId = NormalizeOwnedRecordId(
                        document.DocumentId,
                        "act-document",
                        actId,
                        $"{versionNumber}-{normalizedIndex}",
                        usedDocumentIds),
                    ActId = actId,
                    VersionNumber = versionNumber,
                    TemplateId = document.TemplateId?.Trim() ?? string.Empty,
                    TemplateVersion = document.TemplateVersion?.Trim() ?? string.Empty,
                    Path = path,
                    GeneratedAt = document.GeneratedAt,
                    ContentHash = document.ContentHash?.Trim() ?? string.Empty,
                    IsLatest = document.IsLatest
                });

                normalizedIndex++;
            }

            return normalized
                .OrderBy(static document => document.ActId, StringComparer.Ordinal)
                .ThenBy(static document => document.VersionNumber)
                .ThenBy(static document => document.DocumentId, StringComparer.Ordinal)
                .ToList();
        }

        public static List<KbActNumberSequence> NormalizeActNumberSequences(
            IEnumerable<KbActNumberSequence>? sequences)
        {
            var normalizedByYear = new Dictionary<int, int>();
            if (sequences == null)
                return new List<KbActNumberSequence>();

            foreach (KbActNumberSequence? sequence in sequences)
            {
                if (sequence == null || sequence.Year <= 0)
                    continue;

                int nextNumber = Math.Max(1, sequence.NextNumber);
                if (!normalizedByYear.TryGetValue(sequence.Year, out int existingNextNumber) ||
                    nextNumber > existingNextNumber)
                {
                    normalizedByYear[sequence.Year] = nextNumber;
                }
            }

            return normalizedByYear
                .OrderBy(static pair => pair.Key)
                .Select(static pair => new KbActNumberSequence
                {
                    Year = pair.Key,
                    NextNumber = pair.Value
                })
                .ToList();
        }

        public static KbActEquipmentSnapshot NormalizeActEquipmentSnapshot(KbActEquipmentSnapshot? snapshot)
        {
            if (snapshot == null)
                return new KbActEquipmentSnapshot();

            return new KbActEquipmentSnapshot
            {
                Lvl3Name = snapshot.Lvl3Name?.Trim() ?? string.Empty,
                ObjectPath = snapshot.ObjectPath?.Trim() ?? string.Empty,
                RackId = snapshot.RackId?.Trim() ?? string.Empty,
                RackNumber = NormalizeNullableNonNegativeInt(snapshot.RackNumber),
                RackName = snapshot.RackName?.Trim() ?? string.Empty,
                CompositionEntryId = snapshot.CompositionEntryId?.Trim() ?? string.Empty,
                ComponentType = snapshot.ComponentType?.Trim() ?? string.Empty,
                Model = snapshot.Model?.Trim() ?? string.Empty,
                OrderNumber = snapshot.OrderNumber?.Trim() ?? string.Empty,
                SerialNumber = snapshot.SerialNumber?.Trim() ?? string.Empty,
                Notes = snapshot.Notes?.Trim() ?? string.Empty
            };
        }

        private static List<KbDocumentLink> NormalizeDocumentLinks(
            IEnumerable<KbDocumentLink>? links,
            IReadOnlyDictionary<string, NodeOwnershipState>? nodeOwnershipIndex = null)
        {
            var normalized = new List<KbDocumentLink>();
            if (links == null)
                return normalized;

            var usedDocumentIds = new HashSet<string>(StringComparer.Ordinal);
            int normalizedIndex = 0;

            foreach (var link in links)
            {
                if (link == null)
                    continue;

                string ownerNodeId = ResolveLevel2EngineeringOwnerNodeId(
                    link.OwnerNodeId?.Trim() ?? string.Empty,
                    nodeOwnershipIndex);
                if (string.IsNullOrWhiteSpace(ownerNodeId))
                    continue;

                KbDocumentKind kind = Enum.IsDefined(typeof(KbDocumentKind), link.Kind)
                    ? link.Kind
                    : KbDocumentKind.Manual;

                normalized.Add(new KbDocumentLink
                {
                    DocumentId = NormalizeOwnedRecordId(
                        link.DocumentId,
                        "doc",
                        ownerNodeId,
                        $"{kind.ToString().ToLowerInvariant()}-{normalizedIndex}",
                        usedDocumentIds),
                    OwnerNodeId = ownerNodeId,
                    Kind = kind,
                    Title = link.Title?.Trim() ?? string.Empty,
                    Path = link.Path?.Trim() ?? string.Empty,
                    UpdatedAt = link.UpdatedAt?.Date
                });

                normalizedIndex++;
            }

            return normalized;
        }

        private static List<KbSoftwareRecord> NormalizeSoftwareRecords(
            IEnumerable<KbSoftwareRecord>? records,
            IReadOnlyDictionary<string, NodeOwnershipState>? nodeOwnershipIndex = null)
        {
            var normalized = new List<KbSoftwareRecord>();
            if (records == null)
                return normalized;

            var usedSoftwareIds = new HashSet<string>(StringComparer.Ordinal);
            int normalizedIndex = 0;

            foreach (var record in records)
            {
                if (record == null)
                    continue;

                string ownerNodeId = ResolveLevel2EngineeringOwnerNodeId(
                    record.OwnerNodeId?.Trim() ?? string.Empty,
                    nodeOwnershipIndex);
                if (string.IsNullOrWhiteSpace(ownerNodeId))
                    continue;

                normalized.Add(new KbSoftwareRecord
                {
                    SoftwareId = NormalizeOwnedRecordId(
                        record.SoftwareId,
                        "software",
                        ownerNodeId,
                        normalizedIndex.ToString(),
                        usedSoftwareIds),
                    OwnerNodeId = ownerNodeId,
                    Title = record.Title?.Trim() ?? string.Empty,
                    Path = record.Path?.Trim() ?? string.Empty,
                    AddedAt = record.AddedAt?.Date,
                    LastChangedAt = record.LastChangedAt?.Date,
                    LastBackupAt = record.LastBackupAt?.Date,
                    Notes = record.Notes?.Trim() ?? string.Empty
                });

                normalizedIndex++;
            }

            return normalized;
        }

        private static List<KbMaintenanceScheduleProfile> NormalizeMaintenanceScheduleProfiles(
            IEnumerable<KbMaintenanceScheduleProfile>? profiles)
        {
            var normalized = new List<KbMaintenanceScheduleProfile>();
            if (profiles == null)
                return normalized;

            var usedProfileIds = new HashSet<string>(StringComparer.Ordinal);
            var usedOwnerNodeIds = new HashSet<string>(StringComparer.Ordinal);
            int normalizedIndex = 0;

            foreach (var profile in profiles)
            {
                if (profile == null)
                    continue;

                string ownerNodeId = profile.OwnerNodeId?.Trim() ?? string.Empty;
                if (string.IsNullOrWhiteSpace(ownerNodeId))
                    continue;

                if (!usedOwnerNodeIds.Add(ownerNodeId))
                    continue;

                normalized.Add(new KbMaintenanceScheduleProfile
                {
                    MaintenanceProfileId = NormalizeOwnedRecordId(
                        profile.MaintenanceProfileId,
                        "maintenance",
                        ownerNodeId,
                        normalizedIndex.ToString(),
                        usedProfileIds),
                    OwnerNodeId = ownerNodeId,
                    IsIncludedInSchedule = profile.IsIncludedInSchedule,
                    To1Hours = Math.Max(0, profile.To1Hours),
                    To2Hours = Math.Max(0, profile.To2Hours),
                    To3Hours = Math.Max(0, profile.To3Hours),
                    YearScheduleEntries = NormalizeMaintenanceYearScheduleEntries(profile.YearScheduleEntries)
                });

                normalizedIndex++;
            }

            return normalized;
        }

        public static List<KbEquipmentCatalogItem> NormalizeEquipmentCatalogItems(
            IEnumerable<KbEquipmentCatalogItem>? items)
        {
            var normalized = new List<KbEquipmentCatalogItem>();
            if (items == null)
                return normalized;

            var usedIds = new HashSet<string>(StringComparer.Ordinal);
            var usedSemanticKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            int normalizedIndex = 0;

            foreach (KbEquipmentCatalogItem? item in items)
            {
                if (item == null)
                    continue;

                string equipmentKind = item.EquipmentKind?.Trim() ?? string.Empty;
                string manufacturer = item.Manufacturer?.Trim() ?? string.Empty;
                string series = item.Series?.Trim() ?? string.Empty;
                string model = item.Model?.Trim() ?? string.Empty;
                string catalogItemId = item.CatalogItemId?.Trim() ?? string.Empty;
                if (string.IsNullOrWhiteSpace(equipmentKind) &&
                    string.IsNullOrWhiteSpace(manufacturer) &&
                    string.IsNullOrWhiteSpace(series) &&
                    string.IsNullOrWhiteSpace(model))
                {
                    continue;
                }

                if (!string.IsNullOrWhiteSpace(catalogItemId) && usedIds.Contains(catalogItemId))
                    continue;

                string semanticKey = BuildEquipmentCatalogSemanticKey(equipmentKind, manufacturer, series, model);
                if (!usedSemanticKeys.Add(semanticKey))
                    continue;

                normalized.Add(new KbEquipmentCatalogItem
                {
                    CatalogItemId = NormalizeEquipmentCatalogItemId(catalogItemId, semanticKey, normalizedIndex, usedIds),
                    EquipmentKind = equipmentKind,
                    Manufacturer = manufacturer,
                    Series = series,
                    Model = model,
                    DefaultNodeType = Enum.IsDefined(typeof(KbNodeType), item.DefaultNodeType)
                        ? item.DefaultNodeType
                        : KbNodeType.Device,
                    Description = item.Description?.Trim() ?? string.Empty,
                    Properties = NormalizeEquipmentCatalogProperties(item.Properties)
                });

                normalizedIndex++;
            }

            return normalized
                .OrderBy(static item => item.EquipmentKind, StringComparer.OrdinalIgnoreCase)
                .ThenBy(static item => item.Manufacturer, StringComparer.OrdinalIgnoreCase)
                .ThenBy(static item => item.Series, StringComparer.OrdinalIgnoreCase)
                .ThenBy(static item => item.Model, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        public static List<KbObjectTemplate> NormalizeObjectTemplates(
            IEnumerable<KbObjectTemplate>? templates)
        {
            var normalized = new List<KbObjectTemplate>();
            if (templates == null)
                return normalized;

            var usedTemplateIds = new HashSet<string>(StringComparer.Ordinal);
            int normalizedIndex = 0;

            foreach (KbObjectTemplate? template in templates)
            {
                if (template == null)
                    continue;

                string displayName = template.DisplayName?.Trim() ?? string.Empty;
                var usedTemplateNodeIds = new HashSet<string>(StringComparer.Ordinal);
                if (!TryNormalizeObjectTemplateNode(
                    template.RootNode,
                    fallbackTemplateNodeId: "root",
                    fallbackName: displayName,
                    usedTemplateNodeIds,
                    out KbObjectTemplateNode normalizedRootNode))
                {
                    continue;
                }

                if (string.IsNullOrWhiteSpace(displayName))
                    displayName = normalizedRootNode.Name;

                if (string.IsNullOrWhiteSpace(displayName))
                    displayName = $"Шаблон объекта {normalizedIndex + 1}";

                if (string.IsNullOrWhiteSpace(normalizedRootNode.Name))
                    normalizedRootNode.Name = displayName;

                if (!TryNormalizeObjectTemplateId(
                    template.TemplateId,
                    displayName,
                    normalizedIndex,
                    usedTemplateIds,
                    out string templateId))
                {
                    continue;
                }

                HashSet<string> knownTemplateNodeIds = CollectObjectTemplateNodeIds(normalizedRootNode);
                normalized.Add(new KbObjectTemplate
                {
                    TemplateId = templateId,
                    DisplayName = displayName,
                    Description = template.Description?.Trim() ?? string.Empty,
                    Category = template.Category?.Trim() ?? string.Empty,
                    RootNode = normalizedRootNode,
                    CompositionRacks = NormalizeObjectTemplateCompositionRacks(
                        template.CompositionRacks,
                        knownTemplateNodeIds),
                    CompositionEntries = NormalizeObjectTemplateCompositionEntries(
                        template.CompositionEntries,
                        knownTemplateNodeIds),
                    DocumentLinks = NormalizeObjectTemplateDocumentLinks(template.DocumentLinks, knownTemplateNodeIds),
                    SoftwareRecords = NormalizeObjectTemplateSoftwareRecords(
                        template.SoftwareRecords,
                        knownTemplateNodeIds),
                    MaintenanceScheduleProfiles = NormalizeObjectTemplateMaintenanceScheduleProfiles(
                        template.MaintenanceScheduleProfiles,
                        knownTemplateNodeIds)
                });

                normalizedIndex++;
            }

            return normalized
                .OrderBy(static template => template.Category, StringComparer.OrdinalIgnoreCase)
                .ThenBy(static template => template.DisplayName, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private static bool TryNormalizeObjectTemplateNode(
            KbObjectTemplateNode? node,
            string fallbackTemplateNodeId,
            string fallbackName,
            HashSet<string> usedTemplateNodeIds,
            out KbObjectTemplateNode normalizedNode)
        {
            normalizedNode = new KbObjectTemplateNode();
            if (node == null)
                return false;

            string templateNodeId = NormalizeObjectTemplateNodeId(
                node.TemplateNodeId,
                fallbackTemplateNodeId,
                usedTemplateNodeIds);
            string nodeName = node.Name?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(nodeName))
                nodeName = fallbackName?.Trim() ?? string.Empty;

            normalizedNode = new KbObjectTemplateNode
            {
                TemplateNodeId = templateNodeId,
                CatalogItemId = node.CatalogItemId?.Trim() ?? string.Empty,
                Name = nodeName,
                NodeType = NormalizeObjectTemplateNodeType(node.NodeType),
                Details = NormalizeObjectTemplateDetails(node.Details),
                Children = new List<KbObjectTemplateNode>()
            };

            int childIndex = 0;
            foreach (KbObjectTemplateNode? child in node.Children ?? Enumerable.Empty<KbObjectTemplateNode>())
            {
                string childFallbackId = $"{templateNodeId}-{childIndex + 1}";
                if (TryNormalizeObjectTemplateNode(
                    child,
                    childFallbackId,
                    fallbackName: string.Empty,
                    usedTemplateNodeIds,
                    out KbObjectTemplateNode normalizedChild))
                {
                    normalizedNode.Children.Add(normalizedChild);
                    childIndex++;
                }
            }

            return !string.IsNullOrWhiteSpace(normalizedNode.Name) ||
                   !string.IsNullOrWhiteSpace(normalizedNode.CatalogItemId) ||
                   HasObjectTemplateDetails(normalizedNode.Details) ||
                   normalizedNode.Children.Count > 0;
        }

        private static KbNodeType NormalizeObjectTemplateNodeType(KbNodeType nodeType) =>
            nodeType != KbNodeType.Unknown && Enum.IsDefined(typeof(KbNodeType), nodeType)
                ? nodeType
                : KbNodeType.Device;

        private static KbNodeDetails NormalizeObjectTemplateDetails(KbNodeDetails? details) =>
            new()
            {
                Description = details?.Description?.Trim() ?? string.Empty,
                Location = details?.Location?.Trim() ?? string.Empty,
                InventoryNumber = details?.InventoryNumber?.Trim() ?? string.Empty,
                PhotoPath = details?.PhotoPath?.Trim() ?? string.Empty,
                IpAddress = details?.IpAddress?.Trim() ?? string.Empty,
                SchemaLink = details?.SchemaLink?.Trim() ?? string.Empty,
                NetworkTopology = new KbNetworkTopology()
            };

        private static bool HasObjectTemplateDetails(KbNodeDetails details) =>
            !string.IsNullOrWhiteSpace(details.Description) ||
            !string.IsNullOrWhiteSpace(details.Location) ||
            !string.IsNullOrWhiteSpace(details.InventoryNumber) ||
            !string.IsNullOrWhiteSpace(details.PhotoPath) ||
            !string.IsNullOrWhiteSpace(details.IpAddress) ||
            !string.IsNullOrWhiteSpace(details.SchemaLink);

        public static KbNetworkTopology NormalizeNetworkTopology(KbNetworkTopology? topology)
        {
            var normalized = new KbNetworkTopology();
            if (topology == null)
                return normalized;

            var usedElementIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (KbNetworkElement? element in topology.Elements ?? Enumerable.Empty<KbNetworkElement>())
            {
                if (element == null)
                    continue;

                string elementId = NormalizeStableId(element.ElementId, usedElementIds);
                var kind = Enum.IsDefined(typeof(KbNetworkElementKind), element.Kind)
                    ? element.Kind
                    : KbNetworkElementKind.Other;

                normalized.Elements.Add(new KbNetworkElement
                {
                    ElementId = elementId,
                    Kind = kind,
                    Name = NormalizeNetworkText(element.Name, GetDefaultNetworkElementName(kind)),
                    IpAddress = NormalizeNetworkText(element.IpAddress),
                    AdditionalIpAddresses = NormalizeNetworkIpAddresses(element.AdditionalIpAddresses),
                    X = Math.Clamp(element.X, 0, 5000),
                    Y = Math.Clamp(element.Y, 0, 5000)
                });
            }

            var validElementIds = normalized.Elements
                .Select(static element => element.ElementId)
                .ToHashSet(StringComparer.Ordinal);
            var usedLinkIds = new HashSet<string>(StringComparer.Ordinal);
            var usedPairs = new HashSet<string>(StringComparer.Ordinal);
            foreach (KbNetworkLink? link in topology.Links ?? Enumerable.Empty<KbNetworkLink>())
            {
                if (link == null ||
                    !validElementIds.Contains(link.FromElementId) ||
                    !validElementIds.Contains(link.ToElementId) ||
                    string.Equals(link.FromElementId, link.ToElementId, StringComparison.Ordinal))
                {
                    continue;
                }

                bool fromFirst = string.CompareOrdinal(link.FromElementId, link.ToElementId) <= 0;
                string left = fromFirst ? link.FromElementId : link.ToElementId;
                string right = fromFirst ? link.ToElementId : link.FromElementId;
                string pairKey = $"{left}|{right}";
                if (!usedPairs.Add(pairKey))
                    continue;

                var linkKind = Enum.IsDefined(typeof(KbNetworkLinkKind), link.Kind)
                    ? link.Kind
                    : KbNetworkLinkKind.CopperProfinet;

                normalized.Links.Add(new KbNetworkLink
                {
                    LinkId = NormalizeStableId(link.LinkId, usedLinkIds),
                    FromElementId = link.FromElementId,
                    ToElementId = link.ToElementId,
                    Kind = linkKind,
                    Label = NormalizeNetworkText(link.Label)
                });
            }

            return normalized;
        }

        private static string NormalizeStableId(string? value, ISet<string> usedIds)
        {
            string normalized = value?.Trim() ?? string.Empty;
            if (normalized.Length > 0 && usedIds.Add(normalized))
                return normalized;

            string id;
            do
            {
                id = Guid.NewGuid().ToString("N");
            }
            while (!usedIds.Add(id));

            return id;
        }

        private static string NormalizeNetworkText(string? value, string fallback = "")
        {
            string normalized = value?.Trim() ?? string.Empty;
            return string.IsNullOrWhiteSpace(normalized)
                ? fallback
                : normalized;
        }

        private static List<string> NormalizeNetworkIpAddresses(IEnumerable<string>? values)
        {
            var result = new List<string>();
            var used = new HashSet<string>(StringComparer.Ordinal);
            foreach (string? value in values ?? Enumerable.Empty<string>())
            {
                string normalized = NormalizeNetworkText(value);
                if (string.IsNullOrWhiteSpace(normalized) || !used.Add(normalized))
                    continue;

                result.Add(normalized);
            }

            return result;
        }

        private static string GetDefaultNetworkElementName(KbNetworkElementKind kind) => kind switch
        {
            KbNetworkElementKind.Plc => "PLC",
            KbNetworkElementKind.FrequencyConverter => "ПЧ",
            KbNetworkElementKind.Scalance => "SCALANCE",
            KbNetworkElementKind.Arm => "АРМ",
            KbNetworkElementKind.Hmi => "HMI",
            KbNetworkElementKind.Server => "Сервер",
            KbNetworkElementKind.Et200 => "ET200",
            KbNetworkElementKind.Olm => "OLM",
            KbNetworkElementKind.ExternalConnection => "Внешняя связь",
            _ => "Устройство"
        };

        private static HashSet<string> CollectObjectTemplateNodeIds(KbObjectTemplateNode rootNode)
        {
            var nodeIds = new HashSet<string>(StringComparer.Ordinal);
            CollectObjectTemplateNodeIdsRecursive(rootNode, nodeIds);
            return nodeIds;
        }

        private static void CollectObjectTemplateNodeIdsRecursive(
            KbObjectTemplateNode node,
            ISet<string> nodeIds)
        {
            nodeIds.Add(node.TemplateNodeId);
            foreach (KbObjectTemplateNode child in node.Children)
                CollectObjectTemplateNodeIdsRecursive(child, nodeIds);
        }

        private static List<KbObjectTemplateCompositionEntry> NormalizeObjectTemplateCompositionEntries(
            IEnumerable<KbObjectTemplateCompositionEntry>? entries,
            ISet<string> knownTemplateNodeIds)
        {
            var normalized = new List<KbObjectTemplateCompositionEntry>();
            if (entries == null)
                return normalized;

            int normalizedIndex = 0;
            foreach (KbObjectTemplateCompositionEntry? entry in entries)
            {
                if (entry == null)
                    continue;

                string parentTemplateNodeId = entry.ParentTemplateNodeId?.Trim() ?? string.Empty;
                if (!knownTemplateNodeIds.Contains(parentTemplateNodeId))
                    continue;

                normalized.Add(new KbObjectTemplateCompositionEntry
                {
                    ParentTemplateNodeId = parentTemplateNodeId,
                    RackNumber = KnowledgeBaseCompositionRackSlotRulesService.NormalizeRackNumber(entry.RackNumber),
                    SlotNumber = entry.SlotNumber is > 0 ? entry.SlotNumber : null,
                    PositionOrder = entry.PositionOrder >= 0 ? entry.PositionOrder : normalizedIndex,
                    ComponentType = entry.ComponentType?.Trim() ?? string.Empty,
                    Model = entry.Model?.Trim() ?? string.Empty,
                    OrderNumber = entry.OrderNumber?.Trim() ?? string.Empty,
                    Firmware = entry.Firmware?.Trim() ?? string.Empty,
                    MpiDpPnAddress = entry.MpiDpPnAddress?.Trim() ?? string.Empty,
                    InputAddress = entry.InputAddress?.Trim() ?? string.Empty,
                    OutputAddress = entry.OutputAddress?.Trim() ?? string.Empty,
                    Comment = entry.Comment?.Trim() ?? string.Empty,
                    InterfaceRows = entry.InterfaceRows?.Trim() ?? string.Empty,
                    IpAddress = entry.IpAddress?.Trim() ?? string.Empty,
                    LastCalibrationAt = entry.LastCalibrationAt,
                    NextCalibrationAt = entry.NextCalibrationAt,
                    Notes = entry.Notes?.Trim() ?? string.Empty
                });

                normalizedIndex++;
            }

            return normalized
                .OrderBy(static entry => entry.ParentTemplateNodeId, StringComparer.Ordinal)
                .ThenBy(static entry => entry.RackNumber)
                .ThenBy(static entry => entry.SlotNumber.HasValue ? 0 : 1)
                .ThenBy(static entry => entry.SlotNumber ?? int.MaxValue)
                .ThenBy(static entry => entry.PositionOrder)
                .ToList();
        }

        private static List<KbObjectTemplateCompositionRack> NormalizeObjectTemplateCompositionRacks(
            IEnumerable<KbObjectTemplateCompositionRack>? racks,
            ISet<string> knownTemplateNodeIds)
        {
            var normalized = new List<KbObjectTemplateCompositionRack>();
            if (racks == null)
                return normalized;

            var usedTemplateRackKeys = new HashSet<string>(StringComparer.Ordinal);
            int normalizedIndex = 0;
            foreach (KbObjectTemplateCompositionRack? rack in racks)
            {
                if (rack == null)
                    continue;

                string parentTemplateNodeId = rack.ParentTemplateNodeId?.Trim() ?? string.Empty;
                if (!knownTemplateNodeIds.Contains(parentTemplateNodeId))
                    continue;

                int rackNumber = KnowledgeBaseCompositionRackSlotRulesService.NormalizeRackNumber(rack.RackNumber);
                string rackKey = $"{parentTemplateNodeId}|{rackNumber}";
                if (!usedTemplateRackKeys.Add(rackKey))
                    continue;

                normalized.Add(new KbObjectTemplateCompositionRack
                {
                    ParentTemplateNodeId = parentTemplateNodeId,
                    RackNumber = rackNumber,
                    SortOrder = rack.SortOrder >= 0 ? rack.SortOrder : normalizedIndex,
                    RackType = NormalizeRackType(rack.RackType),
                    Label = rack.Label?.Trim() ?? string.Empty,
                    Notes = rack.Notes?.Trim() ?? string.Empty,
                    Properties = NormalizeCompositionRackProperties(rack.Properties)
                });

                normalizedIndex++;
            }

            return normalized
                .OrderBy(static rack => rack.ParentTemplateNodeId, StringComparer.Ordinal)
                .ThenBy(static rack => rack.SortOrder)
                .ThenBy(static rack => rack.RackNumber)
                .ToList();
        }

        private static List<KbObjectTemplateDocumentLink> NormalizeObjectTemplateDocumentLinks(
            IEnumerable<KbObjectTemplateDocumentLink>? links,
            ISet<string> knownTemplateNodeIds)
        {
            var normalized = new List<KbObjectTemplateDocumentLink>();
            if (links == null)
                return normalized;

            foreach (KbObjectTemplateDocumentLink? link in links)
            {
                if (link == null)
                    continue;

                string ownerTemplateNodeId = link.OwnerTemplateNodeId?.Trim() ?? string.Empty;
                string title = link.Title?.Trim() ?? string.Empty;
                string path = link.Path?.Trim() ?? string.Empty;
                if (!knownTemplateNodeIds.Contains(ownerTemplateNodeId) ||
                    (string.IsNullOrWhiteSpace(title) && string.IsNullOrWhiteSpace(path)))
                {
                    continue;
                }

                normalized.Add(new KbObjectTemplateDocumentLink
                {
                    OwnerTemplateNodeId = ownerTemplateNodeId,
                    Kind = Enum.IsDefined(typeof(KbDocumentKind), link.Kind)
                        ? link.Kind
                        : KbDocumentKind.Manual,
                    Title = title,
                    Path = path,
                    UpdatedAt = link.UpdatedAt?.Date
                });
            }

            return normalized
                .OrderBy(static link => link.OwnerTemplateNodeId, StringComparer.Ordinal)
                .ThenBy(static link => link.Kind)
                .ThenBy(static link => link.Title, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private static List<KbObjectTemplateSoftwareRecord> NormalizeObjectTemplateSoftwareRecords(
            IEnumerable<KbObjectTemplateSoftwareRecord>? records,
            ISet<string> knownTemplateNodeIds)
        {
            var normalized = new List<KbObjectTemplateSoftwareRecord>();
            if (records == null)
                return normalized;

            foreach (KbObjectTemplateSoftwareRecord? record in records)
            {
                if (record == null)
                    continue;

                string ownerTemplateNodeId = record.OwnerTemplateNodeId?.Trim() ?? string.Empty;
                string title = record.Title?.Trim() ?? string.Empty;
                string path = record.Path?.Trim() ?? string.Empty;
                if (!knownTemplateNodeIds.Contains(ownerTemplateNodeId) ||
                    (string.IsNullOrWhiteSpace(title) && string.IsNullOrWhiteSpace(path)))
                {
                    continue;
                }

                normalized.Add(new KbObjectTemplateSoftwareRecord
                {
                    OwnerTemplateNodeId = ownerTemplateNodeId,
                    Title = title,
                    Path = path,
                    AddedAt = record.AddedAt?.Date,
                    LastChangedAt = record.LastChangedAt?.Date,
                    LastBackupAt = record.LastBackupAt?.Date,
                    Notes = record.Notes?.Trim() ?? string.Empty
                });
            }

            return normalized
                .OrderBy(static record => record.OwnerTemplateNodeId, StringComparer.Ordinal)
                .ThenBy(static record => record.Title, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private static List<KbObjectTemplateMaintenanceScheduleProfile>
            NormalizeObjectTemplateMaintenanceScheduleProfiles(
                IEnumerable<KbObjectTemplateMaintenanceScheduleProfile>? profiles,
                ISet<string> knownTemplateNodeIds)
        {
            var normalized = new List<KbObjectTemplateMaintenanceScheduleProfile>();
            if (profiles == null)
                return normalized;

            var usedOwnerTemplateNodeIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (KbObjectTemplateMaintenanceScheduleProfile? profile in profiles)
            {
                if (profile == null)
                    continue;

                string ownerTemplateNodeId = profile.OwnerTemplateNodeId?.Trim() ?? string.Empty;
                if (!knownTemplateNodeIds.Contains(ownerTemplateNodeId) ||
                    !usedOwnerTemplateNodeIds.Add(ownerTemplateNodeId))
                {
                    continue;
                }

                normalized.Add(new KbObjectTemplateMaintenanceScheduleProfile
                {
                    OwnerTemplateNodeId = ownerTemplateNodeId,
                    IsIncludedInSchedule = profile.IsIncludedInSchedule,
                    To1Hours = Math.Max(0, profile.To1Hours),
                    To2Hours = Math.Max(0, profile.To2Hours),
                    To3Hours = Math.Max(0, profile.To3Hours),
                    YearScheduleEntries = NormalizeMaintenanceYearScheduleEntries(profile.YearScheduleEntries)
                });
            }

            return normalized
                .OrderBy(static profile => profile.OwnerTemplateNodeId, StringComparer.Ordinal)
                .ToList();
        }

        private static List<KbMaintenanceYearScheduleEntry> NormalizeMaintenanceYearScheduleEntries(
            IEnumerable<KbMaintenanceYearScheduleEntry>? entries)
        {
            var normalizedByMonth = new SortedDictionary<int, KbMaintenanceYearScheduleEntry>();
            if (entries == null)
                return new List<KbMaintenanceYearScheduleEntry>();

            foreach (KbMaintenanceYearScheduleEntry entry in entries)
            {
                if (entry == null ||
                    entry.Month < 1 ||
                    entry.Month > 12 ||
                    !Enum.IsDefined(typeof(KbMaintenanceWorkKind), entry.WorkKind))
                {
                    continue;
                }

                normalizedByMonth[entry.Month] = new KbMaintenanceYearScheduleEntry
                {
                    Month = entry.Month,
                    WorkKind = entry.WorkKind,
                    Hours = Math.Max(0, entry.Hours)
                };
            }

            return normalizedByMonth
                .Select(static pair => pair.Value)
                .ToList();
        }

        private static List<KbEquipmentCatalogProperty> NormalizeEquipmentCatalogProperties(
            IEnumerable<KbEquipmentCatalogProperty>? properties)
        {
            var normalized = new List<KbEquipmentCatalogProperty>();
            if (properties == null)
                return normalized;

            var usedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (KbEquipmentCatalogProperty? property in properties)
            {
                if (property == null)
                    continue;

                string name = property.Name?.Trim() ?? string.Empty;
                if (string.IsNullOrWhiteSpace(name) || !usedNames.Add(name))
                    continue;

                normalized.Add(new KbEquipmentCatalogProperty
                {
                    Name = name,
                    Value = property.Value?.Trim() ?? string.Empty
                });
            }

            return normalized
                .OrderBy(static property => property.Name, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private static string BuildEquipmentCatalogSemanticKey(
            string equipmentKind,
            string manufacturer,
            string series,
            string model) =>
            string.Join(
                "|",
                NormalizeTextKey(equipmentKind),
                NormalizeTextKey(manufacturer),
                NormalizeTextKey(series),
                NormalizeTextKey(model));

        private static string NormalizeTextKey(string? value) =>
            string.Join(
                " ",
                (value ?? string.Empty)
                    .Trim()
                    .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                .ToUpperInvariant();

        private static string NormalizeEquipmentCatalogItemId(
            string? catalogItemId,
            string semanticKey,
            int normalizedIndex,
            HashSet<string> usedIds)
        {
            string normalizedExistingId = catalogItemId?.Trim() ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(normalizedExistingId))
            {
                usedIds.Add(normalizedExistingId);
                return normalizedExistingId;
            }

            string semanticId = "catalog-" + string.Join(
                "-",
                semanticKey
                    .Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Select(static part => part.Replace(' ', '-').ToLowerInvariant())
                    .Where(static part => part.Length > 0));
            if (semanticId == "catalog-")
                semanticId = $"catalog-{normalizedIndex}";

            if (usedIds.Add(semanticId))
                return semanticId;

            int suffix = 2;
            while (true)
            {
                string candidate = $"{semanticId}-{suffix}";
                if (usedIds.Add(candidate))
                    return candidate;

                suffix++;
            }
        }

        private static bool TryNormalizeObjectTemplateId(
            string? templateId,
            string displayName,
            int normalizedIndex,
            HashSet<string> usedIds,
            out string normalizedTemplateId)
        {
            normalizedTemplateId = templateId?.Trim() ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(normalizedTemplateId))
                return usedIds.Add(normalizedTemplateId);

            string semanticId = "template-" + string.Join(
                "-",
                NormalizeTextKey(displayName)
                    .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Select(static part => part.ToLowerInvariant()));
            if (semanticId == "template-")
                semanticId = $"template-{normalizedIndex}";

            normalizedTemplateId = EnsureUniqueObjectTemplateId(semanticId, usedIds);
            return true;
        }

        private static string EnsureUniqueObjectTemplateId(
            string semanticId,
            HashSet<string> usedIds)
        {
            if (usedIds.Add(semanticId))
                return semanticId;

            int suffix = 2;
            while (true)
            {
                string candidate = $"{semanticId}-{suffix}";
                if (usedIds.Add(candidate))
                    return candidate;

                suffix++;
            }
        }

        private static string NormalizeObjectTemplateNodeId(
            string? templateNodeId,
            string fallbackTemplateNodeId,
            HashSet<string> usedTemplateNodeIds)
        {
            string normalizedExistingId = templateNodeId?.Trim() ?? string.Empty;
            string baseId = !string.IsNullOrWhiteSpace(normalizedExistingId)
                ? normalizedExistingId
                : fallbackTemplateNodeId;

            if (usedTemplateNodeIds.Add(baseId))
                return baseId;

            int suffix = 2;
            while (true)
            {
                string candidate = $"{baseId}-{suffix}";
                if (usedTemplateNodeIds.Add(candidate))
                    return candidate;

                suffix++;
            }
        }

        private static string NormalizeCompositionEntryId(
            string? entryId,
            string parentNodeId,
            int rackNumber,
            int? slotNumber,
            int positionOrder,
            ISet<string> usedEntryIds)
        {
            string normalizedExistingId = entryId?.Trim() ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(normalizedExistingId) && usedEntryIds.Add(normalizedExistingId))
                return normalizedExistingId;

            string deterministicId = slotNumber.HasValue
                ? $"comp-{parentNodeId}-rack{rackNumber}-{slotNumber.Value}-{positionOrder}"
                : $"comp-{parentNodeId}-aux-{positionOrder}";
            if (usedEntryIds.Add(deterministicId))
                return deterministicId;

            int suffix = 2;
            while (true)
            {
                string candidate = $"{deterministicId}-{suffix}";
                if (usedEntryIds.Add(candidate))
                    return candidate;

                suffix++;
            }
        }

        private static string NormalizeActId(
            string? actId,
            int actYear,
            string actNumber,
            int normalizedIndex,
            ISet<string> usedActIds)
        {
            string normalizedExistingId = actId?.Trim() ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(normalizedExistingId) && usedActIds.Add(normalizedExistingId))
                return normalizedExistingId;

            string normalizedNumberPart = string.Join(
                "-",
                NormalizeTextKey(actNumber)
                    .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Select(static part => part.ToLowerInvariant()));
            string deterministicId = !string.IsNullOrWhiteSpace(normalizedNumberPart) && actYear > 0
                ? $"act-{actYear}-{normalizedNumberPart}"
                : $"act-{normalizedIndex}";

            if (usedActIds.Add(deterministicId))
                return deterministicId;

            int suffix = 2;
            while (true)
            {
                string candidate = $"{deterministicId}-{suffix}";
                if (usedActIds.Add(candidate))
                    return candidate;

                suffix++;
            }
        }

        private static int? NormalizeNullableNonNegativeInt(int? value) =>
            value is >= 0 ? value : null;

        private static string NormalizeOwnedRecordId(
            string? recordId,
            string prefix,
            string ownerNodeId,
            string suffixSeed,
            ISet<string> usedIds)
        {
            string normalizedExistingId = recordId?.Trim() ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(normalizedExistingId) && usedIds.Add(normalizedExistingId))
                return normalizedExistingId;

            string deterministicId = $"{prefix}-{ownerNodeId}-{suffixSeed}";
            if (usedIds.Add(deterministicId))
                return deterministicId;

            int suffix = 2;
            while (true)
            {
                string candidate = $"{deterministicId}-{suffix}";
                if (usedIds.Add(candidate))
                    return candidate;

                suffix++;
            }
        }
    }
}
