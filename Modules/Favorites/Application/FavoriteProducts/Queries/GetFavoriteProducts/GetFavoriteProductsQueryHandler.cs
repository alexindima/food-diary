using FoodDiary.Mediator;
using FoodDiary.Modules.Favorites.Contracts.FavoriteProducts.Queries.ReadFavoriteProducts;
using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Modules.Users.Contracts.Common;
using FoodDiary.Modules.Favorites.Contracts.FavoriteProducts.Models;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;

namespace FoodDiary.Modules.Favorites.Application.FavoriteProducts.Queries.GetFavoriteProducts;

public sealed class GetFavoriteProductsQueryHandler(
    ISender sender,
    ICurrentUserAccessService currentUserAccessService)
    : IQueryHandler<GetFavoriteProductsQuery, Result<IReadOnlyList<FavoriteProductModel>>> {
    public async Task<Result<IReadOnlyList<FavoriteProductModel>>> Handle(
        GetFavoriteProductsQuery query,
        CancellationToken cancellationToken) {
        Result<UserId> userIdResult = await CurrentUserAccessResolver.ResolveAsync(
            query.UserId,
            currentUserAccessService,
            cancellationToken).ConfigureAwait(false);
        if (userIdResult.IsFailure) {
            return CurrentUserAccessResolver.ToFailure<IReadOnlyList<FavoriteProductModel>>(userIdResult);
        }

        UserId userId = userIdResult.Value;
        IReadOnlyList<FavoriteProductModel> favorites = await sender.Send(new ReadFavoriteProductsQuery(userId), cancellationToken).ConfigureAwait(false);
        return Result.Success(favorites);
    }
}
