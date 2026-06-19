using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Abstractions.Common.Interfaces.Persistence;
using FoodDiary.Application.Abstractions.Images.Common;
using FoodDiary.Application.Abstractions.Products.Common;
using FoodDiary.Application.Abstractions.Recipes.Common;
using FoodDiary.Application.Common.Nutrition;
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

namespace FoodDiary.Application.Recipes.Commands.UpdateRecipe;

public class UpdateRecipeCommandHandler(
    IRecipeRepository recipeRepository,
    IImageAssetCleanupService imageAssetCleanupService,
    IUserRepository userRepository,
    IImageAssetAccessService imageAssetAccessService,
    IProductLookupService productLookupService,
    IRecipeLookupService recipeLookupService)
    : ICommandHandler<UpdateRecipeCommand, Result<RecipeModel>> {
    private sealed record UpdateRecipeValues(
        UserId UserId,
        RecipeId RecipeId,
        Recipe Recipe,
        Visibility? Visibility,
        ImageAssetId? ImageAssetId,
        ImageAsset? ImageAsset,
        ImageAssetId? OldAssetId,
        IReadOnlyList<ImageAssetId> OldStepAssetIds,
        IReadOnlyList<RecipeStepInput> Steps);

    public async Task<Result<RecipeModel>> Handle(UpdateRecipeCommand command, CancellationToken cancellationToken) {
        Result<UpdateRecipeValues> valuesResult = await PrepareUpdateValuesAsync(command, cancellationToken).ConfigureAwait(false);
        if (valuesResult.IsFailure) {
            return Result.Failure<RecipeModel>(valuesResult.Error);
        }

        UpdateRecipeValues values = valuesResult.Value;
        ApplyRecipeUpdates(values.Recipe, command, values);

        Result stepsResult = await ReplaceStepsAsync(values.Recipe, values.Steps, values.UserId, cancellationToken).ConfigureAwait(false);
        if (stepsResult.IsFailure) {
            return Result.Failure<RecipeModel>(stepsResult.Error);
        }

        Result nutritionResult = ApplyNutrition(values.Recipe, command);
        if (nutritionResult.IsFailure) {
            return Result.Failure<RecipeModel>(nutritionResult.Error);
        }

        Result<Recipe> updatedResult = await SaveAndReloadAsync(values.Recipe, values.UserId, cancellationToken).ConfigureAwait(false);
        if (updatedResult.IsFailure) {
            return Result.Failure<RecipeModel>(updatedResult.Error);
        }

        await CleanupAssetsAsync(command, values, cancellationToken).ConfigureAwait(false);
        Recipe updated = updatedResult.Value;
        return Result.Success(updated.ToModel(updated.MealItems.Count + updated.NestedRecipeUsages.Count, isOwnedByCurrentUser: true));
    }

    private async Task<Result<UpdateRecipeValues>> PrepareUpdateValuesAsync(
        UpdateRecipeCommand command,
        CancellationToken cancellationToken) {
        Result commandValidation = ValidateCommand(command);
        if (commandValidation.IsFailure) {
            return Result.Failure<UpdateRecipeValues>(commandValidation.Error);
        }

        Result<ImageAssetId?> imageAssetIdResult = ImageAssetIdParser.ParseOptional(command.ImageAssetId, nameof(command.ImageAssetId));
        if (imageAssetIdResult.IsFailure) {
            return Result.Failure<UpdateRecipeValues>(imageAssetIdResult.Error);
        }

        var userId = new UserId(command.UserId!.Value);
        Error? accessError = await CurrentUserAccessLoader.EnsureCanAccessAsync(userRepository, userId, cancellationToken).ConfigureAwait(false);
        if (accessError is not null) {
            return Result.Failure<UpdateRecipeValues>(accessError);
        }

        var recipeId = new RecipeId(command.RecipeId);
        Result<Recipe> recipeResult = await ResolveEditableRecipeAsync(command, recipeId, userId, cancellationToken).ConfigureAwait(false);
        if (recipeResult.IsFailure) {
            return Result.Failure<UpdateRecipeValues>(recipeResult.Error);
        }

        Result<Visibility?> visibilityResult = ParseVisibility(command);
        if (visibilityResult.IsFailure) {
            return Result.Failure<UpdateRecipeValues>(visibilityResult.Error);
        }

        IReadOnlyList<RecipeStepInput> steps = command.Steps ?? Array.Empty<RecipeStepInput>();
        Result ingredientAccessResult = await EnsureIngredientsAccessibleAsync(steps, recipeId, userId, cancellationToken).ConfigureAwait(false);
        if (ingredientAccessResult.IsFailure) {
            return Result.Failure<UpdateRecipeValues>(ingredientAccessResult.Error);
        }

        Recipe recipe = recipeResult.Value;
        ImageAssetId? oldAssetId = recipe.ImageAssetId;
        var oldStepAssetIds = recipe.Steps
            .Select(step => step.ImageAssetId)
            .Where(id => id.HasValue)
            .Select(id => id!.Value)
            .Distinct()
            .ToList();
        Result<ImageAsset?> imageAssetResult = await imageAssetAccessService.ResolveOptionalAsync(
            imageAssetIdResult.Value,
            userId,
            cancellationToken).ConfigureAwait(false);
        if (imageAssetResult.IsFailure) {
            return Result.Failure<UpdateRecipeValues>(imageAssetResult.Error);
        }

        return Result.Success(new UpdateRecipeValues(
            userId,
            recipeId,
            recipe,
            visibilityResult.Value,
            imageAssetIdResult.Value,
            imageAssetResult.Value,
            oldAssetId,
            oldStepAssetIds,
            steps));
    }

    private static Result ValidateCommand(UpdateRecipeCommand command) {
        if (command.UserId is null || command.UserId == Guid.Empty) {
            return Result.Failure(Errors.Authentication.InvalidToken);
        }

        return command.RecipeId == Guid.Empty
            ? Result.Failure(Errors.Validation.Invalid(nameof(command.RecipeId), "Recipe id must not be empty."))
            : Result.Success();
    }

    private async Task<Result<Recipe>> ResolveEditableRecipeAsync(
        UpdateRecipeCommand command,
        RecipeId recipeId,
        UserId userId,
        CancellationToken cancellationToken) {
        Recipe? recipe = await recipeRepository.GetByIdAsync(
            recipeId,
            userId,
            includePublic: false,
            includeSteps: true,
            asTracking: true,
            cancellationToken: cancellationToken).ConfigureAwait(false);
        if (recipe is null) {
            return Result.Failure<Recipe>(Errors.Recipe.NotAccessible(command.RecipeId));
        }

        int usageCount = recipe.MealItems.Count + recipe.NestedRecipeUsages.Count;
        return usageCount > 0
            ? Result.Failure<Recipe>(
                Errors.Validation.Invalid("RecipeId", "Recipe is already used and cannot be modified"))
            : Result.Success(recipe);
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

    private static Result<Visibility?> ParseVisibility(UpdateRecipeCommand command) {
        if (string.IsNullOrWhiteSpace(command.Visibility)) {
            return Result.Success<Visibility?>(value: null);
        }

        return EnumValueParser.ParseOptional<Visibility>(
            command.Visibility,
            nameof(command.Visibility),
            "Unknown visibility value.");
    }

    private static void ApplyRecipeUpdates(
        Recipe recipe,
        UpdateRecipeCommand command,
        UpdateRecipeValues values) {
        recipe.UpdateIdentity(
            name: command.Name,
            description: command.Description,
            clearDescription: command.ClearDescription,
            comment: command.Comment,
            clearComment: command.ClearComment,
            category: command.Category,
            clearCategory: command.ClearCategory);
        recipe.UpdateMedia(
            imageUrl: values.ImageAsset?.Url ?? command.ImageUrl,
            clearImageUrl: values.ImageAsset is null && command.ClearImageUrl,
            imageAssetId: values.ImageAssetId,
            clearImageAssetId: command.ClearImageAssetId);
        recipe.UpdateTimingAndServings(
            prepTime: command.PrepTime,
            cookTime: command.CookTime,
            servings: command.Servings);

        if (values.Visibility.HasValue) {
            recipe.ChangeVisibility(values.Visibility.Value);
        }
    }

    private async Task<Result> ReplaceStepsAsync(
        Recipe recipe,
        IReadOnlyList<RecipeStepInput> steps,
        UserId userId,
        CancellationToken cancellationToken) {
        recipe.ClearSteps();
        var orderedSteps = steps
            .Select((step, index) => new { Step = step, Order = step.Order > 0 ? step.Order : index + 1 })
            .OrderBy(x => x.Order)
            .ToList();

        foreach (var entry in orderedSteps) {
            Result<ImageAssetId?> stepImageAssetIdResult = ImageAssetIdParser.ParseOptional(entry.Step.ImageAssetId, nameof(entry.Step.ImageAssetId));
            if (stepImageAssetIdResult.IsFailure) {
                return stepImageAssetIdResult;
            }

            Result<ImageAsset?> stepImageAssetResult = await imageAssetAccessService.ResolveOptionalAsync(
                stepImageAssetIdResult.Value,
                userId,
                cancellationToken).ConfigureAwait(false);
            if (stepImageAssetResult.IsFailure) {
                return stepImageAssetResult;
            }

            RecipeStep step = recipe.AddStep(
                entry.Order,
                entry.Step.Description,
                entry.Step.Title,
                stepImageAssetResult.Value?.Url ?? entry.Step.ImageUrl,
                stepImageAssetIdResult.Value);
            foreach (RecipeIngredientInput ingredient in entry.Step.Ingredients) {
                Result ingredientIdResult = ValidateIngredientIdentifiers(ingredient);
                if (ingredientIdResult.IsFailure) {
                    return ingredientIdResult;
                }

                if (ingredient.ProductId.HasValue) {
                    step.AddProductIngredient(new ProductId(ingredient.ProductId.Value), ingredient.Amount);
                } else if (ingredient.NestedRecipeId.HasValue) {
                    step.AddNestedRecipeIngredient(new RecipeId(ingredient.NestedRecipeId.Value), ingredient.Amount);
                }
            }
        }

        return Result.Success();
    }

    private static Result ApplyNutrition(Recipe recipe, UpdateRecipeCommand command) {
        if (command.CalculateNutritionAutomatically) {
            recipe.EnableAutoNutrition();
            return Result.Success();
        }

        Result<(double Calories, double Proteins, double Fats, double Carbs, double Fiber, double Alcohol)> manualNutritionResult = ValidateManualNutrition(
            command.ManualCalories,
            command.ManualProteins,
            command.ManualFats,
            command.ManualCarbs,
            command.ManualFiber,
            command.ManualAlcohol);
        if (manualNutritionResult.IsFailure) {
            return manualNutritionResult;
        }

        (double Calories, double Proteins, double Fats, double Carbs, double Fiber, double Alcohol) = manualNutritionResult.Value;
        recipe.SetManualNutrition(
            Calories,
            Proteins,
            Fats,
            Carbs,
            Fiber,
            Alcohol);
        return Result.Success();
    }

    private async Task<Result<Recipe>> SaveAndReloadAsync(
        Recipe recipe,
        UserId userId,
        CancellationToken cancellationToken) {
        await recipeRepository.UpdateAsync(recipe, cancellationToken).ConfigureAwait(false);

        Recipe? updated = await recipeRepository.GetByIdAsync(
            recipe.Id,
            userId,
            includePublic: false,
            includeSteps: true,
            asTracking: true,
            cancellationToken: cancellationToken).ConfigureAwait(false);

        if (updated is null) {
            return Result.Failure<Recipe>(Errors.Recipe.InvalidData("Failed to load updated recipe."));
        }

        await RecipeNutritionUpdater.EnsureNutritionAsync(updated, recipeRepository, cancellationToken).ConfigureAwait(false);
        return Result.Success(updated);
    }

    private async Task CleanupAssetsAsync(
        UpdateRecipeCommand command,
        UpdateRecipeValues values,
        CancellationToken cancellationToken) {
        bool imageAssetChanged = command.ClearImageAssetId ||
                                (command.ImageAssetId.HasValue &&
                                 (!values.OldAssetId.HasValue || values.OldAssetId.Value.Value != command.ImageAssetId.Value));

        if (values.OldAssetId.HasValue && imageAssetChanged) {
            await imageAssetCleanupService.DeleteIfUnusedAsync(values.OldAssetId.Value, cancellationToken).ConfigureAwait(false);
        }

        if (values.OldStepAssetIds.Count > 0) {
            var newStepAssetIds = values.Steps
                .Select(step => step.ImageAssetId)
                .Where(id => id.HasValue)
                .Select(id => new ImageAssetId(id!.Value))
                .ToHashSet();

            foreach (ImageAssetId assetId in values.OldStepAssetIds) {
                if (!newStepAssetIds.Contains(assetId)) {
                    await imageAssetCleanupService.DeleteIfUnusedAsync(assetId, cancellationToken).ConfigureAwait(false);
                }
            }
        }
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

        if (calories > ManualNutritionLimits.MaxCalories) {
            return Result.Failure<(double, double, double, double, double, double)>(
                Errors.Validation.Invalid(nameof(calories), ManualNutritionLimits.MaxCaloriesErrorMessage));
        }

        if (proteins > ManualNutritionLimits.MaxNutrient ||
            fats > ManualNutritionLimits.MaxNutrient ||
            carbs > ManualNutritionLimits.MaxNutrient ||
            fiber > ManualNutritionLimits.MaxNutrient ||
            alcohol > ManualNutritionLimits.MaxNutrient) {
            return Result.Failure<(double, double, double, double, double, double)>(
                Errors.Validation.Invalid("ManualNutrition", ManualNutritionLimits.MaxNutrientErrorMessage));
        }

        return Result.Success((calories.Value, proteins.Value, fats.Value, carbs.Value, fiber.Value, alcohol ?? 0));
    }

    private static Result ValidateIngredientIdentifiers(RecipeIngredientInput ingredient) {
        Result productIdResult = OptionalEntityIdValidator.EnsureNotEmpty(ingredient.ProductId, nameof(ingredient.ProductId), "Product id");
        if (productIdResult.IsFailure) {
            return productIdResult;
        }

        Result nestedRecipeIdResult = OptionalEntityIdValidator.EnsureNotEmpty(
            ingredient.NestedRecipeId,
            nameof(ingredient.NestedRecipeId),
            "Nested recipe id");
        return nestedRecipeIdResult.IsFailure ? nestedRecipeIdResult : Result.Success();
    }
}
