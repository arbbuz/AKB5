using AsutpKnowledgeBase.Models;

namespace AsutpKnowledgeBase.Services
{
    /// <summary>
    /// Хранит прикладное состояние текущей сессии без UI-зависимостей:
    /// конфигурацию, набор цехов, выбранный цех и dirty/save-состояние.
    /// </summary>
    public class KnowledgeBaseSessionService
    {
        private string _lastSavedDirtySnapshot = string.Empty;

        public KbConfig Config { get; private set; } = KnowledgeBaseDataService.CreateDefaultConfig();

        public Dictionary<string, List<KbNode>> Workshops { get; private set; } =
            new(KnowledgeBaseDataService.WorkshopNameComparer);

        public List<KbCompositionEntry> CompositionEntries { get; private set; } = new();

        public List<KbDocumentLink> DocumentLinks { get; private set; } = new();

        public List<KbSoftwareRecord> SoftwareRecords { get; private set; } = new();

        public List<KbNetworkFileReference> NetworkFileReferences { get; private set; } = new();

        public List<KbMaintenanceScheduleProfile> MaintenanceScheduleProfiles { get; private set; } = new();

        public string CurrentWorkshop { get; private set; } = string.Empty;

        public string LastSavedWorkshop { get; private set; } = string.Empty;

        public bool IsDirty { get; private set; }

        public bool RequiresSave { get; private set; }

        public void InitializeDefaultData(bool recordAsSavedState) =>
            ApplyLoadedData(KnowledgeBaseDataService.CreateDefaultData(), recordAsSavedState);

        public void ApplyLoadedData(SavedData data, bool recordAsSavedState)
        {
            string? schemaVersionError = KnowledgeBaseDataService.ValidateSupportedSchemaVersion(data.SchemaVersion);
            if (schemaVersionError != null)
                throw new InvalidOperationException(schemaVersionError);

            string? workshopValidationError = KnowledgeBaseDataService.ValidateWorkshopNames(data.Workshops);
            if (workshopValidationError != null)
                throw new InvalidOperationException(workshopValidationError);

            var normalizedData = KnowledgeBaseDataService.NormalizeSavedData(data);
            Config = normalizedData.Config;
            Workshops = normalizedData.Workshops;
            CompositionEntries = normalizedData.CompositionEntries;
            DocumentLinks = normalizedData.DocumentLinks;
            SoftwareRecords = normalizedData.SoftwareRecords;
            NetworkFileReferences = normalizedData.NetworkFileReferences;
            MaintenanceScheduleProfiles = normalizedData.MaintenanceScheduleProfiles;
            CurrentWorkshop = normalizedData.LastWorkshop;

            if (recordAsSavedState)
                RecordSavedState(GetCurrentWorkshopNodes());
            else
                RefreshDirtyState(GetCurrentWorkshopNodes());
        }

        public SavedData CreateSaveData(List<KbNode> currentWorkshopRoots)
        {
            SyncCurrentWorkshop(currentWorkshopRoots);
            return KnowledgeBaseDataService.NormalizeSavedData(new SavedData
            {
                SchemaVersion = SavedData.CurrentSchemaVersion,
                Config = Config,
                Workshops = Workshops,
                CompositionEntries = CompositionEntries,
                DocumentLinks = DocumentLinks,
                SoftwareRecords = SoftwareRecords,
                NetworkFileReferences = NetworkFileReferences,
                MaintenanceScheduleProfiles = MaintenanceScheduleProfiles,
                LastWorkshop = CurrentWorkshop
            });
        }

        public string SerializeSnapshot(List<KbNode> currentWorkshopRoots, bool includeCurrentWorkshop)
        {
            SyncCurrentWorkshop(currentWorkshopRoots);
            return KnowledgeBaseDataService.SerializeSnapshot(
                Config,
                Workshops,
                CompositionEntries,
                DocumentLinks,
                SoftwareRecords,
                NetworkFileReferences,
                MaintenanceScheduleProfiles,
                CurrentWorkshop,
                includeCurrentWorkshop);
        }

        public void RecordSavedState(List<KbNode> currentWorkshopRoots)
        {
            _lastSavedDirtySnapshot = SerializeSnapshot(currentWorkshopRoots, includeCurrentWorkshop: false);
            LastSavedWorkshop = CurrentWorkshop;
            IsDirty = false;
            RequiresSave = false;
        }

        public void RefreshDirtyState(List<KbNode> currentWorkshopRoots)
        {
            IsDirty = SerializeSnapshot(currentWorkshopRoots, includeCurrentWorkshop: false) != _lastSavedDirtySnapshot;
        }

        public void SyncCurrentWorkshop(List<KbNode> currentWorkshopRoots)
        {
            if (string.IsNullOrWhiteSpace(CurrentWorkshop))
                return;

            Workshops[CurrentWorkshop] = currentWorkshopRoots;
        }

