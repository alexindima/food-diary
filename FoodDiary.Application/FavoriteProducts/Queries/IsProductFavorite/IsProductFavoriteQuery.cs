using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Results;

namespace FoodDiary.Application.FavoriteProducts.Queries.IsProductFavorite;

public record IsProductFavoriteQuery(
    Guid? UserId,
    Guid ProductId) : IQuery<Result<bool>>, IUserRequest;
