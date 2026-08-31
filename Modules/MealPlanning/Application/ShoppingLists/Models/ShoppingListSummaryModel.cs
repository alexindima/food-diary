namespace FoodDiary.Application.MealPlanning.ShoppingLists.Models;

public sealed record ShoppingListSummaryModel(
    Guid Id,
    string Name,
    DateTime CreatedAt,
    int ItemsCount);
