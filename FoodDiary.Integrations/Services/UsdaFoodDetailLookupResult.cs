using FoodDiary.Application.Abstractions.Usda.Models;

namespace FoodDiary.Integrations.Services;

internal sealed record UsdaFoodDetailLookupResult(bool Cacheable, UsdaFoodDetailModel? Value);
