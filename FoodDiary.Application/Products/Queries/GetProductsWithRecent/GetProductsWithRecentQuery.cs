using System.Collections.Generic;
using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Contracts.Products;
using FoodDiary.Domain.ValueObjects;

namespace FoodDiary.Application.Products.Queries.GetProductsWithRecent;

public sealed record GetProductsWithRecentQuery(
    UserId? UserId,
    int Page,
    int Limit,
    string? Search,
    bool IncludePublic,
    int RecentLimit = 10,
    IReadOnlyCollection<string>? ProductTypes = null)
    : IQuery<Result<ProductListWithRecentResponse>>;
