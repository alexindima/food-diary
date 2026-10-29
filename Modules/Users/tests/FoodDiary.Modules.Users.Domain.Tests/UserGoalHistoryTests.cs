using FoodDiary.Domain.Primitives;
using FoodDiary.Modules.Users.Domain.Entities;
using FoodDiary.Modules.Users.Domain.ValueObjects;

namespace FoodDiary.Modules.Users.Domain.Tests;

[ExcludeFromCodeCoverage]
public sealed class UserGoalHistoryTests {
    [Fact]
    public void GoalChanges_PreserveEarlierNutritionHydrationAndCycling() {
        using IDisposable clock = DomainTime.Override(new FixedTimeProvider(new DateTimeOffset(2026, 10, 1, 12, 0, 0, TimeSpan.Zero)));
        var user = User.Create("goals-history@example.com", "hash");
        user.UpdateGoals(new UserGoalUpdate(DailyCalorieTarget: 3000, ProteinTarget: 150, FatTarget: 90,
            CarbTarget: 350, FiberTarget: 30, WaterGoal: 2000, CalorieCyclingEnabled: true, MondayCalories: 3200));
        user.UpdateActivity(stepGoal: 10000, hydrationGoal: 2400);
        using (DomainTime.Override(new FixedTimeProvider(new DateTimeOffset(2026, 10, 30, 12, 0, 0, TimeSpan.Zero)))) {
            user.UpdateGoals(dailyCalorieTarget: 2500, proteinTarget: 120);
        }

        UserGoalRevision[] history = [.. user.NutritionProfile.GoalHistory.OrderBy(entry => entry.Revision)];
        Assert.True(history.Length == 4, "Goal history must retain each changed nutrition or hydration version.");
        Assert.Null(history[0].ToDailyGoals().EffectiveWaterGoal);
        Assert.Equal(DateOnly.MinValue, history[0].EffectiveFrom);
        Assert.Equal(3000, history[2].DailyCalorieTarget);
        Assert.Equal(150, history[2].ProteinTarget);
        Assert.Equal(2400, history[2].ToDailyGoals().EffectiveWaterGoal);
        Assert.Equal(10000, history[2].StepGoal);
        Assert.Equal(3200, history[2].MondayCalories);
        Assert.Equal(new DateOnly(2026, 10, 30), history[3].EffectiveFrom);
        Assert.Equal(2500, history[3].DailyCalorieTarget);
        Assert.Equal(120, history[3].ProteinTarget);
    }

    [Theory]
    [InlineData("Asia/Tbilisi", 30)]
    [InlineData("America/Los_Angeles", 29)]
    [InlineData("Asia/Kathmandu", 30)]
    public void GoalChanges_UseSavedLocalCalendarDay(string zone, int expectedDay) {
        using IDisposable clock = DomainTime.Override(new FixedTimeProvider(new DateTimeOffset(2026, 10, 29, 22, 0, 0, TimeSpan.Zero)));
        var user = User.Create("local-goals@example.com", "hash");
        user.SetTimeZone(zone);
        user.UpdateGoals(dailyCalorieTarget: 2500);

        Assert.Equal(new DateOnly(2026, 10, expectedDay), user.NutritionProfile.GoalHistory.Last().EffectiveFrom);
    }

    [Fact]
    public void UnchangedOrInvalidGoals_DoNotAppendHistory() {
        var user = User.Create("no-change-goals@example.com", "hash");
        user.UpdateGoals(dailyCalorieTarget: 2500);
        long version = user.NutritionProfile.GoalRevision;
        user.UpdateGoals(dailyCalorieTarget: 2500);
        user.UpdateActivity(hydrationGoal: null);
        user.UpdatePersonalInfo(firstName: "Changed");
        user.UpdateDesiredWeight(70);
        Assert.Throws<ArgumentOutOfRangeException>(() => user.UpdateGoals(proteinTarget: -1));

        Assert.Equal(version, user.NutritionProfile.GoalRevision);
        Assert.Equal(2, user.NutritionProfile.GoalHistory.Count);
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
