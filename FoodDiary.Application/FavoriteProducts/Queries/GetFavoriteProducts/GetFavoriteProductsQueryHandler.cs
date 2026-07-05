using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Common.Validation;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Application.FavoriteProducts.Common;
using FoodDiary.Application.FavoriteProducts.Models;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.FavoriteProducts.Queries.GetFavoriteProducts;

public sealed class GetFavoriteProductsQueryHandler(
    IFavoriteProductReadService favoriteProductReadService,
    ICurrentUserAccessService currentUserAccessService)
    : IQueryHandler<GetFavoriteProductsQuery, Result<IReadOnlyList<FavoriteProductModel>>> {
    public async Task<Result<IReadOnlyList<FavoriteProductModel>>> Handle(
        GetFavoriteProductsQuery query,
        CancellationToken cancellationToken) {
        Result<UserId> userIdResult = UserIdParser.Parse(query.UserId);
        if (userIdResult.IsFailure) {
            return Result.Failure<IReadOnlyList<FavoriteProductModel>>(userIdResult.Error);
        }

        UserId userId = userIdResult.Value;
        Error? accessError = await currentUserAccessService.EnsureCanAccessAsync(userId, cancellationToken).ConfigureAwait(false);
        if (accessError is not null) {
            return Result.Failure<IReadOnlyList<FavoriteProductModel>>(accessError);
        }

        IReadOnlyList<FavoriteProductModel> favorites = await favoriteProductReadService.GetAllAsync(userId, cancellationToken).ConfigureAwait(false);
        return Result.Success(favorites);
    }
}
