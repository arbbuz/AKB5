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

            Config = KnowledgeBaseDataService.NormalizeConfig(data.Config);
            Workshops = KnowledgeBaseDataService.NormalizeWorkshops(data.Workshops);

            ReindexAllWorkshops();
            CurrentWorkshop = KnowledgeBaseDataService.ResolveWorkshop(Workshops, data.LastWorkshop);

            if (recordAsSavedState)
                RecordSavedState(GetCurrentWorkshopNodes());
            else
                RefreshDirtyState(GetCurrentWorkshopNodes());
        }

        public SavedData CreateSaveData(List<KbNode> currentWorkshopRoots)
        {
            SyncCurrentWorkshop(currentWorkshopRoots);
            return new SavedData
            {
                Config = Config,
                Workshops = Workshops,
                LastWorkshop = CurrentWorkshop
            };
        }

        public string SerializeSnapshot(List<KbNode> currentWorkshopRoots, bool includeCurrentWorkshop)
        {
            SyncCurrentWorkshop(currentWorkshopRoots);
            return KnowledgeBaseDataService.SerializeSnapshot(
                Config,
                Workshops,
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
                    Name = normalizedWorkshop,
                    LevelIndex = 0
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

            RenameTechnicalWrapperIfNeeded(resolvedCurrentWorkshop, normalizedWorkshop, roots);
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
            string currentWorkshop,
            string renamedWorkshop,
            List<KbNode> roots)
        {
            if (roots.Count != 1)
                return;

            var root = roots[0];
            if (root.LevelIndex != 0)
                return;

            if (!KnowledgeBaseDataService.WorkshopNamesEqual(root.Name, currentWorkshop))
                return;

            root.Name = renamedWorkshop;
        }

        private void ReindexAllWorkshops()
        {
            var service = new KnowledgeBaseService(Config, Workshops);
            foreach (var roots in Workshops.Values)
            {
                foreach (var root in roots)
                    service.ReindexSubtree(root, 0);
            }
        }
    }
}
