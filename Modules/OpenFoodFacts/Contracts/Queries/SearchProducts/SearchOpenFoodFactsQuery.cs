using FoodDiary.Mediator;
using FoodDiary.Results;
using FoodDiary.Modules.OpenFoodFacts.Contracts.Models;

namespace FoodDiary.Modules.OpenFoodFacts.Contracts.Queries.SearchProducts;

public record SearchOpenFoodFactsQuery(
    string Search,
    int Limit = 10) : IRequest<Result<IReadOnlyList<OpenFoodFactsProductModel>>>;
