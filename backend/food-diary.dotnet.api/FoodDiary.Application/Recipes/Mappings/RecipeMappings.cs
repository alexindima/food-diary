using System;
using System.Collections.Generic;
using System.Linq;
using FoodDiary.Application.Recipes.Commands.Common;
using FoodDiary.Application.Recipes.Commands.CreateRecipe;
using FoodDiary.Application.Recipes.Commands.UpdateRecipe;
using FoodDiary.Contracts.Recipes;
using FoodDiary.Domain.Entities;
using FoodDiary.Domain.ValueObjects;

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
                step.Instruction,
                step.ImageUrl,
                step.Ingredients
                    .Select(ingredient => new RecipeIngredientResponse(
                        ingredient.Id.Value,
                        ingredient.Amount,
                        ingredient.ProductId?.Value,
                        ingredient.Product?.Name,
                        ingredient.Product?.BaseUnit.ToString(),
                        ingredient.NestedRecipeId?.Value,
                        ingredient.NestedRecipe?.Name))
                    .ToList()))
            .ToList();

        var nutrition = BuildNutrition(recipe);

        return new RecipeResponse(
            recipe.Id.Value,
            recipe.Name,
            recipe.Description,
            recipe.Category,
            recipe.ImageUrl,
            recipe.PrepTime,
            recipe.CookTime,
            recipe.Servings,
            nutrition.TotalCalories,
            nutrition.TotalProteins,
            nutrition.TotalFats,
            nutrition.TotalCarbs,
            nutrition.TotalFiber,
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
            request.Category,
            request.ImageUrl,
            request.PrepTime,
            request.CookTime,
            request.Servings,
            request.Visibility,
            MapSteps(request.Steps));
    }

    public static UpdateRecipeCommand ToCommand(this UpdateRecipeRequest request, Guid? userIdValue, Guid recipeId)
    {
        return new UpdateRecipeCommand(
            userIdValue.HasValue ? new UserId(userIdValue.Value) : null,
            new RecipeId(recipeId),
            request.Name,
            request.Description,
            request.Category,
            request.ImageUrl,
            request.PrepTime,
            request.CookTime,
            request.Servings,
            request.Visibility,
            request.Steps is null ? null : MapSteps(request.Steps));
    }

    private static IReadOnlyList<RecipeStepInput> MapSteps(IReadOnlyList<RecipeStepRequest> steps) =>
        steps.Select((step, index) =>
            new RecipeStepInput(
                index + 1,
                step.Description,
                step.ImageUrl,
                step.Ingredients
                    .Select(ingredient => new RecipeIngredientInput(
                        ingredient.ProductId,
                        ingredient.NestedRecipeId,
                        ingredient.Amount))
                    .ToList()))
        .ToList();

    private static RecipeNutritionSummary BuildNutrition(Recipe recipe)
    {
        if (recipe.Steps.Count == 0)
        {
            return new RecipeNutritionSummary(
                recipe.TotalCalories,
                recipe.TotalProteins,
                recipe.TotalFats,
                recipe.TotalCarbs,
                null);
        }

        var totalCalories = 0d;
        var totalProteins = 0d;
        var totalFats = 0d;
        var totalCarbs = 0d;
        var totalFiber = 0d;
        var hasComputedValues = false;

        foreach (var step in recipe.Steps)
        {
            foreach (var ingredient in step.Ingredients)
            {
                if (ingredient.Product is { } product && product.BaseAmount > 0)
                {
                    var factor = ingredient.Amount / product.BaseAmount;
                    totalCalories += product.CaloriesPerBase * factor;
                    totalProteins += product.ProteinsPerBase * factor;
                    totalFats += product.FatsPerBase * factor;
                    totalCarbs += product.CarbsPerBase * factor;
                    totalFiber += product.FiberPerBase * factor;
                    hasComputedValues = true;
                }
                else if (ingredient.NestedRecipe is { } nested && nested.Servings > 0)
                {
                    var factor = ingredient.Amount / nested.Servings;
                    totalCalories += (nested.TotalCalories ?? 0) * factor;
                    totalProteins += (nested.TotalProteins ?? 0) * factor;
                    totalFats += (nested.TotalFats ?? 0) * factor;
                    totalCarbs += (nested.TotalCarbs ?? 0) * factor;
                    hasComputedValues = true;
                }
            }
        }

        if (!hasComputedValues)
        {
            return new RecipeNutritionSummary(
                recipe.TotalCalories,
                recipe.TotalProteins,
                recipe.TotalFats,
                recipe.TotalCarbs,
                null);
        }

        return new RecipeNutritionSummary(
            Math.Round(totalCalories, 2),
            Math.Round(totalProteins, 2),
            Math.Round(totalFats, 2),
            Math.Round(totalCarbs, 2),
            Math.Round(totalFiber, 2));
    }

    private sealed record RecipeNutritionSummary(
        double? TotalCalories,
        double? TotalProteins,
        double? TotalFats,
        double? TotalCarbs,
        double? TotalFiber);
}
