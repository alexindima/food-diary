namespace FoodDiary.Application.Abstractions.ShoppingLists.Models;

public sealed record ShoppingListItemReadModel(
    Guid Id,
    Guid ShoppingListId,
    Guid? ProductId,
    string Name,
    double? Amount,
    string? Unit,
    string? Category,
    string? Aisle,
    string? Note,
    bool IsChecked,
    DateTime? CheckedOnUtc,
    int SortOrder,
    IReadOnlyList<ShoppingListItemSourceReadModel> Sources);
