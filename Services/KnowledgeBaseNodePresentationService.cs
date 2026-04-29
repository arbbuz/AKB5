using AsutpKnowledgeBase.Models;

namespace AsutpKnowledgeBase.Services
{
    /// <summary>
    /// Собирает display-friendly сведения о KbNode без UI-зависимостей.
    /// </summary>
    public class KnowledgeBaseNodePresentationService
    {
        public string GetLevelName(KbConfig config, int levelIndex) =>
            config.LevelNames.Count > levelIndex
                ? config.LevelNames[levelIndex]
                : $"Ур. {levelIndex + 1}";

        public int GetVisibleLevel(IReadOnlyList<KbNode> roots, KbNode selectedNode)
        {
            if (TryGetVisibleLevel(roots, selectedNode, currentLevel: 1, out int visibleLevel))
                return visibleLevel;

            return Math.Max(1, selectedNode.LevelIndex);
        }

        public string BuildNodePath(IReadOnlyList<KbNode> roots, KbNode selectedNode)
        {
            var pathSegments = new List<string>();
            if (TryBuildNodePath(roots, selectedNode, pathSegments))
                return string.Join(" / ", pathSegments);

            return selectedNode.Name;
        }

        private static bool TryBuildNodePath(
            IEnumerable<KbNode> nodes,
            KbNode selectedNode,
            IList<string> pathSegments)
        {
            foreach (var node in nodes)
            {
                pathSegments.Add(node.Name);
                if (ReferenceEquals(node, selectedNode))
                    return true;

                if (TryBuildNodePath(node.Children, selectedNode, pathSegments))
                    return true;

                pathSegments.RemoveAt(pathSegments.Count - 1);
            }

            return false;
        }

        private static bool TryGetVisibleLevel(
            IEnumerable<KbNode> nodes,
            KbNode selectedNode,
            int currentLevel,
            out int visibleLevel)
        {
            foreach (var node in nodes)
            {
                if (ReferenceEquals(node, selectedNode))
                {
                    visibleLevel = currentLevel;
                    return true;
                }

                if (TryGetVisibleLevel(node.Children, selectedNode, currentLevel + 1, out visibleLevel))
                    return true;
            }

            visibleLevel = 0;
            return false;
        }
    }
}
