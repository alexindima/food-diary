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

    [Fact]
    public async Task Handle_WithUnsupportedEventType_ReturnsValidationFailure() {
        var repository = new RecordingRepository();
        var handler = new RecordMarketingAttributionCommandHandler(repository, new FixedTimeProvider());
        RecordMarketingAttributionCommand command = CreateCommand("2026-04-02T12:00:00Z") with {
            EventType = "unsupported_event",
        };

        Result result = await handler.Handle(command, CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Validation.Invalid", result.Error.Code);
        Assert.Null(repository.Record);
    }

    [Fact]
    public async Task Handle_WithMissingEventId_ReturnsValidationFailure() {
        var repository = new RecordingRepository();
        var handler = new RecordMarketingAttributionCommandHandler(repository, new FixedTimeProvider());
        RecordMarketingAttributionCommand command = CreateCommand("2026-04-02T12:00:00Z") with {
            EventId = Guid.Empty,
        };

        Result result = await handler.Handle(command, CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Validation.Invalid", result.Error.Code);
        Assert.Null(repository.Record);
    }

    [Fact]
    public async Task Handle_WithUserIdOnAnonymousLandingEvent_ReturnsValidationFailure() {
        var repository = new RecordingRepository();
        var handler = new RecordMarketingAttributionCommandHandler(repository, new FixedTimeProvider());
        RecordMarketingAttributionCommand command = CreateCommand("2026-04-02T12:00:00Z") with {
            EventType = MarketingAttributionEventTypes.PageLanding,
            UserId = Guid.NewGuid(),
        };

        Result result = await handler.Handle(command, CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Validation.Invalid", result.Error.Code);
        Assert.Contains("Anonymous", result.Error.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Null(repository.Record);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("00000000-0000-0000-0000-000000000000")]
    public async Task Handle_WithoutAuthenticatedUserOnSignupCompletedEvent_ReturnsValidationFailure(string? userIdText) {
        var repository = new RecordingRepository();
        var handler = new RecordMarketingAttributionCommandHandler(repository, new FixedTimeProvider());
        Guid? userId = userIdText is null ? null : Guid.Parse(userIdText);
        RecordMarketingAttributionCommand command = CreateCommand("2026-04-02T12:00:00Z") with {
            EventType = MarketingAttributionEventTypes.SignupCompleted,
            UserId = userId,
        };

        Result result = await handler.Handle(command, CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Validation.Invalid", result.Error.Code);
        Assert.Contains("Signup", result.Error.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Null(repository.Record);
    }

    [Fact]
    public async Task Handle_WithSignupCompletedEventAndAuthenticatedUser_RecordsAttribution() {
        var repository = new RecordingRepository {
            Landing = CreateLandingRecord(),
        };
        var handler = new RecordMarketingAttributionCommandHandler(repository, new FixedTimeProvider());
        RecordMarketingAttributionCommand command = CreateCommand("2026-04-02T12:00:00Z") with {
            EventType = MarketingAttributionEventTypes.SignupCompleted,
            UserId = Guid.NewGuid(),
        };

        Result result = await handler.Handle(command, CancellationToken.None);

        ResultAssert.Success(result);
        Assert.NotNull(repository.Record);
        Assert.Equal(command.UserId, repository.Record.UserId);
    }

    [Fact]
    public async Task Handle_WithSignupCompletedEvent_CopiesOnlyServerStoredLanding() {
        var repository = new RecordingRepository { Landing = CreateLandingRecord() };
        var handler = new RecordMarketingAttributionCommandHandler(repository, new FixedTimeProvider());
        RecordMarketingAttributionCommand command = CreateCommand("2026-04-02T12:00:00Z") with {
            EventType = MarketingAttributionEventTypes.SignupCompleted,
            UserId = Guid.NewGuid(),
            UtmSource = "attacker",
        };

        Result result = await handler.Handle(command, CancellationToken.None);

        ResultAssert.Success(result);
        Assert.Equal("trusted", repository.Record?.UtmSource);
    }

    private static MarketingAttributionEventRecord CreateLandingRecord() =>
        new(
            EventType: MarketingAttributionEventTypes.PageLanding,
            OccurredAtUtc: new DateTime(2026, 4, 2, 11, 55, 0, DateTimeKind.Utc),
            UserId: null,
            AnonymousId: "anonymous",
            SessionId: "session",
            LandingPath: "/trusted",
            ReferrerHost: null,
            UtmSource: "trusted",
            UtmMedium: null,
            UtmCampaign: null,
            UtmContent: null,
            UtmTerm: null,
            BuildVersion: null,
            EventId: Guid.NewGuid());

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
    private sealed class RecordingRepository : IMarketingAttributionEventRepository {
        public MarketingAttributionEventRecord? Record { get; private set; }
        public MarketingAttributionEventRecord? Landing { get; init; }

        public Task AddAsync(MarketingAttributionEventRecord record, CancellationToken cancellationToken = default) {
            Record = record;
            return Task.CompletedTask;
        }

        public Task<int> DeleteOlderThanAsync(DateTime olderThanUtc, int batchSize, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<MarketingAttributionSummaryRecord> GetSummaryAsync(DateTime sinceUtc, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<MarketingAttributionEventRecord?> GetLandingAsync(string anonymousId, string sessionId, DateTime sinceUtc, CancellationToken cancellationToken = default) =>
            Task.FromResult(Landing);

        public Task<MarketingAttributionEventRecord?> GetLatestForUserAsync(Guid userId, CancellationToken cancellationToken = default) =>
            Task.FromResult<MarketingAttributionEventRecord?>(null);

        public Task<bool> ExistsForUserAsync(Guid userId, string eventType, CancellationToken cancellationToken = default) =>
            Task.FromResult(false);
    }

    [ExcludeFromCodeCoverage]
    private sealed class FixedTimeProvider : TimeProvider {
        public override DateTimeOffset GetUtcNow() => new(2026, 4, 2, 12, 0, 0, TimeSpan.Zero);
    }
}
