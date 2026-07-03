using FoodDiary.Application.Abstractions.Dashboard.Models;
using FoodDiary.Domain.Entities.Tracking;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Infrastructure.Persistence;
using FoodDiary.Infrastructure.Persistence.Dashboard;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Infrastructure.Tests.Persistence;

[ExcludeFromCodeCoverage]
public sealed class DashboardBodyReadServiceTests {
    [Fact]
    public async Task GetBodyAsync_ProjectsBodyMetricsAndHydration() {
        await using FoodDiaryDbContext context = CreateContext();
        var user = User.Create($"dashboard-body-{Guid.NewGuid():N}@example.com", "hash");
        context.Users.Add(user);

        context.WeightEntries.AddRange(
            WeightEntry.Create(user.Id, UtcDate(2026, 5, 30), 80),
            WeightEntry.Create(user.Id, UtcDate(2026, 5, 31), 79),
            WeightEntry.Create(user.Id, UtcDate(2026, 6, 1), 78),
            WeightEntry.Create(user.Id, UtcDate(2026, 6, 2), 77));
        context.WaistEntries.AddRange(
            WaistEntry.Create(user.Id, UtcDate(2026, 5, 31), 90),
            WaistEntry.Create(user.Id, UtcDate(2026, 6, 1), 88),
            WaistEntry.Create(user.Id, UtcDate(2026, 6, 2), 87));
        context.HydrationEntries.AddRange(
            HydrationEntry.Create(user.Id, UtcInstant(2026, 6, 1, 8), 250),
            HydrationEntry.Create(user.Id, UtcInstant(2026, 6, 2, 9), 500),
            HydrationEntry.Create(user.Id, UtcInstant(2026, 6, 3, 9), 750));
        await context.SaveChangesAsync();

        var readService = new DashboardBodyReadService(context);

        DashboardBodyReadModel result = await readService.GetBodyAsync(
            user.Id,
            UtcDate(2026, 6, 1),
            UtcDate(2026, 6, 2),
            UtcDate(2026, 5, 31),
            trendQuantizationDays: 1,
            includeWeight: true,
            includeWaist: true,
            includeHydration: true,
            CancellationToken.None);

        Assert.Collection(
            result.LatestWeightEntries,
            latest => Assert.Equal(77, latest.Weight),
            previous => Assert.Equal(78, previous.Weight));
        Assert.Collection(
            result.LatestWaistEntries,
            latest => Assert.Equal(87, latest.Circumference),
            previous => Assert.Equal(88, previous.Circumference));
        Assert.Collection(
            result.WeightTrend,
            first => Assert.Equal(79, first.AverageWeight),
            second => Assert.Equal(78, second.AverageWeight));
        Assert.Collection(
            result.WaistTrend,
            first => Assert.Equal(90, first.AverageCircumference),
            second => Assert.Equal(88, second.AverageCircumference));
        Assert.Equal(750, result.HydrationTotalMl);
    }

    [Fact]
    public async Task GetBodyAsync_WhenSectionsAreExcluded_ReturnsEmptyModel() {
        await using FoodDiaryDbContext context = CreateContext();
        var user = User.Create($"dashboard-body-empty-{Guid.NewGuid():N}@example.com", "hash");
        context.Users.Add(user);
        context.WeightEntries.Add(WeightEntry.Create(user.Id, UtcDate(2026, 6, 1), 78));
        context.WaistEntries.Add(WaistEntry.Create(user.Id, UtcDate(2026, 6, 1), 88));
        context.HydrationEntries.Add(HydrationEntry.Create(user.Id, UtcInstant(2026, 6, 1, 8), 250));
        await context.SaveChangesAsync();

        var readService = new DashboardBodyReadService(context);

        DashboardBodyReadModel result = await readService.GetBodyAsync(
            user.Id,
            UtcDate(2026, 6, 1),
            UtcDate(2026, 6, 1),
            UtcDate(2026, 6, 1),
            trendQuantizationDays: 1,
            includeWeight: false,
            includeWaist: false,
            includeHydration: false,
            CancellationToken.None);

        Assert.Empty(result.LatestWeightEntries);
        Assert.Empty(result.LatestWaistEntries);
        Assert.All(result.WeightTrend, point => Assert.Equal(0, point.AverageWeight));
        Assert.All(result.WaistTrend, point => Assert.Equal(0, point.AverageCircumference));
        Assert.Equal(0, result.HydrationTotalMl);
    }

    [Fact]
    public async Task GetBodyAsync_WithPartialFinalBucket_ClampsBucketEndToRangeEnd() {
        await using FoodDiaryDbContext context = CreateContext();
        var user = User.Create($"dashboard-body-partial-bucket-{Guid.NewGuid():N}@example.com", "hash");
        context.Users.Add(user);
        context.WeightEntries.AddRange(
            WeightEntry.Create(user.Id, UtcDate(2026, 6, 1), 80),
            WeightEntry.Create(user.Id, UtcDate(2026, 6, 5), 82));
        await context.SaveChangesAsync();
        var readService = new DashboardBodyReadService(context);

        DashboardBodyReadModel result = await readService.GetBodyAsync(
            user.Id,
            UtcDate(2026, 6, 5),
            UtcDate(2026, 6, 5),
            UtcDate(2026, 6, 1),
            trendQuantizationDays: 3,
            includeWeight: true,
            includeWaist: false,
            includeHydration: false,
            CancellationToken.None);

        Assert.Equal(2, result.WeightTrend.Count);
        Assert.Equal(UtcDate(2026, 6, 5), result.WeightTrend[1].DateTo);
    }

    private static FoodDiaryDbContext CreateContext() {
        DbContextOptions<FoodDiaryDbContext> options = new DbContextOptionsBuilder<FoodDiaryDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;

        return new FoodDiaryDbContext(options);
    }

    private static DateTime UtcDate(int year, int month, int day) =>
        new(year, month, day, 0, 0, 0, DateTimeKind.Utc);

    private static DateTime UtcInstant(int year, int month, int day, int hour) =>
        new(year, month, day, hour, 0, 0, DateTimeKind.Utc);
}
