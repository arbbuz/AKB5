using AsutpKnowledgeBase.Models;

namespace AsutpKnowledgeBase.Services
{
    public class KnowledgeBaseService
    {
        private readonly KbConfig _config;
        private readonly Dictionary<string, List<KbNode>> _workshops;

        public KnowledgeBaseService(KbConfig config, Dictionary<string, List<KbNode>> workshops)
        {
            _config = config;
            _workshops = workshops;
        }

        public List<KbNode> GetRootNodes(string workshopName) =>
            _workshops.TryGetValue(workshopName, out var nodes) ? nodes : new List<KbNode>();

        public bool CanAddRootNode() => _config.MaxLevels > 0;

        public void AddRootNode(string workshopName, KbNode node)
        {
            if (!_workshops.ContainsKey(workshopName))
                _workshops[workshopName] = new List<KbNode>();

            ReindexSubtree(node, 0);
            _workshops[workshopName].Add(node);
        }

        public void AddChildNode(KbNode parentNode, KbNode childNode)
        {
            ReindexSubtree(childNode, parentNode.LevelIndex + 1);
            parentNode.Children.Add(childNode);
        }

        public bool DeleteNode(string workshopName, KbNode nodeToRemove)
        {
            if (!_workshops.TryGetValue(workshopName, out var roots))
                return false;

            for (int i = 0; i < roots.Count; i++)
            {
                if (ReferenceEquals(roots[i], nodeToRemove))
                {
                    roots.RemoveAt(i);
                    return true;
                }
            }

            foreach (var root in roots)
            {
                if (RemoveNodeRecursive(root, nodeToRemove))
                    return true;
            }

            return false;
        }

        private bool RemoveNodeRecursive(KbNode parent, KbNode target)
        {
            for (int i = 0; i < parent.Children.Count; i++)
            {
                var child = parent.Children[i];
                if (ReferenceEquals(child, target))
                {
                    parent.Children.RemoveAt(i);
                    return true;
                }

                if (RemoveNodeRecursive(child, target)) return true;
            }

            return false;
        }

        public bool CanAddChild(KbNode parentNode) => CanAttachSubtree(parentNode, new KbNode());

        public bool ContainsNode(KbNode root, KbNode candidate)
        {
            if (ReferenceEquals(root, candidate))
                return true;

            foreach (var child in root.Children)
            {
                if (ContainsNode(child, candidate))
                    return true;
            }

            return false;
        }

        public bool CanAttachSubtree(KbNode? parentNode, KbNode subtreeRoot)
        {
            if (_config.MaxLevels <= 0)
                return false;

            int newRootLevel = parentNode == null ? 0 : parentNode.LevelIndex + 1;
            return newRootLevel + GetSubtreeHeight(subtreeRoot) <= _config.MaxLevels;
        }

        public int GetSubtreeHeight(KbNode node)
        {
            if (node.Children.Count == 0)
                return 1;

            return 1 + node.Children.Max(GetSubtreeHeight);
        }

        public int GetMaxLevelIndex(IEnumerable<KbNode> nodes)
        {
            int maxLevel = -1;
            foreach (var node in nodes)
            {
                maxLevel = System.Math.Max(maxLevel, node.LevelIndex);
                maxLevel = System.Math.Max(maxLevel, GetMaxLevelIndex(node.Children));
            }

            return maxLevel;
        }

        public void ReindexSubtree(KbNode node, int levelIndex)
        {
            node.LevelIndex = levelIndex;
            node.Details ??= new KbNodeDetails();
            if (levelIndex < 2)
            {
                node.Details.IpAddress = string.Empty;
                node.Details.SchemaLink = string.Empty;
            }

            foreach (var child in node.Children)
                ReindexSubtree(child, levelIndex + 1);
        }

        public KbNode CloneNode(KbNode node)
        {
            var details = node.Details ?? new KbNodeDetails();
            var clone = new KbNode
            {
                Name = node.Name,
                LevelIndex = node.LevelIndex,
                Details = new KbNodeDetails
                {
                    Description = details.Description,
                    Location = details.Location,
                    PhotoPath = details.PhotoPath,
                    IpAddress = details.IpAddress,
                    SchemaLink = details.SchemaLink
                }
            };

            foreach (var child in node.Children)
                clone.Children.Add(CloneNode(child));

            return clone;
        }
    }
}
