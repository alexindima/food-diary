using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.OpenFoodFacts.Models;

namespace FoodDiary.Application.OpenFoodFacts.Queries.SearchProducts;

public record SearchOpenFoodFactsQuery(
    string Search,
    int Limit = 10) : IQuery<Result<IReadOnlyList<OpenFoodFactsProductModel>>>;
