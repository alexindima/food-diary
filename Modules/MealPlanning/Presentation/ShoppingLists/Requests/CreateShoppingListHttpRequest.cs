namespace FoodDiary.Modules.MealPlanning.Presentation.ShoppingLists.Requests;

public sealed record CreateShoppingListHttpRequest(
    string Name,
    IReadOnlyList<ShoppingListItemHttpRequest>? Items = null);
