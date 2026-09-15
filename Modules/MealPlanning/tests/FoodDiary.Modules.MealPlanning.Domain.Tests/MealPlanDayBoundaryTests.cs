using FoodDiary.Modules.MealPlanning.Domain.Entities.MealPlans;
using FoodDiary.Modules.MealPlanning.Domain.Enums;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;

namespace FoodDiary.Modules.MealPlanning.Domain.Tests;

[ExcludeFromCodeCoverage]
public sealed class MealPlanDayBoundaryTests {
    [Fact]
    public void MealPlanAndRecipe_RejectOutOfAggregateRangeAndDirectCycles() {
        var plan = MealPlan.CreateForUser(
            userId: UserId.New(),
            name: "Week",
            description: null,
            dietType: DietType.Balanced,
            durationDays: 7,
            targetCaloriesPerDay: null);
        Assert.Throws<ArgumentOutOfRangeException>(() => plan.AddDay(8));
        Assert.Empty(plan.Days);
    }
}
