using AsutpKnowledgeBase.Models;

namespace AsutpKnowledgeBase.Services
{
    public sealed class KnowledgeBaseObjectTemplateInstantiationResult
    {
        public bool IsSuccess { get; init; }

        public string ErrorMessage { get; init; } = string.Empty;

        public KbNode? RootNode { get; init; }

        public Dictionary<string, string> NodeIdMap { get; init; } = new(StringComparer.Ordinal);

        public List<KbCompositionEntry> CompositionEntries { get; init; } = new();

        public List<KbDocumentLink> DocumentLinks { get; init; } = new();

        public List<KbSoftwareRecord> SoftwareRecords { get; init; } = new();

        public List<KbNetworkFileReference> NetworkFileReferences { get; init; } = new();

        public List<KbMaintenanceScheduleProfile> MaintenanceScheduleProfiles { get; init; } = new();
    }

    public sealed class KnowledgeBaseObjectTemplateService
    {
        public KnowledgeBaseObjectTemplateInstantiationResult CreateInstance(
            KbObjectTemplate? template,
            string? rootNameOverride = null)
        {
            if (template == null)
                return Failure("Шаблон объекта не найден или заполнен некорректно.");

            List<KbObjectTemplate> normalizedTemplates =
                KnowledgeBaseDataService.NormalizeObjectTemplates(new[] { template });
            KbObjectTemplate? normalizedTemplate = normalizedTemplates.SingleOrDefault();
            if (normalizedTemplate == null)
                return Failure("Шаблон объекта не найден или заполнен некорректно.");

            var nodeIdMap = new Dictionary<string, string>(StringComparer.Ordinal);
            KbNode rootNode = CreateNodeInstance(
                normalizedTemplate.RootNode,
                levelIndex: 0,
                nodeIdMap);

            string normalizedRootNameOverride = rootNameOverride?.Trim() ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(normalizedRootNameOverride))
                rootNode.Name = normalizedRootNameOverride;

            return new KnowledgeBaseObjectTemplateInstantiationResult
            {
                IsSuccess = true,
                RootNode = rootNode,
                NodeIdMap = nodeIdMap,
                CompositionEntries = CreateCompositionEntries(normalizedTemplate.CompositionEntries, nodeIdMap),
                DocumentLinks = CreateDocumentLinks(normalizedTemplate.DocumentLinks, nodeIdMap),
                SoftwareRecords = CreateSoftwareRecords(normalizedTemplate.SoftwareRecords, nodeIdMap),
                NetworkFileReferences = CreateNetworkFileReferences(normalizedTemplate.NetworkFileReferences, nodeIdMap),
                MaintenanceScheduleProfiles = CreateMaintenanceScheduleProfiles(
                    normalizedTemplate.MaintenanceScheduleProfiles,
                    nodeIdMap)
            };
        }

        private static KbNode CreateNodeInstance(
            KbObjectTemplateNode templateNode,
            int levelIndex,
            Dictionary<string, string> nodeIdMap)
        {
            string nodeId = KnowledgeBaseNodeMetadataService.CreateNewNodeId();
            nodeIdMap[templateNode.TemplateNodeId] = nodeId;

            return new KbNode
            {
                NodeId = nodeId,
                Name = templateNode.Name,
                LevelIndex = levelIndex,
                NodeType = templateNode.NodeType,
                Details = CloneDetails(templateNode.Details),
                Children = templateNode.Children
                    .Select(child => CreateNodeInstance(child, levelIndex + 1, nodeIdMap))
                    .ToList()
            };
        }

        private static List<KbCompositionEntry> CreateCompositionEntries(
            IEnumerable<KbObjectTemplateCompositionEntry> entries,
            IReadOnlyDictionary<string, string> nodeIdMap)
        {
            var result = new List<KbCompositionEntry>();
            foreach (KbObjectTemplateCompositionEntry entry in entries)
            {
                if (!nodeIdMap.TryGetValue(entry.ParentTemplateNodeId, out string? parentNodeId))
                    continue;

                result.Add(new KbCompositionEntry
                {
                    ParentNodeId = parentNodeId,
                    SlotNumber = entry.SlotNumber,
                    PositionOrder = entry.PositionOrder,
                    ComponentType = entry.ComponentType,
                    Model = entry.Model,
                    IpAddress = entry.IpAddress,
                    LastCalibrationAt = entry.LastCalibrationAt,
                    NextCalibrationAt = entry.NextCalibrationAt,
                    Notes = entry.Notes
                });
            }

            return result;
        }

