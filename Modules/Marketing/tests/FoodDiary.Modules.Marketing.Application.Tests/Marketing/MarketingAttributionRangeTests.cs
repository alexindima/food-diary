using FoodDiary.Application.Abstractions.Marketing.Common;
using FoodDiary.Application.Marketing.Models;
using FoodDiary.Application.Marketing.Queries.GetMarketingAttributionRange;
using FoodDiary.Application.Marketing.Services;
using FoodDiary.Results;

namespace FoodDiary.Application.Tests.Marketing;

[ExcludeFromCodeCoverage]
public sealed class MarketingAttributionRangeTests {
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
        var handler = new GetMarketingAttributionRangeQueryHandler(new MarketingAttributionRangeReadService(repository, clock));
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
            "page" => query with { Page = 0 },
            "limit" => query with { Limit = 101 },
            "search" => query with { Search = new string('s', 321) },
            "event" => query with { EventType = "unknown" },
            "channel" => query with { Channel = "unknown" },
            _ => throw new InvalidOperationException(),
        };
        Result<MarketingAttributionRangeModel> result = await new MarketingAttributionRangeReadService(repository, clock).GetAsync(query, CancellationToken.None);
        ResultAssert.Failure(result);
        Assert.Equal("Validation.Invalid", result.Error.Code);
        Assert.Empty(repository.ReceivedCalls());
    }
}
