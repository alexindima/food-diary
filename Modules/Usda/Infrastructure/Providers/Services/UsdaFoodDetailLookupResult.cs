using FoodDiary.Modules.Usda.Contracts.Models;

namespace FoodDiary.Modules.Usda.Infrastructure.Providers.Services;

internal sealed record UsdaFoodDetailLookupResult(bool Cacheable, UsdaFoodDetailModel? Value);
