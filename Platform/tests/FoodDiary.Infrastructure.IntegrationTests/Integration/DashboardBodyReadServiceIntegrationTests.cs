using FoodDiary.Modules.Hydration.Domain.Entities.Tracking;
using FoodDiary.Modules.BodyMetrics.Domain.Entities.Tracking;
using FoodDiary.Modules.Users.Domain.Entities;
using FoodDiary.Modules.Dashboard.Application.Abstractions.Models;
using FoodDiary.Infrastructure.Persistence;
using FoodDiary.ReadModel.Composition.Dashboard;

namespace FoodDiary.Infrastructure.IntegrationTests.Integration;

[Collection(PostgresDatabaseCollection.Name)]
[ExcludeFromCodeCoverage]
public sealed class DashboardBodyReadServiceIntegrationTests(PostgresDatabaseFixture databaseFixture) {
    [RequiresDockerTheory]
    [InlineData(10)]
    [InlineData(12)]
    public async Task GetBodyAsync_BoundsHydrationHistoryInPostgres(int count) {
        await using FoodDiaryDbContext context = await databaseFixture.CreateDbContextAsync();
        var user = User.Create($"dashboard-water-{Guid.NewGuid():N}@example.com", "hash");
        context.Users.Add(user);
        DateTime start = UtcDate(2026, 8, 27, 20);
        HydrationEntry[] entries = [.. Enumerable.Range(0, count)
            .Select(index => HydrationEntry.Create(user.Id, start.AddMinutes(index), 150))];
        context.HydrationEntries.AddRange(entries);
        context.HydrationEntries.Add(HydrationEntry.Create(user.Id, start.AddDays(1), 1000));
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        DashboardBodyReadModel result = await new DashboardBodyReadService(context).GetBodyAsync(
            user.Id, start, start.AddDays(1).AddMilliseconds(-1), start.Date, trendQuantizationDays: 1, includeWeight: false, includeWaist: false, includeHydration: true, cancellationToken: CancellationToken.None);

        Assert.NotNull(result.HydrationHistory);
        Assert.Multiple(
            () => Assert.Equal(count * 150, result.HydrationTotalMl),
            () => Assert.Equal(count > 10, result.HydrationHistory.HasMore),
            () => Assert.Equal(entries.Reverse().Take(10).Select(entry => entry.Id.Value), result.HydrationHistory.Entries.Select(entry => entry.Id)),
            () => Assert.Empty(context.ChangeTracker.Entries()));
    }

    [RequiresDockerFact]
    public async Task GetBodyAsync_CombinesLatestAndTrendQueriesOnPostgres() {
        await using FoodDiaryDbContext context = await databaseFixture.CreateDbContextAsync();
        var user = User.Create($"dashboard-body-{Guid.NewGuid():N}@example.com", "hash");
        var otherUser = User.Create($"dashboard-body-other-{Guid.NewGuid():N}@example.com", "hash");
        context.Users.AddRange(user, otherUser);
        context.WeightEntries.AddRange(
            WeightEntry.Create(user.Id, UtcDate(2026, 8, 26), 80),
            WeightEntry.Create(user.Id, UtcDate(2026, 8, 27), 79),
            WeightEntry.Create(user.Id, UtcDate(2026, 8, 28), 78));
        context.WaistEntries.AddRange(
            WaistEntry.Create(user.Id, UtcDate(2026, 8, 26), 90),
            WaistEntry.Create(user.Id, UtcDate(2026, 8, 28), 88));
        context.WeightEntries.Add(WeightEntry.Create(otherUser.Id, UtcDate(2026, 8, 28), 150));
        context.WaistEntries.Add(WaistEntry.Create(otherUser.Id, UtcDate(2026, 8, 28), 120));
        context.HydrationEntries.AddRange(
            HydrationEntry.Create(user.Id, UtcDate(2026, 8, 28), 250),
            HydrationEntry.Create(user.Id, UtcDate(2026, 8, 28, 23, 59, 59), 300),
            HydrationEntry.Create(user.Id, UtcDate(2026, 8, 29), 400),
            HydrationEntry.Create(otherUser.Id, UtcDate(2026, 8, 28), 1000));
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
        var service = new DashboardBodyReadService(context);

        DashboardBodyReadModel result = await service.GetBodyAsync(
            user.Id,
            UtcDate(2026, 8, 28),
            UtcDate(2026, 8, 28, 23, 59, 59),
            UtcDate(2026, 8, 26),
            trendQuantizationDays: 1,
            includeWeight: true,
            includeWaist: true,
            includeHydration: true,
            CancellationToken.None);

        Assert.Multiple(
            () => Assert.Equal(2, result.LatestWeightEntries.Count),
            () => Assert.Equal(2, result.LatestWaistEntries.Count),
            () => Assert.Equal(3, result.WeightTrend.Count),
            () => Assert.Equal(3, result.WaistTrend.Count),
            () => Assert.Equal(new double[] { 78, 79 }, result.LatestWeightEntries.Select(entry => entry.WeightKg)),
            () => Assert.Equal(new double[] { 88, 90 }, result.LatestWaistEntries.Select(entry => entry.CircumferenceCm)),
            () => Assert.Equal(new double[] { 80, 79, 78 }, result.WeightTrend.Select(entry => entry.AverageWeightKg)),
            () => Assert.Equal(new double[] { 90, 0, 88 }, result.WaistTrend.Select(entry => entry.AverageCircumferenceCm)),
            () => Assert.Equal(550, result.HydrationTotalMl),
            () => Assert.Empty(context.ChangeTracker.Entries()));
    }

    private static DateTime UtcDate(int year, int month, int day, int hour = 0, int minute = 0, int second = 0) =>
        new(year, month, day, hour, minute, second, DateTimeKind.Utc);
}
