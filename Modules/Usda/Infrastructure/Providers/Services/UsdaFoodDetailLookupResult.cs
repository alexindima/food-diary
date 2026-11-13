using FoodDiary.Modules.Usda.Contracts.Models;

namespace FoodDiary.Modules.Usda.Infrastructure.Providers.Services;

internal union UsdaFoodDetailLookupResult(
    UsdaFoodDetailLookupResult.Found,
    UsdaFoodDetailLookupResult.NotFound,
    UsdaFoodDetailLookupResult.LookupFailed) {
    public sealed record Found(UsdaFoodDetailModel Food);
    public sealed class NotFound;
    public sealed class LookupFailed;
}
