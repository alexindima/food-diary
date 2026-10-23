using FoodDiary.Modules.MealPlanning.Application.ShoppingLists.Common;

namespace FoodDiary.Modules.MealPlanning.Application.ShoppingLists.Models;

public sealed record ShoppingListModel(
    Guid Id,
    string Name,
    DateTime CreatedAt,
    IReadOnlyList<ShoppingListItemModel> Items) {
    public string Revision => ShoppingListRevision.Compute(this);
}
