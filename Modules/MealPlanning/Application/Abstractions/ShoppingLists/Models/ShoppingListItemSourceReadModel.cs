namespace FoodDiary.Application.Abstractions.ShoppingLists.Models;

public sealed record ShoppingListItemSourceReadModel(
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
