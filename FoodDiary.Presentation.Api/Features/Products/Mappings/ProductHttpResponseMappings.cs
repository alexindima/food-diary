using FoodDiary.Application.Common.Models;
using FoodDiary.Application.Products.Models;
using FoodDiary.Presentation.Api.Features.Products.Responses;
using FoodDiary.Presentation.Api.Responses;

namespace FoodDiary.Presentation.Api.Features.Products.Mappings;

public static class ProductHttpResponseMappings {
    public static ProductHttpResponse ToHttpResponse(this ProductModel model) {
        return new ProductHttpResponse(
            model.Id,
            model.Barcode,
            model.Name,
            model.Brand,
            model.ProductType,
            model.Category,
            model.Description,
            model.Comment,
            model.ImageUrl,
            model.ImageAssetId,
            model.BaseUnit,
            model.BaseAmount,
            model.DefaultPortionAmount,
            model.CaloriesPerBase,
            model.ProteinsPerBase,
            model.FatsPerBase,
            model.CarbsPerBase,
            model.FiberPerBase,
            model.AlcoholPerBase,
            model.UsageCount,
            model.Visibility,
            model.CreatedAt,
            model.IsOwnedByCurrentUser
        );
    }

    public static ProductListWithRecentHttpResponse ToHttpResponse(this ProductListWithRecentModel model) {
        return new ProductListWithRecentHttpResponse(
            model.RecentItems.ToHttpResponseList(ToHttpResponse),
            model.AllProducts.ToHttpResponse()
        );
    }

    public static PagedHttpResponse<ProductHttpResponse> ToHttpResponse(this PagedResponse<ProductModel> response) {
        return response.ToPagedHttpResponse(ToHttpResponse);
    }
}
