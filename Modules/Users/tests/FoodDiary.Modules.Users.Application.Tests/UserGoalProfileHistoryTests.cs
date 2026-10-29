using FoodDiary.Modules.Users.Contracts.Models;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects;

namespace FoodDiary.Modules.Users.Application.Tests;

[ExcludeFromCodeCoverage]
public sealed class UserGoalProfileHistoryTests {
    [Fact]
    public void GamificationAndWeeklyCheckIn_CompareEachDayWithItsOwnCalorieGoal() {
        var schedule = new UserCalorieSchedule(3000, CalorieCyclingEnabled: true, MondayCalories: 3200, TuesdayCalories: null,
            WednesdayCalories: null, ThursdayCalories: null, FridayCalories: null, SaturdayCalories: null, SundayCalories: null);
        var oldGoals = new UserDailyGoals(schedule, 150, 75, 250, 25, WaterGoal: null, HydrationGoal: null, StepGoal: null);
        var updatedSchedule = new UserCalorieSchedule(2500, CalorieCyclingEnabled: false, MondayCalories: null, TuesdayCalories: null,
            WednesdayCalories: null, ThursdayCalories: null, FridayCalories: null, SaturdayCalories: null, SundayCalories: null);
        UserDailyGoals newGoals = oldGoals with { CalorieSchedule = updatedSchedule };
        var timeline = new UserGoalTimeline([
            new UserGoalVersion(0, DateOnly.MinValue, oldGoals),
            new UserGoalVersion(1, new DateOnly(2026, 10, 30), newGoals),
        ]);
        var gamification = new UserGamificationProfileModel(updatedSchedule, timeline);
        var weekly = new UserWeeklyCheckInProfileModel(2500, timeline);

        Assert.Equal(3200, gamification.GetCalorieTargetForDate(new DateTime(2026, 10, 26)));
        Assert.Equal(3000, gamification.GetCalorieTargetForDate(new DateTime(2026, 10, 29)));
        Assert.Equal(2500, gamification.GetCalorieTargetForDate(new DateTime(2026, 10, 30)));
        // Monday 3200 + three days at 3000 + Friday 2500 = 14700 kcal over five days.
        Assert.Equal(2940, weekly.GetAverageCalorieTarget(new DateOnly(2026, 10, 26), new DateOnly(2026, 10, 30)));
    }
}
