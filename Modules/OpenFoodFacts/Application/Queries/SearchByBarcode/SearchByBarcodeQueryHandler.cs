using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Modules.OpenFoodFacts.Application.Abstractions.Common;
using FoodDiary.Modules.OpenFoodFacts.Contracts.Models;

namespace FoodDiary.Modules.OpenFoodFacts.Application.Queries.SearchByBarcode;

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
