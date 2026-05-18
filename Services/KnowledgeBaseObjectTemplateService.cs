using AsutpKnowledgeBase.Models;

namespace AsutpKnowledgeBase.Services
{
    public sealed class KnowledgeBaseObjectTemplateInstantiationResult
    {
        public bool IsSuccess { get; init; }

        public string ErrorMessage { get; init; } = string.Empty;

        public KbNode? RootNode { get; init; }

        public Dictionary<string, string> NodeIdMap { get; init; } = new(StringComparer.Ordinal);

        public List<KbCompositionRack> CompositionRacks { get; init; } = new();

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
            IEnumerable<KbCompositionRack>? compositionRacks,
            IEnumerable<KbCompositionEntry>? compositionEntries,
            IEnumerable<KbDocumentLink>? documentLinks,
            IEnumerable<KbSoftwareRecord>? softwareRecords,
            IEnumerable<KbNetworkFileReference>? networkFileReferences,
            IEnumerable<KbMaintenanceScheduleProfile>? maintenanceScheduleProfiles)
        {
            if (sourceRoot == null)
                return BuildFailure("Выберите объект, который нужно сохранить как шаблон.");

            string normalizedDisplayName = displayName?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(normalizedDisplayName))
                normalizedDisplayName = sourceRoot.Name?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(normalizedDisplayName))
                return BuildFailure("Укажите название шаблона.");

            var nodeIdMap = new Dictionary<string, string>(StringComparer.Ordinal);
            KbObjectTemplateNode templateRoot = CreateTemplateNode(sourceRoot, nodeIdMap);
            var template = new KbObjectTemplate
            {
                TemplateId = string.Empty,
                DisplayName = normalizedDisplayName,
                Category = category?.Trim() ?? string.Empty,
                Description = description?.Trim() ?? string.Empty,
                RootNode = templateRoot,
                CompositionRacks = CreateTemplateCompositionRacks(compositionRacks, nodeIdMap),
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
                CompositionRacks = CreateCompositionRacks(normalizedTemplate.CompositionRacks, nodeIdMap),
                CompositionEntries = CreateCompositionEntries(normalizedTemplate.CompositionEntries, nodeIdMap),
                DocumentLinks = CreateDocumentLinks(normalizedTemplate.DocumentLinks, nodeIdMap),
                SoftwareRecords = CreateSoftwareRecords(normalizedTemplate.SoftwareRecords, nodeIdMap),
                NetworkFileReferences = CreateNetworkFileReferences(normalizedTemplate.NetworkFileReferences, nodeIdMap),
                MaintenanceScheduleProfiles = CreateMaintenanceScheduleProfiles(
                    normalizedTemplate.MaintenanceScheduleProfiles,
                    nodeIdMap)
            };
        }

        public KnowledgeBaseObjectTemplateApplicationPlan BuildApplyToExistingObjectPlan(
            KbObjectTemplate? template,
            KbNode? targetRoot,
            int maxLevels,
            IEnumerable<KbCompositionRack>? existingCompositionRacks,
            IEnumerable<KbCompositionEntry>? existingCompositionEntries,
            IEnumerable<KbDocumentLink>? existingDocumentLinks,
            IEnumerable<KbSoftwareRecord>? existingSoftwareRecords,
            IEnumerable<KbNetworkFileReference>? existingNetworkFileReferences,
            IEnumerable<KbMaintenanceScheduleProfile>? existingMaintenanceScheduleProfiles)
        {
            if (targetRoot == null)
                return ApplicationFailure("Выберите объект, к которому нужно применить шаблон.");

            if (template == null)
                return ApplicationFailure("Шаблон объекта не найден или заполнен некорректно.");

            List<KbObjectTemplate> normalizedTemplates =
                KnowledgeBaseDataService.NormalizeObjectTemplates(new[] { template });
            KbObjectTemplate? normalizedTemplate = normalizedTemplates.SingleOrDefault();
            if (normalizedTemplate == null)
                return ApplicationFailure("Шаблон объекта не найден или заполнен некорректно.");

            if (targetRoot.NodeType != normalizedTemplate.RootNode.NodeType)
            {
                return ApplicationFailure(
                    "Шаблон предназначен для другого типа объекта. Применение к выбранному объекту отменено.");
            }

            if (maxLevels <= 0)
                return ApplicationFailure("В базе задана нулевая максимальная глубина дерева.");

            var plan = new KnowledgeBaseObjectTemplateApplicationPlan
            {
                IsSuccess = true,
                TemplateDisplayName = normalizedTemplate.DisplayName,
                TargetName = targetRoot.Name
            };
            var nodeIdMap = new Dictionary<string, string>(StringComparer.Ordinal);
            var templateNodePathMap = new Dictionary<string, string>(StringComparer.Ordinal);

            BuildApplyNodePlan(
                normalizedTemplate.RootNode,
                targetRoot,
                BuildNodeDisplayName(targetRoot),
                maxLevels,
                nodeIdMap,
                templateNodePathMap,
                plan);

            AddCompositionRackPlans(
                normalizedTemplate.CompositionRacks,
                existingCompositionRacks,
                nodeIdMap,
                templateNodePathMap,
                plan);
            AddCompositionEntryPlans(
                normalizedTemplate.CompositionEntries,
                existingCompositionEntries,
                nodeIdMap,
                templateNodePathMap,
                plan);
            AddDocumentLinkPlans(
                normalizedTemplate.DocumentLinks,
                existingDocumentLinks,
                nodeIdMap,
                templateNodePathMap,
                plan);
            AddSoftwareRecordPlans(
                normalizedTemplate.SoftwareRecords,
                existingSoftwareRecords,
                nodeIdMap,
                templateNodePathMap,
                plan);
            AddNetworkFileReferencePlans(
                normalizedTemplate.NetworkFileReferences,
                existingNetworkFileReferences,
                nodeIdMap,
                templateNodePathMap,
                plan);
            AddMaintenanceScheduleProfilePlans(
                normalizedTemplate.MaintenanceScheduleProfiles,
                existingMaintenanceScheduleProfiles,
                nodeIdMap,
                templateNodePathMap,
                plan);
            AddNetworkInterfaceStubPlans(
                normalizedTemplate.NetworkInterfaceStubs,
                nodeIdMap,
                templateNodePathMap,
                plan);

            return plan;
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

        private static void BuildApplyNodePlan(
            KbObjectTemplateNode templateNode,
            KbNode targetNode,
            string targetPath,
            int maxLevels,
            Dictionary<string, string> nodeIdMap,
            Dictionary<string, string> templateNodePathMap,
            KnowledgeBaseObjectTemplateApplicationPlan plan)
        {
            nodeIdMap[templateNode.TemplateNodeId] = targetNode.NodeId;
            templateNodePathMap[templateNode.TemplateNodeId] = targetPath;
            AddPreviewItem(
                plan,
                KnowledgeBaseObjectTemplateApplicationAction.Unchanged,
                "Узел",
                targetPath,
                "Существующий объект используется как корень применения; имя и тип не меняются.");

            AddDetailUpdatePlans(templateNode.Details, targetNode, targetPath, plan);

            var usedExistingChildren = new HashSet<KbNode>();
            foreach (KbObjectTemplateNode templateChild in templateNode.Children)
            {
                string childName = BuildTemplateNodeDisplayName(templateChild);
                string childPath = $"{targetPath} / {childName}";
                KbNode? existingChild = targetNode.Children.FirstOrDefault(child =>
                    !usedExistingChildren.Contains(child) &&
                    child.NodeType == templateChild.NodeType &&
                    NamesEqual(child.Name, templateChild.Name));

                if (existingChild != null)
                {
                    usedExistingChildren.Add(existingChild);
                    BuildApplyNodePlan(
                        templateChild,
                        existingChild,
                        childPath,
                        maxLevels,
                        nodeIdMap,
                        templateNodePathMap,
                        plan);
                    continue;
                }

                bool hasSameNameDifferentType = targetNode.Children.Any(child =>
                    child.NodeType != templateChild.NodeType &&
                    NamesEqual(child.Name, templateChild.Name));
                if (hasSameNameDifferentType)
                {
                    AddSkippedTemplateSubtree(
                        templateChild,
                        childPath,
                        "Уже есть дочерний узел с таким именем, но другого типа; шаблон не добавляется автоматически.",
                        templateNodePathMap,
                        plan);
                    continue;
                }

                var additionNodeIdMap = new Dictionary<string, string>(StringComparer.Ordinal);
                KbNode addition = CreateNodeInstance(templateChild, targetNode.LevelIndex + 1, additionNodeIdMap);
                if (!CanAttachNodeAddition(targetNode, addition, maxLevels))
                {
                    AddSkippedTemplateSubtree(
                        templateChild,
                        childPath,
                        $"Поддерево не помещается в максимальную глубину {maxLevels}.",
                        templateNodePathMap,
                        plan);
                    continue;
                }

                foreach (var pair in additionNodeIdMap)
                    nodeIdMap[pair.Key] = pair.Value;

                CollectTemplateNodePaths(templateChild, childPath, templateNodePathMap);
                plan.NodeAdditions.Add(new KnowledgeBaseObjectTemplateNodeAddition
                {
                    ParentNode = targetNode,
                    Node = addition
                });
                AddPreviewItem(
                    plan,
                    KnowledgeBaseObjectTemplateApplicationAction.Added,
                    "Узел",
                    childPath,
                    $"Будет добавлено поддерево: {CountTemplateNodes(templateChild)} узл.");
            }
        }

        private static void AddDetailUpdatePlans(
            KbNodeDetails templateDetails,
            KbNode targetNode,
            string targetPath,
            KnowledgeBaseObjectTemplateApplicationPlan plan)
        {
            targetNode.Details ??= new KbNodeDetails();
            AddDetailUpdatePlan(plan, targetNode, targetPath, "description", "Описание",
                templateDetails.Description, targetNode.Details.Description);
            AddDetailUpdatePlan(plan, targetNode, targetPath, "location", "Местоположение",
                templateDetails.Location, targetNode.Details.Location);
            AddDetailUpdatePlan(plan, targetNode, targetPath, "inventory", "Инвентарный номер",
                templateDetails.InventoryNumber, targetNode.Details.InventoryNumber);
            AddDetailUpdatePlan(plan, targetNode, targetPath, "photo", "Фото",
                templateDetails.PhotoPath, targetNode.Details.PhotoPath);
            AddDetailUpdatePlan(plan, targetNode, targetPath, "ip", "IP-адрес",
                templateDetails.IpAddress, targetNode.Details.IpAddress);
            AddDetailUpdatePlan(plan, targetNode, targetPath, "schema", "Ссылка на схему",
                templateDetails.SchemaLink, targetNode.Details.SchemaLink);
        }

        private static void AddDetailUpdatePlan(
            KnowledgeBaseObjectTemplateApplicationPlan plan,
            KbNode targetNode,
            string targetPath,
            string fieldKey,
            string fieldDisplayName,
            string? templateValue,
            string? currentValue)
        {
            string normalizedTemplateValue = templateValue?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(normalizedTemplateValue))
                return;

            if (!IsDetailFieldSupported(targetNode, fieldKey))
            {
                AddPreviewItem(
                    plan,
                    KnowledgeBaseObjectTemplateApplicationAction.Skipped,
                    "Карточка",
                    targetPath,
                    $"Поле \"{fieldDisplayName}\" недоступно для текущего уровня объекта.");
                return;
            }

            string normalizedCurrentValue = currentValue?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(normalizedCurrentValue))
            {
                plan.DetailUpdates.Add(new KnowledgeBaseObjectTemplateDetailUpdate
                {
                    TargetNode = targetNode,
                    FieldKey = fieldKey,
                    FieldDisplayName = fieldDisplayName,
                    Value = normalizedTemplateValue
                });
                AddPreviewItem(
                    plan,
                    KnowledgeBaseObjectTemplateApplicationAction.Added,
                    "Карточка",
                    targetPath,
                    $"Будет заполнено поле \"{fieldDisplayName}\".");
                return;
            }

            if (string.Equals(normalizedCurrentValue, normalizedTemplateValue, StringComparison.CurrentCulture))
            {
                AddPreviewItem(
                    plan,
                    KnowledgeBaseObjectTemplateApplicationAction.Unchanged,
                    "Карточка",
                    targetPath,
                    $"Поле \"{fieldDisplayName}\" уже совпадает с шаблоном.");
                return;
            }

            AddPreviewItem(
                plan,
                KnowledgeBaseObjectTemplateApplicationAction.Skipped,
                "Карточка",
                targetPath,
                $"Поле \"{fieldDisplayName}\" уже заполнено и не будет перезаписано.");
        }

        private static bool IsDetailFieldSupported(KbNode targetNode, string fieldKey)
        {
            int visibleLevel = targetNode.LevelIndex + 1;
            return fieldKey switch
            {
                "location" => KnowledgeBaseNodeMetadataService.SupportsLocation(visibleLevel),
                "inventory" => KnowledgeBaseNodeMetadataService.SupportsInventoryNumber(visibleLevel),
                "photo" => KnowledgeBaseNodeMetadataService.SupportsPhoto(visibleLevel),
                "ip" => KnowledgeBaseNodeMetadataService.SupportsTechnicalFields(targetNode.NodeType, visibleLevel),
                "schema" => KnowledgeBaseNodeMetadataService.SupportsTechnicalFields(targetNode.NodeType, visibleLevel),
                _ => true
            };
        }

        private static void AddCompositionEntryPlans(
            IEnumerable<KbObjectTemplateCompositionEntry> templateEntries,
            IEnumerable<KbCompositionEntry>? existingEntries,
            IReadOnlyDictionary<string, string> nodeIdMap,
            IReadOnlyDictionary<string, string> templateNodePathMap,
            KnowledgeBaseObjectTemplateApplicationPlan plan)
        {
            var existing = existingEntries?.ToList() ?? new List<KbCompositionEntry>();
            foreach (KbObjectTemplateCompositionEntry templateEntry in templateEntries)
            {
                if (!nodeIdMap.TryGetValue(templateEntry.ParentTemplateNodeId, out string? parentNodeId))
                {
                    AddSkippedForMissingMappedNode("Состав", templateEntry.ParentTemplateNodeId, templateNodePathMap, plan);
                    continue;
                }

                var entry = new KbCompositionEntry
                {
                    ParentNodeId = parentNodeId,
                    RackNumber = templateEntry.RackNumber,
                    SlotNumber = templateEntry.SlotNumber,
                    PositionOrder = templateEntry.PositionOrder,
                    ComponentType = templateEntry.ComponentType,
                    Model = templateEntry.Model,
                    OrderNumber = templateEntry.OrderNumber,
                    Firmware = templateEntry.Firmware,
                    MpiDpPnAddress = templateEntry.MpiDpPnAddress,
                    InputAddress = templateEntry.InputAddress,
                    OutputAddress = templateEntry.OutputAddress,
                    Comment = templateEntry.Comment,
                    InterfaceRows = templateEntry.InterfaceRows,
                    IpAddress = templateEntry.IpAddress,
                    LastCalibrationAt = templateEntry.LastCalibrationAt,
                    NextCalibrationAt = templateEntry.NextCalibrationAt,
                    Notes = templateEntry.Notes
                };
                string targetPath = ResolveTemplateNodePath(templateEntry.ParentTemplateNodeId, templateNodePathMap);
                if (existing.Concat(plan.CompositionEntries).Any(current => CompositionEntriesEqual(current, entry)))
                {
                    AddPreviewItem(
                        plan,
                        KnowledgeBaseObjectTemplateApplicationAction.Unchanged,
                        "Состав",
                        targetPath,
                        BuildCompositionPreviewText(entry, "Запись состава уже есть."));
                    continue;
                }

                plan.CompositionEntries.Add(entry);
                AddPreviewItem(
                    plan,
                    KnowledgeBaseObjectTemplateApplicationAction.Added,
                    "Состав",
                    targetPath,
                    BuildCompositionPreviewText(entry, "Будет добавлена запись состава."));
            }
        }

        private static void AddDocumentLinkPlans(
            IEnumerable<KbObjectTemplateDocumentLink> templateLinks,
            IEnumerable<KbDocumentLink>? existingLinks,
            IReadOnlyDictionary<string, string> nodeIdMap,
            IReadOnlyDictionary<string, string> templateNodePathMap,
            KnowledgeBaseObjectTemplateApplicationPlan plan)
        {
            var existing = existingLinks?.ToList() ?? new List<KbDocumentLink>();
            foreach (KbObjectTemplateDocumentLink templateLink in templateLinks)
            {
                if (!nodeIdMap.TryGetValue(templateLink.OwnerTemplateNodeId, out string? ownerNodeId))
                {
                    AddSkippedForMissingMappedNode("Документы", templateLink.OwnerTemplateNodeId, templateNodePathMap, plan);
                    continue;
                }

                var link = new KbDocumentLink
                {
                    OwnerNodeId = ownerNodeId,
                    Kind = templateLink.Kind,
                    Title = templateLink.Title,
                    Path = templateLink.Path,
                    UpdatedAt = templateLink.UpdatedAt
                };
                string targetPath = ResolveTemplateNodePath(templateLink.OwnerTemplateNodeId, templateNodePathMap);
                if (existing.Concat(plan.DocumentLinks).Any(current => DocumentLinksEqual(current, link)))
                {
                    AddPreviewItem(
                        plan,
                        KnowledgeBaseObjectTemplateApplicationAction.Unchanged,
                        "Документы",
                        targetPath,
                        BuildTitlePathPreviewText(link.Title, link.Path, "Документ уже есть."));
                    continue;
                }

                plan.DocumentLinks.Add(link);
                AddPreviewItem(
                    plan,
                    KnowledgeBaseObjectTemplateApplicationAction.Added,
                    "Документы",
                    targetPath,
                    BuildTitlePathPreviewText(link.Title, link.Path, "Будет добавлен документ."));
            }
        }

        private static void AddSoftwareRecordPlans(
            IEnumerable<KbObjectTemplateSoftwareRecord> templateRecords,
            IEnumerable<KbSoftwareRecord>? existingRecords,
            IReadOnlyDictionary<string, string> nodeIdMap,
            IReadOnlyDictionary<string, string> templateNodePathMap,
            KnowledgeBaseObjectTemplateApplicationPlan plan)
        {
            var existing = existingRecords?.ToList() ?? new List<KbSoftwareRecord>();
            foreach (KbObjectTemplateSoftwareRecord templateRecord in templateRecords)
            {
                if (!nodeIdMap.TryGetValue(templateRecord.OwnerTemplateNodeId, out string? ownerNodeId))
                {
                    AddSkippedForMissingMappedNode("ПО", templateRecord.OwnerTemplateNodeId, templateNodePathMap, plan);
                    continue;
                }

                var record = new KbSoftwareRecord
                {
                    OwnerNodeId = ownerNodeId,
                    Title = templateRecord.Title,
                    Path = templateRecord.Path,
                    AddedAt = templateRecord.AddedAt,
                    LastChangedAt = templateRecord.LastChangedAt,
                    LastBackupAt = templateRecord.LastBackupAt,
                    Notes = templateRecord.Notes
                };
                string targetPath = ResolveTemplateNodePath(templateRecord.OwnerTemplateNodeId, templateNodePathMap);
                if (existing.Concat(plan.SoftwareRecords).Any(current => SoftwareRecordsEqual(current, record)))
                {
                    AddPreviewItem(
                        plan,
                        KnowledgeBaseObjectTemplateApplicationAction.Unchanged,
                        "ПО",
                        targetPath,
                        BuildTitlePathPreviewText(record.Title, record.Path, "Запись ПО уже есть."));
                    continue;
                }

                plan.SoftwareRecords.Add(record);
                AddPreviewItem(
                    plan,
                    KnowledgeBaseObjectTemplateApplicationAction.Added,
                    "ПО",
                    targetPath,
                    BuildTitlePathPreviewText(record.Title, record.Path, "Будет добавлена запись ПО."));
            }
        }

        private static void AddNetworkFileReferencePlans(
            IEnumerable<KbObjectTemplateNetworkFileReference> templateReferences,
            IEnumerable<KbNetworkFileReference>? existingReferences,
            IReadOnlyDictionary<string, string> nodeIdMap,
            IReadOnlyDictionary<string, string> templateNodePathMap,
            KnowledgeBaseObjectTemplateApplicationPlan plan)
        {
            var existing = existingReferences?.ToList() ?? new List<KbNetworkFileReference>();
            foreach (KbObjectTemplateNetworkFileReference templateReference in templateReferences)
            {
                if (!nodeIdMap.TryGetValue(templateReference.OwnerTemplateNodeId, out string? ownerNodeId))
                {
                    AddSkippedForMissingMappedNode("Файлы сети", templateReference.OwnerTemplateNodeId, templateNodePathMap, plan);
                    continue;
                }

                var reference = new KbNetworkFileReference
                {
                    OwnerNodeId = ownerNodeId,
                    Title = templateReference.Title,
                    Path = templateReference.Path,
                    PreviewKind = KnowledgeBaseNetworkPreviewService.ResolvePreviewKind(templateReference.Path)
                };
                string targetPath = ResolveTemplateNodePath(templateReference.OwnerTemplateNodeId, templateNodePathMap);
                if (existing.Concat(plan.NetworkFileReferences).Any(current => NetworkFileReferencesEqual(current, reference)))
                {
                    AddPreviewItem(
                        plan,
                        KnowledgeBaseObjectTemplateApplicationAction.Unchanged,
                        "Файлы сети",
                        targetPath,
                        BuildTitlePathPreviewText(reference.Title, reference.Path, "Файл сети уже есть."));
                    continue;
                }

                plan.NetworkFileReferences.Add(reference);
                AddPreviewItem(
                    plan,
                    KnowledgeBaseObjectTemplateApplicationAction.Added,
                    "Файлы сети",
                    targetPath,
                    BuildTitlePathPreviewText(reference.Title, reference.Path, "Будет добавлен файл сети."));
            }
        }

        private static void AddMaintenanceScheduleProfilePlans(
            IEnumerable<KbObjectTemplateMaintenanceScheduleProfile> templateProfiles,
            IEnumerable<KbMaintenanceScheduleProfile>? existingProfiles,
            IReadOnlyDictionary<string, string> nodeIdMap,
            IReadOnlyDictionary<string, string> templateNodePathMap,
            KnowledgeBaseObjectTemplateApplicationPlan plan)
        {
            var existing = existingProfiles?.ToList() ?? new List<KbMaintenanceScheduleProfile>();
            foreach (KbObjectTemplateMaintenanceScheduleProfile templateProfile in templateProfiles)
            {
                if (!nodeIdMap.TryGetValue(templateProfile.OwnerTemplateNodeId, out string? ownerNodeId))
                {
                    AddSkippedForMissingMappedNode("График ТО", templateProfile.OwnerTemplateNodeId, templateNodePathMap, plan);
                    continue;
                }

                string targetPath = ResolveTemplateNodePath(templateProfile.OwnerTemplateNodeId, templateNodePathMap);
                bool alreadyHasProfile = existing.Concat(plan.MaintenanceScheduleProfiles)
                    .Any(profile => string.Equals(profile.OwnerNodeId, ownerNodeId, StringComparison.Ordinal));
                if (alreadyHasProfile)
                {
                    AddPreviewItem(
                        plan,
                        KnowledgeBaseObjectTemplateApplicationAction.Skipped,
                        "График ТО",
                        targetPath,
                        "Профиль ТО уже есть и не будет перезаписан.");
                    continue;
                }

                var profile = new KbMaintenanceScheduleProfile
                {
                    OwnerNodeId = ownerNodeId,
                    IsIncludedInSchedule = templateProfile.IsIncludedInSchedule,
                    To1Hours = templateProfile.To1Hours,
                    To2Hours = templateProfile.To2Hours,
                    To3Hours = templateProfile.To3Hours,
                    YearScheduleEntries = templateProfile.YearScheduleEntries
                        .Select(static entry => new KbMaintenanceYearScheduleEntry
                        {
                            Month = entry.Month,
                            WorkKind = entry.WorkKind,
                            Hours = entry.Hours
                        })
                        .ToList()
                };
                plan.MaintenanceScheduleProfiles.Add(profile);
                AddPreviewItem(
                    plan,
                    KnowledgeBaseObjectTemplateApplicationAction.Added,
                    "График ТО",
                    targetPath,
                    "Будет добавлен профиль ТО.");
            }
        }

        private static void AddNetworkInterfaceStubPlans(
            IEnumerable<KbObjectTemplateNetworkInterfaceStub> stubs,
            IReadOnlyDictionary<string, string> nodeIdMap,
            IReadOnlyDictionary<string, string> templateNodePathMap,
            KnowledgeBaseObjectTemplateApplicationPlan plan)
        {
            foreach (KbObjectTemplateNetworkInterfaceStub stub in stubs)
            {
                if (!nodeIdMap.ContainsKey(stub.OwnerTemplateNodeId))
                {
                    AddSkippedForMissingMappedNode("Сетевые интерфейсы", stub.OwnerTemplateNodeId, templateNodePathMap, plan);
                    continue;
                }

                AddPreviewItem(
                    plan,
                    KnowledgeBaseObjectTemplateApplicationAction.Skipped,
                    "Сетевые интерфейсы",
                    ResolveTemplateNodePath(stub.OwnerTemplateNodeId, templateNodePathMap),
                    "Заготовка сетевого интерфейса сохранена в шаблоне, но отдельная сущность интерфейсов пока не реализована.");
            }
        }

        private static void AddSkippedTemplateSubtree(
            KbObjectTemplateNode templateNode,
            string path,
            string reason,
            Dictionary<string, string> templateNodePathMap,
            KnowledgeBaseObjectTemplateApplicationPlan plan)
        {
            CollectTemplateNodePaths(templateNode, path, templateNodePathMap);
            AddPreviewItem(
                plan,
                KnowledgeBaseObjectTemplateApplicationAction.Skipped,
                "Узел",
                path,
                reason);
        }

        private static void AddSkippedForMissingMappedNode(
            string area,
            string templateNodeId,
            IReadOnlyDictionary<string, string> templateNodePathMap,
            KnowledgeBaseObjectTemplateApplicationPlan plan)
        {
            AddPreviewItem(
                plan,
                KnowledgeBaseObjectTemplateApplicationAction.Skipped,
                area,
                ResolveTemplateNodePath(templateNodeId, templateNodePathMap),
                "Целевой узел не найден: соответствующая часть шаблона была пропущена.");
        }

        private static void AddCompositionRackPlans(
            IEnumerable<KbObjectTemplateCompositionRack> templateRacks,
            IEnumerable<KbCompositionRack>? existingRacks,
            IReadOnlyDictionary<string, string> nodeIdMap,
            IReadOnlyDictionary<string, string> templateNodePathMap,
            KnowledgeBaseObjectTemplateApplicationPlan plan)
        {
            var existing = existingRacks?.ToList() ?? new List<KbCompositionRack>();
            foreach (KbObjectTemplateCompositionRack templateRack in templateRacks)
            {
                if (!nodeIdMap.TryGetValue(templateRack.ParentTemplateNodeId, out string? parentNodeId))
                {
                    AddSkippedForMissingMappedNode("Rack состава", templateRack.ParentTemplateNodeId, templateNodePathMap, plan);
                    continue;
                }

                var rack = new KbCompositionRack
                {
                    ParentNodeId = parentNodeId,
                    RackNumber = templateRack.RackNumber,
                    SortOrder = templateRack.SortOrder,
                    RackType = templateRack.RackType,
                    Label = templateRack.Label,
                    NetworkLink = templateRack.NetworkLink,
                    Notes = templateRack.Notes,
                    Properties = templateRack.Properties
                        .Select(static property => new KbCompositionRackProperty
                        {
                            Name = property.Name,
                            Value = property.Value
                        })
                        .ToList()
                };
                string targetPath = ResolveTemplateNodePath(templateRack.ParentTemplateNodeId, templateNodePathMap);
                if (existing.Concat(plan.CompositionRacks).Any(current => CompositionRacksEqual(current, rack)))
                {
                    AddPreviewItem(
                        plan,
                        KnowledgeBaseObjectTemplateApplicationAction.Unchanged,
                        "Rack состава",
                        targetPath,
                        BuildCompositionRackPreviewText(rack, "Rack уже есть."));
                    continue;
                }

                bool hasSameRackNumber = existing.Concat(plan.CompositionRacks).Any(current =>
                    string.Equals(current.ParentNodeId, rack.ParentNodeId, StringComparison.Ordinal) &&
                    KnowledgeBaseCompositionRackSlotRulesService.NormalizeRackNumber(current.RackNumber) ==
                    KnowledgeBaseCompositionRackSlotRulesService.NormalizeRackNumber(rack.RackNumber));
                if (hasSameRackNumber)
                {
                    AddPreviewItem(
                        plan,
                        KnowledgeBaseObjectTemplateApplicationAction.Skipped,
                        "Rack состава",
                        targetPath,
                        BuildCompositionRackPreviewText(rack, "Rack с таким номером уже есть и не будет перезаписан."));
                    continue;
                }

                plan.CompositionRacks.Add(rack);
                AddPreviewItem(
                    plan,
                    KnowledgeBaseObjectTemplateApplicationAction.Added,
                    "Rack состава",
                    targetPath,
                    BuildCompositionRackPreviewText(rack, "Будет добавлен Rack."));
            }
        }

        private static void AddPreviewItem(
            KnowledgeBaseObjectTemplateApplicationPlan plan,
            KnowledgeBaseObjectTemplateApplicationAction action,
            string area,
            string target,
            string description) =>
            plan.PreviewItems.Add(new KnowledgeBaseObjectTemplateApplicationPreviewItem
            {
                Action = action,
                Area = area,
                Target = target,
                Description = description
            });

        private static void CollectTemplateNodePaths(
            KbObjectTemplateNode templateNode,
            string path,
            IDictionary<string, string> templateNodePathMap)
        {
            templateNodePathMap[templateNode.TemplateNodeId] = path;
            foreach (KbObjectTemplateNode child in templateNode.Children)
                CollectTemplateNodePaths(child, $"{path} / {BuildTemplateNodeDisplayName(child)}", templateNodePathMap);
        }

        private static bool CanAttachNodeAddition(KbNode parentNode, KbNode addition, int maxLevels) =>
            parentNode.LevelIndex + 1 + GetNodeHeight(addition) <= maxLevels;

        private static int GetNodeHeight(KbNode node)
        {
            if (node.Children.Count == 0)
                return 1;

            return 1 + node.Children.Max(GetNodeHeight);
        }

        private static int CountTemplateNodes(KbObjectTemplateNode node) =>
            1 + node.Children.Sum(CountTemplateNodes);

        private static string BuildNodeDisplayName(KbNode node)
        {
            string name = node.Name?.Trim() ?? string.Empty;
            return string.IsNullOrWhiteSpace(name) ? "(без имени)" : name;
        }

        private static string BuildTemplateNodeDisplayName(KbObjectTemplateNode node)
        {
            string name = node.Name?.Trim() ?? string.Empty;
            return string.IsNullOrWhiteSpace(name) ? "(без имени)" : name;
        }

        private static string ResolveTemplateNodePath(
            string templateNodeId,
            IReadOnlyDictionary<string, string> templateNodePathMap) =>
            templateNodePathMap.TryGetValue(templateNodeId, out string? path)
                ? path
                : "(часть шаблона без целевого узла)";

        private static string BuildCompositionPreviewText(KbCompositionEntry entry, string prefix)
        {
            string component = entry.ComponentType?.Trim() ?? string.Empty;
            string model = entry.Model?.Trim() ?? string.Empty;
            string summary = string.IsNullOrWhiteSpace(component) ? model : component;
            if (!string.IsNullOrWhiteSpace(model) && !string.Equals(component, model, StringComparison.CurrentCulture))
                summary = string.IsNullOrWhiteSpace(summary) ? model : $"{summary}: {model}";

            if (string.IsNullOrWhiteSpace(summary))
                return prefix;

            return entry.SlotNumber.HasValue
                ? $"{prefix} {KnowledgeBaseCompositionRackSlotRulesService.FormatRackText(entry.RackNumber)} / {summary}"
                : $"{prefix} {summary}";
        }

        private static string BuildCompositionRackPreviewText(KbCompositionRack rack, string prefix)
        {
            string title = KnowledgeBaseCompositionRackSlotRulesService.FormatRackTitle(
                rack.RackNumber,
                rack.RackType,
                rack.Label);
            return $"{prefix} {title}";
        }

        private static string BuildTitlePathPreviewText(string? title, string? path, string prefix)
        {
            string normalizedTitle = title?.Trim() ?? string.Empty;
            string normalizedPath = path?.Trim() ?? string.Empty;
            string summary = string.IsNullOrWhiteSpace(normalizedTitle) ? normalizedPath : normalizedTitle;
            return string.IsNullOrWhiteSpace(summary)
                ? prefix
                : $"{prefix} {summary}";
        }

        private static bool NamesEqual(string? first, string? second) =>
            string.Equals(first?.Trim() ?? string.Empty, second?.Trim() ?? string.Empty, StringComparison.CurrentCultureIgnoreCase);

        private static bool TextEqual(string? first, string? second) =>
            string.Equals(first?.Trim() ?? string.Empty, second?.Trim() ?? string.Empty, StringComparison.CurrentCulture);

        private static bool CompositionEntriesEqual(KbCompositionEntry first, KbCompositionEntry second) =>
            string.Equals(first.ParentNodeId, second.ParentNodeId, StringComparison.Ordinal) &&
            first.RackNumber == second.RackNumber &&
            first.SlotNumber == second.SlotNumber &&
            first.PositionOrder == second.PositionOrder &&
            TextEqual(first.ComponentType, second.ComponentType) &&
            TextEqual(first.Model, second.Model) &&
            TextEqual(first.OrderNumber, second.OrderNumber) &&
            TextEqual(first.Firmware, second.Firmware) &&
            TextEqual(first.MpiDpPnAddress, second.MpiDpPnAddress) &&
            TextEqual(first.InputAddress, second.InputAddress) &&
            TextEqual(first.OutputAddress, second.OutputAddress) &&
            TextEqual(first.Comment, second.Comment) &&
            TextEqual(first.InterfaceRows, second.InterfaceRows) &&
            TextEqual(first.IpAddress, second.IpAddress) &&
            first.LastCalibrationAt == second.LastCalibrationAt &&
            first.NextCalibrationAt == second.NextCalibrationAt &&
            TextEqual(first.Notes, second.Notes);

        private static bool CompositionRacksEqual(KbCompositionRack first, KbCompositionRack second) =>
            string.Equals(first.ParentNodeId, second.ParentNodeId, StringComparison.Ordinal) &&
            KnowledgeBaseCompositionRackSlotRulesService.NormalizeRackNumber(first.RackNumber) ==
            KnowledgeBaseCompositionRackSlotRulesService.NormalizeRackNumber(second.RackNumber) &&
            first.SortOrder == second.SortOrder &&
            TextEqual(first.RackType, second.RackType) &&
            TextEqual(first.Label, second.Label) &&
            TextEqual(first.NetworkLink, second.NetworkLink) &&
            TextEqual(first.Notes, second.Notes);

        private static bool DocumentLinksEqual(KbDocumentLink first, KbDocumentLink second) =>
            string.Equals(first.OwnerNodeId, second.OwnerNodeId, StringComparison.Ordinal) &&
            first.Kind == second.Kind &&
            TextEqual(first.Title, second.Title) &&
            TextEqual(first.Path, second.Path) &&
            first.UpdatedAt == second.UpdatedAt;

        private static bool SoftwareRecordsEqual(KbSoftwareRecord first, KbSoftwareRecord second) =>
            string.Equals(first.OwnerNodeId, second.OwnerNodeId, StringComparison.Ordinal) &&
            TextEqual(first.Title, second.Title) &&
            TextEqual(first.Path, second.Path) &&
            first.AddedAt == second.AddedAt &&
            first.LastChangedAt == second.LastChangedAt &&
            first.LastBackupAt == second.LastBackupAt &&
            TextEqual(first.Notes, second.Notes);

        private static bool NetworkFileReferencesEqual(KbNetworkFileReference first, KbNetworkFileReference second) =>
            string.Equals(first.OwnerNodeId, second.OwnerNodeId, StringComparison.Ordinal) &&
            TextEqual(first.Title, second.Title) &&
            TextEqual(first.Path, second.Path);

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
                    RackNumber = entry.RackNumber,
                    SlotNumber = entry.SlotNumber,
                    PositionOrder = entry.PositionOrder,
                    ComponentType = entry.ComponentType,
                    Model = entry.Model,
                    OrderNumber = entry.OrderNumber,
                    Firmware = entry.Firmware,
                    MpiDpPnAddress = entry.MpiDpPnAddress,
                    InputAddress = entry.InputAddress,
                    OutputAddress = entry.OutputAddress,
                    Comment = entry.Comment,
                    InterfaceRows = entry.InterfaceRows,
                    IpAddress = entry.IpAddress,
                    LastCalibrationAt = entry.LastCalibrationAt,
                    NextCalibrationAt = entry.NextCalibrationAt,
                    Notes = entry.Notes
                });
            }

            return result;
        }

        private static List<KbObjectTemplateCompositionRack> CreateTemplateCompositionRacks(
            IEnumerable<KbCompositionRack>? racks,
            IReadOnlyDictionary<string, string> nodeIdMap)
        {
            var result = new List<KbObjectTemplateCompositionRack>();
            foreach (KbCompositionRack rack in racks ?? Enumerable.Empty<KbCompositionRack>())
            {
                if (!nodeIdMap.TryGetValue(rack.ParentNodeId, out string? parentTemplateNodeId))
                    continue;

                result.Add(new KbObjectTemplateCompositionRack
                {
                    ParentTemplateNodeId = parentTemplateNodeId,
                    RackNumber = rack.RackNumber,
                    SortOrder = rack.SortOrder,
                    RackType = rack.RackType,
                    Label = rack.Label,
                    NetworkLink = rack.NetworkLink,
                    Notes = rack.Notes,
                    Properties = rack.Properties
                        .Select(static property => new KbCompositionRackProperty
                        {
                            Name = property.Name,
                            Value = property.Value
                        })
                        .ToList()
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
                            WorkKind = entry.WorkKind,
                            Hours = entry.Hours
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
                    RackNumber = entry.RackNumber,
                    SlotNumber = entry.SlotNumber,
                    PositionOrder = entry.PositionOrder,
                    ComponentType = entry.ComponentType,
                    Model = entry.Model,
                    OrderNumber = entry.OrderNumber,
                    Firmware = entry.Firmware,
                    MpiDpPnAddress = entry.MpiDpPnAddress,
                    InputAddress = entry.InputAddress,
                    OutputAddress = entry.OutputAddress,
                    Comment = entry.Comment,
                    InterfaceRows = entry.InterfaceRows,
                    IpAddress = entry.IpAddress,
                    LastCalibrationAt = entry.LastCalibrationAt,
                    NextCalibrationAt = entry.NextCalibrationAt,
                    Notes = entry.Notes
                });
            }

            return result;
        }

        private static List<KbCompositionRack> CreateCompositionRacks(
            IEnumerable<KbObjectTemplateCompositionRack> racks,
            IReadOnlyDictionary<string, string> nodeIdMap)
        {
            var result = new List<KbCompositionRack>();
            foreach (KbObjectTemplateCompositionRack rack in racks)
            {
                if (!nodeIdMap.TryGetValue(rack.ParentTemplateNodeId, out string? parentNodeId))
                    continue;

                result.Add(new KbCompositionRack
                {
                    ParentNodeId = parentNodeId,
                    RackNumber = rack.RackNumber,
                    SortOrder = rack.SortOrder,
                    RackType = rack.RackType,
                    Label = rack.Label,
                    NetworkLink = rack.NetworkLink,
                    Notes = rack.Notes,
                    Properties = rack.Properties
                        .Select(static property => new KbCompositionRackProperty
                        {
                            Name = property.Name,
                            Value = property.Value
                        })
                        .ToList()
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
                            WorkKind = entry.WorkKind,
                            Hours = entry.Hours
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

        private static KnowledgeBaseObjectTemplateApplicationPlan ApplicationFailure(string errorMessage) =>
            new()
            {
                IsSuccess = false,
                ErrorMessage = errorMessage
            };
    }
}
