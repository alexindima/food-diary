using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Common.Validation;
using FoodDiary.Application.Abstractions.FavoriteRecipes.Common;
using FoodDiary.Application.Abstractions.Recipes.Common;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Application.FavoriteRecipes.Mappings;
using FoodDiary.Application.FavoriteRecipes.Models;
using FoodDiary.Application.Users.Common;
using FoodDiary.Domain.Entities.FavoriteRecipes;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Domain.Entities.Recipes;

namespace FoodDiary.Application.FavoriteRecipes.Commands.AddFavoriteRecipe;

public sealed class AddFavoriteRecipeCommandHandler(
    IFavoriteRecipeWriteRepository favoriteRecipeRepository,
    IRecipeAccessService recipeAccessService,
    ICurrentUserAccessService currentUserAccessService)
    : ICommandHandler<AddFavoriteRecipeCommand, Result<FavoriteRecipeModel>> {
    public async Task<Result<FavoriteRecipeModel>> Handle(
        AddFavoriteRecipeCommand command,
        CancellationToken cancellationToken) {
        Result<UserId> userIdResult = await CurrentUserAccessResolver
            .ResolveAsync(command.UserId, currentUserAccessService, cancellationToken)
            .ConfigureAwait(false);
        if (userIdResult.IsFailure) {
            return UserIdParser.ToFailure<FavoriteRecipeModel>(userIdResult);
        }

        UserId userId = userIdResult.Value;
        var recipeId = new RecipeId(command.RecipeId);
        Recipe? recipe = await recipeAccessService.GetAccessibleByIdAsync(
            recipeId,
            userId,
            includePublic: true,
            includeSteps: true,
            cancellationToken: cancellationToken).ConfigureAwait(false);
        if (recipe is null) {
            return Result.Failure<FavoriteRecipeModel>(Errors.Recipe.NotFound(command.RecipeId));
        }

        FavoriteRecipe? existing = await favoriteRecipeRepository.GetByRecipeIdAsync(recipeId, userId, cancellationToken).ConfigureAwait(false);
        if (existing is not null) {
            return Result.Failure<FavoriteRecipeModel>(Errors.FavoriteRecipe.AlreadyExists);
        }

        var favorite = FavoriteRecipe.Create(userId, recipeId, command.Name);
        await favoriteRecipeRepository.AddAsync(favorite, cancellationToken).ConfigureAwait(false);

        return Result.Success(favorite.ToModel(recipe));
    }
}
