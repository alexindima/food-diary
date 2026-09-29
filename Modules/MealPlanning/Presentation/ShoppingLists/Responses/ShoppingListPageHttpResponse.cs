namespace FoodDiary.Modules.MealPlanning.Presentation.ShoppingLists.Responses;

public sealed record ShoppingListPageHttpResponse(IReadOnlyList<ShoppingListSummaryHttpResponse> Items, bool HasMore, int? NextPage);
