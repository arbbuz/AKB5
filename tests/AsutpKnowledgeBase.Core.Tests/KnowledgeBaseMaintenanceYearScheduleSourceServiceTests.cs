using AsutpKnowledgeBase.Models;
using AsutpKnowledgeBase.Services;

namespace AsutpKnowledgeBase.Core.Tests;

public class KnowledgeBaseMaintenanceYearScheduleSourceServiceTests
{
    private readonly KnowledgeBaseMaintenanceYearScheduleSourceService _service = new();

    [Fact]
    public void BuildRows_ReturnsConfiguredProfilesFromCurrentTreeOnly()
    {
        List<KnowledgeBaseMaintenanceYearScheduleSourceRow> rows = _service.BuildRows(
            CreateRoots(),
            new[]
            {
                new KbMaintenanceScheduleProfile
                {
                    OwnerNodeId = "device-2",
                    IsIncludedInSchedule = true
                },
                new KbMaintenanceScheduleProfile
                {
                    OwnerNodeId = "other-workshop-device",
                    IsIncludedInSchedule = true
                },
                new KbMaintenanceScheduleProfile
                {
                    OwnerNodeId = "device-1",
                    IsIncludedInSchedule = false,
                    YearScheduleEntries = new List<KbMaintenanceYearScheduleEntry>
                    {
                        new() { Month = 4, WorkKind = KbMaintenanceWorkKind.To2 }
                    }
                }
            });

        Assert.Equal(new[] { "device-1", "device-2" }, rows.Select(static row => row.OwnerNodeId).ToArray());
        Assert.Equal("INV-001", rows[0].InventoryNumber);
        Assert.False(rows[0].IsIncludedInSchedule);
        Assert.True(rows[0].HasManualSchedule);
        Assert.False(rows[1].HasManualSchedule);
    }

    [Fact]
    public void ApplyRows_UpdatesOnlyYearScheduleEntriesAndPreservesOtherProfileData()
    {
        KnowledgeBaseMaintenanceYearScheduleSourceApplyResult result = _service.ApplyRows(
            new[]
            {
                new KnowledgeBaseMaintenanceYearScheduleSourceRow
                {
                    OwnerNodeId = "device-1",
                    YearScheduleEntries = new List<KbMaintenanceYearScheduleEntry>
                    {
                        new() { Month = 5, WorkKind = KbMaintenanceWorkKind.To3 }
                    }
                }
            },
            CreateRoots(),
            new[]
            {
                new KbMaintenanceScheduleProfile
                {
                    MaintenanceProfileId = "profile-1",
                    OwnerNodeId = "device-1",
                    IsIncludedInSchedule = false,
                    To1Hours = 10,
                    To2Hours = 20,
                    To3Hours = 30,
                    YearScheduleEntries = new List<KbMaintenanceYearScheduleEntry>
                    {
                        new() { Month = 2, WorkKind = KbMaintenanceWorkKind.To1 }
                    }
                },
                new KbMaintenanceScheduleProfile
                {
                    MaintenanceProfileId = "profile-other",
                    OwnerNodeId = "other-workshop-device",
                    IsIncludedInSchedule = true,
                    To1Hours = 1,
                    To2Hours = 2,
                    To3Hours = 3
                }
            });

        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.EditedRowCount);
        Assert.Equal(1, result.UpdatedProfileCount);
        Assert.Empty(result.UnresolvedRows);

        KbMaintenanceScheduleProfile profile = result.MaintenanceScheduleProfiles.Single(static profile => profile.OwnerNodeId == "device-1");
        Assert.Equal("profile-1", profile.MaintenanceProfileId);
        Assert.False(profile.IsIncludedInSchedule);
        Assert.Equal(10, profile.To1Hours);
        Assert.Equal(20, profile.To2Hours);
        Assert.Equal(30, profile.To3Hours);
        Assert.Equal(
            new[] { (5, KbMaintenanceWorkKind.To3) },
            profile.YearScheduleEntries.Select(static entry => (entry.Month, entry.WorkKind)).ToArray());

