using FoodDiary.Application.Abstractions.Common.Validation;
using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Application.Abstractions.FavoriteRecipes.Common;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Application.Favorites.FavoriteRecipes.Mappings;
using FoodDiary.Application.Abstractions.FavoriteRecipes.Models;
using FoodDiary.Domain.Entities.FavoriteRecipes;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Favorites.FavoriteRecipes.Commands.AddFavoriteRecipe;

public sealed class AddFavoriteRecipeCommandHandler(
    IFavoriteRecipeWriteRepository favoriteRecipeRepository,
    IFavoriteRecipeSourceReadService sourceReadService,
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

        Result<RecipeId> recipeIdResult = RequiredIdParser.Parse(
            command.RecipeId,
            nameof(command.RecipeId),
            "Recipe id must not be empty.",
            value => new RecipeId(value));
        if (recipeIdResult.IsFailure) {
            return RequiredIdParser.ToFailure<FavoriteRecipeModel, RecipeId>(recipeIdResult);
        }

        UserId userId = userIdResult.Value;
        RecipeId recipeId = recipeIdResult.Value;
        Result<FavoriteRecipeSourceModel> sourceResult = await sourceReadService
            .GetAccessibleAsync(recipeId, userId, cancellationToken).ConfigureAwait(false);
        if (sourceResult.IsFailure) {
            return Result.Failure<FavoriteRecipeModel>(sourceResult.Error);
        }

        FavoriteRecipeSourceModel recipe = sourceResult.Value;

        FavoriteRecipe? existing = await favoriteRecipeRepository.GetByRecipeIdAsync(recipeId, userId, cancellationToken).ConfigureAwait(false);
        if (existing is not null) {
            return Result.Failure<FavoriteRecipeModel>(FavoriteRecipeErrors.AlreadyExists);
        }

        var favorite = FavoriteRecipe.Create(userId, recipeId, command.Name);
        await favoriteRecipeRepository.AddAsync(favorite, cancellationToken).ConfigureAwait(false);

        return Result.Success(favorite.ToModel(recipe));
    }
}
