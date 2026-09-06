using FoodDiary.Domain.Enums;
using FoodDiary.Domain.Primitives;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Domain.Entities.MealPlans;

public sealed class MealPlanMeal : Entity<MealPlanMealId> {
    public MealPlanDayId MealPlanDayId { get; private set; }
    public MealPlanDay Day { get; private set; } = null!;
    public MealType MealType { get; private set; }
    public RecipeId RecipeId { get; private set; }
    public int Servings { get; private set; }
    public MealPlanRecipeSnapshot? RecipeSnapshot { get; private set; }

    public void SetRecipeSnapshot(MealPlanRecipeSnapshot? snapshot) {
        if (snapshot is not null && snapshot.Id != RecipeId) {
            throw new ArgumentException("Recipe snapshot must match the planned recipe.", nameof(snapshot));
        }

        RecipeSnapshot = snapshot is null ? null : snapshot with {
            Ingredients = Array.AsReadOnly(snapshot.Ingredients.ToArray()),
        };
    }

    private MealPlanMeal() {
    }

    internal static MealPlanMeal Create(
        MealPlanDayId dayId,
        MealType mealType,
        RecipeId recipeId,
        int servings) {
        if (dayId == MealPlanDayId.Empty) {
            throw new ArgumentException("Meal plan day id is required.", nameof(dayId));
        }

        if (recipeId == RecipeId.Empty) {
            throw new ArgumentException("Recipe id is required.", nameof(recipeId));
        }

        DomainGuard.Defined(mealType, nameof(mealType));
        if (servings <= 0) {
            throw new ArgumentOutOfRangeException(nameof(servings), "Servings must be positive.");
        }

        var meal = new MealPlanMeal {
            Id = MealPlanMealId.New(),
            MealPlanDayId = dayId,
            MealType = mealType,
            RecipeId = recipeId,
            Servings = servings,
        };
        meal.SetCreated();
        return meal;
    }
}
