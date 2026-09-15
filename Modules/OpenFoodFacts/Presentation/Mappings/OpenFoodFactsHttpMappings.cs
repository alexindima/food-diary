using FoodDiary.Modules.OpenFoodFacts.Contracts.Queries.SearchProducts;
using FoodDiary.Modules.OpenFoodFacts.Contracts.Models;
using FoodDiary.Modules.OpenFoodFacts.Application.Queries.SearchByBarcode;
using FoodDiary.Modules.OpenFoodFacts.Presentation.Responses;

namespace FoodDiary.Modules.OpenFoodFacts.Presentation.Mappings;

public static class OpenFoodFactsHttpMappings {
    public static SearchByBarcodeQuery ToQuery(string barcode) =>
        new(barcode);

    public static SearchOpenFoodFactsQuery ToSearchQuery(string search, int limit) =>
        new(search, limit);

    extension(OpenFoodFactsProductModel? model) {
        public OpenFoodFactsProductHttpResponse? ToHttpResponse(
        ) =>
                model is null
                    ? null
                    : MapToResponse(model);
    }

    extension(IReadOnlyList<OpenFoodFactsProductModel> models) {
        public IReadOnlyList<OpenFoodFactsProductHttpResponse> ToListHttpResponse(
        ) =>
                models.Select(MapToResponse).ToList();
    }

    private static OpenFoodFactsProductHttpResponse MapToResponse(OpenFoodFactsProductModel model) =>
        new(model.Barcode,
            model.Name,
            model.Brand,
            model.Category,
            model.ImageUrl,
            model.CaloriesPer100G,
            model.ProteinsPer100G,
            model.FatsPer100G,
            model.CarbsPer100G,
            model.FiberPer100G);
}
