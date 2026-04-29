using AsutpKnowledgeBase.Models;
using AsutpKnowledgeBase.Services;

namespace AsutpKnowledgeBase.Core.Tests;

public class KnowledgeBaseMaintenanceMonthWorkResolverServiceTests
{
    private readonly KnowledgeBaseMaintenanceMonthWorkResolverService _service = new();

    [Fact]
    public void ResolveMonthWorkItems_IncludesMonthlyWorkForIncludedProfile()
    {
        var node = new KbNode
        {
            NodeId = "device-1",
            Name = "Device 1",
            NodeType = KbNodeType.Device
        };

        IReadOnlyList<KbMaintenanceMonthWorkItem> items = _service.ResolveMonthWorkItems(
            2026,
            2,
            new[] { node },
            new[]
            {
                new KbMaintenanceScheduleProfile
                {
                    OwnerNodeId = "device-1",
                    IsIncludedInSchedule = true,
                    To1Hours = 3
                }
            });

        KbMaintenanceMonthWorkItem item = Assert.Single(items);
        Assert.Equal(KbMaintenanceWorkKind.To1, item.WorkKind);
        Assert.Equal(3, item.Hours);
        Assert.Equal("Device 1", item.NodeName);
    }

    [Fact]
    public void ResolveMonthWorkItems_WhenTo1To2AndTo3AreConfigured_UsesQuarterlySlotsAndAnnualReplacement()
    {
        var node = new KbNode
        {
            NodeId = "device-2",
            Name = "Device 2",
            NodeType = KbNodeType.Device
        };

        Dictionary<int, KbMaintenanceWorkKind> dueKindsByMonth = ResolveDueKindsByMonth(
            node,
            new KbMaintenanceScheduleProfile
            {
                OwnerNodeId = "device-2",
                IsIncludedInSchedule = true,
                To1Hours = 2,
                To2Hours = 4,
                To3Hours = 8
            });

        Assert.Equal(12, dueKindsByMonth.Count);
        Assert.Equal(8, dueKindsByMonth.Count(static pair => pair.Value == KbMaintenanceWorkKind.To1));
        Assert.Equal(3, dueKindsByMonth.Count(static pair => pair.Value == KbMaintenanceWorkKind.To2));
        Assert.Equal(1, dueKindsByMonth.Count(static pair => pair.Value == KbMaintenanceWorkKind.To3));

        int[] majorMonths = dueKindsByMonth
            .Where(static pair => pair.Value != KbMaintenanceWorkKind.To1)
            .Select(static pair => pair.Key)
            .OrderBy(static month => month)
            .ToArray();

        Assert.Equal(4, majorMonths.Length);
        Assert.Equal(4, majorMonths.Select(static month => (month - 1) / 3).Distinct().Count());
        Assert.Single(majorMonths.Select(static month => (month - 1) % 3).Distinct());
    }

    [Fact]
    public void ResolveMonthWorkItems_WhenHigherTierMaintenanceIsDue_DoesNotDuplicateIncludedLowerTierWork()
    {
        var node = new KbNode
        {
            NodeId = "device-2",
            Name = "Device 2",
            NodeType = KbNodeType.Device
        };
        var profile = new KbMaintenanceScheduleProfile
        {
            OwnerNodeId = "device-2",
            IsIncludedInSchedule = true,
            To1Hours = 2,
            To2Hours = 4,
            To3Hours = 8
        };

        IReadOnlyList<KbMaintenanceMonthWorkItem> allItems = Enumerable.Range(1, 12)
            .SelectMany(month => _service.ResolveMonthWorkItems(2026, month, new[] { node }, new[] { profile }))
            .ToArray();

        Assert.Equal(12, allItems.Count);
        Assert.Equal(8, allItems.Count(static item => item.WorkKind == KbMaintenanceWorkKind.To1));
        Assert.Equal(3, allItems.Count(static item => item.WorkKind == KbMaintenanceWorkKind.To2));
        Assert.Equal(1, allItems.Count(static item => item.WorkKind == KbMaintenanceWorkKind.To3));
    }

    [Fact]
    public void ResolveMonthWorkItems_WhenFullProfileIsConfigured_UsesAnnualMixOfOneTo3ThreeTo2AndEightTo1()
    {
        var node = new KbNode
        {
            NodeId = "device-2",
            Name = "Device 2",
            NodeType = KbNodeType.Device
        };
        var profile = new KbMaintenanceScheduleProfile
        {
            OwnerNodeId = "device-2",
            IsIncludedInSchedule = true,
            To1Hours = 2,
            To2Hours = 4,
            To3Hours = 8
        };

        IReadOnlyList<KbMaintenanceMonthWorkItem> allItems = Enumerable.Range(1, 12)
            .SelectMany(month => _service.ResolveMonthWorkItems(2026, month, new[] { node }, new[] { profile }))
            .ToArray();

        Assert.Collection(
            allItems.GroupBy(static item => item.WorkKind).OrderBy(static group => group.Key),
            group =>
            {
                Assert.Equal(KbMaintenanceWorkKind.To1, group.Key);
                Assert.Equal(8, group.Count());
            },
            group =>
            {
                Assert.Equal(KbMaintenanceWorkKind.To2, group.Key);
                Assert.Equal(3, group.Count());
            },
            group =>
            {
                Assert.Equal(KbMaintenanceWorkKind.To3, group.Key);
                Assert.Single(group);
            });
    }

