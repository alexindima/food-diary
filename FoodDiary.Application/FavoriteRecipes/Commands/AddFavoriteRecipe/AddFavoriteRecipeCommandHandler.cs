using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Abstractions.Common.Interfaces.Persistence;
using FoodDiary.Application.Common.Validation;
using FoodDiary.Application.Abstractions.FavoriteRecipes.Common;
using FoodDiary.Application.FavoriteRecipes.Mappings;
using FoodDiary.Application.FavoriteRecipes.Models;
using FoodDiary.Application.Users.Common;
using FoodDiary.Domain.Entities.FavoriteRecipes;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Domain.Entities.Recipes;

namespace FoodDiary.Application.FavoriteRecipes.Commands.AddFavoriteRecipe;

public class AddFavoriteRecipeCommandHandler(
    IFavoriteRecipeRepository favoriteRecipeRepository,
    IRecipeRepository recipeRepository,
    IUserRepository userRepository)
    : ICommandHandler<AddFavoriteRecipeCommand, Result<FavoriteRecipeModel>> {
    public async Task<Result<FavoriteRecipeModel>> Handle(
        AddFavoriteRecipeCommand command,
        CancellationToken cancellationToken) {
        Result<UserId> userIdResult = UserIdParser.Parse(command.UserId);
        if (userIdResult.IsFailure) {
            return Result.Failure<FavoriteRecipeModel>(userIdResult.Error);
        }

        UserId userId = userIdResult.Value;
        Error? accessError = await CurrentUserAccessLoader.EnsureCanAccessAsync(userRepository, userId, cancellationToken).ConfigureAwait(false);
        if (accessError is not null) {
            return Result.Failure<FavoriteRecipeModel>(accessError);
        }

        var recipeId = new RecipeId(command.RecipeId);
        Recipe? recipe = await recipeRepository.GetByIdAsync(
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

        FavoriteRecipe? saved = await favoriteRecipeRepository.GetByIdAsync(favorite.Id, userId, cancellationToken: cancellationToken).ConfigureAwait(false);
        return Result.Success(saved!.ToModel());
    }
}
