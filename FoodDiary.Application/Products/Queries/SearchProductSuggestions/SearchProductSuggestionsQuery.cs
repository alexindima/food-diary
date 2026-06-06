using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Products.Models;

namespace FoodDiary.Application.Products.Queries.SearchProductSuggestions;

public sealed record SearchProductSuggestionsQuery(
    string Search,
    int Limit = 5) : IQuery<Result<IReadOnlyList<ProductSearchSuggestionModel>>>;
