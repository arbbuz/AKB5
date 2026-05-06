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

    public sealed class KnowledgeBaseObjectTemplateBuildResult
    {
        public bool IsSuccess { get; init; }

        public string ErrorMessage { get; init; } = string.Empty;

        public KbObjectTemplate? Template { get; init; }

        public Dictionary<string, string> NodeIdMap { get; init; } = new(StringComparer.Ordinal);
    }

    public sealed class KnowledgeBaseObjectTemplateService
    {
        public KnowledgeBaseObjectTemplateBuildResult CreateTemplateFromExistingObject(
            KbNode? sourceRoot,
            string? displayName,
            string? category,
            string? description,
            IEnumerable<KbCompositionEntry>? compositionEntries,
            IEnumerable<KbDocumentLink>? documentLinks,
            IEnumerable<KbSoftwareRecord>? softwareRecords,
            IEnumerable<KbNetworkFileReference>? networkFileReferences,
            IEnumerable<KbMaintenanceScheduleProfile>? maintenanceScheduleProfiles)
        {
            if (sourceRoot == null)
                return BuildFailure("Р’С‹Р±РµСЂРёС‚Рµ РѕР±СЉРµРєС‚, РєРѕС‚РѕСЂС‹Р№ РЅСѓР¶РЅРѕ СЃРѕС…СЂР°РЅРёС‚СЊ РєР°Рє С€Р°Р±Р»РѕРЅ.");

            string normalizedDisplayName = displayName?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(normalizedDisplayName))
                normalizedDisplayName = sourceRoot.Name?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(normalizedDisplayName))
                return BuildFailure("РЈРєР°Р¶РёС‚Рµ РЅР°Р·РІР°РЅРёРµ С€Р°Р±Р»РѕРЅР°.");

            var nodeIdMap = new Dictionary<string, string>(StringComparer.Ordinal);
            KbObjectTemplateNode templateRoot = CreateTemplateNode(sourceRoot, nodeIdMap);
            var template = new KbObjectTemplate
            {
                TemplateId = string.Empty,
                DisplayName = normalizedDisplayName,
                Category = category?.Trim() ?? string.Empty,
                Description = description?.Trim() ?? string.Empty,
                RootNode = templateRoot,
                CompositionEntries = CreateTemplateCompositionEntries(compositionEntries, nodeIdMap),
                DocumentLinks = CreateTemplateDocumentLinks(documentLinks, nodeIdMap),
                SoftwareRecords = CreateTemplateSoftwareRecords(softwareRecords, nodeIdMap),
                NetworkFileReferences = CreateTemplateNetworkFileReferences(networkFileReferences, nodeIdMap),
                MaintenanceScheduleProfiles = CreateTemplateMaintenanceScheduleProfiles(
                    maintenanceScheduleProfiles,
                    nodeIdMap)
            };

            return new KnowledgeBaseObjectTemplateBuildResult
            {
                IsSuccess = true,
                Template = template,
                NodeIdMap = nodeIdMap
            };
        }

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

        private static KbObjectTemplateNode CreateTemplateNode(
            KbNode sourceNode,
            Dictionary<string, string> nodeIdMap)
        {
            string templateNodeId = "template-node-" + Guid.NewGuid().ToString("N");
            string sourceNodeId = sourceNode.NodeId?.Trim() ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(sourceNodeId))
                nodeIdMap[sourceNodeId] = templateNodeId;

            return new KbObjectTemplateNode
            {
                TemplateNodeId = templateNodeId,
                Name = sourceNode.Name?.Trim() ?? string.Empty,
                NodeType = sourceNode.NodeType,
                Details = CloneDetails(sourceNode.Details),
                Children = sourceNode.Children
                    .Select(child => CreateTemplateNode(child, nodeIdMap))
                    .ToList()
            };
        }

        private static List<KbObjectTemplateCompositionEntry> CreateTemplateCompositionEntries(
            IEnumerable<KbCompositionEntry>? entries,
            IReadOnlyDictionary<string, string> nodeIdMap)
        {
            var result = new List<KbObjectTemplateCompositionEntry>();
            foreach (KbCompositionEntry entry in entries ?? Enumerable.Empty<KbCompositionEntry>())
            {
                if (!nodeIdMap.TryGetValue(entry.ParentNodeId, out string? parentTemplateNodeId))
                    continue;

                result.Add(new KbObjectTemplateCompositionEntry
                {
                    ParentTemplateNodeId = parentTemplateNodeId,
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

        private static List<KbObjectTemplateDocumentLink> CreateTemplateDocumentLinks(
            IEnumerable<KbDocumentLink>? links,
            IReadOnlyDictionary<string, string> nodeIdMap)
        {
            var result = new List<KbObjectTemplateDocumentLink>();
            foreach (KbDocumentLink link in links ?? Enumerable.Empty<KbDocumentLink>())
            {
                if (!nodeIdMap.TryGetValue(link.OwnerNodeId, out string? ownerTemplateNodeId))
                    continue;

                result.Add(new KbObjectTemplateDocumentLink
                {
                    OwnerTemplateNodeId = ownerTemplateNodeId,
                    Kind = link.Kind,
                    Title = link.Title,
                    Path = link.Path,
                    UpdatedAt = link.UpdatedAt
                });
            }

            return result;
        }

        private static List<KbObjectTemplateSoftwareRecord> CreateTemplateSoftwareRecords(
            IEnumerable<KbSoftwareRecord>? records,
            IReadOnlyDictionary<string, string> nodeIdMap)
        {
            var result = new List<KbObjectTemplateSoftwareRecord>();
            foreach (KbSoftwareRecord record in records ?? Enumerable.Empty<KbSoftwareRecord>())
            {
                if (!nodeIdMap.TryGetValue(record.OwnerNodeId, out string? ownerTemplateNodeId))
                    continue;

                result.Add(new KbObjectTemplateSoftwareRecord
                {
                    OwnerTemplateNodeId = ownerTemplateNodeId,
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

        private static List<KbObjectTemplateNetworkFileReference> CreateTemplateNetworkFileReferences(
            IEnumerable<KbNetworkFileReference>? references,
            IReadOnlyDictionary<string, string> nodeIdMap)
        {
            var result = new List<KbObjectTemplateNetworkFileReference>();
            foreach (KbNetworkFileReference reference in references ?? Enumerable.Empty<KbNetworkFileReference>())
            {
                if (!nodeIdMap.TryGetValue(reference.OwnerNodeId, out string? ownerTemplateNodeId))
                    continue;

                result.Add(new KbObjectTemplateNetworkFileReference
                {
                    OwnerTemplateNodeId = ownerTemplateNodeId,
                    Title = reference.Title,
                    Path = reference.Path
                });
            }

            return result;
        }

        private static List<KbObjectTemplateMaintenanceScheduleProfile> CreateTemplateMaintenanceScheduleProfiles(
            IEnumerable<KbMaintenanceScheduleProfile>? profiles,
            IReadOnlyDictionary<string, string> nodeIdMap)
        {
            var result = new List<KbObjectTemplateMaintenanceScheduleProfile>();
            foreach (KbMaintenanceScheduleProfile profile in profiles ?? Enumerable.Empty<KbMaintenanceScheduleProfile>())
            {
                if (!nodeIdMap.TryGetValue(profile.OwnerNodeId, out string? ownerTemplateNodeId))
                    continue;

                result.Add(new KbObjectTemplateMaintenanceScheduleProfile
                {
                    OwnerTemplateNodeId = ownerTemplateNodeId,
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

        private static KnowledgeBaseObjectTemplateBuildResult BuildFailure(string errorMessage) =>
            new()
            {
                IsSuccess = false,
                ErrorMessage = errorMessage
            };
    }
}
