using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Abstractions.OpenFoodFacts.Common;
using FoodDiary.Application.Abstractions.OpenFoodFacts.Models;

namespace FoodDiary.Application.OpenFoodFacts.Queries.SearchByBarcode;

public sealed class SearchByBarcodeQueryHandler(
    IOpenFoodFactsService openFoodFactsService)
    : IQueryHandler<SearchByBarcodeQuery, Result<OpenFoodFactsProductModel?>> {
    public async Task<Result<OpenFoodFactsProductModel?>> Handle(
        SearchByBarcodeQuery query,
        CancellationToken cancellationToken) {
        OpenFoodFactsProductModel? product = await openFoodFactsService.GetByBarcodeAsync(query.Barcode, cancellationToken).ConfigureAwait(false);
        return Result.Success(product);
    }
}
