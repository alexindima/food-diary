using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;

namespace FoodDiary.Application.Favorites.FavoriteProducts.Queries.IsProductFavorite;

public record IsProductFavoriteQuery(
    Guid? UserId,
    Guid ProductId) : IQuery<Result<bool>>, IUserRequest;
