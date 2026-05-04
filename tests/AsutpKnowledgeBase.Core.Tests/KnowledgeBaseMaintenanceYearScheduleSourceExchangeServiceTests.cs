using AsutpKnowledgeBase.Models;
using AsutpKnowledgeBase.Services;

namespace AsutpKnowledgeBase.Core.Tests;

public class KnowledgeBaseMaintenanceYearScheduleSourceExchangeServiceTests
{
    private readonly KnowledgeBaseMaintenanceYearScheduleSourceExchangeService _service = new();

    [Fact]
    public void ExportWorkbook_BuildsWorkbookAndCountsManualAndFallbackProfiles()
    {
        KnowledgeBaseMaintenanceYearScheduleSourceExportResult result = _service.ExportWorkbook(
            CreateRoots(),
            new[]
            {
                new KbMaintenanceScheduleProfile
                {
                    OwnerNodeId = "device-1",
                    IsIncludedInSchedule = true,
                    YearScheduleEntries = new List<KbMaintenanceYearScheduleEntry>
                    {
                        new() { Month = 2, WorkKind = KbMaintenanceWorkKind.To3 }
                    }
                },
                new KbMaintenanceScheduleProfile
                {
                    OwnerNodeId = "device-2",
                    IsIncludedInSchedule = true
                }
            });

        Assert.True(result.IsSuccess);
        Assert.NotEmpty(result.WorkbookPackage);
        Assert.Equal(2, result.ExportedProfileCount);
        Assert.Equal(1, result.ManualScheduleProfileCount);
        Assert.Equal(1, result.AutomaticFallbackProfileCount);
    }

    [Fact]
    public void ImportWorkbook_UpdatesOnlyYearScheduleEntries()
    {
        KnowledgeBaseMaintenanceYearScheduleSourceExportResult exportResult = _service.ExportWorkbook(
            CreateRoots(),
            new[]
            {
                new KbMaintenanceScheduleProfile
                {
                    OwnerNodeId = "device-1",
                    IsIncludedInSchedule = true,
                    To1Hours = 1,
                    To2Hours = 2,
                    To3Hours = 3,
                    YearScheduleEntries = new List<KbMaintenanceYearScheduleEntry>
                    {
                        new() { Month = 2, WorkKind = KbMaintenanceWorkKind.To3 },
                        new() { Month = 4, WorkKind = KbMaintenanceWorkKind.To2 }
                    }
                }
            });

        KnowledgeBaseMaintenanceYearScheduleSourceImportResult importResult = _service.ImportWorkbook(
            exportResult.WorkbookPackage,
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
                    To3Hours = 30
                }
            });

        Assert.True(importResult.IsSuccess);
        Assert.Equal(1, importResult.ImportedRowCount);
        Assert.Equal(1, importResult.UpdatedProfileCount);
        Assert.Empty(importResult.UnresolvedRows);

        KbMaintenanceScheduleProfile profile = Assert.Single(importResult.MaintenanceScheduleProfiles);
        Assert.Equal("profile-1", profile.MaintenanceProfileId);
        Assert.False(profile.IsIncludedInSchedule);
        Assert.Equal(10, profile.To1Hours);
        Assert.Equal(20, profile.To2Hours);
        Assert.Equal(30, profile.To3Hours);
        Assert.Equal(
            new[] { (2, KbMaintenanceWorkKind.To3), (4, KbMaintenanceWorkKind.To2) },
            profile.YearScheduleEntries.Select(static entry => (entry.Month, entry.WorkKind)).ToArray());
    }

    [Fact]
    public void ImportWorkbook_ClearsManualScheduleWhenWorkbookHasEmptyMonths()
    {
        KnowledgeBaseMaintenanceYearScheduleSourceExportResult exportResult = _service.ExportWorkbook(
            CreateRoots(),
            new[]
            {
                new KbMaintenanceScheduleProfile
                {
                    OwnerNodeId = "device-2",
                    IsIncludedInSchedule = true
                }
            });

        KnowledgeBaseMaintenanceYearScheduleSourceImportResult importResult = _service.ImportWorkbook(
            exportResult.WorkbookPackage,
            CreateRoots(),
            new[]
            {
                new KbMaintenanceScheduleProfile
                {
                    OwnerNodeId = "device-2",
                    IsIncludedInSchedule = true,
                    YearScheduleEntries = new List<KbMaintenanceYearScheduleEntry>
                    {
                        new() { Month = 8, WorkKind = KbMaintenanceWorkKind.To2 }
                    }
                }
            });

        Assert.True(importResult.IsSuccess);
        Assert.Equal(1, importResult.ClearedProfileCount);
        Assert.Empty(Assert.Single(importResult.MaintenanceScheduleProfiles).YearScheduleEntries);
    }

    [Fact]
    public void ImportWorkbook_ReportsRowsWithoutConfiguredProfile()
    {
        KnowledgeBaseMaintenanceYearScheduleSourceExportResult exportResult = _service.ExportWorkbook(
            CreateRoots(),
            new[]
            {
                new KbMaintenanceScheduleProfile
                {
                    OwnerNodeId = "device-1",
                    IsIncludedInSchedule = true,
                    YearScheduleEntries = new List<KbMaintenanceYearScheduleEntry>
                    {
                        new() { Month = 1, WorkKind = KbMaintenanceWorkKind.To1 }
                    }
                }
            });

        KnowledgeBaseMaintenanceYearScheduleSourceImportResult importResult = _service.ImportWorkbook(
            exportResult.WorkbookPackage,
            CreateRoots(),
            Array.Empty<KbMaintenanceScheduleProfile>());

        Assert.True(importResult.IsSuccess);
        Assert.Equal(1, importResult.ImportedRowCount);
        Assert.Empty(importResult.MaintenanceScheduleProfiles);
        string unresolved = Assert.Single(importResult.UnresolvedRows);
        Assert.Contains("device-1", unresolved, StringComparison.Ordinal);
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
        };
    }
}
