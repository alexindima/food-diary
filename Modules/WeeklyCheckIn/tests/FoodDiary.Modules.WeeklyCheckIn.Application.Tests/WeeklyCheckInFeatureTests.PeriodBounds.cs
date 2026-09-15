using FoodDiary.Mediator;
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
            () => Assert.Equal(100, result.Value.LastWeek.TotalCalories));
    }
}
