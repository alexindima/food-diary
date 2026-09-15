using FoodDiary.Results;
using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Modules.Products.Application.Models;

namespace FoodDiary.Modules.Products.Application.Queries.SearchProductSuggestions;

public sealed record SearchProductSuggestionsQuery(
    string Search,
    int Limit = 5) : IQuery<Result<IReadOnlyList<ProductSearchSuggestionModel>>>;
