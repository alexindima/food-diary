using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.Common.Interfaces.Persistence;
using FoodDiary.Application.Recipes.Mappings;
using FoodDiary.Contracts.Recipes;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Recipes.Queries.GetRecipeById;

public class GetRecipeByIdQueryHandler(IRecipeRepository recipeRepository)
    : IQueryHandler<GetRecipeByIdQuery, Result<RecipeResponse>> {
    public async Task<Result<RecipeResponse>> Handle(GetRecipeByIdQuery query, CancellationToken cancellationToken) {
        if (query.UserId is null || query.UserId == UserId.Empty) {
            return Result.Failure<RecipeResponse>(Errors.Authentication.InvalidToken);
        }

        var recipe = await recipeRepository.GetByIdAsync(
            query.RecipeId,
            query.UserId.Value,
            includePublic: query.IncludePublic,
            includeSteps: true,
            cancellationToken: cancellationToken);

        if (recipe is null) {
            return Result.Failure<RecipeResponse>(Errors.Recipe.NotFound(query.RecipeId.Value));
        }

        var usageCount = recipe.MealItems.Count + recipe.NestedRecipeUsages.Count;
        var isOwner = recipe.UserId == query.UserId.Value;

        return Result.Success(recipe.ToResponse(usageCount, isOwner));
    }
}
