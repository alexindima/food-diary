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
                step.Instruction,
                step.ImageUrl,
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
                        ingredient.NestedRecipeId?.Value,
                        ingredient.NestedRecipe?.Name))
                    .ToList()))
            .ToList();

        var nutrition = RecipeNutritionCalculator.Calculate(recipe);

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

}
