using FoodDiary.Domain.Entities.Tracking;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Application.Abstractions.Dashboard.Models;
using FoodDiary.Infrastructure.Persistence;
using FoodDiary.Infrastructure.Persistence.Dashboard;

namespace FoodDiary.Infrastructure.IntegrationTests.Integration;

[Collection(PostgresDatabaseCollection.Name)]
[ExcludeFromCodeCoverage]
public sealed class DashboardBodyReadServiceIntegrationTests(PostgresDatabaseFixture databaseFixture) {
    [RequiresDockerFact]
    public async Task GetBodyAsync_CombinesLatestAndTrendQueriesOnPostgres() {
        await using FoodDiaryDbContext context = await databaseFixture.CreateDbContextAsync();
        var user = User.Create($"dashboard-body-{Guid.NewGuid():N}@example.com", "hash");
        context.Users.Add(user);
        context.WeightEntries.AddRange(
            WeightEntry.Create(user.Id, UtcDate(2026, 8, 26), 80),
            WeightEntry.Create(user.Id, UtcDate(2026, 8, 27), 79),
            WeightEntry.Create(user.Id, UtcDate(2026, 8, 28), 78));
        context.WaistEntries.AddRange(
            WaistEntry.Create(user.Id, UtcDate(2026, 8, 26), 90),
            WaistEntry.Create(user.Id, UtcDate(2026, 8, 28), 88));
        await context.SaveChangesAsync();
        var service = new DashboardBodyReadService(context);

        DashboardBodyReadModel result = await service.GetBodyAsync(
            user.Id,
            UtcDate(2026, 8, 28),
            UtcDate(2026, 8, 28, 23, 59, 59),
            UtcDate(2026, 8, 26),
            trendQuantizationDays: 1,
            includeWeight: true,
            includeWaist: true,
            includeHydration: false,
            CancellationToken.None);

        Assert.Multiple(
            () => Assert.Equal(2, result.LatestWeightEntries.Count),
            () => Assert.Equal(2, result.LatestWaistEntries.Count),
            () => Assert.Equal(3, result.WeightTrend.Count),
            () => Assert.Equal(3, result.WaistTrend.Count));
    }

    private static DateTime UtcDate(int year, int month, int day, int hour = 0, int minute = 0, int second = 0) =>
        new(year, month, day, hour, minute, second, DateTimeKind.Utc);
}
