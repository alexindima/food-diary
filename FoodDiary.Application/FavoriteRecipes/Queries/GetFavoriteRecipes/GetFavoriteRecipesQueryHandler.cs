using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Application.FavoriteRecipes.Common;
using FoodDiary.Application.FavoriteRecipes.Models;
using FoodDiary.Application.Users.Common;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.FavoriteRecipes.Queries.GetFavoriteRecipes;

public sealed class GetFavoriteRecipesQueryHandler(
    IFavoriteRecipeReadService favoriteRecipeReadService,
    ICurrentUserAccessService currentUserAccessService)
    : IQueryHandler<GetFavoriteRecipesQuery, Result<IReadOnlyList<FavoriteRecipeModel>>> {
    public async Task<Result<IReadOnlyList<FavoriteRecipeModel>>> Handle(
        GetFavoriteRecipesQuery query,
        CancellationToken cancellationToken) {
        Result<UserId> userIdResult = await CurrentUserAccessResolver.ResolveAsync(
            query.UserId,
            currentUserAccessService,
            cancellationToken).ConfigureAwait(false);
        if (userIdResult.IsFailure) {
            return CurrentUserAccessResolver.ToFailure<IReadOnlyList<FavoriteRecipeModel>>(userIdResult);
        }

        UserId userId = userIdResult.Value;
        IReadOnlyList<FavoriteRecipeModel> favorites = await favoriteRecipeReadService.GetAllAsync(userId, cancellationToken).ConfigureAwait(false);
        return Result.Success(favorites);
    }
}
