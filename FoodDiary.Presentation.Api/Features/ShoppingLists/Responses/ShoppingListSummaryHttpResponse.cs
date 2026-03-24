namespace FoodDiary.Presentation.Api.Features.ShoppingLists.Responses;

public sealed record ShoppingListSummaryHttpResponse(
    Guid Id,
    string Name,
    DateTime CreatedAt,
    int ItemsCount);
