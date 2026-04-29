using System.Text.Json;
using AsutpKnowledgeBase.Models;

namespace AsutpKnowledgeBase.Services
{
    public static class KnowledgeBaseDataService
    {
        private static readonly JsonSerializerOptions SnapshotOptions = new() { WriteIndented = false };

        public static StringComparer WorkshopNameComparer { get; } = StringComparer.OrdinalIgnoreCase;

        public static KbConfig CreateDefaultConfig() =>
            new()
            {
                MaxLevels = 10,
                LevelNames = Enumerable
                    .Range(1, 10)
                    .Select(static level => $"Уровень {level}")
                    .ToList()
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
                CompositionEntries = new List<KbCompositionEntry>(),
                DocumentLinks = new List<KbDocumentLink>(),
                SoftwareRecords = new List<KbSoftwareRecord>(),
                NetworkFileReferences = new List<KbNetworkFileReference>(),
                MaintenanceScheduleProfiles = new List<KbMaintenanceScheduleProfile>(),
                LastWorkshop = "Новый цех"
            };

        public static SavedData NormalizeSavedData(SavedData? data)
        {
            var source = data ?? CreateDefaultData();
            var normalizedConfig = NormalizeConfig(source.Config);
            var normalizedWorkshops = NormalizeWorkshops(source.Workshops);
            var normalizedCompositionEntries = NormalizeCompositionEntries(source.CompositionEntries);
            var normalizedDocumentLinks = NormalizeDocumentLinks(source.DocumentLinks);
            var normalizedSoftwareRecords = NormalizeSoftwareRecords(source.SoftwareRecords);
            var normalizedNetworkFileReferences = NormalizeNetworkFileReferences(source.NetworkFileReferences);
            var normalizedMaintenanceScheduleProfiles = NormalizeMaintenanceScheduleProfiles(source.MaintenanceScheduleProfiles);
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
                CompositionEntries = normalizedCompositionEntries,
                DocumentLinks = normalizedDocumentLinks,
                SoftwareRecords = normalizedSoftwareRecords,
                NetworkFileReferences = normalizedNetworkFileReferences,
                MaintenanceScheduleProfiles = normalizedMaintenanceScheduleProfiles,
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
                MaxLevels = config.MaxLevels > 0 ? config.MaxLevels : defaults.MaxLevels
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

            return normalized;
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
                compositionEntries: null,
                documentLinks: null,
                softwareRecords: null,
                networkFileReferences: null,
                maintenanceScheduleProfiles: null,
                currentWorkshop,
                includeCurrentWorkshop);

        public static string SerializeSnapshot(
            KbConfig config,
            Dictionary<string, List<KbNode>> workshops,
            IReadOnlyList<KbCompositionEntry>? compositionEntries,
            IReadOnlyList<KbDocumentLink>? documentLinks,
            IReadOnlyList<KbSoftwareRecord>? softwareRecords,
            IReadOnlyList<KbNetworkFileReference>? networkFileReferences,
            IReadOnlyList<KbMaintenanceScheduleProfile>? maintenanceScheduleProfiles,
            string currentWorkshop,
            bool includeCurrentWorkshop)
        {
            var data = new SavedData
            {
                SchemaVersion = SavedData.CurrentSchemaVersion,
                Config = config,
                Workshops = workshops,
                CompositionEntries = compositionEntries?.ToList() ?? new List<KbCompositionEntry>(),
                DocumentLinks = documentLinks?.ToList() ?? new List<KbDocumentLink>(),
                SoftwareRecords = softwareRecords?.ToList() ?? new List<KbSoftwareRecord>(),
                NetworkFileReferences = networkFileReferences?.ToList() ?? new List<KbNetworkFileReference>(),
                MaintenanceScheduleProfiles = maintenanceScheduleProfiles?.ToList() ?? new List<KbMaintenanceScheduleProfile>(),
                LastWorkshop = includeCurrentWorkshop ? currentWorkshop : string.Empty
            };

            return JsonSerializer.Serialize(data, SnapshotOptions);
        }

        private static void NormalizeNodes(string workshopName, IList<KbNode> nodes, ISet<string> usedNodeIds)
        {
            KnowledgeBaseNodeMetadataService.NormalizePersistentWorkshopNodes(workshopName, nodes, usedNodeIds);

            foreach (var node in nodes)
                NormalizeNodeDetailsRecursive(node);
        }

