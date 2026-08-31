namespace FoodDiary.Application.MealPlanning.ShoppingLists.Models;

public sealed record ShoppingListModel(
    Guid Id,
    string Name,
    DateTime CreatedAt,
    IReadOnlyList<ShoppingListItemModel> Items);
