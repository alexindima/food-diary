using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Abstractions.Common.Interfaces.Persistence;
using FoodDiary.Application.Recipes.Mappings;
using FoodDiary.Application.Recipes.Models;
using FoodDiary.Application.Recipes.Services;
using FoodDiary.Domain.Entities.Recipes;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Recipes.Commands.DuplicateRecipe;

public class DuplicateRecipeCommandHandler(IRecipeRepository recipeRepository)
    : ICommandHandler<DuplicateRecipeCommand, Result<RecipeModel>> {
    public async Task<Result<RecipeModel>> Handle(DuplicateRecipeCommand command, CancellationToken cancellationToken) {
        if (command.UserId is null || command.UserId == Guid.Empty) {
            return Result.Failure<RecipeModel>(Errors.Authentication.InvalidToken);
        }

        if (command.RecipeId == Guid.Empty) {
            return Result.Failure<RecipeModel>(Errors.Validation.Invalid(nameof(command.RecipeId), "Recipe id must not be empty."));
        }

        var userId = new UserId(command.UserId!.Value);
        var recipeId = new RecipeId(command.RecipeId);

        Recipe? original = await recipeRepository.GetByIdAsync(
            recipeId,
            userId,
            includePublic: true,
            includeSteps: true,
            cancellationToken: cancellationToken).ConfigureAwait(false);

        if (original is null) {
            return Result.Failure<RecipeModel>(Errors.Recipe.NotFound(command.RecipeId));
        }

        Recipe duplicate = CreateDuplicate(userId, original);
        await recipeRepository.AddAsync(duplicate, cancellationToken).ConfigureAwait(false);

        Recipe? created = await recipeRepository.GetByIdAsync(
            duplicate.Id,
            userId,
            includePublic: false,
            includeSteps: true,
            asTracking: true,
            cancellationToken: cancellationToken).ConfigureAwait(false);

        if (created is null) {
            return Result.Failure<RecipeModel>(Errors.Recipe.InvalidData("Failed to load duplicated recipe."));
        }

        await RecipeNutritionUpdater.EnsureNutritionAsync(created, recipeRepository, cancellationToken).ConfigureAwait(false);

        return Result.Success(created.ToModel(0, isOwnedByCurrentUser: true));
    }

    private static Recipe CreateDuplicate(UserId userId, Recipe original) {
        var duplicate = Recipe.Create(
            userId,
            original.Name,
            original.Servings,
            original.Description,
            original.Comment,
            original.Category,
            original.ImageUrl,
            imageAssetId: null,
            original.PrepTime,
            original.CookTime,
            original.Visibility);

        AddStepsFromOriginal(duplicate, original);
        ApplyNutritionSettings(duplicate, original);
        return duplicate;
    }

    private static void ApplyNutritionSettings(Recipe duplicate, Recipe original) {
        if (original.IsNutritionAutoCalculated) {
            duplicate.EnableAutoNutrition();
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

    private static void AddStepsFromOriginal(Recipe target, Recipe source) {
        var orderedSteps = source.Steps
            .OrderBy(step => step.StepNumber)
            .ToList();

        foreach (RecipeStep step in orderedSteps) {
            RecipeStep newStep = target.AddStep(step.StepNumber, step.Instruction, step.Title, step.ImageUrl, step.ImageAssetId);

            foreach (RecipeIngredient ingredient in step.Ingredients) {
                if (ingredient.ProductId.HasValue) {
                    newStep.AddProductIngredient(ingredient.ProductId.Value, ingredient.Amount);
                } else if (ingredient.NestedRecipeId.HasValue) {
                    newStep.AddNestedRecipeIngredient(ingredient.NestedRecipeId.Value, ingredient.Amount);
                }
            }
        }
    }
}
