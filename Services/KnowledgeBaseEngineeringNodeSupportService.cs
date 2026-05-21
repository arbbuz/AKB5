using AsutpKnowledgeBase.Models;

namespace AsutpKnowledgeBase.Services
{
    public static class KnowledgeBaseEngineeringNodeSupportService
    {
        public static bool SupportsEngineeringWorkspace(KbNodeType nodeType, int visibleLevel = 0) =>
            SupportsComposition(nodeType, visibleLevel) ||
            SupportsDocsAndSoftwareRecords(nodeType, visibleLevel) ||
            SupportsMaintenanceProfile(nodeType, visibleLevel);

        public static bool SupportsComposition(KbNodeType nodeType, int visibleLevel = 0) =>
            HasVisibleLevel(visibleLevel)
                ? visibleLevel == 3
                : IsEngineeringNodeType(nodeType);

        public static bool SupportsDocsAndSoftwareRecords(KbNodeType nodeType, int visibleLevel = 0) =>
            HasVisibleLevel(visibleLevel)
                ? visibleLevel == 2
                : IsEngineeringNodeType(nodeType);

        public static bool SupportsMaintenanceProfile(KbNodeType nodeType, int visibleLevel = 0) =>
            HasVisibleLevel(visibleLevel)
                ? visibleLevel == 3
                : IsEngineeringNodeType(nodeType);

        public static bool IsEngineeringNodeType(KbNodeType nodeType) => nodeType switch
        {
            KbNodeType.Cabinet => true,
            KbNodeType.Device => true,
            KbNodeType.Controller => true,
            KbNodeType.Module => true,
            _ => false
        };

        private static bool HasVisibleLevel(int visibleLevel) => visibleLevel > 0;
    }
}
