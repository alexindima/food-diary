using FoodDiary.Modules.OpenFoodFacts.Contracts.Models;

namespace FoodDiary.Modules.OpenFoodFacts.Application.Abstractions.Common;

public interface IOpenFoodFactsService {
    Task<OpenFoodFactsProductModel?> GetByBarcodeAsync(
        string barcode,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<OpenFoodFactsProductModel>> SearchAsync(
        string query,
        int limit = 10,
        CancellationToken cancellationToken = default);
}
