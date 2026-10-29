using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects;

namespace FoodDiary.Modules.Users.Domain.Tests;

[ExcludeFromCodeCoverage]
public sealed class UserGoalTimelineTests {
    [Fact]
    public void Timeline_UsesLastApplicableRevision_IncludingSameDayAndTimeZoneChanges() {
        var original = new UserDailyGoals(new UserCalorieSchedule(3000, CalorieCyclingEnabled: false, MondayCalories: null,
            TuesdayCalories: null, WednesdayCalories: null, ThursdayCalories: null, FridayCalories: null, SaturdayCalories: null, SundayCalories: null),
            150, 90, 350, 30, WaterGoal: null, HydrationGoal: null, StepGoal: null);
        UserDailyGoals updated = original with { ProteinTarget = 120 };
        UserDailyGoals last = updated with { HydrationGoal = 2400 };
        var timeline = new UserGoalTimeline([
            new UserGoalVersion(3, new DateOnly(2026, 10, 30), last),
            new UserGoalVersion(0, DateOnly.MinValue, original),
            new UserGoalVersion(2, new DateOnly(2026, 10, 30), updated),
        ]);

        Assert.Same(original, timeline.GetGoalsForDate(new DateOnly(2026, 10, 29)));
        Assert.Same(last, timeline.GetGoalsForDate(new DateOnly(2026, 10, 30)));
        Assert.Same(last, timeline.GetGoalsForDate(new DateOnly(2026, 11, 1)));
        var afterZoneChange = new UserGoalTimeline([.. timeline.Versions,
            new UserGoalVersion(4, new DateOnly(2026, 10, 29), updated)]);
        Assert.Same(updated, afterZoneChange.GetGoalsForDate(new DateOnly(2026, 10, 30)));
    }
}
