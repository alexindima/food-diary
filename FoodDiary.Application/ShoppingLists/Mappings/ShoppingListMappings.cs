using System.Linq;
using FoodDiary.Contracts.ShoppingLists;
using FoodDiary.Domain.Entities;

namespace FoodDiary.Application.ShoppingLists.Mappings;

public static class ShoppingListMappings
{
    public static ShoppingListResponse ToResponse(this ShoppingList list)
    {
        var items = list.Items
            .OrderBy(i => i.SortOrder)
            .ThenBy(i => i.Id.Value)
            .Select(item => new ShoppingListItemResponse(
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

        return new ShoppingListResponse(
            list.Id.Value,
            list.Name,
            list.CreatedOnUtc,
            items);
    }

    public static ShoppingListSummaryResponse ToSummaryResponse(this ShoppingList list) =>
        new(
            list.Id.Value,
            list.Name,
            list.CreatedOnUtc,
            list.Items.Count);
}
