using FoodDiary.Modules.Users.Contracts.Common.Validation;
using FoodDiary.Modules.Recipes.Application.Mappings;
using FoodDiary.Modules.Recipes.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Domain.Primitives;
using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Modules.Recipes.Application.Abstractions.Common;
using FoodDiary.Modules.Recipes.Contracts.Common;
using FoodDiary.Modules.Users.Contracts.Common;
using FoodDiary.Modules.Recipes.Application.Common;

using FoodDiary.Modules.Recipes.Application.Models;
using FoodDiary.Modules.Recipes.Application.Services;
using FoodDiary.Modules.Recipes.Domain.Entities;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;

namespace FoodDiary.Modules.Recipes.Application.Commands.DuplicateRecipe;

public sealed class DuplicateRecipeCommandHandler(
    IRecipeReadRepository recipeReadRepository,
    IRecipeWriteRepository recipeWriteRepository,
    IRecipeNutritionWriter recipeNutritionWriter,
    ICurrentUserAccessService currentUserAccessService,
    IRecipeMutationTransactionRunner transactionRunner)
    : ICommandHandler<DuplicateRecipeCommand, Result<RecipeModel>> {
    public Task<Result<RecipeModel>> Handle(DuplicateRecipeCommand command, CancellationToken cancellationToken) =>
        transactionRunner.ExecuteAsync(
            token => HandleCoreAsync(command, token),
            cancellationToken);

    private async Task<Result<RecipeModel>> HandleCoreAsync(DuplicateRecipeCommand command, CancellationToken cancellationToken) {
        Result<RecipeId> recipeIdResult = RecipeRequiredIdParser.Parse(
            command.RecipeId,
            nameof(command.RecipeId),
            "Recipe id must not be empty.",
            value => new RecipeId(value));
        if (recipeIdResult.IsFailure) {
            return RecipeRequiredIdParser.ToFailure<RecipeModel, RecipeId>(recipeIdResult);
        }

        Result<UserId> userIdResult = await CurrentUserAccessResolver.ResolveAsync(
            command.UserId,
            currentUserAccessService,
            cancellationToken).ConfigureAwait(false);
        if (userIdResult.IsFailure) {
            return UserIdParser.ToFailure<RecipeModel>(userIdResult);
        }

        UserId userId = userIdResult.Value;
        RecipeId recipeId = recipeIdResult.Value;

        Recipe? original = await recipeReadRepository.GetByIdAsync(
            recipeId,
            userId,
            includePublic: true,
            includeSteps: true,
            cancellationToken: cancellationToken).ConfigureAwait(false);

        if (original is null) {
            return Result.Failure<RecipeModel>(RecipeErrors.NotAccessible(command.RecipeId));
        }

        Recipe duplicate = CreateDuplicate(userId, original);
        await recipeWriteRepository.AddAsync(duplicate, cancellationToken).ConfigureAwait(false);
        await RecipeNutritionUpdater.EnsureNutritionAsync(duplicate, recipeNutritionWriter, cancellationToken).ConfigureAwait(false);

        return Result.Success(duplicate.ToModel(0, isOwnedByCurrentUser: true));
    }

    private static Recipe CreateDuplicate(UserId userId, Recipe original) {
        bool isOwnerDuplicate = original.UserId == userId;
        var duplicate = Recipe.Create(
            userId,
            original.Name,
            original.Servings,
            original.Description,
            isOwnerDuplicate ? original.Comment : null,
            original.Category,
            original.ImageUrl,
            imageAssetId: null,
            original.PrepTime,
            original.CookTime,
            original.Visibility);

        AddStepsFromOriginal(duplicate, original, isOwnerDuplicate);
        ApplyNutritionSettings(duplicate, original);
        return duplicate;
    }

    private static void ApplyNutritionSettings(Recipe duplicate, Recipe original) {
        if (original.IsNutritionAutoCalculated) {
            duplicate.EnableAutoNutrition();
            duplicate.ApplyComputedNutrition(
                original.TotalCalories,
                original.TotalProteins,
                original.TotalFats,
                original.TotalCarbs,
                original.TotalFiber,
                original.TotalAlcohol);
            return;
        }

        duplicate.SetManualNutrition(
            original.ManualCalories ?? original.TotalCalories,
            original.ManualProteins ?? original.TotalProteins,
            original.ManualFats ?? original.TotalFats,
            original.ManualCarbs ?? original.TotalCarbs,
            original.ManualFiber ?? original.TotalFiber,
            original.ManualAlcohol ?? original.TotalAlcohol);
    }

    private static void AddStepsFromOriginal(Recipe target, Recipe source, bool preserveManagedAssets) {
        var orderedSteps = source.Steps
            .OrderBy(step => step.StepNumber)
            .ToList();

        foreach (RecipeStep step in orderedSteps) {
            RecipeStep newStep = target.AddStep(
                step.StepNumber,
                step.Instruction,
                step.Title,
                step.ImageUrl,
                preserveManagedAssets ? step.ImageAssetId : null);

            foreach (RecipeIngredient ingredient in step.Ingredients) {
                if (ingredient.ProductId.HasValue &&
                    (preserveManagedAssets || ingredient.ProductSnapshot?.Visibility == Visibility.Public)) {
                    newStep.AddProductIngredient(ingredient.ProductId.Value, ingredient.Amount);
                } else if (ingredient.NestedRecipeId.HasValue &&
                    (preserveManagedAssets || ingredient.NestedRecipe?.Visibility == Visibility.Public)) {
                    newStep.AddNestedRecipeIngredient(ingredient.NestedRecipeId.Value, ingredient.Amount);
                }
            }
        }
    }
}
