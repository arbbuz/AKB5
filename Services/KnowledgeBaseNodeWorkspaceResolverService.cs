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
        AdditionalEquipment = 2,
        DocsAndSoftware = 3,
        Network = 4,
        Maintenance = 5
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
            var layoutKind = tabs.Count > 1 ||
                (tabs.Count == 1 && tabs[0].Kind != KnowledgeBaseNodeWorkspaceTabKind.Info)
                    ? KnowledgeBaseNodeWorkspaceLayoutKind.TabHost
                    : KnowledgeBaseNodeWorkspaceLayoutKind.InfoOnly;

            return new KnowledgeBaseNodeWorkspaceState
            {
                LayoutKind = layoutKind,
                Tabs = tabs
            };
        }

        private static IReadOnlyList<KnowledgeBaseNodeWorkspaceTabState> CreateWorkspaceTabs(
            KbNodeType nodeType,
            int visibleLevel)
        {
            var tabs = new List<KnowledgeBaseNodeWorkspaceTabState>();

            if (visibleLevel != 3)
            {
                tabs.Add(new KnowledgeBaseNodeWorkspaceTabState
                {
                    Kind = KnowledgeBaseNodeWorkspaceTabKind.Info,
                    Title = "Карточка"
                });
            }

            if (KnowledgeBaseCompositionStateService.SupportsComposition(nodeType, visibleLevel))
            {
                tabs.Add(new KnowledgeBaseNodeWorkspaceTabState
                {
                    Kind = KnowledgeBaseNodeWorkspaceTabKind.Composition,
                    Title = "Состав",
                    Description = "Показывает Rack и слотовые записи выбранного шкафа или щита."
                });
                tabs.Add(new KnowledgeBaseNodeWorkspaceTabState
                {
                    Kind = KnowledgeBaseNodeWorkspaceTabKind.AdditionalEquipment,
                    Title = "Доп. оборудование",
                    Description = "Показывает оборудование выбранного шкафа или щита вне Rack-слотов."
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

            if (KnowledgeBaseNodeMetadataService.SupportsNetworkTopology(visibleLevel))
            {
                tabs.Add(new KnowledgeBaseNodeWorkspaceTabState
                {
                    Kind = KnowledgeBaseNodeWorkspaceTabKind.Network,
                    Title = "Сеть",
                    Description = "Графическая топология сети выбранного узла."
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
