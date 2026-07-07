using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Application.FavoriteProducts.Common;
using FoodDiary.Application.Users.Common;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.FavoriteProducts.Queries.IsProductFavorite;

public sealed class IsProductFavoriteQueryHandler(
    IFavoriteProductReadService favoriteProductReadService,
    ICurrentUserAccessService currentUserAccessService)
    : IQueryHandler<IsProductFavoriteQuery, Result<bool>> {
    public async Task<Result<bool>> Handle(
        IsProductFavoriteQuery query,
        CancellationToken cancellationToken) {
        Result<UserId> userIdResult = await CurrentUserAccessResolver.ResolveAsync(
            query.UserId,
            currentUserAccessService,
            cancellationToken).ConfigureAwait(false);
        if (userIdResult.IsFailure) {
            return CurrentUserAccessResolver.ToFailure<bool>(userIdResult);
        }

        UserId userId = userIdResult.Value;
        var productId = new ProductId(query.ProductId);
        bool isFavorite = await favoriteProductReadService.ExistsByProductIdAsync(productId, userId, cancellationToken).ConfigureAwait(false);
        return Result.Success(isFavorite);
    }
}
