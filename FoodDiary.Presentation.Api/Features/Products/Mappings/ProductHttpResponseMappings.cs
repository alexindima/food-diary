using FoodDiary.Application.Abstractions.Common.Models;
using FoodDiary.Application.Products.Models;
using FoodDiary.Presentation.Api.Features.FavoriteProducts.Mappings;
using FoodDiary.Presentation.Api.Features.Products.Responses;
using FoodDiary.Presentation.Api.Responses;

namespace FoodDiary.Presentation.Api.Features.Products.Mappings;

public static class ProductHttpResponseMappings {
    extension(ProductModel model) {
        public ProductHttpResponse ToHttpResponse() {
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
    }

    extension(ProductOverviewModel model) {
        public ProductOverviewHttpResponse ToHttpResponse() {
            return new ProductOverviewHttpResponse(
                model.RecentItems.ToHttpResponseList(ToHttpResponse),
                model.AllProducts.ToHttpResponse(),
                model.FavoriteItems.Select(FavoriteProductHttpMappings.ToHttpResponse).ToList(),
                model.FavoriteTotalCount
            );
        }
    }

    extension(PagedResponse<ProductModel> response) {
        public PagedHttpResponse<ProductHttpResponse> ToHttpResponse() {
            return response.ToPagedHttpResponse(ToHttpResponse);
        }
    }
}