    [Fact]
    public void ResolveMonthWorkItems_WhenTo2IsConfiguredWithoutTo3_SchedulesQuarterlyWorkFourTimesPerYear()
    {
        var node = new KbNode
        {
            NodeId = "device-3",
            Name = "Device 3",
            NodeType = KbNodeType.Device
        };

        Dictionary<int, KbMaintenanceWorkKind> dueKindsByMonth = ResolveDueKindsByMonth(
            node,
            new KbMaintenanceScheduleProfile
            {
                OwnerNodeId = "device-3",
                IsIncludedInSchedule = true,
                To1Hours = 2,
                To2Hours = 4
            });

        Assert.Equal(8, dueKindsByMonth.Count(static pair => pair.Value == KbMaintenanceWorkKind.To1));
        int[] quarterlyMonths = dueKindsByMonth
            .Where(static pair => pair.Value == KbMaintenanceWorkKind.To2)
            .Select(static pair => pair.Key)
            .OrderBy(static month => month)
            .ToArray();

        Assert.Equal(4, quarterlyMonths.Length);
        Assert.Equal(4, quarterlyMonths.Select(static month => (month - 1) / 3).Distinct().Count());
        Assert.Single(quarterlyMonths.Select(static month => (month - 1) % 3).Distinct());
    }

    [Fact]
    public void ResolveMonthWorkItems_WhenTo3IsConfiguredWithoutTo2_ReplacesOnlyOneMonthlySlot()
    {
        var node = new KbNode
        {
            NodeId = "device-4",
            Name = "Device 4",
            NodeType = KbNodeType.Device
        };

        Dictionary<int, KbMaintenanceWorkKind> dueKindsByMonth = ResolveDueKindsByMonth(
            node,
            new KbMaintenanceScheduleProfile
            {
                OwnerNodeId = "device-4",
                IsIncludedInSchedule = true,
                To1Hours = 2,
                To3Hours = 7
            });

        Assert.Equal(11, dueKindsByMonth.Count(static pair => pair.Value == KbMaintenanceWorkKind.To1));
        int annualMonth = Assert.Single(dueKindsByMonth.Where(static pair => pair.Value == KbMaintenanceWorkKind.To3)).Key;
        Assert.InRange(annualMonth, 1, 12);
    }

    [Fact]
    public void ResolveMonthWorkItems_PreservesTreeOrderWithinMonth()
    {
        var firstNode = new KbNode
        {
            NodeId = "device-10",
            Name = "Device 10",
            NodeType = KbNodeType.Device
        };
        var secondNode = new KbNode
        {
            NodeId = "device-20",
            Name = "Device 20",
            NodeType = KbNodeType.Device
        };

        IReadOnlyList<KbMaintenanceMonthWorkItem> items = _service.ResolveMonthWorkItems(
            2026,
            1,
            new[] { firstNode, secondNode },
            new[]
            {
                new KbMaintenanceScheduleProfile
                {
                    OwnerNodeId = "device-20",
                    IsIncludedInSchedule = true,
                    To1Hours = 2
                },
                new KbMaintenanceScheduleProfile
                {
                    OwnerNodeId = "device-10",
                    IsIncludedInSchedule = true,
                    To1Hours = 1
                }
            });

        Assert.Equal("device-10", items[0].OwnerNodeId);
        Assert.Equal(KbMaintenanceWorkKind.To1, items[0].WorkKind);
        Assert.Equal("device-20", items[^1].OwnerNodeId);
    }

    [Fact]
    public void ResolveMonthWorkItems_SkipsExcludedProfilesUnsupportedNodesAndMissingOwners()
    {
        var supportedNode = new KbNode
        {
            NodeId = "device-1",
            Name = "Device 1",
            NodeType = KbNodeType.Device
        };
        var unsupportedNode = new KbNode
        {
            NodeId = "system-1",
            Name = "Line 1",
            NodeType = KbNodeType.System
        };

        IReadOnlyList<KbMaintenanceMonthWorkItem> items = _service.ResolveMonthWorkItems(
            2026,
            1,
            new[] { supportedNode, unsupportedNode },
            new[]
            {
                new KbMaintenanceScheduleProfile
                {
                    OwnerNodeId = "device-1",
                    IsIncludedInSchedule = false,
                    To1Hours = 3
                },
                new KbMaintenanceScheduleProfile
                {
                    OwnerNodeId = "system-1",
                    IsIncludedInSchedule = true,
                    To1Hours = 4
                },
                new KbMaintenanceScheduleProfile
                {
                    OwnerNodeId = "missing-node",
                    IsIncludedInSchedule = true,
                    To1Hours = 5
                }
            });

        Assert.Empty(items);
    }

    private Dictionary<int, KbMaintenanceWorkKind> ResolveDueKindsByMonth(KbNode node, KbMaintenanceScheduleProfile profile)
    {
        return Enumerable.Range(1, 12)
            .Select(month => new
            {
                Month = month,
                Items = _service.ResolveMonthWorkItems(2026, month, new[] { node }, new[] { profile })
            })
            .Where(static result => result.Items.Count > 0)
            .ToDictionary(
                static result => result.Month,
                static result => Assert.Single(result.Items).WorkKind);
    }
}
