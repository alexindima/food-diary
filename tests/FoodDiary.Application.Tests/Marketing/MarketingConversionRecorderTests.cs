using FoodDiary.Application.Abstractions.Marketing.Common;
using FoodDiary.Application.Marketing.Services;

namespace FoodDiary.Application.Tests.Marketing;

[ExcludeFromCodeCoverage]
public sealed class MarketingConversionRecorderTests {
    private static readonly DateTime Now = new(2026, 7, 9, 10, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task RecordPremiumStartedAsync_WithUserAttribution_CopiesLatestAttributionContext() {
        var userId = Guid.NewGuid();
        var repository = new InMemoryMarketingAttributionEventRepository(
            new MarketingAttributionEventRecord(
                "signup_completed",
                Now.AddMinutes(-5),
                userId,
                "anon-1",
                "session-1",
                "/?utm_source=telegram",
                "t.me",
                "telegram",
                "social",
                "2026_07_launch",
                "story",
                "food",
                "1.2.3"));
        var recorder = new MarketingConversionRecorder(repository, repository, new FixedDateTimeProvider(Now));

        await recorder.RecordPremiumStartedAsync(userId, CancellationToken.None);

        MarketingAttributionEventRecord premiumEvent = Assert.Single(repository.Records, record =>
            string.Equals(record.EventType, "premium_started", StringComparison.Ordinal));
        Assert.Equal(userId, premiumEvent.UserId);
        Assert.Equal(Now, premiumEvent.OccurredAtUtc);
        Assert.Equal("telegram", premiumEvent.UtmSource);
        Assert.Equal("social", premiumEvent.UtmMedium);
        Assert.Equal("2026_07_launch", premiumEvent.UtmCampaign);
        Assert.Equal("anon-1", premiumEvent.AnonymousId);
        Assert.Equal("session-1", premiumEvent.SessionId);
    }

    [Fact]
    public async Task RecordPremiumStartedAsync_WhenAlreadyRecorded_DoesNotAddDuplicate() {
        var userId = Guid.NewGuid();
        var repository = new InMemoryMarketingAttributionEventRepository(
            new MarketingAttributionEventRecord(
                "premium_started",
                Now.AddMinutes(-1),
                userId,
                "anon-1",
                "session-1",
                "/",
                ReferrerHost: null,
                UtmSource: null,
                UtmMedium: null,
                UtmCampaign: null,
                UtmContent: null,
                UtmTerm: null,
                BuildVersion: null));
        var recorder = new MarketingConversionRecorder(repository, repository, new FixedDateTimeProvider(Now));

        await recorder.RecordPremiumStartedAsync(userId, CancellationToken.None);

        Assert.Single(repository.Records);
    }

    [ExcludeFromCodeCoverage]
    private sealed class FixedDateTimeProvider(DateTime utcNow) : TimeProvider {
        public override DateTimeOffset GetUtcNow() => new(utcNow);
    }

    [ExcludeFromCodeCoverage]
    private sealed class InMemoryMarketingAttributionEventRepository(params MarketingAttributionEventRecord[] seedRecords)
        : IMarketingAttributionEventRepository {
        public List<MarketingAttributionEventRecord> Records { get; } = [.. seedRecords];

        public Task AddAsync(MarketingAttributionEventRecord record, CancellationToken cancellationToken = default) {
            Records.Add(record);
            return Task.CompletedTask;
        }

        public Task<int> DeleteOlderThanAsync(DateTime olderThanUtc, int batchSize, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<MarketingAttributionEventRecord>> GetSinceAsync(DateTime sinceUtc, CancellationToken cancellationToken = default) {
            IReadOnlyList<MarketingAttributionEventRecord> matchingRecords = [
                .. Records
                .Where(record => record.OccurredAtUtc >= sinceUtc)
                .OrderByDescending(record => record.OccurredAtUtc),
            ];
            return Task.FromResult(matchingRecords);
        }

        public Task<MarketingAttributionEventRecord?> GetLatestForUserAsync(Guid userId, CancellationToken cancellationToken = default) =>
            Task.FromResult(Records
                .Where(record => record.UserId == userId)
                .OrderByDescending(record => record.OccurredAtUtc)
                .FirstOrDefault());

        public Task<bool> ExistsForUserAsync(Guid userId, string eventType, CancellationToken cancellationToken = default) =>
            Task.FromResult(Records.Any(record =>
                record.UserId == userId &&
                string.Equals(record.EventType, eventType, StringComparison.Ordinal)));
    }
}
