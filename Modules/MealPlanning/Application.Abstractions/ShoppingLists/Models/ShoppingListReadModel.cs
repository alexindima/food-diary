namespace FoodDiary.Modules.MealPlanning.Application.Abstractions.ShoppingLists.Models;

public sealed record ShoppingListReadModel(
    Guid Id,
    string Name,
    DateTime CreatedAt,
    IReadOnlyList<ShoppingListItemReadModel> Items);
