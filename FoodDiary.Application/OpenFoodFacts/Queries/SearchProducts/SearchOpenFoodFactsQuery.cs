using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Application.Abstractions.OpenFoodFacts.Models;

namespace FoodDiary.Application.OpenFoodFacts.Queries.SearchProducts;

public record SearchOpenFoodFactsQuery(
    string Search,
    int Limit = 10) : IQuery<Result<IReadOnlyList<OpenFoodFactsProductModel>>>;
