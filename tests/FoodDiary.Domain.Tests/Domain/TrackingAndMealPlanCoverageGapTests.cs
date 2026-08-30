using System.Reflection;
using System.Runtime.ExceptionServices;
using FoodDiary.Domain.Entities.MealPlans;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Domain.Tests.Domain;

[ExcludeFromCodeCoverage]
public sealed class TrackingAndMealPlanCoverageGapTests {
    [Fact]
    public void MealPlanFactories_RejectInvalidIdentifiersEnumsAndCounts() {
        Assert.Throws<ArgumentException>(() => InvokeStatic<MealPlanDay>(typeof(MealPlanDay), "Create", MealPlanId.Empty, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => InvokeStatic<MealPlanDay>(typeof(MealPlanDay), "Create", MealPlanId.New(), 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => InvokeStatic<MealPlanDay>(typeof(MealPlanDay), "Create", MealPlanId.New(), 32));

        Assert.Throws<ArgumentException>(() => CreateMeal(MealPlanDayId.Empty, MealType.Breakfast, RecipeId.New(), 1));
        Assert.Throws<ArgumentException>(() => CreateMeal(MealPlanDayId.New(), MealType.Breakfast, RecipeId.Empty, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => CreateMeal(MealPlanDayId.New(), (MealType)999, RecipeId.New(), 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => CreateMeal(MealPlanDayId.New(), MealType.Breakfast, RecipeId.New(), 0));
    }

    private static MealPlanMeal CreateMeal(MealPlanDayId dayId, MealType mealType, RecipeId recipeId, int servings) =>
        InvokeStatic<MealPlanMeal>(typeof(MealPlanMeal), "Create", dayId, mealType, recipeId, servings);

    private static T InvokeStatic<T>(Type type, string methodName, params object?[] arguments) =>
        Invoke<T>(type.GetMethod(methodName, BindingFlags.Static | BindingFlags.NonPublic)!, instance: null, arguments);

    private static T Invoke<T>(MethodInfo method, object? instance, object?[] arguments) {
        try {
            return (T)method.Invoke(instance, arguments)!;
        } catch (TargetInvocationException exception) when (exception.InnerException is not null) {
            ExceptionDispatchInfo.Capture(exception.InnerException).Throw();
            throw;
        }
    }
}
