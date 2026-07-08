using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Application.Common.Validation;
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

        Result<ProductId> productIdResult = RequiredIdParser.Parse(
            query.ProductId,
            nameof(query.ProductId),
            "Product id must not be empty.",
            value => new ProductId(value));
        if (productIdResult.IsFailure) {
            return RequiredIdParser.ToFailure<bool, ProductId>(productIdResult);
        }

        UserId userId = userIdResult.Value;
        ProductId productId = productIdResult.Value;
        bool isFavorite = await favoriteProductReadService.ExistsByProductIdAsync(productId, userId, cancellationToken).ConfigureAwait(false);
        return Result.Success(isFavorite);
    }
}
