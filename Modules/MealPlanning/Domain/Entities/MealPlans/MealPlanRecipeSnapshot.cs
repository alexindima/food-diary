using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Domain.Entities.MealPlans;

public sealed record MealPlanRecipeSnapshot(
    RecipeId Id,
    string Name,
    int Servings,
    IReadOnlyList<MealPlanRecipeIngredientSnapshot> Ingredients,
    double? TotalCalories = null,
    double? TotalProteins = null,
    double? TotalFats = null,
    double? TotalCarbs = null);

