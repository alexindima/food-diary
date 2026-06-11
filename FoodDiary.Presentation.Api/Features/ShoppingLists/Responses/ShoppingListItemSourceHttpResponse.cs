namespace FoodDiary.Presentation.Api.Features.ShoppingLists.Responses;

public sealed record ShoppingListItemSourceHttpResponse(
    Guid Id,
    string SourceType,
    Guid? MealPlanId,
    Guid? MealPlanMealId,
    Guid? RecipeId,
    string Label,
    int? DayNumber,
    string? MealType,
    double Amount,
    string? Unit);
