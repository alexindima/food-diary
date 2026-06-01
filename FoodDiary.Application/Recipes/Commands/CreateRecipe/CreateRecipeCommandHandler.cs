using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Result;
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
    public async Task<Result<RecipeModel>> Handle(CreateRecipeCommand command, CancellationToken cancellationToken) {
        if (command.UserId is null || command.UserId == Guid.Empty) {
            return Result.Failure<RecipeModel>(Errors.Authentication.InvalidToken);
        }

        var imageAssetIdResult = ImageAssetIdParser.ParseOptional(command.ImageAssetId, nameof(command.ImageAssetId));
        if (imageAssetIdResult.IsFailure) {
            return Result.Failure<RecipeModel>(imageAssetIdResult.Error);
        }

        var userId = new UserId(command.UserId!.Value);
        var accessError = await CurrentUserAccessLoader.EnsureCanAccessAsync(userRepository, userId, cancellationToken).ConfigureAwait(false);
        if (accessError is not null) {
            return Result.Failure<RecipeModel>(accessError);
        }

        var imageAssetResult = await imageAssetAccessService.ResolveOptionalAsync(
            imageAssetIdResult.Value,
            userId,
            cancellationToken).ConfigureAwait(false);
        if (imageAssetResult.IsFailure) {
            return Result.Failure<RecipeModel>(imageAssetResult.Error);
        }

        var imageUrl = imageAssetResult.Value?.Url ?? command.ImageUrl;

        var visibilityResult = EnumValueParser.ParseRequired<Visibility>(
            command.Visibility,
            nameof(command.Visibility),
            "Unknown visibility value.");
        if (visibilityResult.IsFailure) {
            return Result.Failure<RecipeModel>(visibilityResult.Error);
        }

        var recipe = Recipe.Create(
            userId,
            command.Name,
            command.Servings,
            command.Description,
            command.Comment,
            command.Category,
            imageUrl,
            imageAssetIdResult.Value,
            command.PrepTime ?? 0,
            command.CookTime,
            visibilityResult.Value);

        var addStepsResult = await AddStepsAsync(recipe, command, userId, cancellationToken).ConfigureAwait(false);
        if (addStepsResult.IsFailure) {
            return Result.Failure<RecipeModel>(addStepsResult.Error);
        }

        var ingredientAccessResult = await RecipeIngredientAccessValidator.EnsureIngredientsAccessibleAsync(
            command.Steps,
            recipe.Id,
            userId,
            productLookupService,
            recipeLookupService,
            cancellationToken).ConfigureAwait(false);
        if (ingredientAccessResult.IsFailure) {
            return Result.Failure<RecipeModel>(ingredientAccessResult.Error);
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

        await recipeRepository.AddAsync(recipe, cancellationToken).ConfigureAwait(false);
        await RecipeNutritionUpdater.EnsureNutritionAsync(recipe, recipeRepository, cancellationToken).ConfigureAwait(false);

        var created = await recipeRepository.GetByIdAsync(
            recipe.Id,
            userId,
            includePublic: false,
            includeSteps: true,
            asTracking: false,
            cancellationToken: cancellationToken).ConfigureAwait(false);

        return created is null
            ? Result.Failure<RecipeModel>(Errors.Recipe.InvalidData("Failed to load created recipe."))
            : Result.Success(created.ToModel(0, true));
    }

    private async Task<Result> AddStepsAsync(
        Recipe recipe,
        CreateRecipeCommand command,
        UserId userId,
        CancellationToken cancellationToken) {
        var orderedSteps = command.Steps
            .Select((step, index) => new { Step = step, Order = step.Order > 0 ? step.Order : index + 1 })
            .OrderBy(x => x.Order);

        foreach (var entry in orderedSteps) {
            var stepImageAssetIdResult = ImageAssetIdParser.ParseOptional(entry.Step.ImageAssetId, nameof(entry.Step.ImageAssetId));
            if (stepImageAssetIdResult.IsFailure) {
                return stepImageAssetIdResult;
            }

            var stepImageAssetResult = await imageAssetAccessService.ResolveOptionalAsync(
                stepImageAssetIdResult.Value,
                userId,
                cancellationToken).ConfigureAwait(false);
            if (stepImageAssetResult.IsFailure) {
                return stepImageAssetResult;
            }

            var step = recipe.AddStep(
                entry.Order,
                entry.Step.Description,
                entry.Step.Title,
                stepImageAssetResult.Value?.Url ?? entry.Step.ImageUrl,
                stepImageAssetIdResult.Value);
            foreach (var ingredient in entry.Step.Ingredients) {
                var ingredientIdResult = ValidateIngredientIdentifiers(ingredient);
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

        return Result.Success((calories.Value, proteins.Value, fats.Value, carbs.Value, fiber.Value, alcohol ?? 0));
    }

    private static Result ValidateIngredientIdentifiers(RecipeIngredientInput ingredient) {
        var productIdResult = OptionalEntityIdValidator.EnsureNotEmpty(ingredient.ProductId, nameof(ingredient.ProductId), "Product id");
        if (productIdResult.IsFailure) {
            return productIdResult;
        }

        var nestedRecipeIdResult = OptionalEntityIdValidator.EnsureNotEmpty(
            ingredient.NestedRecipeId,
            nameof(ingredient.NestedRecipeId),
            "Nested recipe id");
        if (nestedRecipeIdResult.IsFailure) {
            return nestedRecipeIdResult;
        }

        return Result.Success();
    }
}
