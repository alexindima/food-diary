using FoodDiary.Modules.Recipes.Application.Mappings;
using FoodDiary.Modules.Recipes.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
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
    IRecipeMutationTransactionRunner transactionRunner)
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
            cancellationToken).ConfigureAwait(false);
        if (valuesResult.IsFailure) {
            return Result.Failure<RecipeModel>(valuesResult.Error);
        }

        CreateRecipeValues values = valuesResult.Value;
        Recipe recipe = RecipeCreateFactory.Create(command, values);
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

    private async Task<Result<RecipeModel>> SaveAsync(
        Recipe recipe,
        CancellationToken cancellationToken) {
        await recipeRepository.AddAsync(recipe, cancellationToken).ConfigureAwait(false);
        await RecipeNutritionUpdater.EnsureNutritionAsync(recipe, recipeNutritionWriter, cancellationToken).ConfigureAwait(false);

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
