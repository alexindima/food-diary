using FoodDiary.Modules.Users.Contracts.Models;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects;
using FoodDiary.Modules.Recipes.Application.Mappings;
using FoodDiary.Modules.Recipes.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Application.Contracts.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Modules.Images.Service.Contracts.Common;
using FoodDiary.Modules.Products.Contracts.Common;
using FoodDiary.Modules.Recipes.Application.Abstractions.Common;
using FoodDiary.Modules.Recipes.Contracts.Common;
using FoodDiary.Modules.Users.Contracts.Common;
using FoodDiary.Modules.Recipes.Application.Common;

using FoodDiary.Modules.Recipes.Application.Models;
using FoodDiary.Modules.Recipes.Application.Services;
using FoodDiary.Modules.Recipes.Domain.Entities;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;

namespace FoodDiary.Modules.Recipes.Application.Commands.CreateRecipe;

public sealed class CreateRecipeCommandHandler(
    IRecipeWriteRepository recipeRepository,
    IRecipeNutritionWriter recipeNutritionWriter,
    ICurrentUserAccessService currentUserAccessService,
    IImageAssetAccessService imageAssetAccessService,
    IProductLookupService productLookupService,
    IRecipeLookupService recipeLookupService,
    IRecipeMutationTransactionRunner transactionRunner,
    IUserProfileReadService userProfileReadService)
    : ICommandHandler<CreateRecipeCommand, Result<RecipeModel>> {
    public Task<Result<RecipeModel>> Handle(CreateRecipeCommand command, CancellationToken cancellationToken) =>
        transactionRunner.ExecuteAsync(
            token => HandleCoreAsync(command, token),
            cancellationToken);

    private async Task<Result<RecipeModel>> HandleCoreAsync(CreateRecipeCommand command, CancellationToken cancellationToken) {
        Result<CreateRecipeValues> valuesResult = await CreateRecipeValuePreparer.PrepareAsync(
            command,
            currentUserAccessService,
            imageAssetAccessService,
            cancellationToken);
        if (valuesResult.IsFailure) {
            return Result.Failure<RecipeModel>(valuesResult.Error);
        }

        CreateRecipeValues values = valuesResult.Value;
        Result<IReadOnlyList<RecipeImage>?> gallery = await RecipeImageAssetResolver.ResolveGalleryAsync(command.ImageAssetIds, values.UserId, imageAssetAccessService, cancellationToken);
        if (gallery.IsFailure) { return Result.Failure<RecipeModel>(gallery.Error); }
        Recipe recipe = RecipeCreateFactory.Create(command, values);
        string language = command.Language ?? "en";
        if (command.Language is null) {
            Result<UserModel> profile = await userProfileReadService.GetUserAsync(values.UserId, cancellationToken);
            if (profile.IsFailure) { return Result.Failure<RecipeModel>(profile.Error); }
            language = LanguageCode.FromPreferred(profile.Value.Language).Value;
        }
        recipe.ChangeLanguage(language);
        recipe.SetLanguageConfirmation(command.LanguageConfirmed);
        if (gallery.Value is { } images) { recipe.ReplaceImages(images); }
        Result stepsResult = await RecipeStepAppender.AddAsync(
            recipe,
            command.Steps,
            values.UserId,
            imageAssetAccessService,
            cancellationToken);
        if (stepsResult.IsFailure) {
            return Result.Failure<RecipeModel>(stepsResult.Error);
        }

        Result ingredientsResult = await EnsureIngredientsAccessibleAsync(command.Steps, recipe.Id, values.UserId, cancellationToken);
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

        return await SaveAsync(recipe, cancellationToken);
    }

    private async Task<Result<RecipeModel>> SaveAsync(
        Recipe recipe,
        CancellationToken cancellationToken) {
        await recipeRepository.AddAsync(recipe, cancellationToken);
        await RecipeNutritionUpdater.EnsureNutritionAsync(recipe, recipeNutritionWriter, cancellationToken);

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
