namespace FoodDiary.Presentation.Api.Features.ShoppingLists.Responses;

public sealed record ShoppingListItemHttpResponse(
    Guid Id,
    Guid ShoppingListId,
    Guid? ProductId,
    string Name,
    double? Amount,
    string? Unit,
    string? Category,
    bool IsChecked,
    int SortOrder);
