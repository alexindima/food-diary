using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Result;

namespace FoodDiary.Application.FavoriteProducts.Queries.IsProductFavorite;

public record IsProductFavoriteQuery(
    Guid? UserId,
    Guid ProductId) : IQuery<Result<bool>>, IUserRequest;
