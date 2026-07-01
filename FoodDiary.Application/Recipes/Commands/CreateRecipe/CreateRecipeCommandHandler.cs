using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Abstractions.Common.Interfaces.Persistence;
using FoodDiary.Application.Abstractions.Images.Common;
using FoodDiary.Application.Abstractions.Products.Common;
using FoodDiary.Application.Abstractions.Recipes.Common;
using FoodDiary.Application.Common.Validation;
using FoodDiary.Application.Recipes.Common;
using FoodDiary.Application.Recipes.Mappings;
using FoodDiary.Application.Recipes.Models;
using FoodDiary.Application.Recipes.Services;
using FoodDiary.Application.Users.Common;
using FoodDiary.Domain.Entities.Assets;
using FoodDiary.Domain.Entities.Recipes;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Recipes.Commands.CreateRecipe;

public class CreateRecipeCommandHandler(
    IRecipeRepository recipeRepository,
    IUserRepository userRepository,
    IImageAssetAccessService imageAssetAccessService,
    IProductLookupService productLookupService,
    IRecipeLookupService recipeLookupService)
    : ICommandHandler<CreateRecipeCommand, Result<RecipeModel>> {
    private sealed record CreateRecipeValues(
        UserId UserId,
        Visibility Visibility,
        ImageAssetId? ImageAssetId,
        ImageAsset? ImageAsset);

    public async Task<Result<RecipeModel>> Handle(CreateRecipeCommand command, CancellationToken cancellationToken) {
        Result<CreateRecipeValues> valuesResult = await PrepareCreateValuesAsync(command, cancellationToken).ConfigureAwait(false);
        if (valuesResult.IsFailure) {
            return Result.Failure<RecipeModel>(valuesResult.Error);
        }

        CreateRecipeValues values = valuesResult.Value;
        Recipe recipe = CreateRecipe(command, values);
        Result stepsResult = await RecipeStepAppender.AddAsync(
            recipe,
            command.Steps,
            values.UserId,
            imageAssetAccessService,
            cancellationToken).ConfigureAwait(false);
        if (stepsResult.IsFailure) {
            return Result.Failure<RecipeModel>(stepsResult.Error);
        }

        Result ingredientsResult = await EnsureIngredientsAccessibleAsync(command.Steps, recipe.Id, values.UserId, cancellationToken).ConfigureAwait(false);
        if (ingredientsResult.IsFailure) {
            return Result.Failure<RecipeModel>(ingredientsResult.Error);
        }

        Result nutritionResult = RecipeNutritionApplier.Apply(
            recipe,
            command.CalculateNutritionAutomatically,
            command.ManualCalories,
            command.ManualProteins,
            command.ManualFats,
            command.ManualCarbs,
            command.ManualFiber,
            command.ManualAlcohol);
        if (nutritionResult.IsFailure) {
            return Result.Failure<RecipeModel>(nutritionResult.Error);
        }

        return await SaveAsync(recipe, cancellationToken).ConfigureAwait(false);
    }

    private async Task<Result<CreateRecipeValues>> PrepareCreateValuesAsync(
        CreateRecipeCommand command,
        CancellationToken cancellationToken) {
        if (command.UserId is null || command.UserId == Guid.Empty) {
            return Result.Failure<CreateRecipeValues>(Errors.Authentication.InvalidToken);
        }

        Result<ImageAssetId?> imageAssetIdResult = ImageAssetIdParser.ParseOptional(command.ImageAssetId, nameof(command.ImageAssetId));
        if (imageAssetIdResult.IsFailure) {
            return Result.Failure<CreateRecipeValues>(imageAssetIdResult.Error);
        }

        var userId = new UserId(command.UserId!.Value);
        Error? accessError = await CurrentUserAccessLoader.EnsureCanAccessAsync(userRepository, userId, cancellationToken).ConfigureAwait(false);
        if (accessError is not null) {
            return Result.Failure<CreateRecipeValues>(accessError);
        }

        Result<ImageAsset?> imageAssetResult = await imageAssetAccessService.ResolveOptionalAsync(
            imageAssetIdResult.Value,
            userId,
            cancellationToken).ConfigureAwait(false);
        if (imageAssetResult.IsFailure) {
            return Result.Failure<CreateRecipeValues>(imageAssetResult.Error);
        }

        Result<Visibility> visibilityResult = EnumValueParser.ParseRequired<Visibility>(
            command.Visibility,
            nameof(command.Visibility),
            "Unknown visibility value.");
        if (visibilityResult.IsFailure) {
            return Result.Failure<CreateRecipeValues>(visibilityResult.Error);
        }

        return Result.Success(new CreateRecipeValues(
            userId,
            visibilityResult.Value,
            imageAssetIdResult.Value,
            imageAssetResult.Value));
    }

    private static Recipe CreateRecipe(CreateRecipeCommand command, CreateRecipeValues values) =>
        Recipe.Create(
            values.UserId,
            command.Name,
            command.Servings,
            command.Description,
            command.Comment,
            command.Category,
            values.ImageAsset?.Url ?? command.ImageUrl,
            values.ImageAssetId,
            command.PrepTime ?? 0,
            command.CookTime,
            values.Visibility);

    private async Task<Result<RecipeModel>> SaveAsync(
        Recipe recipe,
        CancellationToken cancellationToken) {
        await recipeRepository.AddAsync(recipe, cancellationToken).ConfigureAwait(false);
        await RecipeNutritionUpdater.EnsureNutritionAsync(recipe, recipeRepository, cancellationToken).ConfigureAwait(false);

        return Result.Success(recipe.ToModel(0, isOwnedByCurrentUser: true));
    }

    private Task<Result> EnsureIngredientsAccessibleAsync(
        IReadOnlyList<RecipeStepInput> steps,
        RecipeId recipeId,
        UserId userId,
        CancellationToken cancellationToken) =>
        RecipeIngredientAccessValidator.EnsureIngredientsAccessibleAsync(
            steps,
            recipeId,
            userId,
            productLookupService,
            recipeLookupService,
            cancellationToken);

}
