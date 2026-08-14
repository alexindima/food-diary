using FoodDiary.Application.MealPlanning.ShoppingLists.Models;
using FoodDiary.Presentation.Api.Features.ShoppingLists.Responses;
using FoodDiary.Presentation.Api.Responses;

namespace FoodDiary.Presentation.Api.Features.ShoppingLists.Mappings;

public static class ShoppingListHttpResponseMappings {
    extension(ShoppingListModel model) {
        public ShoppingListHttpResponse ToHttpResponse() {
            return new ShoppingListHttpResponse(
                model.Id,
                model.Name,
                model.CreatedAt,
                model.Items.ToHttpResponseList(ToHttpResponse)
            );
        }
    }

    extension(ShoppingListSummaryModel model) {
        public ShoppingListSummaryHttpResponse ToHttpResponse() {
            return new ShoppingListSummaryHttpResponse(
                model.Id,
                model.Name,
                model.CreatedAt,
                model.ItemsCount
            );
        }
    }

    extension(ShoppingListItemModel model) {
        private ShoppingListItemHttpResponse ToHttpResponse() {
            return new ShoppingListItemHttpResponse(
                model.Id,
                model.ShoppingListId,
                model.ProductId,
                model.Name,
                model.Amount,
                model.Unit,
                model.Category,
                model.Aisle,
                model.Note,
                model.IsChecked,
                model.CheckedOnUtc,
                model.SortOrder,
                model.Sources.Select(source => new ShoppingListItemSourceHttpResponse(
                    source.Id,
                    source.SourceType,
                    source.MealPlanId,
                    source.MealPlanMealId,
                    source.RecipeId,
                    source.Label,
                    source.DayNumber,
                    source.MealType,
                    source.Amount,
                    source.Unit)).ToList()
            );
        }
    }
}
