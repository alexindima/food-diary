using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Abstractions.Recipes.Common;
using FoodDiary.Application.Abstractions.Recipes.Models;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Application.Recipes.Mappings;
using FoodDiary.Application.Recipes.Models;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Recipes.Queries.GetRecipeById;

public sealed class GetRecipeByIdQueryHandler(
    IRecipeOverviewReadService recipeOverviewReadService,
    ICurrentUserAccessService currentUserAccessService)
    : IQueryHandler<GetRecipeByIdQuery, Result<RecipeModel>> {
    public async Task<Result<RecipeModel>> Handle(GetRecipeByIdQuery query, CancellationToken cancellationToken) {
        if (query.UserId is null || query.UserId == Guid.Empty) {
            return Result.Failure<RecipeModel>(Errors.Authentication.InvalidToken);
        }

        if (query.RecipeId == Guid.Empty) {
            return Result.Failure<RecipeModel>(Errors.Validation.Invalid(nameof(query.RecipeId), "Recipe id must not be empty."));
        }

        var userId = new UserId(query.UserId!.Value);
        Error? accessError = await currentUserAccessService.EnsureCanAccessAsync(userId, cancellationToken).ConfigureAwait(false);
        if (accessError is not null) {
            return Result.Failure<RecipeModel>(accessError);
        }
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
