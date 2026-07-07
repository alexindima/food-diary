using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Abstractions.Recipes.Common;
using FoodDiary.Application.Abstractions.Recipes.Models;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Application.Recipes.Mappings;
using FoodDiary.Application.Recipes.Models;
using FoodDiary.Application.Users.Common;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Recipes.Queries.GetRecipeById;

public sealed class GetRecipeByIdQueryHandler(
    IRecipeOverviewReadService recipeOverviewReadService,
    ICurrentUserAccessService currentUserAccessService)
    : IQueryHandler<GetRecipeByIdQuery, Result<RecipeModel>> {
    public async Task<Result<RecipeModel>> Handle(GetRecipeByIdQuery query, CancellationToken cancellationToken) {
        if (query.RecipeId == Guid.Empty) {
            return Result.Failure<RecipeModel>(Errors.Validation.Invalid(nameof(query.RecipeId), "Recipe id must not be empty."));
        }

        Result<UserId> userIdResult = await CurrentUserAccessResolver.ResolveAsync(
            query.UserId,
            currentUserAccessService,
            cancellationToken).ConfigureAwait(false);
        if (userIdResult.IsFailure) {
            return CurrentUserAccessResolver.ToFailure<RecipeModel>(userIdResult);
        }

        UserId userId = userIdResult.Value;
        var recipeId = new RecipeId(query.RecipeId);

        IReadOnlyDictionary<RecipeId, RecipeOverviewReadItem> recipesById = await recipeOverviewReadService.GetByIdsWithUsageAsync(
            [recipeId],
            userId,
            includePublic: query.IncludePublic,
            cancellationToken).ConfigureAwait(false);
        RecipeOverviewReadItem? recipe = recipesById.GetValueOrDefault(recipeId);

        if (recipe is null) {
            return Result.Failure<RecipeModel>(Errors.Recipe.NotFound(query.RecipeId));
        }

        return Result.Success(recipe.ToModel());
    }
}
