using FoodDiary.Modules.Recipes.Application.Mappings;
using FoodDiary.Modules.Recipes.Contracts.Models;
using FoodDiary.Modules.Recipes.Application.Models;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Modules.Recipes.Application.Services;
using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Application.Abstractions.Users.Common;

namespace FoodDiary.Modules.Recipes.Application.Queries.GetRecentRecipes;

public sealed class GetRecentRecipesQueryHandler(
    RecentRecipeLoader recentRecipeLoader,
    ICurrentUserAccessService currentUserAccessService)
    : IQueryHandler<GetRecentRecipesQuery, Result<IReadOnlyList<RecipeModel>>> {
    public async Task<Result<IReadOnlyList<RecipeModel>>> Handle(
        GetRecentRecipesQuery query,
        CancellationToken cancellationToken) {
        Result<UserId> userIdResult = await CurrentUserAccessResolver.ResolveAsync(
            query.UserId,
            currentUserAccessService,
            cancellationToken).ConfigureAwait(false);
        if (userIdResult.IsFailure) {
            return CurrentUserAccessResolver.ToFailure<IReadOnlyList<RecipeModel>>(userIdResult);
        }

        UserId userId = userIdResult.Value;
        int recentLimit = Math.Clamp(query.Limit, 1, 50);

        IReadOnlyList<RecipeModel> response = await GetRecentAsync(
            userId,
            recentLimit,
            query.IncludePublic,
            cancellationToken).ConfigureAwait(false);

        return Result.Success(response);
    }
    private async Task<IReadOnlyList<RecipeModel>> GetRecentAsync(
        UserId userId,
        int limit,
        bool includePublic,
        CancellationToken cancellationToken = default) {
        IReadOnlyList<RecipeOverviewReadItem> items = await recentRecipeLoader.LoadAsync(
            userId,
            limit,
            includePublic,
            cancellationToken).ConfigureAwait(false);

        return [.. items.Select(item => item.ToModel())];
    }
}
