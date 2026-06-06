namespace FoodDiary.Presentation.Api.Features.MealPlans.Responses;

public sealed record MealPlanMealHttpResponse(
    Guid Id,
    string MealType,
    Guid RecipeId,
    string? RecipeName,
    int Servings,
    double? Calories,
    double? Proteins,
    double? Fats,
    double? Carbs);
