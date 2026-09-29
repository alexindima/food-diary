namespace FoodDiary.Modules.MealPlanning.Application.ShoppingLists.Models;

public sealed record ShoppingListPageModel(IReadOnlyList<ShoppingListSummaryModel> Items, bool HasMore, int? NextPage);
