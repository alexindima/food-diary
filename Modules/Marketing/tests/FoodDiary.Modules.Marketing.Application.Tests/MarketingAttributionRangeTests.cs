using FoodDiary.Modules.Marketing.Application.Abstractions.Common;
using FoodDiary.Modules.Marketing.Contracts.Models;
using FoodDiary.Modules.Marketing.Application.Queries.GetMarketingAttributionRange;
using FoodDiary.Modules.Marketing.Contracts.Queries.GetMarketingAttributionRange;
using FoodDiary.Results;
using FoodDiary.Application.Contracts.Common.Validation;
using FoodDiary.Modules.Marketing.Domain.ValueObjects;

namespace FoodDiary.Modules.Marketing.Application.Tests;

[ExcludeFromCodeCoverage]
public sealed class MarketingAttributionRangeTests {
    [Fact]
    public async Task Handle_WithHistoricalLanding_SanitizesReportProjection() {
        TimeProvider clock = Substitute.For<TimeProvider>();
        var to = new DateTimeOffset(2026, 9, 9, 12, 0, 0, TimeSpan.Zero);
        clock.GetUtcNow().Returns(to);
        IMarketingAttributionRangeReadRepository repository = Substitute.For<IMarketingAttributionRangeReadRepository>();
        var historical = new MarketingAttributionEventRecord("page_landing", to.AddMinutes(-5).UtcDateTime,
            UserId: null, new AnonymousVisitorId("visitor"), new MarketingSessionId("session"),
            "/verify-email?token=historical-secret&utm_source=telegram", "t.me", "telegram",
            UtmMedium: null, UtmCampaign: null, UtmContent: null, UtmTerm: null, BuildVersion: null);
        var summary = new MarketingAttributionSummaryRecord(1, 1, 0, 0, 1, 1, 1, 1, historical.OccurredAtUtc, [], [], [historical]);
        repository.GetRangeAsync(Arg.Any<MarketingAttributionRangeFilter>(), CancellationToken.None)
            .Returns(new MarketingAttributionRangeRecord(summary, summary, [], 1));

        Result<MarketingAttributionRangeModel> result = await new GetMarketingAttributionRangeQueryHandler(repository, clock)
            .Handle(new GetMarketingAttributionRangeQuery(to.AddDays(-1), to), CancellationToken.None);

        ResultAssert.Success(result);
        Assert.Equal("/verify-email?utm_source=telegram", Assert.Single(result.Value.Current.RecentEvents).LandingPath);
        Assert.Equal("/verify-email?utm_source=telegram", Assert.Single(result.Value.Previous.RecentEvents).LandingPath);
        Assert.Equal("/verify-email?token=historical-secret&utm_source=telegram", historical.LandingPath);
    }

