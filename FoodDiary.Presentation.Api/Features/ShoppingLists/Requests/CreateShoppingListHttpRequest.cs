namespace FoodDiary.Presentation.Api.Features.ShoppingLists.Requests;

public sealed record CreateShoppingListHttpRequest(
    string Name,
    IReadOnlyList<ShoppingListItemHttpRequest>? Items = null);
