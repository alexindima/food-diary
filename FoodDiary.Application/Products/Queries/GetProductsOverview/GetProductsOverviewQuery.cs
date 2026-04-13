using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.Products.Models;

namespace FoodDiary.Application.Products.Queries.GetProductsOverview;

public sealed record GetProductsOverviewQuery(
    Guid? UserId,
    int Page,
    int Limit,
    string? Search,
    bool IncludePublic,
    int RecentLimit = 10,
    int FavoriteLimit = 10,
    IReadOnlyCollection<string>? ProductTypes = null)
    : IQuery<Result<ProductOverviewModel>>, IUserRequest;
