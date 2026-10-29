using FoodDiary.Modules.Dashboard.Application.Abstractions.Models;
using FoodDiary.Modules.Dashboard.Application.Abstractions.Common;
using FoodDiary.Modules.Dashboard.Application.Common;
using FoodDiary.Modules.Dashboard.Application.Models;
using FoodDiary.Modules.Dashboard.Application.Services;
using FoodDiary.Modules.Dashboard.Contracts.Models;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Results;
using FoodDiary.Mediator;
using Microsoft.Extensions.Logging.Abstractions;

namespace FoodDiary.Modules.Dashboard.Application.Tests;

[ExcludeFromCodeCoverage]
public sealed class DashboardGoalHistoryTests {
    [Theory]
    [InlineData(29, 29, 3000, 21000, 150, 2000)]
    [InlineData(30, 30, 2500, 20500, 120, 2500)]
    [InlineData(29, 30, 3000, 5500, 135, 4500)]
    public async Task Snapshot_UsesGoalsForEachSelectedCalendarDay(int fromDay, int toDay, double dailyCalories,
        double periodCalories, double proteins, double water) {
        DashboardSnapshotModel snapshot = await BuildAsync(fromDay, toDay);

        Assert.Multiple(
            () => Assert.Equal(dailyCalories, snapshot.DailyGoal),
            () => Assert.Equal(periodCalories, snapshot.WeeklyCalorieGoal),
            () => Assert.Equal(proteins, snapshot.Statistics.ProteinGoal),
            () => Assert.Equal(75, snapshot.Statistics.FatGoal),
            () => Assert.Equal(250, snapshot.Statistics.CarbGoal),
            () => Assert.Equal(25, snapshot.Statistics.FiberGoal),
            () => Assert.Equal(water, snapshot.Hydration?.GoalMl));
    }

    [Fact]
    public async Task Snapshot_DoesNotInventAPeriodWaterGoalWhenSomeDaysHaveNoGoal() {
        DashboardSnapshotModel snapshot = await BuildAsync(29, 30, missingOldWater: true);
        Assert.Null(snapshot.Hydration?.GoalMl);
        Assert.Equal(135, snapshot.Statistics.ProteinGoal);
    }

    private static async Task<DashboardSnapshotModel> BuildAsync(int fromDay, int toDay, bool missingOldWater = false) {
        var start = new DateTime(2026, 10, fromDay, 0, 0, 0, DateTimeKind.Utc);
        var end = new DateTime(2026, 10, toDay, 0, 0, 0, DateTimeKind.Utc);
        var userId = UserId.New();
        var oldSchedule = new UserCalorieSchedule(3000, CalorieCyclingEnabled: true, MondayCalories: 3000, TuesdayCalories: null,
            WednesdayCalories: null, ThursdayCalories: null, FridayCalories: null, SaturdayCalories: null, SundayCalories: null);
        var newSchedule = new UserCalorieSchedule(2500, CalorieCyclingEnabled: false, MondayCalories: null, TuesdayCalories: null,
            WednesdayCalories: null, ThursdayCalories: null, FridayCalories: null, SaturdayCalories: null, SundayCalories: null);
        var oldGoals = new UserDailyGoals(oldSchedule, 150, 75, 250, 25, missingOldWater ? null : 2000, HydrationGoal: null, StepGoal: null);
        UserDailyGoals newGoals = oldGoals with { CalorieSchedule = newSchedule, ProteinTarget = 120, WaterGoal = 2500 };
        var user = new DashboardUserContextModel(userId.Value, Email: null, Language: null, DashboardLayoutJson: null,
            DesiredWeightKg: null, DesiredWaistCm: null, HydrationGoal: null, 2500,
            120, 75, 250, 25, newSchedule, "UTC", new UserGoalTimeline([
                new UserGoalVersion(0, DateOnly.MinValue, oldGoals),
                new UserGoalVersion(1, new DateOnly(2026, 10, 30), newGoals),
            ]));
        IDashboardReadService read = Substitute.For<IDashboardReadService>();
        read.GetSnapshotDataAsync(Arg.Any<UserId>(), Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<DateTime>(),
            Arg.Any<int>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<DashboardReadSections>(), Arg.Any<CancellationToken>(),
            Arg.Any<DashboardCalendarRange?>()).Returns(Result.Success(new DashboardReadModel([], [],
            new DashboardBodyReadModel([], [], [], [], 0), new DashboardMealsReadModel([], 1, 10, 0, 0))));
        var builder = new DashboardSnapshotBuilder(Substitute.For<ISender>(), Substitute.For<IDashboardUserContextService>(),
            read, NullLogger<DashboardSnapshotBuilder>.Instance);
        var sections = new DashboardSnapshotSections(IncludeStatistics: true, IncludeMeals: false, IncludeWeight: false,
            IncludeWaist: false, IncludeHydration: true, IncludeFasting: false, IncludeAdvice: false, IncludeLayout: false,
            IncludeExercise: false, IncludeTdee: false, IncludeCycle: false);
        return ResultAssert.Success(await builder.BuildAsync(new DashboardSnapshotRequest(userId.Value, start, end, "en", 7, 1, 10,
            sections, UserContext: user)));
    }
}
