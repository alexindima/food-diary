using FoodDiary.Mediator;
using FoodDiary.Modules.Meals.Contracts.Models;
using FoodDiary.Modules.Meals.Contracts.Queries.ReadMealNutritionStatistics;
using FoodDiary.Modules.Statistics.Application.Models;
using FoodDiary.Modules.Statistics.Application.Queries.GetStatistics;
using FoodDiary.Modules.Statistics.Application.Queries.GetStatisticsSummary;
using FoodDiary.Modules.Users.Contracts.Common;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Results;

namespace FoodDiary.Modules.Statistics.Application.Tests;

[ExcludeFromCodeCoverage]
public sealed class StatisticsCalendarZoneTests {
    [Theory]
    [InlineData(null)]
    [InlineData("Asia/Tbilisi")]
    public async Task Statistics_ForwardsExplicitZoneAndPreservesLegacyRequests(string? zoneId) {
        ISender sender = Substitute.For<ISender>();
        sender.Send(Arg.Any<ReadMealNutritionStatisticsQuery>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result.Success<IReadOnlyList<MealNutritionStatisticsBucket>>([])));
        ICurrentUserAccessService access = CreateAccess();
        var user = UserId.New();
        var from = new DateTime(2026, 10, 3, 20, 0, 0, DateTimeKind.Utc);
        DateTime to = from.AddDays(1).AddTicks(-1);
        using var cancellation = new CancellationTokenSource();

        Result<IReadOnlyList<AggregatedStatisticsModel>> result = await new GetStatisticsQueryHandler(sender, access)
            .Handle(new GetStatisticsQuery(user.Value, from, to, 1, zoneId), cancellation.Token);

        ResultAssert.Success(result);
        await sender.Received(1).Send(Arg.Is<ReadMealNutritionStatisticsQuery>(query => query.UserId == user &&
            query.DateFrom == from && query.DateTo == to && query.TimeZoneId == zoneId), cancellation.Token);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("Not/A_TimeZone")]
    public async Task Queries_RejectInvalidZonesWithoutDispatchingReads(string zoneId) {
        ISender sender = Substitute.For<ISender>();
        ICurrentUserAccessService access = CreateAccess();
        var from = new DateTime(2026, 10, 4, 0, 0, 0, DateTimeKind.Utc);
        var query = new GetStatisticsQuery(Guid.NewGuid(), from, from, 1, zoneId);
        var summary = new GetStatisticsSummaryQuery(query.UserId, from, from, 1, TimeZoneId: zoneId);

        ResultAssert.Failure(await new GetStatisticsQueryHandler(sender, access).Handle(query, CancellationToken.None), "Validation.Invalid");
        ResultAssert.Failure(await new GetStatisticsSummaryQueryHandler(sender, access).Handle(summary, CancellationToken.None), "Validation.Invalid");
        Assert.False((await new GetStatisticsQueryValidator().ValidateAsync(query)).IsValid);
        Assert.False((await new GetStatisticsSummaryQueryValidator().ValidateAsync(summary)).IsValid);
        Assert.Empty(sender.ReceivedCalls());
    }

    [Theory]
    [InlineData(366, true)]
    [InlineData(367, false)]
    public async Task Queries_CountCalendarDaysAcrossFallBackRatherThanElapsedHours(int days, bool valid) {
        var zone = TimeZoneInfo.FindSystemTimeZoneById("America/Los_Angeles");
        var localStart = new DateTime(2025, 11, 1);
        DateTime from = TimeZoneInfo.ConvertTimeToUtc(localStart, zone);
        DateTime to = TimeZoneInfo.ConvertTimeToUtc(localStart.AddDays(days), zone).AddTicks(-1);
        Assert.True(to - from > TimeSpan.FromDays(days));
        var query = new GetStatisticsQuery(Guid.NewGuid(), from, to, 1, zone.Id);
        var summary = new GetStatisticsSummaryQuery(query.UserId, from, to, 1, TimeZoneId: zone.Id);
        ISender sender = Substitute.For<ISender>();
        sender.Send(Arg.Any<ReadMealNutritionStatisticsQuery>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result.Success<IReadOnlyList<MealNutritionStatisticsBucket>>([])));
        Result<IReadOnlyList<AggregatedStatisticsModel>> result = await new GetStatisticsQueryHandler(sender, CreateAccess())
            .Handle(query, CancellationToken.None);

        Assert.Equal(valid, result.IsSuccess);
        Assert.Equal(valid ? 1 : 0, sender.ReceivedCalls().Count());
        Assert.Equal(valid, (await new GetStatisticsQueryValidator().ValidateAsync(query)).IsValid);
        Assert.Equal(valid, (await new GetStatisticsSummaryQueryValidator().ValidateAsync(summary)).IsValid);
        Assert.False((await new GetStatisticsQueryValidator().ValidateAsync(query with { TimeZoneId = null })).IsValid);
        Assert.False((await new GetStatisticsSummaryQueryValidator().ValidateAsync(summary with { TimeZoneId = null })).IsValid);
    }

    private static ICurrentUserAccessService CreateAccess() {
        ICurrentUserAccessService access = Substitute.For<ICurrentUserAccessService>();
        access.EnsureCanAccessAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult<Error?>(null));
        return access;
    }
}
