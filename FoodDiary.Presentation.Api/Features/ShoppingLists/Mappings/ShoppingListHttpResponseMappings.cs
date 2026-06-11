using FoodDiary.Application.ShoppingLists.Models;
using FoodDiary.Presentation.Api.Features.ShoppingLists.Responses;
using FoodDiary.Presentation.Api.Responses;

namespace FoodDiary.Presentation.Api.Features.ShoppingLists.Mappings;

public static class ShoppingListHttpResponseMappings {
    public static ShoppingListHttpResponse ToHttpResponse(this ShoppingListModel model) {
        return new ShoppingListHttpResponse(
            model.Id,
            model.Name,
            model.CreatedAt,
            model.Items.ToHttpResponseList(ToHttpResponse)
        );
    }

    public static ShoppingListSummaryHttpResponse ToHttpResponse(this ShoppingListSummaryModel model) {
        return new ShoppingListSummaryHttpResponse(
            model.Id,
            model.Name,
            model.CreatedAt,
            model.ItemsCount
        );
    }

    private static ShoppingListItemHttpResponse ToHttpResponse(this ShoppingListItemModel model) {
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
