using FoodDiary.Application.Abstractions.OpenFoodFacts.Models;
using FoodDiary.Application.OpenFoodFacts.Queries.SearchByBarcode;
using FoodDiary.Application.OpenFoodFacts.Queries.SearchProducts;
using FoodDiary.Presentation.Api.Features.OpenFoodFacts.Responses;

namespace FoodDiary.Presentation.Api.Features.OpenFoodFacts.Mappings;

public static class OpenFoodFactsHttpMappings {
    public static SearchByBarcodeQuery ToQuery(string barcode) =>
        new(barcode);

    public static SearchOpenFoodFactsQuery ToSearchQuery(string search, int limit) =>
        new(search, limit);

    public static OpenFoodFactsProductHttpResponse? ToHttpResponse(
        this OpenFoodFactsProductModel? model) =>
        model is null
            ? null
            : MapToResponse(model);

    public static IReadOnlyList<OpenFoodFactsProductHttpResponse> ToListHttpResponse(
        this IReadOnlyList<OpenFoodFactsProductModel> models) =>
        models.Select(MapToResponse).ToList();

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