        public List<KbNode> GetCurrentWorkshopNodes()
        {
            if (string.IsNullOrWhiteSpace(CurrentWorkshop) || !Workshops.ContainsKey(CurrentWorkshop))
                CurrentWorkshop = KnowledgeBaseDataService.ResolveWorkshop(Workshops, null);

            return Workshops.TryGetValue(CurrentWorkshop, out var nodes)
                ? nodes
                : new List<KbNode>();
        }

        public bool TrySelectWorkshop(string selectedWorkshop, List<KbNode> currentWorkshopRoots)
        {
            string? resolvedWorkshop = KnowledgeBaseDataService.FindWorkshopName(Workshops.Keys, selectedWorkshop);
            if (string.IsNullOrWhiteSpace(resolvedWorkshop))
                return false;

            if (KnowledgeBaseDataService.WorkshopNamesEqual(resolvedWorkshop, CurrentWorkshop))
                return false;

            SyncCurrentWorkshop(currentWorkshopRoots);
            CurrentWorkshop = resolvedWorkshop;
            return true;
        }

        public bool HasWorkshop(string workshopName) =>
            KnowledgeBaseDataService.FindWorkshopName(Workshops.Keys, workshopName) != null;

        public bool TryAddWorkshop(string workshopName, List<KbNode> currentWorkshopRoots)
        {
            string normalizedWorkshop = KnowledgeBaseDataService.NormalizeWorkshopName(workshopName);
            if (string.IsNullOrWhiteSpace(normalizedWorkshop))
                return false;

            if (HasWorkshop(normalizedWorkshop))
                return false;

            SyncCurrentWorkshop(currentWorkshopRoots);
            Workshops[normalizedWorkshop] =
            [
                new KbNode
                {
                    NodeId = KnowledgeBaseNodeMetadataService.CreateNewNodeId(),
                    Name = normalizedWorkshop,
                    LevelIndex = 0,
                    NodeType = KbNodeType.WorkshopRoot
                }
            ];
            CurrentWorkshop = normalizedWorkshop;
            return true;
        }

        public bool TryRenameCurrentWorkshop(string workshopName, List<KbNode> currentWorkshopRoots)
        {
            string? resolvedCurrentWorkshop = KnowledgeBaseDataService.FindWorkshopName(Workshops.Keys, CurrentWorkshop);
            string normalizedWorkshop = KnowledgeBaseDataService.NormalizeWorkshopName(workshopName);
            if (string.IsNullOrWhiteSpace(resolvedCurrentWorkshop) || string.IsNullOrWhiteSpace(normalizedWorkshop))
                return false;

            string? existingWorkshop = KnowledgeBaseDataService.FindWorkshopName(Workshops.Keys, normalizedWorkshop);
            if (!string.IsNullOrWhiteSpace(existingWorkshop) &&
                !KnowledgeBaseDataService.WorkshopNamesEqual(existingWorkshop, resolvedCurrentWorkshop))
            {
                return false;
            }

            SyncCurrentWorkshop(currentWorkshopRoots);
            if (!Workshops.TryGetValue(resolvedCurrentWorkshop, out var roots))
                return false;

            RenameTechnicalWrapperIfNeeded(normalizedWorkshop, roots);
            Workshops = ReplaceWorkshopKey(resolvedCurrentWorkshop, normalizedWorkshop);
            CurrentWorkshop = normalizedWorkshop;
            return true;
        }

        public bool TryDeleteCurrentWorkshop(List<KbNode> currentWorkshopRoots)
        {
            string? resolvedCurrentWorkshop = KnowledgeBaseDataService.FindWorkshopName(Workshops.Keys, CurrentWorkshop);
            if (string.IsNullOrWhiteSpace(resolvedCurrentWorkshop) || Workshops.Count <= 1)
                return false;

            SyncCurrentWorkshop(currentWorkshopRoots);

            var remainingWorkshops = new Dictionary<string, List<KbNode>>(KnowledgeBaseDataService.WorkshopNameComparer);
            foreach (var pair in Workshops)
            {
                if (KnowledgeBaseDataService.WorkshopNamesEqual(pair.Key, resolvedCurrentWorkshop))
                    continue;

                remainingWorkshops[pair.Key] = pair.Value;
            }

            if (remainingWorkshops.Count == 0)
                return false;

            Workshops = remainingWorkshops;
            CurrentWorkshop = KnowledgeBaseDataService.ResolveWorkshop(Workshops, preferredWorkshop: null);
            return true;
        }

        public void UpdateConfig(KbConfig config) => Config = config;

