using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Models;
using FoodDiary.Modules.Favorites.Contracts.FavoriteProducts.Models;
using FoodDiary.Results;

namespace FoodDiary.Modules.Favorites.Application.FavoriteProducts.Queries.GetFavoriteProductPage;

public sealed record GetFavoriteProductPageQuery(Guid? UserId, int Page = 1, int Limit = 10, string? Search = null)
    : IQuery<Result<PagedResponse<FavoriteProductModel>>>, IUserRequest;
