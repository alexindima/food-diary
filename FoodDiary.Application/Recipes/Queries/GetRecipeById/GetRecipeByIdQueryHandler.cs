using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Result;
using FoodDiary.Application.Abstractions.Common.Interfaces.Persistence;
using FoodDiary.Application.Recipes.Mappings;
using FoodDiary.Application.Recipes.Models;
using FoodDiary.Application.Users.Common;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Recipes.Queries.GetRecipeById;

public class GetRecipeByIdQueryHandler(
    IRecipeRepository recipeRepository,
    IUserRepository userRepository)
    : IQueryHandler<GetRecipeByIdQuery, Result<RecipeModel>> {
    public async Task<Result<RecipeModel>> Handle(GetRecipeByIdQuery query, CancellationToken cancellationToken) {
        if (query.UserId is null || query.UserId == Guid.Empty) {
            return Result.Failure<RecipeModel>(Errors.Authentication.InvalidToken);
        }

        if (query.RecipeId == Guid.Empty) {
            return Result.Failure<RecipeModel>(Errors.Validation.Invalid(nameof(query.RecipeId), "Recipe id must not be empty."));
        }

        var userId = new UserId(query.UserId!.Value);
        var accessError = await CurrentUserAccessLoader.EnsureCanAccessAsync(userRepository, userId, cancellationToken);
        if (accessError is not null) {
            return Result.Failure<RecipeModel>(accessError);
        }
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
