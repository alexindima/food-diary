using FoodDiary.Modules.MealPlanning.Domain.Entities.MealPlans;
using FoodDiary.Modules.MealPlanning.Domain.Enums;
using FoodDiary.Modules.MealPlanning.Domain.ValueObjects;

namespace FoodDiary.Modules.MealPlanning.Domain.Tests;

[ExcludeFromCodeCoverage]
public sealed class PlannedQuantityTests {
    [Fact]
    public void PlanDay_EnforcesPlanDurationWithoutRejectingLargeIntegerServings() {
        var plan = MealPlan.CreateCuratedWithDuration("Plan", description: null, DietType.Balanced, PlanDurationDays.FromDays(2), targetCaloriesPerDay: null);
        Assert.Equal(2, plan.AddTypedDay(PlanDayNumber.FromIndex(2)).DayNumber);
        Assert.Throws<ArgumentOutOfRangeException>(() => plan.AddTypedDay(PlanDayNumber.FromIndex(3)));
        Assert.Equal(int.MaxValue, PlannedServings.FromCount(int.MaxValue).Value);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(32)]
    public void PlanLengthAndDay_RejectOutOfRangeNumbers(int value) {
        Assert.Throws<ArgumentOutOfRangeException>(() => PlanDurationDays.FromDays(value));
        Assert.Throws<ArgumentOutOfRangeException>(() => PlanDayNumber.FromIndex(value));
    }
}
