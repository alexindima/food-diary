using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Modules.Favorites.Contracts.FavoriteProducts.Models;

namespace FoodDiary.Modules.Favorites.Application.FavoriteProducts.Queries.GetFavoriteProducts;

public record GetFavoriteProductsQuery(
    Guid? UserId) : IQuery<Result<IReadOnlyList<FavoriteProductModel>>>, IUserRequest;
