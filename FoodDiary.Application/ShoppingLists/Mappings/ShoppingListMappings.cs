using FoodDiary.Application.ShoppingLists.Models;
using FoodDiary.Domain.Entities.Shopping;

namespace FoodDiary.Application.ShoppingLists.Mappings;

public static class ShoppingListMappings {
    public static ShoppingListModel ToModel(this ShoppingList list) {
        var items = list.Items
            .OrderBy(i => i.SortOrder)
            .ThenBy(i => i.Id.Value)
            .Select(item => new ShoppingListItemModel(
                item.Id.Value,
                item.ShoppingListId.Value,
                item.ProductId?.Value,
                item.Name,
                item.Amount,
                item.Unit?.ToString(),
                item.Category,
                item.IsChecked,
                item.SortOrder))
            .ToList();

        return new ShoppingListModel(
            list.Id.Value,
            list.Name,
            list.CreatedOnUtc,
            items);
    }

    public static ShoppingListSummaryModel ToSummaryModel(this ShoppingList list)
        => new(
            list.Id.Value,
            list.Name,
            list.CreatedOnUtc,
            list.Items.Count);
}
