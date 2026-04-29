using AsutpKnowledgeBase.Models;
using AsutpKnowledgeBase.Services;

namespace AsutpKnowledgeBase.Core.Tests;

public class KnowledgeBaseMaintenanceMonthDemandSummaryServiceTests
{
    private readonly KnowledgeBaseMaintenanceMonthDemandSummaryService _service = new();

    [Fact]
    public void Build_SummarizesWorkKindsAcrossYearWithoutLosingMonthlyDemand()
    {
        var device1 = new KbNode
        {
            NodeId = "device-1",
            Name = "Узел 1",
            NodeType = KbNodeType.Device
        };
        var device2 = new KbNode
        {
            NodeId = "device-2",
            Name = "Узел 2",
            NodeType = KbNodeType.Device
        };
        var roots = new[]
        {
            new KbNode
            {
                NodeId = "department-1",
                Name = "Отделение 1",
                NodeType = KbNodeType.Department,
                Children =
                {
                    new KbNode
                    {
                        NodeId = "system-1",
                        Name = "Система 1",
                        NodeType = KbNodeType.System,
                        Children = { device1, device2 }
                    }
                }
            }
        };
        var profiles = new[]
        {
            new KbMaintenanceScheduleProfile
            {
                OwnerNodeId = "device-1",
                IsIncludedInSchedule = true,
                To1Hours = 2,
                To2Hours = 4,
                To3Hours = 8
            },
            new KbMaintenanceScheduleProfile
            {
                OwnerNodeId = "device-2",
                IsIncludedInSchedule = true,
                To1Hours = 3
            }
        };

        KnowledgeBaseMaintenanceMonthDemandSummary[] summaries = Enumerable.Range(1, 12)
            .Select(month => _service.Build(2026, month, roots, profiles))
            .ToArray();

        Assert.Equal(12, summaries.Length);
        Assert.Contains(summaries, static summary => summary.To2ItemCount > 0 || summary.To3ItemCount > 0);
        Assert.Equal(20, summaries.Sum(static summary => summary.To1ItemCount));
        Assert.Equal(52, summaries.Sum(static summary => summary.To1Hours));
        Assert.Equal(3, summaries.Sum(static summary => summary.To2ItemCount));
        Assert.Equal(12, summaries.Sum(static summary => summary.To2Hours));
        Assert.Equal(1, summaries.Sum(static summary => summary.To3ItemCount));
        Assert.Equal(8, summaries.Sum(static summary => summary.To3Hours));
        Assert.Equal(24, summaries.Sum(static summary => summary.TotalItemCount));
        Assert.Equal(72, summaries.Sum(static summary => summary.TotalHours));
    }

    [Fact]
    public void Build_WhenNothingIsDue_ReturnsZeroSummary()
    {
        var roots = new[]
        {
            new KbNode
            {
                NodeId = "cabinet-1",
                Name = "Шкаф 1",
                NodeType = KbNodeType.Cabinet
            }
        };

        KnowledgeBaseMaintenanceMonthDemandSummary summary = _service.Build(
            2026,
            1,
            roots,
            new[]
            {
                new KbMaintenanceScheduleProfile
                {
                    OwnerNodeId = "cabinet-1",
                    IsIncludedInSchedule = false,
                    To1Hours = 2,
                    To2Hours = 6,
                    To3Hours = 10
                }
            });

        Assert.Equal(0, summary.To1ItemCount);
        Assert.Equal(0, summary.To2ItemCount);
        Assert.Equal(0, summary.To3ItemCount);
        Assert.Equal(0, summary.TotalItemCount);
        Assert.Equal(0, summary.TotalHours);
    }
}
