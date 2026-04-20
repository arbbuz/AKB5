using AsutpKnowledgeBase.Models;

namespace AsutpKnowledgeBase.Services
{
    /// <summary>
    /// Представляет выбранный цех в двух видах:
    /// persisted roots для session/save и visible roots для TreeView.
    /// Скрывает техническую обёртку-цех только в безопасном случае.
    /// </summary>
    public sealed class KnowledgeBaseWorkshopTreeProjection
    {
        private KnowledgeBaseWorkshopTreeProjection(
            string workshopName,
            IReadOnlyList<KbNode> persistedRoots,
            IReadOnlyList<KbNode> visibleRoots,
            KbNode? hiddenWrapperRoot)
        {
            WorkshopName = workshopName;
            PersistedRoots = persistedRoots;
            VisibleRoots = visibleRoots;
            HiddenWrapperRoot = hiddenWrapperRoot;
        }

        public string WorkshopName { get; }

        public IReadOnlyList<KbNode> PersistedRoots { get; }

        public IReadOnlyList<KbNode> VisibleRoots { get; }

        public KbNode? HiddenWrapperRoot { get; }

        public bool HasHiddenWrapper => HiddenWrapperRoot != null;

        public static KnowledgeBaseWorkshopTreeProjection Create(
            string workshopName,
            IReadOnlyList<KbNode>? persistedRoots)
        {
            var roots = persistedRoots ?? Array.Empty<KbNode>();
            if (roots.Count == 0 && !string.IsNullOrWhiteSpace(workshopName))
            {
                var virtualHiddenWrapperRoot = CreateVirtualHiddenWrapperRoot(workshopName);
                return new KnowledgeBaseWorkshopTreeProjection(
                    workshopName,
                    roots,
                    virtualHiddenWrapperRoot.Children,
                    virtualHiddenWrapperRoot);
            }

            if (TryGetHiddenWrapperRoot(workshopName, roots, out var hiddenWrapperRoot))
            {
                return new KnowledgeBaseWorkshopTreeProjection(
                    workshopName,
                    roots,
                    hiddenWrapperRoot.Children,
                    hiddenWrapperRoot);
            }

            return new KnowledgeBaseWorkshopTreeProjection(
                workshopName,
                roots,
                roots,
                hiddenWrapperRoot: null);
        }

        public KbNode? GetEffectiveParentForRootOperations() => HiddenWrapperRoot;

        public KbNode? ResolveActualParent(KbNode node, KbNode? visibleParentNode)
        {
            if (visibleParentNode != null)
                return visibleParentNode;

            return IsVisibleRoot(node) ? HiddenWrapperRoot : null;
        }

        public List<KbNode> CreatePersistedRootsSnapshot(IEnumerable<KbNode> visibleRoots)
        {
            var visibleRootList = visibleRoots.ToList();
            if (HiddenWrapperRoot == null)
                return visibleRootList;

            HiddenWrapperRoot.Children.Clear();
            HiddenWrapperRoot.Children.AddRange(visibleRootList);

            if (HiddenWrapperRoot.Children.Count == 0)
                return new List<KbNode>();

            return new List<KbNode> { HiddenWrapperRoot };
        }

        private bool IsVisibleRoot(KbNode node)
        {
            foreach (var visibleRoot in VisibleRoots)
            {
                if (ReferenceEquals(visibleRoot, node))
                    return true;
            }

            return false;
        }

        private static bool TryGetHiddenWrapperRoot(
            string workshopName,
            IReadOnlyList<KbNode> persistedRoots,
            out KbNode hiddenWrapperRoot)
        {
            hiddenWrapperRoot = null!;

            if (persistedRoots.Count != 1)
                return false;

            var candidate = persistedRoots[0];
            if (!KnowledgeBaseDataService.WorkshopNamesEqual(candidate.Name, workshopName))
                return false;

            if (candidate.LevelIndex != 0)
                return false;

            if (!IsDetailsEmpty(candidate.Details))
                return false;

            hiddenWrapperRoot = candidate;
            return true;
        }

        private static KbNode CreateVirtualHiddenWrapperRoot(string workshopName) =>
            new()
            {
                Name = workshopName.Trim(),
                LevelIndex = 0
            };

        private static bool IsDetailsEmpty(KbNodeDetails? details) =>
            details == null ||
            (string.IsNullOrWhiteSpace(details.Description) &&
             string.IsNullOrWhiteSpace(details.Location) &&
             string.IsNullOrWhiteSpace(details.PhotoPath) &&
             string.IsNullOrWhiteSpace(details.IpAddress) &&
             string.IsNullOrWhiteSpace(details.SchemaLink));
    }
}
