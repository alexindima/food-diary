using FoodDiary.Modules.Favorites.Contracts.FavoriteProducts.Models;
using FoodDiary.Modules.Favorites.Presentation.Contracts.Features.FavoriteProducts.Responses;

namespace FoodDiary.Modules.Favorites.Presentation.Mappings.Features.FavoriteProducts.Mappings;

public static class FavoriteProductHttpResponseMappings {
    extension(FavoriteProductModel model) {
        public FavoriteProductHttpResponse ToHttpResponse() =>
                new(
                    model.Id,
                    model.ProductId,
                    model.Name,
                    model.CreatedAtUtc,
                    model.ProductName,
                    model.Brand,
                    model.Barcode,
                    model.Comment,
                    model.ImageUrl,
                    model.CaloriesPerBase,
                    model.ProteinsPerBase,
                    model.FatsPerBase,
                    model.CarbsPerBase,
                    model.FiberPerBase,
                    model.AlcoholPerBase,
                    model.QualityScore,
                    model.QualityGrade,
                    model.IsOwnedByCurrentUser,
                    model.BaseUnit,
                    model.PreferredPortionAmount,
                    model.DefaultPortionAmount);
    }
}
