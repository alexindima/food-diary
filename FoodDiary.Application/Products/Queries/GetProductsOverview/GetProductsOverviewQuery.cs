using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
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
    IReadOnlyCollection<string>? ProductTypes = null,
    double? CaloriesFrom = null,
    double? CaloriesTo = null,
    bool? HasImage = null)
    : IQuery<Result<ProductOverviewModel>>, IUserRequest;
