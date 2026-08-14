using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Application.Products.Products.Models;

namespace FoodDiary.Application.Products.Products.Queries.GetProductsOverview;

public sealed record GetProductsOverviewQuery(
    Guid? UserId,
    int Page,
    int Limit,
    string? Search,
    bool IncludePublic,
    int RecentLimit = 10,
    int FavoriteLimit = 10,
    IReadOnlyCollection<string>? ProductTypes = null,
    double? CaloriesFrom = null,
    double? CaloriesTo = null,
    bool? HasImage = null)
    : IQuery<Result<ProductOverviewModel>>, IUserRequest;
