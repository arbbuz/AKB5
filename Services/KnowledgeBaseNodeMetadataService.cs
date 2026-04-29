using System.Security.Cryptography;
using System.Text;
using AsutpKnowledgeBase.Models;

namespace AsutpKnowledgeBase.Services
{
    public static class KnowledgeBaseNodeMetadataService
    {
        public static string CreateNewNodeId() => Guid.NewGuid().ToString("N");

        public static bool SupportsTechnicalFields(KbNodeType nodeType) => nodeType switch
        {
            KbNodeType.System => true,
            KbNodeType.Cabinet => true,
            KbNodeType.Device => true,
            KbNodeType.Controller => true,
            KbNodeType.Module => true,
            _ => false
        };

        public static bool SupportsTechnicalFields(KbNodeType nodeType, int visibleLevel) =>
            visibleLevel >= 3 && SupportsTechnicalFields(nodeType);

        public static bool SupportsInventoryNumber(int visibleLevel) => visibleLevel == 2;

        public static bool SupportsLocation(int visibleLevel) => visibleLevel >= 3;

        public static bool SupportsPhoto(int visibleLevel) => visibleLevel >= 3;

        public static void NormalizePersistentWorkshopNodes(string workshopName, IList<KbNode> nodes, ISet<string> usedNodeIds)
        {
            var siblingPath = new List<int>();
            NormalizePersistentNodes(
                workshopName,
                nodes,
                parentNodeType: null,
                usedNodeIds,
                siblingPath);
        }

        public static void NormalizeRuntimeSubtree(KbNode node, int levelIndex, KbNodeType? parentNodeType)
        {
            node.Name ??= string.Empty;
            node.NodeId = NormalizeRuntimeNodeId(node.NodeId);
            node.LevelIndex = levelIndex;
            node.NodeType = ResolveNodeType(
                node.NodeType,
                node.Name,
                levelIndex,
                parentNodeType,
                isWorkshopRootCandidate: parentNodeType == null && node.NodeType == KbNodeType.WorkshopRoot);
            node.Details ??= new KbNodeDetails();
            NormalizeTechnicalFields(node);
            node.Children ??= new List<KbNode>();

            foreach (var child in node.Children)
                NormalizeRuntimeSubtree(child, levelIndex + 1, node.NodeType);
        }

        public static KbNodeType ResolveNodeType(
            KbNodeType currentType,
            string nodeName,
            int levelIndex,
            KbNodeType? parentNodeType,
            bool isWorkshopRootCandidate)
        {
            if (IsKnownNodeType(currentType))
                return currentType;

            if (isWorkshopRootCandidate)
                return KbNodeType.WorkshopRoot;

            string normalizedName = NormalizeNameForMatching(nodeName);

            if (LooksLikeDocumentNode(normalizedName))
                return KbNodeType.DocumentNode;

            if (LooksLikeModule(normalizedName))
                return KbNodeType.Module;

            if (LooksLikeController(normalizedName))
                return KbNodeType.Controller;

            if (LooksLikeCabinet(normalizedName))
                return KbNodeType.Cabinet;

            if (LooksLikeSystem(normalizedName))
                return KbNodeType.System;

            if (LooksLikeDepartment(normalizedName))
                return KbNodeType.Department;

            if (parentNodeType.HasValue)
                return GetChildFallbackType(parentNodeType.Value, levelIndex);

            return levelIndex switch
            {
                <= 1 => KbNodeType.Department,
                2 => KbNodeType.System,
                3 => KbNodeType.Cabinet,
                _ => KbNodeType.Device
            };
        }

        private static void NormalizePersistentNodes(
            string workshopName,
            IList<KbNode> nodes,
            KbNodeType? parentNodeType,
            ISet<string> usedNodeIds,
            List<int> siblingPath)
        {
            bool topLevelSingleRoot = parentNodeType == null && nodes.Count == 1;

            for (int index = 0; index < nodes.Count; index++)
            {
                var node = nodes[index];
                siblingPath.Add(index);

                node.Name ??= string.Empty;
                node.Details ??= new KbNodeDetails();
                node.Children ??= new List<KbNode>();
                node.NodeId = NormalizePersistentNodeId(node.NodeId, workshopName, siblingPath, usedNodeIds);
                node.NodeType = ResolveNodeType(
                    node.NodeType,
                    node.Name,
                    node.LevelIndex,
                    parentNodeType,
                    isWorkshopRootCandidate: topLevelSingleRoot && node.LevelIndex == 0);
                NormalizeTechnicalFields(node);

                NormalizePersistentNodes(
                    workshopName,
                    node.Children,
                    node.NodeType,
                    usedNodeIds,
                    siblingPath);

                siblingPath.RemoveAt(siblingPath.Count - 1);
            }
        }

