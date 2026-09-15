using FoodDiary.Mediator;
using FoodDiary.Modules.Hydration.Contracts.Queries.ReadHydrationDailyTotals;
using FoodDiary.Modules.Dashboard.Contracts.Models;
using FoodDiary.Modules.Dashboard.Contracts.Queries.ReadDashboardStatistics;
using FoodDiary.Modules.Users.Domain.Entities;
using FoodDiary.Modules.WeeklyCheckIn.Application.Models;
using FoodDiary.Modules.WeeklyCheckIn.Application.Queries.GetWeeklyCheckIn;
using FoodDiary.Results;

namespace FoodDiary.Modules.WeeklyCheckIn.Application.Tests;

public sealed partial class WeeklyCheckInFeatureTests {
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task GetWeeklyCheckIn_IncludesLastDayMealsButExcludesNextDay(bool currentWeek) {
        var user = User.Create("weekly-bounds@example.com", "hashed");
        DateTime weekStart = currentWeek ? Today : Today.AddDays(-7);
        DateTime lastDay = currentWeek ? Today : weekStart.AddDays(6);
        (DateTime Date, double Calories)[] meals = [
            (weekStart.AddDays(-1).AddHours(15), 100),
            (lastDay.AddHours(15), 200),
            (lastDay.AddDays(1), 10000),
        ];
        ISender statistics = Substitute.For<ISender>();
        statistics.Send(Arg.Any<ReadDashboardStatisticsQuery>(), Arg.Any<CancellationToken>())
            .Returns(call => {
                ReadDashboardStatisticsQuery query = call.Arg<ReadDashboardStatisticsQuery>();
                double calories = meals.Where(meal => meal.Date >= query.DateFrom && meal.Date <= query.DateTo)
                    .Sum(meal => meal.Calories);
                return Result.Success<IReadOnlyList<DashboardStatisticsBucketReadModel>>([
                    new(query.DateFrom, query.DateTo, calories, 0, 0, 0, 0),
                ]);
            });
        GetWeeklyCheckInQueryHandler handler = CreateHandler(
            statisticsReadService: statistics,
            profileService: CreateProfileService(user));

        Result<WeeklyCheckInModel> result = await handler.Handle(
            new GetWeeklyCheckInQuery(user.Id.Value, DateOnly.FromDateTime(weekStart)), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Multiple(
            () => Assert.Equal(200, result.Value.ThisWeek.TotalCalories),
            () => Assert.Equal(100, result.Value.LastWeek.TotalCalories),
            () => Assert.Equal(currentWeek ? 200 : 28.6, result.Value.ThisWeek.AvgDailyCalories),
            () => Assert.Equal(14.3, result.Value.LastWeek.AvgDailyCalories));
    }
    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    [InlineData(7)]
    public async Task GetWeeklyCheckIn_NormalizesAveragesByElapsedCalendarDays(int elapsedDays) {
        var user = User.Create("weekly-averages@example.com", "hashed");
        ISender statistics = Substitute.For<ISender>();
        statistics.Send(Arg.Any<ReadDashboardStatisticsQuery>(), Arg.Any<CancellationToken>())
            .Returns(call => {
                ReadDashboardStatisticsQuery query = call.Arg<ReadDashboardStatisticsQuery>();
                int days = (query.DateTo.Date - query.DateFrom.Date).Days + 1;
                return Result.Success<IReadOnlyList<DashboardStatisticsBucketReadModel>>([
                    .. Enumerable.Range(0, days).Select(day => new DashboardStatisticsBucketReadModel(
                        query.DateFrom.AddDays(day), query.DateFrom.AddDays(day), 2000, 0, 0, 0, 0,
                        TotalProteins: 100, TotalFats: 60, TotalCarbs: 250)),
                ]);
            });
        ISender hydration = Substitute.For<ISender>();
        hydration.Send(Arg.Any<ReadHydrationDailyTotalsQuery>(), Arg.Any<CancellationToken>())
            .Returns(call => {
                ReadHydrationDailyTotalsQuery query = call.Arg<ReadHydrationDailyTotalsQuery>();
                int days = (query.DateTo.Date - query.DateFrom.Date).Days + 1;
                return (IReadOnlyList<(DateTime Date, int TotalMl)>)[
                    .. Enumerable.Range(0, days).Select(day => (query.DateFrom.AddDays(day), 2000)),
                ];
            });
        GetWeeklyCheckInQueryHandler handler = CreateHandler(
            statisticsReadService: statistics, hydrationEntryReadService: hydration,
            profileService: CreateProfileService(user), today: Today.AddDays(elapsedDays - 1));

        Result<WeeklyCheckInModel> result = await handler.Handle(new GetWeeklyCheckInQuery(user.Id.Value), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(2000 * elapsedDays, result.Value.ThisWeek.TotalCalories);
        Assert.Equal(2000, result.Value.ThisWeek.AvgDailyCalories);
        Assert.Equal(100, result.Value.ThisWeek.AvgProteins);
        Assert.Equal(60, result.Value.ThisWeek.AvgFats);
        Assert.Equal(250, result.Value.ThisWeek.AvgCarbs);
        Assert.Equal(2000, result.Value.ThisWeek.AvgDailyHydrationMl);
        Assert.Equal(2000, result.Value.LastWeek.AvgDailyCalories);
        Assert.Equal(2000, result.Value.LastWeek.AvgDailyHydrationMl);
    }

}
