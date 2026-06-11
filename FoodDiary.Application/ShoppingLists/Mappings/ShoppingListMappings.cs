using FoodDiary.Application.ShoppingLists.Models;
using FoodDiary.Domain.Entities.Shopping;

namespace FoodDiary.Application.ShoppingLists.Mappings;

public static class ShoppingListMappings {
    extension(ShoppingList list) {
        public ShoppingListModel ToModel() {
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
                    item.Aisle,
                    item.Note,
                    item.IsChecked,
                    item.CheckedOnUtc,
                    item.SortOrder,
                    item.Sources
                        .OrderBy(source => source.DayNumber ?? int.MaxValue)
                        .ThenBy(source => source.Label, StringComparer.Ordinal)
                        .Select(source => new ShoppingListItemSourceModel(
                            source.Id.Value,
                            source.SourceType.ToString(),
                            source.MealPlanId?.Value,
                            source.MealPlanMealId?.Value,
                            source.RecipeId?.Value,
                            source.Label,
                            source.DayNumber,
                            source.MealType,
                            source.Amount,
                            source.Unit?.ToString()))
                        .ToList()))
                .ToList();

            return new ShoppingListModel(
                list.Id.Value,
                list.Name,
                list.CreatedOnUtc,
                items);
        }
        public ShoppingListSummaryModel ToSummaryModel()
            => new(
                list.Id.Value,
                list.Name,
                list.CreatedOnUtc,
                list.Items.Count);
    }
}
