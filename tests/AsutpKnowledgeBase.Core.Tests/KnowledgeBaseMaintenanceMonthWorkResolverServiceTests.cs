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
            Name = "Насос 1",
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
        Assert.Equal("Насос 1", item.NodeName);
    }

    [Fact]
    public void ResolveMonthWorkItems_SemiannualWorkOccursTwicePerYearWithStableOffset()
    {
        var node = new KbNode
        {
            NodeId = "device-2",
            Name = "Насос 2",
            NodeType = KbNodeType.Device
        };

        int[] dueMonths2026 = Enumerable.Range(1, 12)
            .Where(month => _service.ResolveMonthWorkItems(
                    2026,
                    month,
                    new[] { node },
                    new[]
                    {
                        new KbMaintenanceScheduleProfile
                        {
                            OwnerNodeId = "device-2",
                            IsIncludedInSchedule = true,
                            To2Hours = 4
                        }
                    })
                .Any(item => item.WorkKind == KbMaintenanceWorkKind.To2))
            .ToArray();

        int[] dueMonths2027 = Enumerable.Range(1, 12)
            .Where(month => _service.ResolveMonthWorkItems(
                    2027,
                    month,
                    new[] { node },
                    new[]
                    {
                        new KbMaintenanceScheduleProfile
                        {
                            OwnerNodeId = "device-2",
                            IsIncludedInSchedule = true,
                            To2Hours = 4
                        }
                    })
                .Any(item => item.WorkKind == KbMaintenanceWorkKind.To2))
            .ToArray();

        Assert.Equal(2, dueMonths2026.Length);
        Assert.Equal(6, dueMonths2026[1] - dueMonths2026[0]);
        Assert.Equal(dueMonths2026, dueMonths2027);
    }

    [Fact]
    public void ResolveMonthWorkItems_AnnualWorkOccursOncePerYearWithStableOffset()
    {
        var node = new KbNode
        {
            NodeId = "device-3",
            Name = "Насос 3",
            NodeType = KbNodeType.Device
        };

        int[] dueMonths2026 = Enumerable.Range(1, 12)
            .Where(month => _service.ResolveMonthWorkItems(
                    2026,
                    month,
                    new[] { node },
                    new[]
                    {
                        new KbMaintenanceScheduleProfile
                        {
                            OwnerNodeId = "device-3",
                            IsIncludedInSchedule = true,
                            To3Hours = 7
                        }
                    })
                .Any(item => item.WorkKind == KbMaintenanceWorkKind.To3))
            .ToArray();

        int[] dueMonths2027 = Enumerable.Range(1, 12)
            .Where(month => _service.ResolveMonthWorkItems(
                    2027,
                    month,
                    new[] { node },
                    new[]
                    {
                        new KbMaintenanceScheduleProfile
                        {
                            OwnerNodeId = "device-3",
                            IsIncludedInSchedule = true,
                            To3Hours = 7
                        }
                    })
                .Any(item => item.WorkKind == KbMaintenanceWorkKind.To3))
            .ToArray();

        int annualMonth = Assert.Single(dueMonths2026);
        Assert.Equal(new[] { annualMonth }, dueMonths2027);
    }

    [Fact]
    public void ResolveMonthWorkItems_PreservesTreeOrderAndWorkKindOrderWithinMonth()
    {
        var firstNode = new KbNode
        {
            NodeId = "device-10",
            Name = "Насос 10",
            NodeType = KbNodeType.Device
        };
        var secondNode = new KbNode
        {
            NodeId = "device-20",
            Name = "Насос 20",
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
                    To1Hours = 1,
                    To2Hours = 3,
                    To3Hours = 4
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
            Name = "Насос 1",
            NodeType = KbNodeType.Device
        };
        var unsupportedNode = new KbNode
        {
            NodeId = "system-1",
            Name = "Линия 1",
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
}