    [Fact]
    public async Task Handle_UsesUtcRangePreviousPeriodAndRoundedWindow() {
        using var cancellation = new CancellationTokenSource();
        TimeProvider clock = Substitute.For<TimeProvider>();
        clock.GetUtcNow().Returns(new DateTimeOffset(2026, 9, 9, 12, 0, 0, TimeSpan.Zero));
        IMarketingAttributionRangeReadRepository repository = Substitute.For<IMarketingAttributionRangeReadRepository>();
        var from = new DateTimeOffset(2026, 9, 8, 0, 0, 0, TimeSpan.FromHours(3));
        DateTimeOffset to = from.AddHours(24).AddMinutes(30);
        var summary = new MarketingAttributionSummaryRecord(12, 10, 2, 1, 8, 9, 5, 4, from.UtcDateTime, [], [], []);
        MarketingAttributionSummaryRecord previous = summary with { Events = 4, Visits = 4 };
        var record = new MarketingAttributionRangeRecord(summary, previous, [new MarketingAttributionDayRecord(from.UtcDateTime.Date, 10, 2, 1)], 31);
        var filter = new MarketingAttributionRangeFilter(from.UtcDateTime, to.UtcDateTime, 2, 10, "page_landing", "tracked", "source");
        repository.GetRangeAsync(filter, cancellation.Token).Returns(record);
        var handler = new GetMarketingAttributionRangeQueryHandler(repository, clock);
        Result<MarketingAttributionRangeModel> result = await handler.Handle(new GetMarketingAttributionRangeQuery(from, to, 2, 10, "page_landing", "tracked", "source"), cancellation.Token);
        ResultAssert.Success(result);
        MarketingAttributionDayModel day = Assert.Single(result.Value.ByDay);
        Assert.Multiple(
            () => Assert.Equal(from.UtcDateTime, result.Value.FromUtc),
            () => Assert.Equal(to.UtcDateTime, result.Value.ToUtc),
            () => Assert.Equal(from.UtcDateTime - (to - from), result.Value.PreviousFromUtc),
            () => Assert.Equal(25, result.Value.Current.WindowHours),
            () => Assert.Equal(12, result.Value.Current.Events),
            () => Assert.Equal(4, result.Value.Previous.Events),
            () => Assert.Equal(20, result.Value.Current.SignupRatePercent),
            () => Assert.Equal(31, result.Value.EventTotal),
            () => Assert.Equal(new MarketingAttributionDayModel(from.UtcDateTime.Date, 10, 2, 1), day));
        await repository.Received(1).GetRangeAsync(filter, cancellation.Token);
    }

    [Theory]
    [InlineData("before-epoch")]
    [InlineData("empty-range")]
    [InlineData("future")]
    [InlineData("range")]
    [InlineData("page")]
    [InlineData("limit")]
    [InlineData("search")]
    [InlineData("event")]
    [InlineData("channel")]
    public async Task InvalidRangeOrFilter_DoesNotReadRepository(string invalid) {
        TimeProvider clock = Substitute.For<TimeProvider>();
        clock.GetUtcNow().Returns(DateTimeOffset.UnixEpoch.AddDays(2));
        IMarketingAttributionRangeReadRepository repository = Substitute.For<IMarketingAttributionRangeReadRepository>();
        var query = new GetMarketingAttributionRangeQuery(DateTimeOffset.UnixEpoch, DateTimeOffset.UnixEpoch.AddDays(1));
        query = invalid switch {
            "before-epoch" => query with { FromUtc = DateTimeOffset.UnixEpoch.AddTicks(-1) },
            "empty-range" => query with { ToUtc = query.FromUtc },
            "future" => query with { ToUtc = DateTimeOffset.UnixEpoch.AddDays(4) },
            "range" => query with {
                ToUtc = query.FromUtc.AddDays(TemporalRangePolicy.MaxPeriodDays).AddTicks(1),
            },
            "page" => query with { Page = 0 },
            "limit" => query with { Limit = 101 },
            "search" => query with { Search = new string('s', 321) },
            "event" => query with { EventType = "unknown" },
            "channel" => query with { Channel = "unknown" },
            _ => throw new InvalidOperationException(),
        };
        Result<MarketingAttributionRangeModel> result = await new GetMarketingAttributionRangeQueryHandler(repository, clock).Handle(query, CancellationToken.None);
        ResultAssert.Failure(result);
        Assert.Equal("Validation.Invalid", result.Error.Code);
        Assert.Empty(repository.ReceivedCalls());
    }

