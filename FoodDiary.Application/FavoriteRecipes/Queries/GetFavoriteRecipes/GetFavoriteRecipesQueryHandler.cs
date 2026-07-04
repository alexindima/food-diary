using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Common.Validation;
using FoodDiary.Application.Abstractions.FavoriteRecipes.Common;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Application.FavoriteRecipes.Mappings;
using FoodDiary.Application.FavoriteRecipes.Models;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Domain.Entities.FavoriteRecipes;

namespace FoodDiary.Application.FavoriteRecipes.Queries.GetFavoriteRecipes;

public sealed class GetFavoriteRecipesQueryHandler(
    IFavoriteRecipeReadRepository favoriteRecipeRepository,
    ICurrentUserAccessService currentUserAccessService)
    : IQueryHandler<GetFavoriteRecipesQuery, Result<IReadOnlyList<FavoriteRecipeModel>>> {
    public async Task<Result<IReadOnlyList<FavoriteRecipeModel>>> Handle(
        GetFavoriteRecipesQuery query,
        CancellationToken cancellationToken) {
        Result<UserId> userIdResult = UserIdParser.Parse(query.UserId);
        if (userIdResult.IsFailure) {
            return Result.Failure<IReadOnlyList<FavoriteRecipeModel>>(userIdResult.Error);
        }

        UserId userId = userIdResult.Value;
        Error? accessError = await currentUserAccessService.EnsureCanAccessAsync(userId, cancellationToken).ConfigureAwait(false);
        if (accessError is not null) {
            return Result.Failure<IReadOnlyList<FavoriteRecipeModel>>(accessError);
        }

        IReadOnlyList<FavoriteRecipe> favorites = await favoriteRecipeRepository.GetAllAsync(userId, cancellationToken).ConfigureAwait(false);
        var models = favorites.Select(f => f.ToModel()).ToList();
        return Result.Success<IReadOnlyList<FavoriteRecipeModel>>(models);
    }
}
