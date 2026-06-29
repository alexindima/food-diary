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
        Result stepsResult = await AddStepsAsync(recipe, command, values.UserId, cancellationToken).ConfigureAwait(false);
        if (stepsResult.IsFailure) {
            return Result.Failure<RecipeModel>(stepsResult.Error);
        }

        Result ingredientsResult = await EnsureIngredientsAccessibleAsync(command.Steps, recipe.Id, values.UserId, cancellationToken).ConfigureAwait(false);
        if (ingredientsResult.IsFailure) {
            return Result.Failure<RecipeModel>(ingredientsResult.Error);
        }

        Result nutritionResult = ApplyNutrition(recipe, command);
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

    private static Result ApplyNutrition(Recipe recipe, CreateRecipeCommand command) {
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

    private async Task<Result> AddStepsAsync(
        Recipe recipe,
        CreateRecipeCommand command,
        UserId userId,
        CancellationToken cancellationToken) {
        var orderedSteps = command.Steps
            .Select((step, index) => new { Step = step, Order = step.Order > 0 ? step.Order : index + 1 })
            .OrderBy(x => x.Order);

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
