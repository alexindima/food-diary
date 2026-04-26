using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Result;
using FoodDiary.Application.Abstractions.Usda.Models;

namespace FoodDiary.Application.Usda.Queries.SearchUsdaFoods;

public record SearchUsdaFoodsQuery(
    string Search,
    int Limit = 20) : IQuery<Result<IReadOnlyList<UsdaFoodModel>>>;
