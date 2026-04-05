using FoodDiary.Domain.Common;
using FoodDiary.Domain.Entities.Recipes;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Domain.Entities.MealPlans;

public sealed class MealPlanMeal : Entity<MealPlanMealId> {
    public MealPlanDayId MealPlanDayId { get; private set; }
    public MealPlanDay Day { get; private set; } = null!;
    public MealType MealType { get; private set; }
    public RecipeId RecipeId { get; private set; }
    public Recipe Recipe { get; private set; } = null!;
    public int Servings { get; private set; }

    private MealPlanMeal() {
    }

    internal static MealPlanMeal Create(
        MealPlanDayId dayId,
        MealType mealType,
        RecipeId recipeId,
        int servings) {
        if (servings <= 0) {
            throw new ArgumentOutOfRangeException(nameof(servings), "Servings must be positive.");
        }

        var meal = new MealPlanMeal {
            Id = MealPlanMealId.New(),
            MealPlanDayId = dayId,
            MealType = mealType,
            RecipeId = recipeId,
            Servings = servings
        };
        meal.SetCreated();
        return meal;
    }
}
