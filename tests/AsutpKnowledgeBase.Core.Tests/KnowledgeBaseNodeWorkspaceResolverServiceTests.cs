using AsutpKnowledgeBase.Models;
using AsutpKnowledgeBase.Services;

namespace AsutpKnowledgeBase.Core.Tests;

public class KnowledgeBaseNodeWorkspaceResolverServiceTests
{
    private readonly KnowledgeBaseNodeWorkspaceResolverService _service = new();

    [Fact]
    public void Resolve_ForDepartment_ReturnsInfoOnlyWorkspace()
    {
        var workspace = _service.Resolve(KbNodeType.Department);

        Assert.False(workspace.UseTabHost);
        var tab = Assert.Single(workspace.Tabs);
        Assert.Equal(KnowledgeBaseNodeWorkspaceTabKind.Info, tab.Kind);
        Assert.Equal("Карточка", tab.Title);
    }

    [Fact]
    public void Resolve_ForLevel2System_ReturnsDocsTabWithoutNetwork()
    {
        var workspace = _service.Resolve(KbNodeType.System, visibleLevel: 2);

        Assert.True(workspace.UseTabHost);
        Assert.Equal(
            new[]
            {
                KnowledgeBaseNodeWorkspaceTabKind.Info,
                KnowledgeBaseNodeWorkspaceTabKind.DocsAndSoftware
            },
            workspace.Tabs.Select(static tab => tab.Kind).ToArray());
    }

    [Fact]
    public void Resolve_ForUnknown_ReturnsInfoOnlyWorkspace()
    {
        var workspace = _service.Resolve(KbNodeType.Unknown);

        Assert.False(workspace.UseTabHost);
        Assert.Single(workspace.Tabs);
        Assert.Equal(KnowledgeBaseNodeWorkspaceTabKind.Info, workspace.Tabs[0].Kind);
    }

    [Fact]
    public void Resolve_ForLevel3Cabinet_ReturnsCompositionAndMaintenanceTabs()
    {
        var workspace = _service.Resolve(KbNodeType.Cabinet, visibleLevel: 3);

        Assert.True(workspace.UseTabHost);
        Assert.Equal(
            new[]
            {
                KnowledgeBaseNodeWorkspaceTabKind.Info,
                KnowledgeBaseNodeWorkspaceTabKind.Composition,
                KnowledgeBaseNodeWorkspaceTabKind.AdditionalEquipment,
                KnowledgeBaseNodeWorkspaceTabKind.Maintenance
            },
            workspace.Tabs.Select(static tab => tab.Kind).ToArray());
    }
}
