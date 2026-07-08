using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Application.FavoriteProducts.Models;

namespace FoodDiary.Application.FavoriteProducts.Queries.GetFavoriteProducts;

public record GetFavoriteProductsQuery(
    Guid? UserId) : IQuery<Result<IReadOnlyList<FavoriteProductModel>>>, IUserRequest;
