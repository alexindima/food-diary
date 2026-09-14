using FoodDiary.Domain.Entities.Tracking;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Application.Abstractions.Dashboard.Models;
using FoodDiary.Infrastructure.Persistence;
using FoodDiary.Modules.Dashboard.Infrastructure.Persistence.Dashboard;

namespace FoodDiary.Infrastructure.IntegrationTests.Integration;

[Collection(PostgresDatabaseCollection.Name)]
[ExcludeFromCodeCoverage]
public sealed class DashboardBodyReadServiceIntegrationTests(PostgresDatabaseFixture databaseFixture) {
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
