using FoodDiary.Modules.Products.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Mediator;
using FoodDiary.Modules.Favorites.Contracts.FavoriteProducts.Queries.ReadProductFavoriteStatus;
using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Modules.Users.Contracts.Common;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;

namespace FoodDiary.Modules.Favorites.Application.FavoriteProducts.Queries.IsProductFavorite;

public sealed class IsProductFavoriteQueryHandler(
    ISender sender,
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
        bool isFavorite = await sender.Send(new ReadProductFavoriteStatusQuery(productId, userId), cancellationToken).ConfigureAwait(false);
        return Result.Success(isFavorite);
    }
}
