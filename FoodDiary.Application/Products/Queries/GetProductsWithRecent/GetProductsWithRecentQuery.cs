using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.Products.Models;

namespace FoodDiary.Application.Products.Queries.GetProductsWithRecent;

public sealed record GetProductsWithRecentQuery(
    Guid? UserId,
    int Page,
    int Limit,
    string? Search,
    bool IncludePublic,
    int RecentLimit = 10,
    IReadOnlyCollection<string>? ProductTypes = null)
    : IQuery<Result<ProductListWithRecentModel>>;
