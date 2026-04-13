using FoodDiary.Application.Recipes.Models;
using FoodDiary.Application.Recipes.Services;
using FoodDiary.Domain.Entities.Recipes;
using FoodDiary.Domain.ValueObjects;

namespace FoodDiary.Application.Recipes.Mappings;

public static class RecipeMappings {
    public static RecipeModel ToModel(
        this Recipe recipe,
        int usageCount,
        bool isOwnedByCurrentUser,
        bool isFavorite = false,
        Guid? favoriteRecipeId = null) {
        var steps = recipe.Steps
            .OrderBy(s => s.StepNumber)
            .Select(step => new RecipeStepModel(
                step.Id.Value,
                step.StepNumber,
                step.Title,
                step.Instruction,
                step.ImageUrl,
                step.ImageAssetId?.Value,
                step.Ingredients
                    .Select(ingredient => new RecipeIngredientModel(
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
        var quality = FoodQualityScore.Calculate(
            nutrition.TotalCalories ?? 0,
            nutrition.TotalProteins ?? 0,
            nutrition.TotalFats ?? 0,
            nutrition.TotalCarbs ?? 0,
            nutrition.TotalFiber ?? 0,
            nutrition.TotalAlcohol ?? 0);

        return new RecipeModel(
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
            quality.Score,
            quality.Grade.ToString().ToLowerInvariant(),
            steps,
            isFavorite,
            favoriteRecipeId);
    }

    private static RecipeNutritionSummary BuildNutrition(Recipe recipe) {
        if (!recipe.IsNutritionAutoCalculated) {
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
