using FoodDiary.Application.Abstractions.Marketing.Common;
using FoodDiary.Application.Marketing.Commands.RecordMarketingAttribution;
using FoodDiary.Application.Marketing.Common;
using FoodDiary.Results;

namespace FoodDiary.Application.Tests.Marketing;

[ExcludeFromCodeCoverage]
public sealed class MarketingAttributionCoverageTests {
    [Fact]
    public async Task Handle_WithWhitespaceAndOversizedValues_NormalizesStoredRecord() {
        var repository = new RecordingRepository();
        var handler = new RecordMarketingAttributionCommandHandler(repository, new FixedTimeProvider());
        RecordMarketingAttributionCommand command = CreateCommand("2026-04-02T12:00:00Z") with {
            AnonymousId = "   ",
            SessionId = new string('s', 120),
            LandingPath = "  /landing  ",
            UtmSource = "  source  ",
        };

        Result result = await handler.Handle(command, CancellationToken.None);

        ResultAssert.Success(result);
        MarketingAttributionEventRecord record = Assert.IsType<MarketingAttributionEventRecord>(repository.Record);
        Assert.Multiple(
            () => Assert.Equal("unknown", record.AnonymousId),
            () => Assert.Equal(96, record.SessionId.Length),
            () => Assert.Equal("/landing", record.LandingPath),
            () => Assert.Equal("source", record.UtmSource));
    }

    [Theory]
    [InlineData("not-a-date")]
    [InlineData("2026-04-01T00:00:00Z")]
    [InlineData("2026-04-03T00:00:00Z")]
    public async Task Handle_WithInvalidOrOutOfWindowTimestamp_ReturnsValidationFailure(string timestamp) {
        var repository = new RecordingRepository();
        var handler = new RecordMarketingAttributionCommandHandler(repository, new FixedTimeProvider());

        Result result = await handler.Handle(
            CreateCommand(timestamp),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Validation.Invalid", result.Error.Code);
        Assert.Null(repository.Record);
    }

    private static RecordMarketingAttributionCommand CreateCommand(string timestamp) =>
        new(
            EventType: MarketingAttributionEventTypes.PageLanding,
            Timestamp: timestamp,
            UserId: null,
            AnonymousId: "anonymous",
            SessionId: "session",
            LandingPath: "/",
            ReferrerHost: null,
            UtmSource: null,
            UtmMedium: null,
            UtmCampaign: null,
            UtmContent: null,
            UtmTerm: null,
            BuildVersion: null,
            EventId: Guid.NewGuid());

    [ExcludeFromCodeCoverage]
    private sealed class RecordingRepository : IMarketingAttributionEventWriteRepository {
        public MarketingAttributionEventRecord? Record { get; private set; }

        public Task AddAsync(MarketingAttributionEventRecord record, CancellationToken cancellationToken = default) {
            Record = record;
            return Task.CompletedTask;
        }

        public Task<int> DeleteOlderThanAsync(DateTime olderThanUtc, int batchSize, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }

    [ExcludeFromCodeCoverage]
    private sealed class FixedTimeProvider : TimeProvider {
        public override DateTimeOffset GetUtcNow() => new(2026, 4, 2, 12, 0, 0, TimeSpan.Zero);
    }
}
