using FoodDiary.Application.Common.Models;
using FoodDiary.Application.Products.Models;
using FoodDiary.Presentation.Api.Features.FavoriteProducts.Mappings;
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
            model.IsOwnedByCurrentUser,
            model.QualityScore,
            model.QualityGrade,
            model.IsFavorite,
            model.FavoriteProductId
        );
    }

    public static ProductOverviewHttpResponse ToHttpResponse(this ProductOverviewModel model) {
        return new ProductOverviewHttpResponse(
            model.RecentItems.ToHttpResponseList(ToHttpResponse),
            model.AllProducts.ToHttpResponse(),
            model.FavoriteItems.Select(FavoriteProductHttpMappings.ToHttpResponse).ToList(),
            model.FavoriteTotalCount
        );
    }

    public static PagedHttpResponse<ProductHttpResponse> ToHttpResponse(this PagedResponse<ProductModel> response) {
        return response.ToPagedHttpResponse(ToHttpResponse);
    }
}
