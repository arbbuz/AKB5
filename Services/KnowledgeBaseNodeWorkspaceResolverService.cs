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
        Network = 3,
        Maintenance = 4
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
        public KnowledgeBaseNodeWorkspaceState Resolve(KbNodeType nodeType, int visibleLevel = 0)
        {
            var tabs = CreateWorkspaceTabs(nodeType, visibleLevel);
            return new KnowledgeBaseNodeWorkspaceState
            {
                LayoutKind = tabs.Count > 1
                    ? KnowledgeBaseNodeWorkspaceLayoutKind.TabHost
                    : KnowledgeBaseNodeWorkspaceLayoutKind.InfoOnly,
                Tabs = tabs
            };
        }

        private static IReadOnlyList<KnowledgeBaseNodeWorkspaceTabState> CreateWorkspaceTabs(
            KbNodeType nodeType,
            int visibleLevel)
        {
            var tabs = new List<KnowledgeBaseNodeWorkspaceTabState>
            {
                new()
                {
                    Kind = KnowledgeBaseNodeWorkspaceTabKind.Info,
                    Title = "Карточка"
                }
            };

            if (KnowledgeBaseCompositionStateService.SupportsComposition(nodeType, visibleLevel))
            {
                tabs.Add(new KnowledgeBaseNodeWorkspaceTabState
                {
                    Kind = KnowledgeBaseNodeWorkspaceTabKind.Composition,
                    Title = "Состав",
                    Description = "Показывает типизированные записи состава для этого типа узла."
                });
            }

            if (KnowledgeBaseDocsAndSoftwareStateService.SupportsRecords(nodeType, visibleLevel))
            {
                tabs.Add(new KnowledgeBaseNodeWorkspaceTabState
                {
                    Kind = KnowledgeBaseNodeWorkspaceTabKind.DocsAndSoftware,
                    Title = "Документация и ПО",
                    Description = "Показывает ссылки на документы и программное обеспечение для этого узла."
                });
            }

            if (KnowledgeBaseNetworkStateService.SupportsRecords(nodeType, visibleLevel))
            {
                tabs.Add(new KnowledgeBaseNodeWorkspaceTabState
                {
                    Kind = KnowledgeBaseNodeWorkspaceTabKind.Network,
                    Title = "Сеть",
                    Description = "Показывает сетевые схемы, адресацию и другие файлы по сети для этого узла."
                });
            }

            if (KnowledgeBaseMaintenanceScheduleStateService.SupportsProfile(nodeType, visibleLevel))
            {
                tabs.Add(new KnowledgeBaseNodeWorkspaceTabState
                {
                    Kind = KnowledgeBaseNodeWorkspaceTabKind.Maintenance,
                    Title = "График ТО",
                    Description = "Показывает участие узла в графике ТО и нормы часов для ТО1, ТО2 и ТО3."
                });
            }

            return tabs;
        }
    }
}
