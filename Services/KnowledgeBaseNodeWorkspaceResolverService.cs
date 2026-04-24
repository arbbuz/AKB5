using AsutpKnowledgeBase.Models;

namespace AsutpKnowledgeBase.Services
{
    public enum KnowledgeBaseNodeWorkspaceLayoutKind
    {
        InfoOnly = 0,
        TabHost = 1
    }

    public enum KnowledgeBaseNodeWorkspaceTabKind
    {
        Info = 0,
        Composition = 1,
        DocsAndSoftware = 2,
        Network = 3
    }

    public sealed class KnowledgeBaseNodeWorkspaceTabState
    {
        public KnowledgeBaseNodeWorkspaceTabKind Kind { get; init; }

        public string Title { get; init; } = string.Empty;

        public string Description { get; init; } = string.Empty;
    }

    public sealed class KnowledgeBaseNodeWorkspaceState
    {
        public KnowledgeBaseNodeWorkspaceLayoutKind LayoutKind { get; init; }

        public IReadOnlyList<KnowledgeBaseNodeWorkspaceTabState> Tabs { get; init; } =
            Array.Empty<KnowledgeBaseNodeWorkspaceTabState>();

        public bool UseTabHost => LayoutKind == KnowledgeBaseNodeWorkspaceLayoutKind.TabHost;
    }

    public class KnowledgeBaseNodeWorkspaceResolverService
    {
        public KnowledgeBaseNodeWorkspaceState Resolve(KbNodeType nodeType) =>
            UsesEngineeringTabHost(nodeType)
                ? CreateEngineeringWorkspace()
                : CreateInfoWorkspace();

        private static bool UsesEngineeringTabHost(KbNodeType nodeType) => nodeType switch
        {
            KbNodeType.Cabinet => true,
            KbNodeType.Device => true,
            KbNodeType.Controller => true,
            KbNodeType.Module => true,
            _ => false
        };

        private static KnowledgeBaseNodeWorkspaceState CreateInfoWorkspace() =>
            new()
            {
                LayoutKind = KnowledgeBaseNodeWorkspaceLayoutKind.InfoOnly,
                Tabs =
                [
                    new KnowledgeBaseNodeWorkspaceTabState
                    {
                        Kind = KnowledgeBaseNodeWorkspaceTabKind.Info,
                        Title = "Info"
                    }
                ]
            };

        private static KnowledgeBaseNodeWorkspaceState CreateEngineeringWorkspace() =>
            new()
            {
                LayoutKind = KnowledgeBaseNodeWorkspaceLayoutKind.TabHost,
                Tabs =
                [
                    new KnowledgeBaseNodeWorkspaceTabState
                    {
                        Kind = KnowledgeBaseNodeWorkspaceTabKind.Info,
                        Title = "Info"
                    },
                    new KnowledgeBaseNodeWorkspaceTabState
                    {
                        Kind = KnowledgeBaseNodeWorkspaceTabKind.Composition,
                        Title = "Composition",
                        Description = "Phase 3 will introduce the Composition screen for this node type."
                    },
                    new KnowledgeBaseNodeWorkspaceTabState
                    {
                        Kind = KnowledgeBaseNodeWorkspaceTabKind.DocsAndSoftware,
                        Title = "Documentation and Software",
                        Description = "Phase 4 will introduce document and software records for this node type."
                    },
                    new KnowledgeBaseNodeWorkspaceTabState
                    {
                        Kind = KnowledgeBaseNodeWorkspaceTabKind.Network,
                        Title = "Network",
                        Description = "Phase 6 will introduce the Network screen for this node type."
                    }
                ]
            };
    }
}