        Assert.Contains(result.MaintenanceScheduleProfiles, static profile => profile.OwnerNodeId == "other-workshop-device");
    }

    [Fact]
    public void ApplyRows_ClearsManualScheduleWhenEditedRowHasNoMonths()
    {
        KnowledgeBaseMaintenanceYearScheduleSourceApplyResult result = _service.ApplyRows(
            new[]
            {
                new KnowledgeBaseMaintenanceYearScheduleSourceRow
                {
                    OwnerNodeId = "device-1"
                }
            },
            CreateRoots(),
            new[]
            {
                new KbMaintenanceScheduleProfile
                {
                    OwnerNodeId = "device-1",
                    IsIncludedInSchedule = true,
                    YearScheduleEntries = new List<KbMaintenanceYearScheduleEntry>
                    {
                        new() { Month = 8, WorkKind = KbMaintenanceWorkKind.To2 }
                    }
                }
            });

        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.ClearedProfileCount);
        Assert.Empty(Assert.Single(result.MaintenanceScheduleProfiles).YearScheduleEntries);
    }

    [Fact]
    public void ApplyRows_ReportsRowsWithoutConfiguredProfile()
    {
        KnowledgeBaseMaintenanceYearScheduleSourceApplyResult result = _service.ApplyRows(
            new[]
            {
                new KnowledgeBaseMaintenanceYearScheduleSourceRow
                {
                    OwnerNodeId = "device-2",
                    YearScheduleEntries = new List<KbMaintenanceYearScheduleEntry>
                    {
                        new() { Month = 1, WorkKind = KbMaintenanceWorkKind.To1 }
                    }
                }
            },
            CreateRoots(),
            new[]
            {
                new KbMaintenanceScheduleProfile
                {
                    OwnerNodeId = "device-1",
                    IsIncludedInSchedule = true
                }
            });

        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.EditedRowCount);
        Assert.Empty(result.MaintenanceScheduleProfiles.Single(static profile => profile.OwnerNodeId == "device-1").YearScheduleEntries);
        string unresolved = Assert.Single(result.UnresolvedRows);
        Assert.Contains("device-2", unresolved, StringComparison.Ordinal);
    }

    [Fact]
    public void ApplyRows_RejectsDuplicateMonthEntries()
    {
        KnowledgeBaseMaintenanceYearScheduleSourceApplyResult result = _service.ApplyRows(
            new[]
            {
                new KnowledgeBaseMaintenanceYearScheduleSourceRow
                {
                    OwnerNodeId = "device-1",
                    YearScheduleEntries = new List<KbMaintenanceYearScheduleEntry>
                    {
                        new() { Month = 1, WorkKind = KbMaintenanceWorkKind.To1 },
                        new() { Month = 1, WorkKind = KbMaintenanceWorkKind.To2 }
                    }
                }
            },
            CreateRoots(),
            new[]
            {
                new KbMaintenanceScheduleProfile
                {
                    OwnerNodeId = "device-1",
                    IsIncludedInSchedule = true
                }
            });

        Assert.False(result.IsSuccess);
        Assert.NotEmpty(result.ErrorMessage);
    }

    private static KbNode[] CreateRoots()
    {
        return new[]
        {
            new KbNode
            {
                NodeId = "department-1",
                Name = "Цех",
                NodeType = KbNodeType.Department,
                Children =
                {
                    new KbNode
                    {
                        NodeId = "system-1",
                        Name = "АСУ установки",
                        NodeType = KbNodeType.System,
                        Children =
                        {
                            new KbNode
                            {
                                NodeId = "device-1",
                                Name = "Шкаф 1",
                                NodeType = KbNodeType.Cabinet,
                                Details = new KbNodeDetails { InventoryNumber = "INV-001" }
                            },
                            new KbNode
                            {
                                NodeId = "device-2",
                                Name = "Шкаф 2",
                                NodeType = KbNodeType.Cabinet,
                                Details = new KbNodeDetails { InventoryNumber = "INV-002" }
                            }
                        }
                    }
                }
            }
        };
    }
}
