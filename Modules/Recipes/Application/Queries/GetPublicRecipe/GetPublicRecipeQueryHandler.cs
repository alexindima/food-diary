using FoodDiary.Application.Contracts.Common.Abstractions.Messaging;
using FoodDiary.Domain.Primitives;
using FoodDiary.Modules.Recipes.Application.Mappings;
using FoodDiary.Modules.Recipes.Application.Common;
using FoodDiary.Modules.Recipes.Application.Models;
using FoodDiary.Modules.Recipes.Contracts.Common;
using FoodDiary.Modules.Recipes.Contracts.Models;
using FoodDiary.Modules.Recipes.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Results;

namespace FoodDiary.Modules.Recipes.Application.Queries.GetPublicRecipe;

public sealed class GetPublicRecipeQueryHandler(IRecipeOverviewReadService recipes)
    : IQueryHandler<GetPublicRecipeQuery, Result<PublicRecipeModel>> {
    public async Task<Result<PublicRecipeModel>> Handle(GetPublicRecipeQuery query, CancellationToken cancellationToken) {
        Result<RecipeId> idResult = RecipeRequiredIdParser.Parse(query.RecipeId, nameof(query.RecipeId),
            "Recipe id must not be empty.", value => new RecipeId(value));
        if (idResult.IsFailure) {
            return Result.Failure<PublicRecipeModel>(RecipeErrors.NotFound(query.RecipeId));
        }
        RecipeId id = idResult.Value;
        IReadOnlyDictionary<RecipeId, RecipeOverviewReadItem> items = await recipes.GetByIdsWithUsageAsync([id], UserId.Empty, includePublic: true, cancellationToken).ConfigureAwait(false);
        return items.TryGetValue(id, out RecipeOverviewReadItem? recipe) && recipe.Visibility == Visibility.Public
            ? Result.Success(recipe.ToPublicModel())
            : Result.Failure<PublicRecipeModel>(RecipeErrors.NotFound(query.RecipeId));
    }
}
