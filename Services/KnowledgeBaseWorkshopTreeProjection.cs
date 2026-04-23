using AsutpKnowledgeBase.Models;

namespace AsutpKnowledgeBase.Services
{
    public sealed class KnowledgeBaseWorkshopTreeProjection
    {
        private KnowledgeBaseWorkshopTreeProjection(
            string workshopName,
            IReadOnlyList<KbNode> persistedRoots,
            IReadOnlyList<KbNode> visibleRoots,
            KbNode? hiddenWrapperRoot,
            bool hiddenWrapperIsVirtual)
        {
            WorkshopName = workshopName;
            PersistedRoots = persistedRoots;
            VisibleRoots = visibleRoots;
            HiddenWrapperRoot = hiddenWrapperRoot;
            HiddenWrapperIsVirtual = hiddenWrapperIsVirtual;
        }

        public string WorkshopName { get; }

        public IReadOnlyList<KbNode> PersistedRoots { get; }

        public IReadOnlyList<KbNode> VisibleRoots { get; }

        public KbNode? HiddenWrapperRoot { get; }

        public bool HasHiddenWrapper => HiddenWrapperRoot != null;

        private bool HiddenWrapperIsVirtual { get; }

        public static KnowledgeBaseWorkshopTreeProjection Create(
            string workshopName,
            IReadOnlyList<KbNode>? persistedRoots)
        {
            var roots = persistedRoots ?? Array.Empty<KbNode>();
            if (TryGetHiddenWrapperRoot(roots, out var hiddenWrapperRoot))
            {
                return new KnowledgeBaseWorkshopTreeProjection(
                    workshopName,
                    roots,
                    hiddenWrapperRoot.Children,
                    hiddenWrapperRoot,
                    hiddenWrapperIsVirtual: false);
            }

            if (roots.Count == 0 && !string.IsNullOrWhiteSpace(workshopName))
            {
                var virtualHiddenWrapperRoot = CreateVirtualHiddenWrapperRoot(workshopName);
                return new KnowledgeBaseWorkshopTreeProjection(
                    workshopName,
                    roots,
                    virtualHiddenWrapperRoot.Children,
                    virtualHiddenWrapperRoot,
                    hiddenWrapperIsVirtual: true);
            }

            return new KnowledgeBaseWorkshopTreeProjection(
                workshopName,
                roots,
                roots,
                hiddenWrapperRoot: null,
                hiddenWrapperIsVirtual: false);
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

            if (HiddenWrapperRoot.Children.Count == 0 && HiddenWrapperIsVirtual)
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
            IReadOnlyList<KbNode> persistedRoots,
            out KbNode hiddenWrapperRoot)
        {
            hiddenWrapperRoot = null!;

            if (persistedRoots.Count != 1)
                return false;

            var candidate = persistedRoots[0];
            if (candidate.LevelIndex != 0)
                return false;

            if (candidate.NodeType != KbNodeType.WorkshopRoot)
                return false;

            hiddenWrapperRoot = candidate;
            return true;
        }

        private static KbNode CreateVirtualHiddenWrapperRoot(string workshopName) =>
            new()
            {
                NodeId = KnowledgeBaseNodeMetadataService.CreateNewNodeId(),
                Name = workshopName.Trim(),
                LevelIndex = 0,
                NodeType = KbNodeType.WorkshopRoot
            };
    }
}
