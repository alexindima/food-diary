using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Result;
using FoodDiary.Application.Abstractions.Common.Interfaces.Persistence;
using FoodDiary.Application.Common.Validation;
using FoodDiary.Application.Abstractions.FavoriteProducts.Common;
using FoodDiary.Application.Users.Common;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.FavoriteProducts.Queries.IsProductFavorite;

public class IsProductFavoriteQueryHandler(
    IFavoriteProductRepository favoriteProductRepository,
    IUserRepository userRepository)
    : IQueryHandler<IsProductFavoriteQuery, Result<bool>> {
    public async Task<Result<bool>> Handle(
        IsProductFavoriteQuery query,
        CancellationToken cancellationToken) {
        var userIdResult = UserIdParser.Parse(query.UserId);
        if (userIdResult.IsFailure) {
            return Result.Failure<bool>(userIdResult.Error);
        }

        var userId = userIdResult.Value;
        var accessError = await CurrentUserAccessLoader.EnsureCanAccessAsync(userRepository, userId, cancellationToken);
        if (accessError is not null) {
            return Result.Failure<bool>(accessError);
        }

        var productId = new ProductId(query.ProductId);
        var favorite = await favoriteProductRepository.GetByProductIdAsync(productId, userId, cancellationToken);
        return Result.Success(favorite is not null);
    }
}
