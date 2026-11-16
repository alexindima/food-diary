using FoodDiary.Application.Contracts.Common.Models;
using FoodDiary.Modules.Products.Application.Models;
using FoodDiary.Modules.Favorites.Presentation.Mappings.Features.FavoriteProducts.Mappings;
using FoodDiary.Modules.Products.Presentation.Responses;
using FoodDiary.Modules.Products.Contracts.Models;
using FoodDiary.Presentation.Api.Responses;
using System.Globalization;

namespace FoodDiary.Modules.Products.Presentation.Mappings;

public static class ProductHttpResponseMappings {
    internal static ProductCatalogLanguage Language => CultureInfo.CurrentUICulture.TwoLetterISOLanguageName switch {
        "ru" => ProductCatalogLanguage.Russian,
        "en" => ProductCatalogLanguage.English,
        _ => ProductCatalogLanguage.Unspecified,
    };

    internal static string LocalizeName(string name, string? usdaEnglishName) =>
        Language == ProductCatalogLanguage.English ? usdaEnglishName ?? name : name;

    extension(ProductModel model) {
        public ProductHttpResponse ToHttpResponse() {
            return new ProductHttpResponse(
                model.Id,
                model.Barcode,
                LocalizeName(model.Name, model.UsdaEnglishName),
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
            ) {
                UsdaFdcId = model.UsdaFdcId,
                Images = model.Images.Select(image => new ProductImageHttpResponse(image.ImageAssetId, image.ImageUrl)).ToList(),
            };
        }
    }

    extension(ProductOverviewModel model) {
        public ProductOverviewHttpResponse ToHttpResponse() {
            return new ProductOverviewHttpResponse(
                model.RecentItems.ToHttpResponseList(ToHttpResponse),
                model.AllProducts.ToHttpResponse(),
                model.FavoriteItems.Select(item => item.ToHttpResponse(Language == ProductCatalogLanguage.English)).ToList(),
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
