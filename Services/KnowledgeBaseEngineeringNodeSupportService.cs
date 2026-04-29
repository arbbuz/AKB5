using AsutpKnowledgeBase.Models;

namespace AsutpKnowledgeBase.Services
{
    public static class KnowledgeBaseEngineeringNodeSupportService
    {
        public static bool SupportsEngineeringWorkspace(KbNodeType nodeType, int visibleLevel = 0) =>
            IsEngineeringNodeType(nodeType) || visibleLevel >= 3;

        public static bool IsEngineeringNodeType(KbNodeType nodeType) => nodeType switch
        {
            KbNodeType.Cabinet => true,
            KbNodeType.Device => true,
            KbNodeType.Controller => true,
            KbNodeType.Module => true,
            _ => false
        };
    }
}
