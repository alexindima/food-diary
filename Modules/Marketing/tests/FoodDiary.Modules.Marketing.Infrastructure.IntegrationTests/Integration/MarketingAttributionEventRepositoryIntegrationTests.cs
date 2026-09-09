using FoodDiary.Application.Abstractions.Marketing.Common;
using FoodDiary.Infrastructure.Persistence;
using FoodDiary.Modules.Marketing.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Infrastructure.IntegrationTests.Integration;

[Collection(PostgresDatabaseCollection.Name)]
[ExcludeFromCodeCoverage]
public sealed class MarketingAttributionEventRepositoryIntegrationTests(PostgresDatabaseFixture databaseFixture) {
    [RequiresDockerFact]
    public async Task Range_DirectChannelExcludesEveryAttributionSignal() {
        await using FoodDiaryDbContext context = await databaseFixture.CreateDbContextAsync();
        var repository = new MarketingAttributionEventRepository(context);
        DateTime start = new(2030, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        MarketingAttributionEventRecord direct = CreateRecord("page_landing", start, "direct") with {
            UtmSource = null, UtmMedium = null, UtmCampaign = null, UtmContent = null, UtmTerm = null, ReferrerHost = null,
        };
        await repository.AddAsync(direct);
        await repository.AddAsync(direct with { EventId = Guid.NewGuid(), SessionId = "tracked", UtmSource = "source" });
        await context.SaveChangesAsync();
        var filter = new MarketingAttributionRangeFilter(start, start.AddDays(1), 1, 50, EventType: null, Channel: "direct", Search: null);
        MarketingAttributionRangeRecord result = await repository.GetRangeAsync(filter, CancellationToken.None);
        Assert.Equal(1, result.EventTotal);
        Assert.Equal("direct", Assert.Single(result.Current.RecentEvents).SessionId);
        Assert.Equal(2, result.Current.Events);
    }

    [RequiresDockerFact]
    public async Task GetRangeAsync_SeparatesSummaryFromPagedJournalAndPreviousPeriod() {
        await using FoodDiaryDbContext context = await databaseFixture.CreateDbContextAsync();
        var start = new DateTime(2030, 7, 9, 0, 0, 0, DateTimeKind.Utc);
        var repository = new MarketingAttributionEventRepository(context);
        await repository.AddAsync(CreateRecord("page_landing", start.AddDays(-1), "previous"));
        await repository.AddAsync(CreateRecord("page_landing", start, "first") with { UtmCampaign = "literal_%" });
        await repository.AddAsync(CreateRecord("page_landing", start.AddHours(1), "second") with { UtmCampaign = "literal_%" });
        await repository.AddAsync(CreateRecord("signup_completed", start.AddHours(2), "signup"));
        await repository.AddAsync(CreateRecord("page_landing", start.AddDays(1), "exclusive-end"));
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
        var filter = new MarketingAttributionRangeFilter(start, start.AddDays(1), Page: 2, Limit: 1, EventType: "page_landing", Channel: "tracked", Search: "_%");

        MarketingAttributionRangeRecord result = await repository.GetRangeAsync(filter, CancellationToken.None);

        Assert.Equal(3, result.Current.Events);
        Assert.Equal(2, result.Current.Visits);
        Assert.Equal(1, result.Current.Signups);
        Assert.Equal(1, result.Previous.Visits);
        Assert.Equal(2, result.EventTotal);
        Assert.Equal("first", Assert.Single(result.Current.RecentEvents).SessionId);
        Assert.Equal(new MarketingAttributionDayRecord(start, Visits: 2, Signups: 1, PremiumStarts: 0), Assert.Single(result.ByDay));
        Assert.Empty(context.ChangeTracker.Entries());
    }

    [RequiresDockerFact]
    public async Task AddAsync_UsesProvidedEventIdAsDurableDeduplicationKey() {
        var eventId = Guid.NewGuid();
        var occurredAtUtc = new DateTime(2030, 7, 9, 12, 0, 0, DateTimeKind.Utc);
        MarketingAttributionEventRecord record = CreateRecord(
            "page_landing",
            occurredAtUtc,
            "session-dedup") with { EventId = eventId, };
        string connectionString = await databaseFixture.CreateIsolatedDatabaseAsync();

        await using (FoodDiaryDbContext firstContext = databaseFixture.CreateDbContext(connectionString)) {
            await firstContext.Database.MigrateAsync();
            var firstRepository = new MarketingAttributionEventRepository(firstContext);
            await firstRepository.AddAsync(record);
            await firstContext.SaveChangesAsync();
        }

        await using FoodDiaryDbContext duplicateContext = databaseFixture.CreateDbContext(connectionString);
        var duplicateRepository = new MarketingAttributionEventRepository(duplicateContext);
        await duplicateRepository.AddAsync(record);

        await Assert.ThrowsAsync<DbUpdateException>(() => duplicateContext.SaveChangesAsync());
    }

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
        MarketingAttributionSummaryRecord summary = await repository.GetSummaryAsync(cutoffUtc.AddDays(-10));
        MarketingAttributionEventRecord remaining = Assert.Single(summary.RecentEvents);
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
            () => Assert.NotNull(latest?.EventId),
            () => Assert.True(premiumExists),
            () => Assert.False(trialExists),
            () => Assert.Null(missing));
    }

    [RequiresDockerFact]
    public async Task LifecycleEvents_EnforceOneEventPerUserAndType() {
        await using FoodDiaryDbContext context = await databaseFixture.CreateDbContextAsync();
        var userId = Guid.NewGuid();
        var now = new DateTime(2030, 7, 9, 12, 0, 0, DateTimeKind.Utc);
        var repository = new MarketingAttributionEventRepository(context);
        await repository.AddAsync(CreateRecord("premium_started", now, "session-first", userId));
        await repository.AddAsync(CreateRecord("premium_started", now.AddSeconds(1), "session-second", userId));

        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }

    [RequiresDockerFact]
    public async Task GetLandingAsync_RequiresMatchingServerObservedIdentity() {
        await using FoodDiaryDbContext context = await databaseFixture.CreateDbContextAsync();
        var now = new DateTime(2030, 7, 9, 12, 0, 0, DateTimeKind.Utc);
        var repository = new MarketingAttributionEventRepository(context);
        await repository.AddAsync(CreateRecord("page_landing", now, "session-trusted"));
        await context.SaveChangesAsync();

        MarketingAttributionEventRecord? trusted = await repository.GetLandingAsync(
            "anon-session-trusted", "session-trusted", now.AddMinutes(-1));
        MarketingAttributionEventRecord? forged = await repository.GetLandingAsync(
            "anon-session-trusted", "session-forged", now.AddMinutes(-1));

        Assert.NotNull(trusted);
        Assert.Null(forged);
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
