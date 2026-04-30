using AsutpKnowledgeBase.Models;
using AsutpKnowledgeBase.Services;

namespace AsutpKnowledgeBase.Core.Tests;

public class KnowledgeBaseMaintenanceScheduleProfileMutationServiceTests
{
    private readonly KnowledgeBaseMaintenanceScheduleProfileMutationService _service = new();

    [Fact]
    public void UpsertMaintenanceScheduleProfile_AddsNewProfileForNode()
    {
        var ownerNode = new KbNode
        {
            NodeId = "device-1",
            Name = "Device 1",
            NodeType = KbNodeType.Device
        };

        var result = _service.UpsertMaintenanceScheduleProfile(
            ownerNode,
            Array.Empty<KbMaintenanceScheduleProfile>(),
            new KbMaintenanceScheduleProfile
            {
                IsIncludedInSchedule = true,
                To1Hours = 2,
                To2Hours = 4,
                To3Hours = 8,
                YearScheduleEntries = new List<KbMaintenanceYearScheduleEntry>
                {
                    new() { Month = 2, WorkKind = KbMaintenanceWorkKind.To2 },
                    new() { Month = 11, WorkKind = KbMaintenanceWorkKind.To3 }
                }
            });

        Assert.True(result.IsSuccess);
        var profile = Assert.Single(result.MaintenanceScheduleProfiles);
        Assert.Equal("device-1", profile.OwnerNodeId);
        Assert.True(profile.IsIncludedInSchedule);
        Assert.Equal(2, profile.To1Hours);
        Assert.Equal(4, profile.To2Hours);
        Assert.Equal(8, profile.To3Hours);
        Assert.Equal(new[] { 2, 11 }, profile.YearScheduleEntries.Select(static entry => entry.Month));
    }

    [Fact]
    public void UpsertMaintenanceScheduleProfile_ReplacesExistingProfileForSameNode()
    {
        var ownerNode = new KbNode
        {
            NodeId = "device-1",
            Name = "Device 1",
            NodeType = KbNodeType.Device
        };

        var result = _service.UpsertMaintenanceScheduleProfile(
            ownerNode,
            new[]
            {
                new KbMaintenanceScheduleProfile
                {
                    MaintenanceProfileId = "profile-1",
                    OwnerNodeId = "device-1",
                    IsIncludedInSchedule = true,
                    To1Hours = 2,
                    To2Hours = 2,
                    To3Hours = 2
                }
            },
            new KbMaintenanceScheduleProfile
            {
                MaintenanceProfileId = "profile-1",
                IsIncludedInSchedule = false,
                To1Hours = 1,
                To2Hours = 3,
                To3Hours = 5
            });

        Assert.True(result.IsSuccess);
        var profile = Assert.Single(result.MaintenanceScheduleProfiles);
        Assert.False(profile.IsIncludedInSchedule);
        Assert.Equal(1, profile.To1Hours);
        Assert.Equal(3, profile.To2Hours);
        Assert.Equal(5, profile.To3Hours);
    }

    [Fact]
    public void UpsertMaintenanceScheduleProfile_AllowsHoursAboveEight()
    {
        var ownerNode = new KbNode
        {
            NodeId = "device-1",
            Name = "Device 1",
            NodeType = KbNodeType.Device
        };

        var result = _service.UpsertMaintenanceScheduleProfile(
            ownerNode,
            Array.Empty<KbMaintenanceScheduleProfile>(),
            new KbMaintenanceScheduleProfile
            {
                IsIncludedInSchedule = true,
                To1Hours = 9,
                To3Hours = 16
            });

        Assert.True(result.IsSuccess);
        var profile = Assert.Single(result.MaintenanceScheduleProfiles);
        Assert.Equal(9, profile.To1Hours);
        Assert.Equal(16, profile.To3Hours);
    }

    [Fact]
    public void UpsertMaintenanceScheduleProfile_RejectsUnsupportedNodeType()
    {
        var ownerNode = new KbNode
        {
            NodeId = "system-1",
            Name = "Line 1",
            NodeType = KbNodeType.System
        };

        var result = _service.UpsertMaintenanceScheduleProfile(
            ownerNode,
            Array.Empty<KbMaintenanceScheduleProfile>(),
            new KbMaintenanceScheduleProfile
            {
                IsIncludedInSchedule = true,
                To1Hours = 2
            });

        Assert.False(result.IsSuccess);
        Assert.Contains("График ТО", result.ErrorMessage, StringComparison.Ordinal);
    }

    [Fact]
    public void UpsertMaintenanceScheduleProfile_RejectsDuplicateYearScheduleMonths()
    {
        var ownerNode = new KbNode
        {
            NodeId = "device-1",
            Name = "Device 1",
            NodeType = KbNodeType.Device
        };

        var result = _service.UpsertMaintenanceScheduleProfile(
            ownerNode,
            Array.Empty<KbMaintenanceScheduleProfile>(),
            new KbMaintenanceScheduleProfile
            {
                IsIncludedInSchedule = true,
                To1Hours = 2,
                YearScheduleEntries = new List<KbMaintenanceYearScheduleEntry>
                {
                    new() { Month = 5, WorkKind = KbMaintenanceWorkKind.To1 },
                    new() { Month = 5, WorkKind = KbMaintenanceWorkKind.To2 }
                }
            });

        Assert.False(result.IsSuccess);
        Assert.Contains("дублей", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void DeleteMaintenanceScheduleProfile_RemovesOwnedProfile()
    {
        var ownerNode = new KbNode
        {
            NodeId = "device-1",
            Name = "Device 1",
            NodeType = KbNodeType.Device
        };

        var result = _service.DeleteMaintenanceScheduleProfile(
            ownerNode,
            new[]
            {
                new KbMaintenanceScheduleProfile
                {
                    MaintenanceProfileId = "profile-1",
                    OwnerNodeId = "device-1"
                },
                new KbMaintenanceScheduleProfile
                {
                    MaintenanceProfileId = "profile-2",
                    OwnerNodeId = "device-2"
                }
            },
            "profile-1");

        Assert.True(result.IsSuccess);
        var remainingProfile = Assert.Single(result.MaintenanceScheduleProfiles);
        Assert.Equal("device-2", remainingProfile.OwnerNodeId);
    }
    [Fact]
    public void UpsertMaintenanceScheduleProfile_ForVisibleLevel3System_ReturnsSuccess()
    {
        var ownerNode = new KbNode
        {
            NodeId = "legacy-cabinet-1",
            Name = "Шкаф 1",
            NodeType = KbNodeType.System
        };

        var result = _service.UpsertMaintenanceScheduleProfile(
            ownerNode,
            Array.Empty<KbMaintenanceScheduleProfile>(),
            new KbMaintenanceScheduleProfile
            {
                IsIncludedInSchedule = true,
                To1Hours = 2
            },
            visibleLevel: 3);

        Assert.True(result.IsSuccess);
        Assert.Single(result.MaintenanceScheduleProfiles);
    }
}
