using FoodDiary.Application.Abstractions.Marketing.Common;
using FoodDiary.Infrastructure.Persistence;
using FoodDiary.Infrastructure.Persistence.Tracking;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Infrastructure.Tests.Integration;

[Collection(PostgresDatabaseCollection.Name)]
[ExcludeFromCodeCoverage]
public sealed class MarketingAttributionEventRepositoryIntegrationTests(PostgresDatabaseFixture databaseFixture) {
    [RequiresDockerFact]
    public async Task DeleteOlderThanAsync_DeletesOnlyExpiredEventsWithinBatch() {
        await using FoodDiaryDbContext context = await databaseFixture.CreateDbContextAsync();
        var cutoffUtc = new DateTime(2030, 7, 9, 12, 0, 0, DateTimeKind.Utc);
        var repository = new MarketingAttributionEventRepository(context);

        await repository.AddAsync(CreateRecord("page_landing", cutoffUtc.AddDays(-3), "session-oldest"));
        await repository.AddAsync(CreateRecord("signup_completed", cutoffUtc.AddDays(-2), "session-older"));
        await repository.AddAsync(CreateRecord("premium_started", cutoffUtc.AddMinutes(1), "session-fresh"));
        await context.SaveChangesAsync();

        int firstDeletedCount = await repository.DeleteOlderThanAsync(cutoffUtc, batchSize: 1);
        int secondDeletedCount = await repository.DeleteOlderThanAsync(cutoffUtc, batchSize: 10);

        Assert.Equal(1, firstDeletedCount);
        Assert.Equal(1, secondDeletedCount);
        MarketingAttributionEventRecord remaining = Assert.Single(await repository.GetSinceAsync(cutoffUtc.AddDays(-10)));
        Assert.Equal("premium_started", remaining.EventType);
        Assert.Equal("session-fresh", remaining.SessionId);
        Assert.Equal(1, await context.MarketingAttributionEvents.AsNoTracking().CountAsync());
    }

    private static MarketingAttributionEventRecord CreateRecord(
        string eventType,
        DateTime occurredAtUtc,
        string sessionId) =>
        new(
            eventType,
            occurredAtUtc,
            UserId: null,
            AnonymousId: $"anon-{sessionId}",
            sessionId,
            LandingPath: "/",
            ReferrerHost: null,
            UtmSource: "telegram",
            UtmMedium: "social",
            UtmCampaign: "2026_07_launch",
            UtmContent: null,
            UtmTerm: null,
            BuildVersion: null);
}
