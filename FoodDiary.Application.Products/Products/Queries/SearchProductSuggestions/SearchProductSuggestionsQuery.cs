using FoodDiary.Results;
using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Application.Products.Products.Models;

namespace FoodDiary.Application.Products.Products.Queries.SearchProductSuggestions;

public sealed record SearchProductSuggestionsQuery(
    string Search,
    int Limit = 5) : IQuery<Result<IReadOnlyList<ProductSearchSuggestionModel>>>;