        private static void NormalizeNodeDetailsRecursive(KbNode node)
        {
            node.Name ??= string.Empty;
            node.Details = NormalizeDetails(node.Details, node.NodeType, node.LevelIndex);
            node.Children ??= new List<KbNode>();

            foreach (var child in node.Children)
                NormalizeNodeDetailsRecursive(child);
        }

        private static KbNodeDetails NormalizeDetails(KbNodeDetails? details, KbNodeType nodeType, int levelIndex) =>
            new()
            {
                Description = details?.Description ?? string.Empty,
                Location = details?.Location ?? string.Empty,
                InventoryNumber = KnowledgeBaseNodeMetadataService.SupportsInventoryNumber(nodeType, levelIndex)
                    ? details?.InventoryNumber ?? string.Empty
                    : string.Empty,
                PhotoPath = details?.PhotoPath ?? string.Empty,
                IpAddress = KnowledgeBaseNodeMetadataService.SupportsTechnicalFields(nodeType)
                    ? details?.IpAddress ?? string.Empty
                    : string.Empty,
                SchemaLink = KnowledgeBaseNodeMetadataService.SupportsTechnicalFields(nodeType)
                    ? details?.SchemaLink ?? string.Empty
                    : string.Empty
            };

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
                        entry.SlotNumber,
                        positionOrder,
                        usedEntryIds),
                    ParentNodeId = parentNodeId,
                    SlotNumber = entry.SlotNumber is > 0 ? entry.SlotNumber : null,
                    PositionOrder = positionOrder,
                    ComponentType = entry.ComponentType?.Trim() ?? string.Empty,
                    Model = entry.Model?.Trim() ?? string.Empty,
                    IpAddress = entry.IpAddress?.Trim() ?? string.Empty,
                    LastCalibrationAt = entry.LastCalibrationAt,
                    NextCalibrationAt = entry.NextCalibrationAt,
                    Notes = entry.Notes?.Trim() ?? string.Empty
                });

                normalizedIndex++;
            }

            return normalized;
        }

        private static List<KbDocumentLink> NormalizeDocumentLinks(IEnumerable<KbDocumentLink>? links)
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

                string ownerNodeId = link.OwnerNodeId?.Trim() ?? string.Empty;
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

        private static List<KbSoftwareRecord> NormalizeSoftwareRecords(IEnumerable<KbSoftwareRecord>? records)
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

                string ownerNodeId = record.OwnerNodeId?.Trim() ?? string.Empty;
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

        private static List<KbNetworkFileReference> NormalizeNetworkFileReferences(
            IEnumerable<KbNetworkFileReference>? references)
        {
            var normalized = new List<KbNetworkFileReference>();
            if (references == null)
                return normalized;

            var usedNetworkAssetIds = new HashSet<string>(StringComparer.Ordinal);
            int normalizedIndex = 0;

            foreach (var reference in references)
            {
                if (reference == null)
                    continue;

                string ownerNodeId = reference.OwnerNodeId?.Trim() ?? string.Empty;
                if (string.IsNullOrWhiteSpace(ownerNodeId))
                    continue;

                string path = reference.Path?.Trim() ?? string.Empty;

                normalized.Add(new KbNetworkFileReference
                {
                    NetworkAssetId = NormalizeOwnedRecordId(
                        reference.NetworkAssetId,
                        "network",
                        ownerNodeId,
                        normalizedIndex.ToString(),
                        usedNetworkAssetIds),
                    OwnerNodeId = ownerNodeId,
                    Title = reference.Title?.Trim() ?? string.Empty,
                    Path = path,
                    PreviewKind = KnowledgeBaseNetworkPreviewService.ResolvePreviewKind(path)
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
                    To3Hours = Math.Max(0, profile.To3Hours)
                });

                normalizedIndex++;
            }

            return normalized;
        }

        private static string NormalizeCompositionEntryId(
            string? entryId,
            string parentNodeId,
            int? slotNumber,
            int positionOrder,
            ISet<string> usedEntryIds)
        {
            string normalizedExistingId = entryId?.Trim() ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(normalizedExistingId) && usedEntryIds.Add(normalizedExistingId))
                return normalizedExistingId;

            string deterministicId = $"comp-{parentNodeId}-{slotNumber?.ToString() ?? "aux"}-{positionOrder}";
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
