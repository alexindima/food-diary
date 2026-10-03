using FoodDiary.Modules.Products.Application.Commands.ImportCatalogProduct;
using FoodDiary.Modules.Products.Application.Models;
using FoodDiary.Modules.Products.Application.Queries.ExportCatalogProducts;
using FoodDiary.Modules.Products.Presentation.Requests;
using FoodDiary.Modules.Products.Presentation.Responses;

namespace FoodDiary.Modules.Products.Presentation.Mappings;

public static class CatalogProductHttpMappings {
    public static ExportCatalogProductsQuery ToExportQuery() => new();

    extension(CatalogProductHttpRequest request) {
        public ImportCatalogProductCommand ToImportCommand(Guid userId, bool preview) =>
            new(userId, new CatalogProductModel(
                request.Id, request.Name, request.Barcode, request.Brand, request.ProductType,
                request.Category, request.Description, request.ImageUrl, request.BaseUnit,
                request.BaseAmount, request.DefaultPortionAmount, request.CaloriesPerBase,
                request.ProteinsPerBase, request.FatsPerBase, request.CarbsPerBase,
                request.FiberPerBase, request.AlcoholPerBase), preview);
    }

    extension(CatalogProductModel model) {
        public CatalogProductHttpResponse ToHttpResponse() =>
            new(model.Id, model.Name, model.Barcode, model.Brand, model.ProductType,
                model.Category, model.Description, model.ImageUrl, model.BaseUnit,
                model.BaseAmount, model.DefaultPortionAmount, model.CaloriesPerBase,
                model.ProteinsPerBase, model.FatsPerBase, model.CarbsPerBase,
                model.FiberPerBase, model.AlcoholPerBase);
    }

    extension(CatalogProductImportResult result) {
        public CatalogProductImportHttpResponse ToHttpResponse() =>
            new(result.Id, result.Status, result.Errors);
    }
}
