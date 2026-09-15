namespace FoodDiary.Modules.MealPlanning.Presentation.ShoppingLists.Responses;

public sealed record ShoppingListHttpResponse(
    Guid Id,
    string Name,
    DateTime CreatedAt,
    IReadOnlyList<ShoppingListItemHttpResponse> Items);
