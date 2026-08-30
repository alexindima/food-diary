using FoodDiary.Application.Abstractions.OpenFoodFacts.Models;

namespace FoodDiary.Application.Abstractions.OpenFoodFacts.Common;

public interface IOpenFoodFactsService {
    Task<OpenFoodFactsProductModel?> GetByBarcodeAsync(
        string barcode,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<OpenFoodFactsProductModel>> SearchAsync(
        string query,
        int limit = 10,
        CancellationToken cancellationToken = default);
}
