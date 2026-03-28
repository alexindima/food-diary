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

        var imageAssetIdResult = NormalizeImageAssetId(command.ImageAssetId, nameof(command.ImageAssetId));
        if (imageAssetIdResult.IsFailure) {
            return Result.Failure<RecipeModel>(imageAssetIdResult.Error);
        }

        var userId = new UserId(command.UserId!.Value);
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
            clearDescription: command.ClearDescription,
            comment: command.Comment,
            clearComment: command.ClearComment,
            category: command.Category,
            clearCategory: command.ClearCategory);
        recipe.UpdateMedia(
            imageUrl: command.ImageUrl,
            clearImageUrl: command.ClearImageUrl,
            imageAssetId: imageAssetIdResult.Value,
            clearImageAssetId: command.ClearImageAssetId);
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
            var stepImageAssetIdResult = NormalizeImageAssetId(entry.Step.ImageAssetId, nameof(entry.Step.ImageAssetId));
            if (stepImageAssetIdResult.IsFailure) {
                return Result.Failure<RecipeModel>(stepImageAssetIdResult.Error);
            }

            var step = recipe.AddStep(
                entry.Order,
                entry.Step.Description,
                entry.Step.Title,
                entry.Step.ImageUrl,
                stepImageAssetIdResult.Value);
            foreach (var ingredient in entry.Step.Ingredients) {
                var ingredientIdResult = ValidateIngredientIdentifiers(ingredient);
                if (ingredientIdResult.IsFailure) {
                    return Result.Failure<RecipeModel>(ingredientIdResult.Error);
                }

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
            var manualNutritionResult = ValidateManualNutrition(
                command.ManualCalories,
                command.ManualProteins,
                command.ManualFats,
                command.ManualCarbs,
                command.ManualFiber,
                command.ManualAlcohol);

            if (manualNutritionResult.IsFailure) {
                return Result.Failure<RecipeModel>(manualNutritionResult.Error);
            }

            var manual = manualNutritionResult.Value;
            recipe.SetManualNutrition(
                manual.Calories,
                manual.Proteins,
                manual.Fats,
                manual.Carbs,
                manual.Fiber,
                manual.Alcohol);
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

        if (oldAssetId.HasValue && (command.ClearImageAssetId || !command.ImageAssetId.HasValue || oldAssetId.Value.Value != command.ImageAssetId.Value)) {
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

    private static Result<(double Calories, double Proteins, double Fats, double Carbs, double Fiber, double Alcohol)> ValidateManualNutrition(
        double? calories,
        double? proteins,
        double? fats,
        double? carbs,
        double? fiber,
        double? alcohol) {
        if (calories is null) {
            return Result.Failure<(double, double, double, double, double, double)>(Errors.Validation.Required(nameof(calories)));
        }

        if (proteins is null) {
            return Result.Failure<(double, double, double, double, double, double)>(Errors.Validation.Required(nameof(proteins)));
        }

        if (fats is null) {
            return Result.Failure<(double, double, double, double, double, double)>(Errors.Validation.Required(nameof(fats)));
        }

        if (carbs is null) {
            return Result.Failure<(double, double, double, double, double, double)>(Errors.Validation.Required(nameof(carbs)));
        }

        if (fiber is null) {
            return Result.Failure<(double, double, double, double, double, double)>(Errors.Validation.Required(nameof(fiber)));
        }

        if (calories < 0 || proteins < 0 || fats < 0 || carbs < 0 || fiber < 0 || alcohol < 0) {
            return Result.Failure<(double, double, double, double, double, double)>(
                Errors.Validation.Invalid("ManualNutrition", "Manual nutrition values must be greater than or equal to 0."));
        }

        return Result.Success((calories.Value, proteins.Value, fats.Value, carbs.Value, fiber.Value, alcohol ?? 0));
    }

    private static Result<ImageAssetId?> NormalizeImageAssetId(Guid? value, string fieldName) {
        if (!value.HasValue) {
            return Result.Success<ImageAssetId?>(null);
        }

        return value.Value == Guid.Empty
            ? Result.Failure<ImageAssetId?>(Errors.Validation.Invalid(fieldName, "Image asset id must not be empty."))
            : Result.Success<ImageAssetId?>(new ImageAssetId(value.Value));
    }

    private static Result ValidateIngredientIdentifiers(RecipeIngredientInput ingredient) {
        if (ingredient.ProductId == Guid.Empty) {
            return Result.Failure(Errors.Validation.Invalid(nameof(ingredient.ProductId), "Product id must not be empty."));
        }

        if (ingredient.NestedRecipeId == Guid.Empty) {
            return Result.Failure(Errors.Validation.Invalid(nameof(ingredient.NestedRecipeId), "Nested recipe id must not be empty."));
        }

        return Result.Success();
    }
}
