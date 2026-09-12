using FoodDiary.Application.Abstractions.Dashboard.Common;
using FoodDiary.Application.Abstractions.Dashboard.Models;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Application.Abstractions.Users.Models;
using FoodDiary.Application.Hydration.Common;
using FoodDiary.Application.Statistics.Models;
using FoodDiary.Application.Statistics.Queries.GetDiaryStatistics;
using FoodDiary.Domain.ValueObjects;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Results;

namespace FoodDiary.Application.Tests.Statistics;

[ExcludeFromCodeCoverage]
public sealed class DiaryStatisticsQueryTests {
    [Fact]
    public async Task SevenDays_UsesLocalBoundariesAndIncludesEmptyDaysInAverage() {
        var owner = new UserId(Guid.NewGuid());
        IUserDashboardProfileReadService profiles = Substitute.For<IUserDashboardProfileReadService>();
        profiles.GetDashboardProfileAsync(owner, Arg.Any<CancellationToken>()).Returns(Result.Success(Profile(owner)));
        IDashboardStatisticsReadService statistics = Substitute.For<IDashboardStatisticsReadService>();
        statistics.GetStatisticsAsync(owner, Arg.Any<DateTime>(), Arg.Any<DateTime>(), 2, Arg.Any<CancellationToken>()).Returns(call => {
            DateTime from = call.ArgAt<DateTime>(1);
            DateTime to = call.ArgAt<DateTime>(2);
            bool recorded = from.Day == 4;
            return Result.Success<IReadOnlyList<DashboardStatisticsBucketReadModel>>([
                new(from, to, recorded ? 700 : 0, 0, 0, 0, 0, TotalProteins: recorded ? 70 : 0, MealCount: recorded ? 2 : 0),
            ]);
        });
        IHydrationIntervalReadService hydration = Substitute.For<IHydrationIntervalReadService>();
        hydration.GetTotalAsync(owner, Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>()).Returns(250L);
        var handler = new GetDiaryStatisticsQueryHandler(Substitute.For<ICurrentUserAccessService>(), profiles, statistics, hydration, new Clock());

        Result<DiaryStatisticsSummaryModel> result = await handler.Handle(new GetDiaryStatisticsQuery(owner.Value, 7), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(7, result.Value.CalendarDays);
        Assert.Equal(1, result.Value.DaysWithMeals);
        Assert.Equal(100, result.Value.AverageCaloriesPerCalendarDay);
        Assert.Equal(70, result.Value.TotalProteins);
        Assert.Equal(1750, result.Value.TotalWaterMl);
        Assert.Equal(2500, result.Value.Days[4].CalorieGoal);
        await statistics.Received(1).GetStatisticsAsync(owner, new DateTime(2026, 3, 8, 5, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 3, 9, 4, 0, 0, DateTimeKind.Utc).AddTicks(-10), 2, Arg.Any<CancellationToken>());
        await hydration.Received(1).GetTotalAsync(owner, new DateTime(2026, 3, 8, 5, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 3, 9, 4, 0, 0, DateTimeKind.Utc), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ReadFailure_IsNotReportedAsAnEmptyDiary() {
        var owner = new UserId(Guid.NewGuid());
        IUserDashboardProfileReadService profiles = Substitute.For<IUserDashboardProfileReadService>();
        profiles.GetDashboardProfileAsync(owner, Arg.Any<CancellationToken>()).Returns(Result.Success(Profile(owner)));
        IDashboardStatisticsReadService statistics = Substitute.For<IDashboardStatisticsReadService>();
        statistics.GetStatisticsAsync(owner, Arg.Any<DateTime>(), Arg.Any<DateTime>(), 2, Arg.Any<CancellationToken>())
            .Returns(Result.Failure<IReadOnlyList<DashboardStatisticsBucketReadModel>>(new Error("Statistics.Unavailable", "Unavailable.", ErrorKind.Internal)));
        IHydrationIntervalReadService hydration = Substitute.For<IHydrationIntervalReadService>();
        var handler = new GetDiaryStatisticsQueryHandler(Substitute.For<ICurrentUserAccessService>(), profiles, statistics, hydration, new Clock());

        Result<DiaryStatisticsSummaryModel> result = await handler.Handle(new GetDiaryStatisticsQuery(owner.Value), CancellationToken.None);

        Assert.Equal("Statistics.Unavailable", result.Error.Code);
        await hydration.DidNotReceive().GetTotalAsync(Arg.Any<UserId>(), Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>());
    }

    private static UserDashboardProfileModel Profile(UserId owner) => new(owner.Value, Email: null, Language: "en", DashboardLayoutJson: null,
        DesiredWeightKg: null, DesiredWaistCm: null, HydrationGoal: 2000, WaterGoal: null, ProteinTarget: null, FatTarget: null,
        CarbTarget: null, FiberTarget: null, new UserCalorieSchedule(2000, CalorieCyclingEnabled: true, MondayCalories: null,
            TuesdayCalories: null, WednesdayCalories: null, ThursdayCalories: null, FridayCalories: null, SaturdayCalories: null, SundayCalories: 2500),
        "America/New_York");

    [ExcludeFromCodeCoverage]
    private sealed class Clock : TimeProvider {
        public override DateTimeOffset GetUtcNow() => new(2026, 3, 10, 12, 0, 0, TimeSpan.Zero);
    }
}
