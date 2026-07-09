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

        int noneDeletedCount = await repository.DeleteOlderThanAsync(cutoffUtc.AddDays(-10), batchSize: 10);
        int firstDeletedCount = await repository.DeleteOlderThanAsync(cutoffUtc, batchSize: 1);
        int secondDeletedCount = await repository.DeleteOlderThanAsync(cutoffUtc, batchSize: 10);

        Assert.Equal(0, noneDeletedCount);
        Assert.Equal(1, firstDeletedCount);
        Assert.Equal(1, secondDeletedCount);
        MarketingAttributionEventRecord remaining = Assert.Single(await repository.GetSinceAsync(cutoffUtc.AddDays(-10)));
        Assert.Equal("premium_started", remaining.EventType);
        Assert.Equal("session-fresh", remaining.SessionId);
        Assert.Equal(1, await context.MarketingAttributionEvents.AsNoTracking().CountAsync());
    }

    [RequiresDockerFact]
    public async Task UserScopedQueries_ReturnLatestAndExistenceFlags() {
        await using FoodDiaryDbContext context = await databaseFixture.CreateDbContextAsync();
        var userId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var now = new DateTime(2030, 7, 9, 12, 0, 0, DateTimeKind.Utc);
        var repository = new MarketingAttributionEventRepository(context);

        await repository.AddAsync(CreateRecord("signup_completed", now.AddMinutes(-10), "session-old", userId));
        await repository.AddAsync(CreateRecord("premium_started", now, "session-new", userId));
        await repository.AddAsync(CreateRecord("signup_completed", now.AddMinutes(1), "session-other", otherUserId));
        await context.SaveChangesAsync();

        MarketingAttributionEventRecord? latest = await repository.GetLatestForUserAsync(userId);
        bool premiumExists = await repository.ExistsForUserAsync(userId, "premium_started");
        bool trialExists = await repository.ExistsForUserAsync(userId, "trial_started");
        MarketingAttributionEventRecord? missing = await repository.GetLatestForUserAsync(Guid.NewGuid());

        Assert.Multiple(
            () => Assert.NotNull(latest),
            () => Assert.Equal("premium_started", latest?.EventType),
            () => Assert.Equal("session-new", latest?.SessionId),
            () => Assert.True(premiumExists),
            () => Assert.False(trialExists),
            () => Assert.Null(missing));
    }

    private static MarketingAttributionEventRecord CreateRecord(
        string eventType,
        DateTime occurredAtUtc,
        string sessionId,
        Guid? userId = null) =>
        new(
            eventType,
            occurredAtUtc,
            userId,
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
