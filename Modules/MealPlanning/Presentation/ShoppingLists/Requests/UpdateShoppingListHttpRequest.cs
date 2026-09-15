namespace FoodDiary.Modules.MealPlanning.Presentation.ShoppingLists.Requests;

public sealed record UpdateShoppingListHttpRequest(
    string? Name = null,
    IReadOnlyList<ShoppingListItemHttpRequest>? Items = null);
