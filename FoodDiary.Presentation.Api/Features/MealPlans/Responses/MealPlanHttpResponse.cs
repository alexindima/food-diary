namespace FoodDiary.Presentation.Api.Features.MealPlans.Responses;

public sealed record MealPlanHttpResponse(
    Guid Id,
    string Name,
    string? Description,
    string DietType,
    int DurationDays,
    double? TargetCaloriesPerDay,
    bool IsCurated,
    IReadOnlyList<MealPlanDayHttpResponse> Days);

public sealed record MealPlanDayHttpResponse(
    Guid Id,
    int DayNumber,
    IReadOnlyList<MealPlanMealHttpResponse> Meals);

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

public sealed record MealPlanSummaryHttpResponse(
    Guid Id,
    string Name,
    string? Description,
    string DietType,
    int DurationDays,
    double? TargetCaloriesPerDay,
    bool IsCurated,
    int TotalRecipes);
