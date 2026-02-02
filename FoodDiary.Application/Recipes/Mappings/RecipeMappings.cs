using System;
using System.Collections.Generic;
using System.Linq;
using FoodDiary.Application.Recipes.Commands.Common;
using FoodDiary.Application.Recipes.Commands.CreateRecipe;
using FoodDiary.Application.Recipes.Commands.UpdateRecipe;
using FoodDiary.Contracts.Recipes;
using FoodDiary.Domain.Entities;
using FoodDiary.Domain.ValueObjects;
using FoodDiary.Application.Recipes.Services;

namespace FoodDiary.Application.Recipes.Mappings;

public static class RecipeMappings
{
    public static RecipeResponse ToResponse(this Recipe recipe, int usageCount, bool isOwnedByCurrentUser)
    {
        var steps = recipe.Steps
            .OrderBy(s => s.StepNumber)
            .Select(step => new RecipeStepResponse(
                step.Id.Value,
                step.StepNumber,
                step.Title,
                step.Instruction,
                step.ImageUrl,
                step.ImageAssetId?.Value,
                step.Ingredients
                    .Select(ingredient => new RecipeIngredientResponse(
                        ingredient.Id.Value,
                        ingredient.Amount,
                        ingredient.ProductId?.Value,
                        ingredient.Product?.Name,
                        ingredient.Product?.BaseUnit.ToString(),
                        ingredient.Product?.BaseAmount,
                        ingredient.Product?.CaloriesPerBase,
                        ingredient.Product?.ProteinsPerBase,
                        ingredient.Product?.FatsPerBase,
                        ingredient.Product?.CarbsPerBase,
                        ingredient.Product?.FiberPerBase,
                        ingredient.Product?.AlcoholPerBase,
                        ingredient.NestedRecipeId?.Value,
                        ingredient.NestedRecipe?.Name))
                    .ToList()))
            .ToList();

        var nutrition = BuildNutrition(recipe);

        return new RecipeResponse(
            recipe.Id.Value,
            recipe.Name,
            recipe.Description,
            isOwnedByCurrentUser ? recipe.Comment : null,
            recipe.Category,
            recipe.ImageUrl,
            recipe.ImageAssetId?.Value,
            recipe.PrepTime,
            recipe.CookTime,
            recipe.Servings,
            nutrition.TotalCalories,
            nutrition.TotalProteins,
            nutrition.TotalFats,
            nutrition.TotalCarbs,
            nutrition.TotalFiber,
            nutrition.TotalAlcohol,
            recipe.IsNutritionAutoCalculated,
            recipe.ManualCalories,
            recipe.ManualProteins,
            recipe.ManualFats,
            recipe.ManualCarbs,
            recipe.ManualFiber,
            recipe.ManualAlcohol,
            recipe.Visibility.ToString(),
            usageCount,
            recipe.CreatedOnUtc,
            isOwnedByCurrentUser,
            steps);
    }

    public static CreateRecipeCommand ToCommand(this CreateRecipeRequest request, Guid? userIdValue)
    {
        return new CreateRecipeCommand(
            userIdValue.HasValue ? new UserId(userIdValue.Value) : null,
            request.Name,
            request.Description,
            request.Comment,
            request.Category,
            request.ImageUrl,
            request.ImageAssetId,
            request.PrepTime,
            request.CookTime,
            request.Servings,
            request.Visibility,
            request.CalculateNutritionAutomatically,
            request.ManualCalories,
            request.ManualProteins,
            request.ManualFats,
            request.ManualCarbs,
            request.ManualFiber,
            request.ManualAlcohol,
            MapSteps(request.Steps));
    }

    public static UpdateRecipeCommand ToCommand(this UpdateRecipeRequest request, Guid? userIdValue, Guid recipeId)
    {
        return new UpdateRecipeCommand(
            userIdValue.HasValue ? new UserId(userIdValue.Value) : null,
            new RecipeId(recipeId),
            request.Name,
            request.Description,
            request.Comment,
            request.Category,
            request.ImageUrl,
            request.ImageAssetId,
            request.PrepTime,
            request.CookTime,
            request.Servings,
            request.Visibility,
            request.CalculateNutritionAutomatically,
            request.ManualCalories,
            request.ManualProteins,
            request.ManualFats,
            request.ManualCarbs,
            request.ManualFiber,
            request.ManualAlcohol,
            request.Steps is null ? null : MapSteps(request.Steps));
    }

    private static IReadOnlyList<RecipeStepInput> MapSteps(IReadOnlyList<RecipeStepRequest> steps) =>
        steps.Select((step, index) =>
            new RecipeStepInput(
                index + 1,
                step.Description,
                step.Title,
                step.ImageUrl,
                step.ImageAssetId,
                step.Ingredients
                    .Select(ingredient => new RecipeIngredientInput(
                        ingredient.ProductId,
                        ingredient.NestedRecipeId,
                        ingredient.Amount))
                    .ToList()))
        .ToList();

    private static RecipeNutritionSummary BuildNutrition(Recipe recipe)
    {
        if (!recipe.IsNutritionAutoCalculated)
        {
            return new RecipeNutritionSummary(
                recipe.ManualCalories ?? recipe.TotalCalories,
                recipe.ManualProteins ?? recipe.TotalProteins,
                recipe.ManualFats ?? recipe.TotalFats,
                recipe.ManualCarbs ?? recipe.TotalCarbs,
                recipe.ManualFiber ?? recipe.TotalFiber,
                recipe.ManualAlcohol ?? recipe.TotalAlcohol);
        }

        return RecipeNutritionCalculator.Calculate(recipe);
    }
}
