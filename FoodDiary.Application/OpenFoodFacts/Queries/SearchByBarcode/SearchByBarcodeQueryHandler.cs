using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Result;
using FoodDiary.Application.Abstractions.OpenFoodFacts.Common;
using FoodDiary.Application.Abstractions.OpenFoodFacts.Models;

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