        private static string NormalizePersistentNodeId(
            string nodeId,
            string workshopName,
            IReadOnlyList<int> siblingPath,
            ISet<string> usedNodeIds)
        {
            string normalizedExistingId = NormalizeExistingNodeId(nodeId);
            if (!string.IsNullOrWhiteSpace(normalizedExistingId) && usedNodeIds.Add(normalizedExistingId))
                return normalizedExistingId;

            string deterministicId = CreateDeterministicNodeId(workshopName, siblingPath);
            if (usedNodeIds.Add(deterministicId))
                return deterministicId;

            int suffix = 2;
            while (true)
            {
                string candidate = $"{deterministicId}-{suffix}";
                if (usedNodeIds.Add(candidate))
                    return candidate;

                suffix++;
            }
        }

        private static string NormalizeRuntimeNodeId(string nodeId)
        {
            string normalizedExistingId = NormalizeExistingNodeId(nodeId);
            return string.IsNullOrWhiteSpace(normalizedExistingId)
                ? CreateNewNodeId()
                : normalizedExistingId;
        }

        private static string NormalizeExistingNodeId(string nodeId) => nodeId?.Trim() ?? string.Empty;

        private static string CreateDeterministicNodeId(string workshopName, IReadOnlyList<int> siblingPath)
        {
            string normalizedWorkshop = KnowledgeBaseDataService.NormalizeWorkshopName(workshopName);
            string identitySource = $"{normalizedWorkshop}|{string.Join(".", siblingPath)}";
            byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(identitySource));
            byte[] guidBytes = hash[..16];
            return new Guid(guidBytes).ToString("N");
        }

        private static void NormalizeTechnicalFields(KbNode node)
        {
            if (SupportsTechnicalFields(node.NodeType))
                return;

            node.Details.IpAddress = string.Empty;
            node.Details.SchemaLink = string.Empty;
        }

        private static bool IsKnownNodeType(KbNodeType nodeType) =>
            nodeType != KbNodeType.Unknown &&
            Enum.IsDefined(typeof(KbNodeType), nodeType);

        private static KbNodeType GetChildFallbackType(KbNodeType parentNodeType, int levelIndex) => parentNodeType switch
        {
            KbNodeType.WorkshopRoot => KbNodeType.Department,
            KbNodeType.Department => KbNodeType.System,
            KbNodeType.System => KbNodeType.Cabinet,
            KbNodeType.Cabinet => KbNodeType.Device,
            KbNodeType.Controller => KbNodeType.Module,
            KbNodeType.Module => KbNodeType.Module,
            KbNodeType.DocumentNode => KbNodeType.DocumentNode,
            KbNodeType.Device => KbNodeType.Device,
            _ => levelIndex switch
            {
                <= 1 => KbNodeType.Department,
                2 => KbNodeType.System,
                3 => KbNodeType.Cabinet,
                _ => KbNodeType.Device
            }
        };

        private static bool LooksLikeDepartment(string normalizedName) =>
            ContainsAny(normalizedName, "отделен", "участок", "section", "department", "zone");

        private static bool LooksLikeSystem(string normalizedName) =>
            ContainsAny(normalizedName, "систем", "линия", "line", "установка", "system");

        private static bool LooksLikeCabinet(string normalizedName) =>
            ContainsAny(normalizedName, "шкаф", "щит", "cabinet", "panel");

        private static bool LooksLikeController(string normalizedName) =>
            ContainsAny(normalizedName, "контроллер", "controller", "plc", "cpu");

        private static bool LooksLikeModule(string normalizedName) =>
            ContainsAny(normalizedName, "модул", "module");

        private static bool LooksLikeDocumentNode(string normalizedName) =>
            ContainsAny(normalizedName, "док", "схем", "инструк", "руковод", "manual", "schema", "document");

        private static string NormalizeNameForMatching(string nodeName) =>
            (nodeName ?? string.Empty).Trim().ToLowerInvariant();

        private static bool ContainsAny(string value, params string[] fragments)
        {
            foreach (string fragment in fragments)
            {
                if (value.Contains(fragment, StringComparison.Ordinal))
                    return true;
            }

            return false;
        }
    }
}
