namespace FoodDiary.Presentation.Api.Features.ShoppingLists.Requests;

public sealed record UpdateShoppingListHttpRequest(
    string? Name = null,
    IReadOnlyList<ShoppingListItemHttpRequest>? Items = null);
