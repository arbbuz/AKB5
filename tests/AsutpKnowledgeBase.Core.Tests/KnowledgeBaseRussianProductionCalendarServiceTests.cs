using AsutpKnowledgeBase.Services;

namespace AsutpKnowledgeBase.Core.Tests;

public class KnowledgeBaseRussianProductionCalendarServiceTests
{
    private readonly KnowledgeBaseRussianProductionCalendarService _service = new();

    [Fact]
    public void GetWorkingDays_ForJanuary2026_UsesFiveTwoCalendarAndOfficialTransfers()
    {
        IReadOnlyList<DateOnly> workingDays = _service.GetWorkingDays(2026, 1);

        Assert.Equal(15, workingDays.Count);
        Assert.Equal(new DateOnly(2026, 1, 12), workingDays[0]);
        Assert.DoesNotContain(new DateOnly(2026, 1, 9), workingDays);
        Assert.DoesNotContain(new DateOnly(2026, 1, 10), workingDays);
        Assert.DoesNotContain(new DateOnly(2026, 1, 11), workingDays);
        Assert.Contains(new DateOnly(2026, 1, 30), workingDays);
    }

    [Fact]
    public void IsWorkingDay_RespectsRussianHolidayTransfersFor2026()
    {
        Assert.False(_service.IsWorkingDay(new DateOnly(2026, 1, 9)));
        Assert.False(_service.IsWorkingDay(new DateOnly(2026, 3, 9)));
        Assert.False(_service.IsWorkingDay(new DateOnly(2026, 5, 11)));
        Assert.False(_service.IsWorkingDay(new DateOnly(2026, 12, 31)));
        Assert.True(_service.IsWorkingDay(new DateOnly(2026, 1, 12)));
    }

    [Fact]
    public void CountWorkingDays_ForMay2025_ReturnsExpectedResult()
    {
        int workingDayCount = _service.CountWorkingDays(2025, 5);
        IReadOnlyList<DateOnly> workingDays = _service.GetWorkingDays(2025, 5);

        Assert.Equal(18, workingDayCount);
        Assert.DoesNotContain(new DateOnly(2025, 5, 2), workingDays);
        Assert.DoesNotContain(new DateOnly(2025, 5, 8), workingDays);
        Assert.DoesNotContain(new DateOnly(2025, 5, 9), workingDays);
        Assert.Contains(new DateOnly(2025, 5, 5), workingDays);
    }

    [Fact]
    public void Constructor_AllowsReplacingConfiguredYearWithoutChangingServiceLogic()
    {
        var service = new KnowledgeBaseRussianProductionCalendarService(
            new Dictionary<int, IReadOnlyCollection<DateOnly>>
            {
                [2026] = new[]
                {
                    new DateOnly(2026, 1, 12)
                }
            });

        Assert.False(service.IsWorkingDay(new DateOnly(2026, 1, 12)));
        Assert.True(service.IsWorkingDay(new DateOnly(2026, 1, 13)));
        Assert.False(service.IsWorkingDay(new DateOnly(2025, 5, 2)));
    }

    [Fact]
    public void GetWorkingDays_WhenYearIsNotConfigured_ThrowsReadableError()
    {
        var exception = Assert.Throws<InvalidOperationException>(() => _service.GetWorkingDays(2027, 1));

        Assert.Contains("2027", exception.Message, StringComparison.Ordinal);
    }
}
