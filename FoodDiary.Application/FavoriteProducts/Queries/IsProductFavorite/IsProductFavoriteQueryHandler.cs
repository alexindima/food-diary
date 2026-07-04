using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Common.Validation;
using FoodDiary.Application.Abstractions.FavoriteProducts.Common;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Domain.Entities.FavoriteProducts;

namespace FoodDiary.Application.FavoriteProducts.Queries.IsProductFavorite;

public sealed class IsProductFavoriteQueryHandler(
    IFavoriteProductReadRepository favoriteProductRepository,
    ICurrentUserAccessService currentUserAccessService)
    : IQueryHandler<IsProductFavoriteQuery, Result<bool>> {
    public async Task<Result<bool>> Handle(
        IsProductFavoriteQuery query,
        CancellationToken cancellationToken) {
        Result<UserId> userIdResult = UserIdParser.Parse(query.UserId);
        if (userIdResult.IsFailure) {
            return Result.Failure<bool>(userIdResult.Error);
        }

        UserId userId = userIdResult.Value;
        Error? accessError = await currentUserAccessService.EnsureCanAccessAsync(userId, cancellationToken).ConfigureAwait(false);
        if (accessError is not null) {
            return Result.Failure<bool>(accessError);
        }

        var productId = new ProductId(query.ProductId);
        FavoriteProduct? favorite = await favoriteProductRepository.GetByProductIdAsync(productId, userId, cancellationToken).ConfigureAwait(false);
        return Result.Success(favorite is not null);
    }
}
