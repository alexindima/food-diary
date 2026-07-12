using FoodDiary.Application.Abstractions.Usda.Models;

namespace FoodDiary.Application.Usda.Common;

public interface IUsdaProductSuggestionReadService {
    Task<IReadOnlyList<UsdaFoodReadModel>> SearchAsync(
        string search,
        int limit,
        CancellationToken cancellationToken);
}
