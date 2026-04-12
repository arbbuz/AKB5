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

        public Dictionary<string, List<KbNode>> Workshops { get; private set; } = new();

        public string CurrentWorkshop { get; private set; } = string.Empty;

        public string LastSavedWorkshop { get; private set; } = string.Empty;

        public bool IsDirty { get; private set; }

        public bool RequiresSave { get; private set; }

        public void InitializeDefaultData(bool recordAsSavedState) =>
            ApplyLoadedData(KnowledgeBaseDataService.CreateDefaultData(), recordAsSavedState);

        public void ApplyLoadedData(SavedData data, bool recordAsSavedState)
        {
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
            if (string.IsNullOrWhiteSpace(selectedWorkshop))
                return false;

            string normalizedWorkshop = selectedWorkshop.Trim();
            if (normalizedWorkshop == CurrentWorkshop || !Workshops.ContainsKey(normalizedWorkshop))
                return false;

            SyncCurrentWorkshop(currentWorkshopRoots);
            CurrentWorkshop = normalizedWorkshop;
            return true;
        }

        public bool HasWorkshop(string workshopName) =>
            Workshops.Keys.Any(existing =>
                string.Equals(existing, workshopName.Trim(), StringComparison.CurrentCultureIgnoreCase));

        public bool TryAddWorkshop(string workshopName, List<KbNode> currentWorkshopRoots)
        {
            if (string.IsNullOrWhiteSpace(workshopName))
                return false;

            string normalizedWorkshop = workshopName.Trim();
            if (HasWorkshop(normalizedWorkshop))
                return false;

            SyncCurrentWorkshop(currentWorkshopRoots);
            Workshops[normalizedWorkshop] = new List<KbNode>();
            CurrentWorkshop = normalizedWorkshop;
            return true;
        }

        public void UpdateConfig(KbConfig config) => Config = config;

        public void SetRequiresSave(bool requiresSave) => RequiresSave = requiresSave;

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
