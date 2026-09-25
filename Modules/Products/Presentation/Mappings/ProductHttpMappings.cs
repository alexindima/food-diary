using FoodDiary.Modules.Products.Application.Commands.CreateProduct;
using FoodDiary.Modules.Products.Application.Commands.DeleteProduct;
using FoodDiary.Modules.Products.Application.Commands.DuplicateProduct;
using FoodDiary.Modules.Products.Application.Commands.UpdateProduct;
using FoodDiary.Modules.Products.Application.Models;
using FoodDiary.Modules.Products.Application.Queries.SearchProductSuggestions;
using FoodDiary.Modules.Products.Presentation.Requests;
using FoodDiary.Modules.Products.Presentation.Responses;

namespace FoodDiary.Modules.Products.Presentation.Mappings;

public static class ProductHttpMappings {
    public static SearchProductSuggestionsQuery ToSuggestionsQuery(string search, int limit) =>
        new(search, limit);

    extension(Guid productId) {
        public DeleteProductCommand ToDeleteCommand(Guid userId) =>
            new(userId, productId);
        public DuplicateProductCommand ToDuplicateCommand(Guid userId) =>
            new(userId, productId);
    }

    extension(CreateProductHttpRequest request) {
        public CreateProductCommand ToCommand(Guid userIdValue) {
            return new CreateProductCommand(
                UserId: userIdValue,
                Barcode: request.Barcode,
                Name: request.Name,
                Brand: request.Brand,
                ProductType: request.ProductType,
                Category: request.Category,
                Description: request.Description,
                Comment: request.Comment,
                ImageUrl: request.ImageUrl,
                ImageAssetId: request.ImageAssetId,
                BaseUnit: request.BaseUnit,
                BaseAmount: request.BaseAmount,
                DefaultPortionAmount: request.DefaultPortionAmount,
                CaloriesPerBase: request.CaloriesPerBase,
                ProteinsPerBase: request.ProteinsPerBase,
                FatsPerBase: request.FatsPerBase,
                CarbsPerBase: request.CarbsPerBase,
                FiberPerBase: request.FiberPerBase,
                AlcoholPerBase: request.AlcoholPerBase,
                Visibility: request.Visibility
            ) { ImageAssetIds = request.ImageAssetIds };
        }
    }

    extension(UpdateProductHttpRequest request) {
        public UpdateProductCommand ToCommand(Guid userIdValue, Guid productId) {
            return new UpdateProductCommand(
                UserId: userIdValue,
                ProductId: productId,
                Barcode: request.Barcode,
                ClearBarcode: request.ClearBarcode,
                Name: request.Name,
                Brand: request.Brand,
                ClearBrand: request.ClearBrand,
                ProductType: request.ProductType,
                Category: request.Category,
                ClearCategory: request.ClearCategory,
                Description: request.Description,
                ClearDescription: request.ClearDescription,
                Comment: request.Comment,
                ClearComment: request.ClearComment,
                ImageUrl: request.ImageUrl,
                ClearImageUrl: request.ClearImageUrl,
                ImageAssetId: request.ImageAssetId,
                ClearImageAssetId: request.ClearImageAssetId,
                BaseUnit: request.BaseUnit,
                BaseAmount: request.BaseAmount,
                DefaultPortionAmount: request.DefaultPortionAmount,
                CaloriesPerBase: request.CaloriesPerBase,
                ProteinsPerBase: request.ProteinsPerBase,
                FatsPerBase: request.FatsPerBase,
                CarbsPerBase: request.CarbsPerBase,
                FiberPerBase: request.FiberPerBase,
                AlcoholPerBase: request.AlcoholPerBase,
                Visibility: request.Visibility) { ImageAssetIds = request.ImageAssetIds };
        }
    }

    extension(IReadOnlyList<ProductSearchSuggestionModel> models) {
        public IReadOnlyList<ProductSearchSuggestionHttpResponse> ToHttpResponse(
        ) =>
                models.Select(m => new ProductSearchSuggestionHttpResponse(
                    m.Source,
                    m.Name,
                    m.Brand,
                    m.Category,
                    m.Barcode,
                    m.UsdaFdcId,
                    m.ImageUrl,
                    m.CaloriesPer100G,
                    m.ProteinsPer100G,
                    m.FatsPer100G,
                    m.CarbsPer100G,
                    m.FiberPer100G)).ToList();
    }
}
