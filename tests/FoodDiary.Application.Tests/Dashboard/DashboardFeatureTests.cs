using FoodDiary.Application.Dashboard.Queries.GetDashboardSnapshot;
using FoodDiary.Application.Dashboard.Services;
using FoodDiary.Contracts.Statistics;
using FoodDiary.Domain.Entities.Tracking;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Tests.Dashboard;

public class DashboardFeatureTests
{
    [Fact]
    public async Task GetDashboardSnapshotQueryValidator_WithEmptyUserId_Fails()
    {
        var validator = new GetDashboardSnapshotQueryValidator();
        var query = new GetDashboardSnapshotQuery(
            UserId.Empty,
            DateTime.UtcNow,
            Page: 1,
            PageSize: 10,
            Locale: "en",
            TrendDays: 7);

        var result = await validator.ValidateAsync(query);

        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task GetDashboardSnapshotQueryValidator_WithValidInput_Passes()
    {
        var validator = new GetDashboardSnapshotQueryValidator();
        var query = new GetDashboardSnapshotQuery(
            UserId.New(),
            DateTime.UtcNow,
            Page: 1,
            PageSize: 10,
            Locale: "en",
            TrendDays: 7);

        var result = await validator.ValidateAsync(query);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void DashboardMapping_ToWeeklyCalories_OrdersByDateAscending()
    {
        var day1 = new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc);
        var day2 = day1.AddDays(1);
        var responses = new List<AggregatedStatisticsResponse>
        {
            new(day2, day2, 2000, 100, 70, 250, 30),
            new(day1, day1, 1800, 90, 60, 220, 25)
        };

        var calories = DashboardMapping.ToWeeklyCalories(responses);

        Assert.Collection(
            calories,
            c => Assert.Equal(day1, c.Date),
            c => Assert.Equal(day2, c.Date));
    }

    [Fact]
    public void DashboardMapping_ToWeightDto_MapsLatestAndPreviousEntries()
    {
        var userId = UserId.New();
        var latestDate = new DateTime(2026, 2, 20, 0, 0, 0, DateTimeKind.Utc);
        var previousDate = latestDate.AddDays(-1);
        var entries = new List<WeightEntry>
        {
            WeightEntry.Create(userId, latestDate, 82.5),
            WeightEntry.Create(userId, previousDate, 83)
        };

        var dto = DashboardMapping.ToWeightDto(entries, desired: 80);

        Assert.NotNull(dto.Latest);
        Assert.NotNull(dto.Previous);
        Assert.Equal(82.5, dto.Latest!.Weight);
        Assert.Equal(83, dto.Previous!.Weight);
        Assert.Equal(80, dto.Desired);
    }

    [Fact]
    public void DashboardMapping_ToWaistDto_MapsLatestAndPreviousEntries()
    {
        var userId = UserId.New();
        var latestDate = new DateTime(2026, 2, 20, 0, 0, 0, DateTimeKind.Utc);
        var previousDate = latestDate.AddDays(-1);
        var entries = new List<WaistEntry>
        {
            WaistEntry.Create(userId, latestDate, 92.1),
            WaistEntry.Create(userId, previousDate, 92.8)
        };

        var dto = DashboardMapping.ToWaistDto(entries, desired: 90);

        Assert.NotNull(dto.Latest);
        Assert.NotNull(dto.Previous);
        Assert.Equal(92.1, dto.Latest!.Circumference);
        Assert.Equal(92.8, dto.Previous!.Circumference);
        Assert.Equal(90, dto.Desired);
    }
}