        private static List<KbDocumentLink> CreateDocumentLinks(
            IEnumerable<KbObjectTemplateDocumentLink> links,
            IReadOnlyDictionary<string, string> nodeIdMap)
        {
            var result = new List<KbDocumentLink>();
            foreach (KbObjectTemplateDocumentLink link in links)
            {
                if (!nodeIdMap.TryGetValue(link.OwnerTemplateNodeId, out string? ownerNodeId))
                    continue;

                result.Add(new KbDocumentLink
                {
                    OwnerNodeId = ownerNodeId,
                    Kind = link.Kind,
                    Title = link.Title,
                    Path = link.Path,
                    UpdatedAt = link.UpdatedAt
                });
            }

            return result;
        }

        private static List<KbSoftwareRecord> CreateSoftwareRecords(
            IEnumerable<KbObjectTemplateSoftwareRecord> records,
            IReadOnlyDictionary<string, string> nodeIdMap)
        {
            var result = new List<KbSoftwareRecord>();
            foreach (KbObjectTemplateSoftwareRecord record in records)
            {
                if (!nodeIdMap.TryGetValue(record.OwnerTemplateNodeId, out string? ownerNodeId))
                    continue;

                result.Add(new KbSoftwareRecord
                {
                    OwnerNodeId = ownerNodeId,
                    Title = record.Title,
                    Path = record.Path,
                    AddedAt = record.AddedAt,
                    LastChangedAt = record.LastChangedAt,
                    LastBackupAt = record.LastBackupAt,
                    Notes = record.Notes
                });
            }

            return result;
        }

        private static List<KbNetworkFileReference> CreateNetworkFileReferences(
            IEnumerable<KbObjectTemplateNetworkFileReference> references,
            IReadOnlyDictionary<string, string> nodeIdMap)
        {
            var result = new List<KbNetworkFileReference>();
            foreach (KbObjectTemplateNetworkFileReference reference in references)
            {
                if (!nodeIdMap.TryGetValue(reference.OwnerTemplateNodeId, out string? ownerNodeId))
                    continue;

                result.Add(new KbNetworkFileReference
                {
                    OwnerNodeId = ownerNodeId,
                    Title = reference.Title,
                    Path = reference.Path,
                    PreviewKind = KnowledgeBaseNetworkPreviewService.ResolvePreviewKind(reference.Path)
                });
            }

            return result;
        }

        private static List<KbMaintenanceScheduleProfile> CreateMaintenanceScheduleProfiles(
            IEnumerable<KbObjectTemplateMaintenanceScheduleProfile> profiles,
            IReadOnlyDictionary<string, string> nodeIdMap)
        {
            var result = new List<KbMaintenanceScheduleProfile>();
            foreach (KbObjectTemplateMaintenanceScheduleProfile profile in profiles)
            {
                if (!nodeIdMap.TryGetValue(profile.OwnerTemplateNodeId, out string? ownerNodeId))
                    continue;

                result.Add(new KbMaintenanceScheduleProfile
                {
                    OwnerNodeId = ownerNodeId,
                    IsIncludedInSchedule = profile.IsIncludedInSchedule,
                    To1Hours = profile.To1Hours,
                    To2Hours = profile.To2Hours,
                    To3Hours = profile.To3Hours,
                    YearScheduleEntries = profile.YearScheduleEntries
                        .Select(static entry => new KbMaintenanceYearScheduleEntry
                        {
                            Month = entry.Month,
                            WorkKind = entry.WorkKind
                        })
                        .ToList()
                });
            }

            return result;
        }

        private static KbNodeDetails CloneDetails(KbNodeDetails details) =>
            new()
            {
                Description = details.Description,
                Location = details.Location,
                InventoryNumber = details.InventoryNumber,
                PhotoPath = details.PhotoPath,
                IpAddress = details.IpAddress,
                SchemaLink = details.SchemaLink
            };

        private static KnowledgeBaseObjectTemplateInstantiationResult Failure(string errorMessage) =>
            new()
            {
                IsSuccess = false,
                ErrorMessage = errorMessage
            };
    }
}
