namespace FoodDiary.Application.ShoppingLists.Models;

public sealed record ShoppingListItemModel(
    Guid Id,
    Guid ShoppingListId,
    Guid? ProductId,
    string Name,
    double? Amount,
    string? Unit,
    string? Category,
    bool IsChecked,
    int SortOrder);