        public void ReplaceCompositionEntries(IEnumerable<KbCompositionEntry> compositionEntries) =>
            CompositionEntries = KnowledgeBaseDataService.NormalizeSavedData(new SavedData
            {
                SchemaVersion = SavedData.CurrentSchemaVersion,
                Config = Config,
                Workshops = Workshops,
                CompositionEntries = compositionEntries?.ToList() ?? new List<KbCompositionEntry>(),
                DocumentLinks = DocumentLinks,
                SoftwareRecords = SoftwareRecords,
                NetworkFileReferences = NetworkFileReferences,
                MaintenanceScheduleProfiles = MaintenanceScheduleProfiles,
                LastWorkshop = CurrentWorkshop
            }).CompositionEntries;

        public void ReplaceDocumentLinks(IEnumerable<KbDocumentLink> documentLinks) =>
            DocumentLinks = KnowledgeBaseDataService.NormalizeSavedData(new SavedData
            {
                SchemaVersion = SavedData.CurrentSchemaVersion,
                Config = Config,
                Workshops = Workshops,
                CompositionEntries = CompositionEntries,
                DocumentLinks = documentLinks?.ToList() ?? new List<KbDocumentLink>(),
                SoftwareRecords = SoftwareRecords,
                NetworkFileReferences = NetworkFileReferences,
                MaintenanceScheduleProfiles = MaintenanceScheduleProfiles,
                LastWorkshop = CurrentWorkshop
            }).DocumentLinks;

        public void ReplaceSoftwareRecords(IEnumerable<KbSoftwareRecord> softwareRecords) =>
            SoftwareRecords = KnowledgeBaseDataService.NormalizeSavedData(new SavedData
            {
                SchemaVersion = SavedData.CurrentSchemaVersion,
                Config = Config,
                Workshops = Workshops,
                CompositionEntries = CompositionEntries,
                DocumentLinks = DocumentLinks,
                SoftwareRecords = softwareRecords?.ToList() ?? new List<KbSoftwareRecord>(),
                NetworkFileReferences = NetworkFileReferences,
                MaintenanceScheduleProfiles = MaintenanceScheduleProfiles,
                LastWorkshop = CurrentWorkshop
            }).SoftwareRecords;

        public void ReplaceNetworkFileReferences(IEnumerable<KbNetworkFileReference> networkFileReferences) =>
            NetworkFileReferences = KnowledgeBaseDataService.NormalizeSavedData(new SavedData
            {
                SchemaVersion = SavedData.CurrentSchemaVersion,
                Config = Config,
                Workshops = Workshops,
                CompositionEntries = CompositionEntries,
                DocumentLinks = DocumentLinks,
                SoftwareRecords = SoftwareRecords,
                NetworkFileReferences = networkFileReferences?.ToList() ?? new List<KbNetworkFileReference>(),
                MaintenanceScheduleProfiles = MaintenanceScheduleProfiles,
                LastWorkshop = CurrentWorkshop
            }).NetworkFileReferences;

        public void ReplaceMaintenanceScheduleProfiles(IEnumerable<KbMaintenanceScheduleProfile> maintenanceScheduleProfiles) =>
            MaintenanceScheduleProfiles = KnowledgeBaseDataService.NormalizeSavedData(new SavedData
            {
                SchemaVersion = SavedData.CurrentSchemaVersion,
                Config = Config,
                Workshops = Workshops,
                CompositionEntries = CompositionEntries,
                DocumentLinks = DocumentLinks,
                SoftwareRecords = SoftwareRecords,
                NetworkFileReferences = NetworkFileReferences,
                MaintenanceScheduleProfiles = maintenanceScheduleProfiles?.ToList() ?? new List<KbMaintenanceScheduleProfile>(),
                LastWorkshop = CurrentWorkshop
            }).MaintenanceScheduleProfiles;

        public void SetRequiresSave(bool requiresSave) => RequiresSave = requiresSave;

        private Dictionary<string, List<KbNode>> ReplaceWorkshopKey(string existingWorkshop, string renamedWorkshop)
        {
            var renamedWorkshops = new Dictionary<string, List<KbNode>>(KnowledgeBaseDataService.WorkshopNameComparer);
            foreach (var pair in Workshops)
            {
                if (KnowledgeBaseDataService.WorkshopNamesEqual(pair.Key, existingWorkshop))
                    renamedWorkshops[renamedWorkshop] = pair.Value;
                else
                    renamedWorkshops[pair.Key] = pair.Value;
            }

            return renamedWorkshops;
        }

        private static void RenameTechnicalWrapperIfNeeded(
            string renamedWorkshop,
            List<KbNode> roots)
        {
            if (roots.Count != 1)
                return;

            var root = roots[0];
            if (root.NodeType != KbNodeType.WorkshopRoot)
                return;

            root.Name = renamedWorkshop;
        }
    }
}
