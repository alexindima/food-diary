namespace FoodDiary.Application.ShoppingLists.Commands.Common;

public record ShoppingListItemInput(
    Guid? Id,
    Guid? ProductId,
    string? Name,
    double? Amount,
    string? Unit,
    string? Category,
    string? Aisle,
    string? Note,
    bool IsChecked,
    DateTime? CheckedOnUtc,
    int? SortOrder);
