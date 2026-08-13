using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Application.Abstractions.FavoriteProducts.Models;

namespace FoodDiary.Application.FavoriteProducts.Queries.GetFavoriteProducts;

public record GetFavoriteProductsQuery(
    Guid? UserId) : IQuery<Result<IReadOnlyList<FavoriteProductModel>>>, IUserRequest;
