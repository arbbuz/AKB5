using AsutpKnowledgeBase.Models;

namespace AsutpKnowledgeBase.Services
{
    public class KnowledgeBaseTreeSearchMatch
    {
        public KbNode Node { get; init; } = null!;

        public string SearchText { get; init; } = string.Empty;

        public string MatchFieldLabel { get; init; } = string.Empty;

        public string MatchValue { get; init; } = string.Empty;

        public string NodePath { get; init; } = string.Empty;
    }

    /// <summary>
    /// Ищет совпадения в дереве по имени узла, полному пути и имени уровня.
    /// </summary>
    public class KnowledgeBaseTreeSearchService
    {
        private readonly KnowledgeBaseNodePresentationService _nodePresentationService = new();

        public IReadOnlyList<KnowledgeBaseTreeSearchMatch> FindMatches(
            IReadOnlyList<KbNode> roots,
            KbConfig config,
            string searchText)
        {
            string normalizedSearch = searchText.Trim();
            if (string.IsNullOrWhiteSpace(normalizedSearch))
                return Array.Empty<KnowledgeBaseTreeSearchMatch>();

            var matches = new List<KnowledgeBaseTreeSearchMatch>();
            var pathSegments = new List<string>();

            foreach (var root in roots)
                CollectMatches(root, config, normalizedSearch, pathSegments, matches);

            return matches;
        }

        private void CollectMatches(
            KbNode node,
            KbConfig config,
            string normalizedSearch,
            IList<string> pathSegments,
            ICollection<KnowledgeBaseTreeSearchMatch> matches)
        {
            pathSegments.Add(node.Name);
            string nodePath = string.Join(" / ", pathSegments);
            string levelName = _nodePresentationService.GetLevelName(config, node.LevelIndex);

            if (TryCreateMatch(node, normalizedSearch, levelName, nodePath, out var match))
                matches.Add(match);

            foreach (var child in node.Children)
                CollectMatches(child, config, normalizedSearch, pathSegments, matches);

            pathSegments.RemoveAt(pathSegments.Count - 1);
        }

        private static bool TryCreateMatch(
            KbNode node,
            string searchText,
            string levelName,
            string nodePath,
            out KnowledgeBaseTreeSearchMatch match)
        {
            if (Contains(node.Name, searchText))
            {
                match = BuildMatch(node, searchText, "имя узла", node.Name, nodePath);
                return true;
            }

            if (Contains(nodePath, searchText))
            {
                match = BuildMatch(node, searchText, "полный путь", nodePath, nodePath);
                return true;
            }

            if (Contains(levelName, searchText))
            {
                match = BuildMatch(node, searchText, "имя уровня", levelName, nodePath);
                return true;
            }

            match = null!;
            return false;
        }

        private static bool Contains(string value, string searchText) =>
            value.Contains(searchText, StringComparison.CurrentCultureIgnoreCase);

        private static KnowledgeBaseTreeSearchMatch BuildMatch(
            KbNode node,
            string searchText,
            string matchFieldLabel,
            string matchValue,
            string nodePath) =>
            new()
            {
                Node = node,
                SearchText = searchText,
                MatchFieldLabel = matchFieldLabel,
                MatchValue = matchValue,
                NodePath = nodePath
            };
    }
}
