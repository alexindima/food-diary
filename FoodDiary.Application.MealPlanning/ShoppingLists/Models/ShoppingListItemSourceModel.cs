namespace FoodDiary.Application.ShoppingLists.Models;

public sealed record ShoppingListItemSourceModel(
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
