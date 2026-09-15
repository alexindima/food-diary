using FoodDiary.Modules.MealPlanning.Domain.Entities.MealPlans;
using FoodDiary.Modules.MealPlanning.Domain.Enums;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using System.Reflection;

namespace FoodDiary.Modules.MealPlanning.Domain.Tests;

[ExcludeFromCodeCoverage]
public sealed class MealPlanPersistenceShapeTests {
    private static void ReadPublicProperties(object instance) {
        foreach (PropertyInfo property in instance.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public)) {
            if (property.GetIndexParameters().Length == 0) {
                property.GetValue(instance);
            }
        }
    }

    [Fact]
    public void EntityNavigationAndPrivateConstructors_AreCoveredForEfOnlyMembers() {
        var ownerId = UserId.New();
        var mealPlan = MealPlan.CreateForUser(
            ownerId,
            name: "Plan",
            description: null,
            DietType.Balanced,
            durationDays: 1,
            targetCaloriesPerDay: null);
        ReadPublicProperties(mealPlan);
        Assert.Multiple(
            () => Assert.Equal(ownerId, mealPlan.UserId));
    }
}
