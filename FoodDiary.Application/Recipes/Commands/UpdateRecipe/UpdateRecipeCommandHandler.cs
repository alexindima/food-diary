using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.Common.Interfaces.Persistence;
using FoodDiary.Application.Images.Common;
using FoodDiary.Application.Recipes.Common;
using FoodDiary.Application.Recipes.Mappings;
using FoodDiary.Application.Recipes.Models;
using FoodDiary.Application.Recipes.Services;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Recipes.Commands.UpdateRecipe;

public class UpdateRecipeCommandHandler(
    IRecipeRepository recipeRepository,
    IImageAssetCleanupService imageAssetCleanupService)
    : ICommandHandler<UpdateRecipeCommand, Result<RecipeModel>> {
    public async Task<Result<RecipeModel>> Handle(UpdateRecipeCommand command, CancellationToken cancellationToken) {
        if (command.UserId is null || command.UserId == Guid.Empty) {
            return Result.Failure<RecipeModel>(Errors.Authentication.InvalidToken);
        }

        var userId = new UserId(command.UserId.Value);
        var recipeId = new RecipeId(command.RecipeId);

        var recipe = await recipeRepository.GetByIdAsync(
            recipeId,
            userId,
            includePublic: false,
            includeSteps: true,
            asTracking: true,
            cancellationToken: cancellationToken);

        if (recipe is null) {
            return Result.Failure<RecipeModel>(Errors.Recipe.NotAccessible(command.RecipeId));
        }

        var usageCount = recipe.MealItems.Count + recipe.NestedRecipeUsages.Count;
        if (usageCount > 0) {
            return Result.Failure<RecipeModel>(
                Errors.Validation.Invalid("RecipeId", "Recipe is already used and cannot be modified"));
        }

        Visibility? visibility = null;
        if (!string.IsNullOrWhiteSpace(command.Visibility)) {
            if (!Enum.TryParse<Visibility>(command.Visibility, true, out var parsedVisibility)) {
                return Result.Failure<RecipeModel>(
                    Errors.Validation.Invalid(nameof(command.Visibility), "Unknown visibility value."));
            }

            visibility = parsedVisibility;
        }

        var oldAssetId = recipe.ImageAssetId;
        var oldStepAssetIds = recipe.Steps
            .Select(step => step.ImageAssetId)
            .Where(id => id.HasValue)
            .Select(id => id!.Value)
            .Distinct()
            .ToList();

        recipe.UpdateIdentity(
            name: command.Name,
            description: command.Description,
            comment: command.Comment,
            category: command.Category);
        recipe.UpdateMedia(
            imageUrl: command.ImageUrl,
            imageAssetId: command.ImageAssetId.HasValue ? new ImageAssetId(command.ImageAssetId.Value) : null);
        recipe.UpdateTimingAndServings(
            prepTime: command.PrepTime ?? 0,
            cookTime: command.CookTime,
            servings: command.Servings);

        if (visibility.HasValue) {
            recipe.ChangeVisibility(visibility.Value);
        }

        recipe.ClearSteps();
        var steps = command.Steps ?? Array.Empty<RecipeStepInput>();
        var orderedSteps = steps
            .Select((step, index) => new { Step = step, Order = step.Order > 0 ? step.Order : index + 1 })
            .OrderBy(x => x.Order)
            .ToList();

        foreach (var entry in orderedSteps) {
            var step = recipe.AddStep(
                entry.Order,
                entry.Step.Description,
                entry.Step.Title,
                entry.Step.ImageUrl,
                entry.Step.ImageAssetId.HasValue ? new ImageAssetId(entry.Step.ImageAssetId.Value) : null);
            foreach (var ingredient in entry.Step.Ingredients) {
                if (ingredient.ProductId.HasValue) {
                    step.AddProductIngredient(new ProductId(ingredient.ProductId.Value), ingredient.Amount);
                } else if (ingredient.NestedRecipeId.HasValue) {
                    step.AddNestedRecipeIngredient(new RecipeId(ingredient.NestedRecipeId.Value), ingredient.Amount);
                }
            }
        }

        if (command.CalculateNutritionAutomatically) {
            recipe.EnableAutoNutrition();
        } else {
            recipe.SetManualNutrition(
                command.ManualCalories ?? 0,
                command.ManualProteins ?? 0,
                command.ManualFats ?? 0,
                command.ManualCarbs ?? 0,
                command.ManualFiber ?? 0,
                command.ManualAlcohol ?? 0);
        }

        await recipeRepository.UpdateAsync(recipe, cancellationToken);

        var updated = await recipeRepository.GetByIdAsync(
            recipe.Id,
            userId,
            includePublic: false,
            includeSteps: true,
            asTracking: true,
            cancellationToken: cancellationToken);

        if (updated is null) {
            return Result.Failure<RecipeModel>(Errors.Recipe.InvalidData("Failed to load updated recipe."));
        }

        await RecipeNutritionUpdater.EnsureNutritionAsync(updated, recipeRepository, cancellationToken);

        if (oldAssetId.HasValue && (!command.ImageAssetId.HasValue || oldAssetId.Value.Value != command.ImageAssetId.Value)) {
            await imageAssetCleanupService.DeleteIfUnusedAsync(oldAssetId.Value, cancellationToken);
        }

        if (oldStepAssetIds.Count > 0) {
            var newStepAssetIds = orderedSteps
                .Select(entry => entry.Step.ImageAssetId)
                .Where(id => id.HasValue)
                .Select(id => new ImageAssetId(id!.Value))
                .ToHashSet();

            foreach (var assetId in oldStepAssetIds) {
                if (!newStepAssetIds.Contains(assetId)) {
                    await imageAssetCleanupService.DeleteIfUnusedAsync(assetId, cancellationToken);
                }
            }
        }

        return Result.Success(updated.ToModel(updated.MealItems.Count + updated.NestedRecipeUsages.Count, true));
    }
}
