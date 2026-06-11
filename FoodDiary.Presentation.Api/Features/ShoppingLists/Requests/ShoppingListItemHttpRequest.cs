namespace FoodDiary.Presentation.Api.Features.ShoppingLists.Requests;

public sealed record ShoppingListItemHttpRequest(
    Guid? Id,
    Guid? ProductId,
    string? Name,
    double? Amount,
    string? Unit,
    string? Category,
    string? Aisle,
    string? Note,
    bool IsChecked = false,
    DateTime? CheckedOnUtc = null,
    int? SortOrder = null);
