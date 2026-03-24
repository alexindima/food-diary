namespace FoodDiary.Presentation.Api.Features.ShoppingLists.Requests;

public sealed record ShoppingListItemHttpRequest(
    Guid? ProductId,
    string? Name,
    double? Amount,
    string? Unit,
    string? Category,
    bool IsChecked = false,
    int? SortOrder = null);
