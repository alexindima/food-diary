using FoodDiary.Modules.Recipes.Application.Mappings;
using FoodDiary.Modules.Recipes.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Modules.Recipes.Contracts.Common;
using FoodDiary.Modules.Recipes.Contracts.Models;
using FoodDiary.Modules.Users.Contracts.Common;
using FoodDiary.Modules.Recipes.Application.Common;

using FoodDiary.Modules.Recipes.Application.Models;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;

namespace FoodDiary.Modules.Recipes.Application.Queries.GetRecipeById;

public sealed class GetRecipeByIdQueryHandler(
    IRecipeOverviewReadService recipeOverviewReadService,
    ICurrentUserAccessService currentUserAccessService)
    : IQueryHandler<GetRecipeByIdQuery, Result<RecipeModel>> {
    public async Task<Result<RecipeModel>> Handle(GetRecipeByIdQuery query, CancellationToken cancellationToken) {
        Result<RecipeId> recipeIdResult = RecipeRequiredIdParser.Parse(
            query.RecipeId,
            nameof(query.RecipeId),
            "Recipe id must not be empty.",
            value => new RecipeId(value));
        if (recipeIdResult.IsFailure) {
            return RecipeRequiredIdParser.ToFailure<RecipeModel, RecipeId>(recipeIdResult);
        }

        Result<UserId> userIdResult = await CurrentUserAccessResolver.ResolveAsync(
            query.UserId,
            currentUserAccessService,
            cancellationToken).ConfigureAwait(false);
        if (userIdResult.IsFailure) {
            return CurrentUserAccessResolver.ToFailure<RecipeModel>(userIdResult);
        }

        UserId userId = userIdResult.Value;
        RecipeId recipeId = recipeIdResult.Value;

        IReadOnlyDictionary<RecipeId, RecipeOverviewReadItem> recipesById = await recipeOverviewReadService.GetByIdsWithUsageAsync(
            [recipeId],
            userId,
            includePublic: query.IncludePublic,
            cancellationToken).ConfigureAwait(false);
        RecipeOverviewReadItem? recipe = recipesById.GetValueOrDefault(recipeId);

        if (recipe is null) {
            return Result.Failure<RecipeModel>(RecipeErrors.NotFound(query.RecipeId));
        }

        return Result.Success(recipe.ToModel());
    }
}
