using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Application.FavoriteProducts.Common;
using FoodDiary.Application.FavoriteProducts.Models;
using FoodDiary.Application.Users.Common;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.FavoriteProducts.Queries.GetFavoriteProducts;

public sealed class GetFavoriteProductsQueryHandler(
    IFavoriteProductReadService favoriteProductReadService,
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
        IReadOnlyList<FavoriteProductModel> favorites = await favoriteProductReadService.GetAllAsync(userId, cancellationToken).ConfigureAwait(false);
        return Result.Success(favorites);
    }
}
