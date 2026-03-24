using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.Common.Interfaces.Persistence;
using FoodDiary.Application.Recipes.Mappings;
using FoodDiary.Application.Recipes.Models;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Recipes.Queries.GetRecipeById;

public class GetRecipeByIdQueryHandler(IRecipeRepository recipeRepository)
    : IQueryHandler<GetRecipeByIdQuery, Result<RecipeModel>> {
    public async Task<Result<RecipeModel>> Handle(GetRecipeByIdQuery query, CancellationToken cancellationToken) {
        if (query.UserId is null || query.UserId == Guid.Empty) {
            return Result.Failure<RecipeModel>(Errors.Authentication.InvalidToken);
        }

        var userId = new UserId(query.UserId.Value);
        var recipeId = new RecipeId(query.RecipeId);

        var recipe = await recipeRepository.GetByIdAsync(
            recipeId,
            userId,
            includePublic: query.IncludePublic,
            includeSteps: true,
            cancellationToken: cancellationToken);

        if (recipe is null) {
            return Result.Failure<RecipeModel>(Errors.Recipe.NotFound(query.RecipeId));
        }

        var usageCount = recipe.MealItems.Count + recipe.NestedRecipeUsages.Count;
        var isOwner = recipe.UserId == userId;

        return Result.Success(recipe.ToModel(usageCount, isOwner));
    }
}