    [Fact]
    public async Task MaximumRange_ReadsRepository() {
        TimeProvider clock = Substitute.For<TimeProvider>();
        DateTimeOffset to = DateTimeOffset.UnixEpoch.AddDays(TemporalRangePolicy.MaxPeriodDays);
        clock.GetUtcNow().Returns(to);
        IMarketingAttributionRangeReadRepository repository = Substitute.For<IMarketingAttributionRangeReadRepository>();
        repository.GetRangeAsync(Arg.Any<MarketingAttributionRangeFilter>(), Arg.Any<CancellationToken>())
            .Returns(new MarketingAttributionRangeRecord(
                new MarketingAttributionSummaryRecord(0, 0, 0, 0, 0, 0, 0, 0, DateTime.UnixEpoch, [], [], []),
                new MarketingAttributionSummaryRecord(0, 0, 0, 0, 0, 0, 0, 0, DateTime.UnixEpoch, [], [], []),
                [],
                0));

        Result<MarketingAttributionRangeModel> result = await new GetMarketingAttributionRangeQueryHandler(repository, clock)
            .Handle(new GetMarketingAttributionRangeQuery(DateTimeOffset.UnixEpoch, to), CancellationToken.None);

        ResultAssert.Success(result);
        await repository.Received(1).GetRangeAsync(Arg.Any<MarketingAttributionRangeFilter>(), Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData("all-time")]
    [InlineData("long-custom")]
    [InlineData("custom-limit")]
    public async Task AdminReportingPeriod_ReadsTheRequestedRetainedRange(string period) {
        TimeProvider clock = Substitute.For<TimeProvider>();
        clock.GetUtcNow().Returns(new DateTimeOffset(2026, 10, 17, 12, 0, 0, TimeSpan.Zero));
        var to = new DateTimeOffset(2026, 10, 18, 0, 0, 0, TimeSpan.Zero);
        DateTimeOffset from = period switch {
            "all-time" => DateTimeOffset.UnixEpoch,
            "long-custom" => new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero),
            "custom-limit" => to.AddDays(-3661),
            _ => throw new InvalidOperationException(),
        };
        IMarketingAttributionRangeReadRepository repository = Substitute.For<IMarketingAttributionRangeReadRepository>();
        repository.GetRangeAsync(Arg.Any<MarketingAttributionRangeFilter>(), Arg.Any<CancellationToken>()).Returns(EmptyRange());

        Result<MarketingAttributionRangeModel> result = await new GetMarketingAttributionRangeQueryHandler(repository, clock)
            .Handle(new GetMarketingAttributionRangeQuery(from, to), CancellationToken.None);

        ResultAssert.Success(result);
        Assert.Multiple(
            () => Assert.Equal(from.UtcDateTime, result.Value.FromUtc),
            () => Assert.Equal(to.UtcDateTime, result.Value.ToUtc));
        await repository.Received(1).GetRangeAsync(
            Arg.Is<MarketingAttributionRangeFilter>(filter => filter.FromUtc == from.UtcDateTime && filter.ToUtc == to.UtcDateTime),
            CancellationToken.None);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task OverlongCustomPeriod_DoesNotReadRepository(bool almostAllTime) {
        TimeProvider clock = Substitute.For<TimeProvider>();
        clock.GetUtcNow().Returns(new DateTimeOffset(2026, 10, 17, 12, 0, 0, TimeSpan.Zero));
        var to = new DateTimeOffset(2026, 10, 18, 0, 0, 0, TimeSpan.Zero);
        DateTimeOffset from = almostAllTime ? DateTimeOffset.UnixEpoch.AddTicks(1) : to.AddDays(-3661).AddTicks(-1);
        IMarketingAttributionRangeReadRepository repository = Substitute.For<IMarketingAttributionRangeReadRepository>();

        Result<MarketingAttributionRangeModel> result = await new GetMarketingAttributionRangeQueryHandler(repository, clock)
            .Handle(new GetMarketingAttributionRangeQuery(from, to), CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Validation.Invalid", result.Error.Code);
        Assert.Empty(repository.ReceivedCalls());
    }

    private static MarketingAttributionRangeRecord EmptyRange() => new(
        new MarketingAttributionSummaryRecord(0, 0, 0, 0, 0, 0, 0, 0, LastEventAtUtc: null, [], [], []),
        new MarketingAttributionSummaryRecord(0, 0, 0, 0, 0, 0, 0, 0, LastEventAtUtc: null, [], [], []),
        [],
        0);
}
