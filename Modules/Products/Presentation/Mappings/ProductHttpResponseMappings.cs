using FoodDiary.Application.Abstractions.Common.Models;
using FoodDiary.Modules.Products.Application.Models;
using FoodDiary.Modules.Favorites.Presentation.Mappings.Features.FavoriteProducts.Mappings;
using FoodDiary.Modules.Products.Presentation.Responses;
using FoodDiary.Presentation.Api.Responses;

namespace FoodDiary.Modules.Products.Presentation.Mappings;

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
            ) { Images = model.Images.Select(image => new ProductImageHttpResponse(image.ImageAssetId, image.ImageUrl)).ToList() };
        }
    }

    extension(ProductOverviewModel model) {
        public ProductOverviewHttpResponse ToHttpResponse() {
            return new ProductOverviewHttpResponse(
                model.RecentItems.ToHttpResponseList(ToHttpResponse),
                model.AllProducts.ToHttpResponse(),
                model.FavoriteItems.Select(FavoriteProductHttpResponseMappings.ToHttpResponse).ToList(),
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
