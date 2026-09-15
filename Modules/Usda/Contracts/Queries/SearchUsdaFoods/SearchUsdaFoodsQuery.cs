using FoodDiary.Mediator;
using FoodDiary.Results;
using FoodDiary.Modules.Usda.Contracts.Models;

namespace FoodDiary.Modules.Usda.Contracts.Queries.SearchUsdaFoods;

public record SearchUsdaFoodsQuery(
    string Search,
    int Limit = 20) : IRequest<Result<IReadOnlyList<UsdaFoodModel>>>;
