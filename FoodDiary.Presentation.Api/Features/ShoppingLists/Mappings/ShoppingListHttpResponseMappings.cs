using FoodDiary.Application.ShoppingLists.Models;
using FoodDiary.Presentation.Api.Features.ShoppingLists.Responses;

namespace FoodDiary.Presentation.Api.Features.ShoppingLists.Mappings;

public static class ShoppingListHttpResponseMappings {
    public static ShoppingListHttpResponse ToHttpResponse(this ShoppingListModel model) {
        return new ShoppingListHttpResponse(
            model.Id,
            model.Name,
            model.CreatedAt,
            model.Items.Select(ToHttpResponse).ToList()
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
            model.IsChecked,
            model.SortOrder
        );
    }
}
