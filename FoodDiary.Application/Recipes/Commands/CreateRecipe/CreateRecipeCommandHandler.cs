using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.Common.Interfaces.Persistence;
using FoodDiary.Application.Recipes.Mappings;
using FoodDiary.Application.Recipes.Models;
using FoodDiary.Application.Recipes.Services;
using FoodDiary.Domain.Entities.Recipes;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Recipes.Commands.CreateRecipe;

public class CreateRecipeCommandHandler(IRecipeRepository recipeRepository)
    : ICommandHandler<CreateRecipeCommand, Result<RecipeModel>> {
    public async Task<Result<RecipeModel>> Handle(CreateRecipeCommand command, CancellationToken cancellationToken) {
        if (command.UserId is null || command.UserId == Guid.Empty) {
            return Result.Failure<RecipeModel>(Errors.Authentication.InvalidToken);
        }

        var userId = new UserId(command.UserId!.Value);

        if (!Enum.TryParse<Visibility>(command.Visibility, true, out var visibility)) {
            return Result.Failure<RecipeModel>(
                Errors.Validation.Invalid(nameof(command.Visibility), "Unknown visibility value."));
        }

        var recipe = Recipe.Create(
            userId,
            command.Name,
            command.Servings,
            command.Description,
            command.Comment,
            command.Category,
            command.ImageUrl,
            command.ImageAssetId.HasValue ? new ImageAssetId(command.ImageAssetId.Value) : null,
            command.PrepTime ?? 0,
            command.CookTime,
            visibility);

        AddSteps(recipe, command);

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

        await recipeRepository.AddAsync(recipe, cancellationToken);
        await RecipeNutritionUpdater.EnsureNutritionAsync(recipe, recipeRepository, cancellationToken);

        var created = await recipeRepository.GetByIdAsync(
            recipe.Id,
            userId,
            includePublic: false,
            includeSteps: true,
            asTracking: false,
            cancellationToken: cancellationToken);

        return created is null
            ? Result.Failure<RecipeModel>(Errors.Recipe.InvalidData("Failed to load created recipe."))
            : Result.Success(created.ToModel(0, true));
    }

    private static void AddSteps(Recipe recipe, CreateRecipeCommand command) {
        var orderedSteps = command.Steps
            .Select((step, index) => new { Step = step, Order = step.Order > 0 ? step.Order : index + 1 })
            .OrderBy(x => x.Order);

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
}
