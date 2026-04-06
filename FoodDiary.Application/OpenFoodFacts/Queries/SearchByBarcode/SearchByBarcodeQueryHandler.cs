using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.OpenFoodFacts.Common;
using FoodDiary.Application.OpenFoodFacts.Models;

namespace FoodDiary.Application.OpenFoodFacts.Queries.SearchByBarcode;

public class SearchByBarcodeQueryHandler(
    IOpenFoodFactsService openFoodFactsService)
    : IQueryHandler<SearchByBarcodeQuery, Result<OpenFoodFactsProductModel?>> {
    public async Task<Result<OpenFoodFactsProductModel?>> Handle(
        SearchByBarcodeQuery query,
        CancellationToken cancellationToken) {
        var product = await openFoodFactsService.GetByBarcodeAsync(query.Barcode, cancellationToken);
        return Result.Success(product);
    }
}
