namespace FoodDiary.Application.ShoppingLists.Models;

public sealed record ShoppingListSummaryModel(
    Guid Id,
    string Name,
    DateTime CreatedAt,
    int ItemsCount);
