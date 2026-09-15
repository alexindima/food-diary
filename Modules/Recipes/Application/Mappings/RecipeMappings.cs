using FoodDiary.Modules.Recipes.Domain.Nutrition;
using FoodDiary.Modules.Products.FoodQuality.ValueObjects;
using FoodDiary.Modules.Recipes.Application.Models;
using FoodDiary.Modules.Recipes.Application.Services;
using FoodDiary.Modules.Recipes.Domain.Entities;

namespace FoodDiary.Modules.Recipes.Application.Mappings;

public static class RecipeMappings {
    public static RecipeModel ToModel(
        this Recipe recipe,
        int usageCount,
        bool isOwnedByCurrentUser,
        bool isFavorite = false,
        Guid? favoriteRecipeId = null) {
        var steps = recipe.Steps
            .OrderBy(s => s.StepNumber)
            .Select(ToStepModel)
            .ToList();

        RecipeNutritionSummary nutrition = BuildNutrition(recipe);
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

    private static RecipeStepModel ToStepModel(RecipeStep step) {
        return new RecipeStepModel(
            step.Id.Value,
            step.StepNumber,
            step.Title,
            step.Instruction,
            step.ImageUrl,
            step.ImageAssetId?.Value,
            step.Ingredients.Select(ToIngredientModel).ToList());
    }

    private static RecipeIngredientModel ToIngredientModel(RecipeIngredient ingredient) {
        return new RecipeIngredientModel(
            ingredient.Id.Value,
            ingredient.Amount,
            ingredient.ProductId?.Value,
            ingredient.ProductSnapshot?.Name,
            ingredient.ProductSnapshot?.BaseUnit.ToString(),
            ingredient.ProductSnapshot?.BaseAmount,
            ingredient.ProductSnapshot?.CaloriesPerBase,
            ingredient.ProductSnapshot?.ProteinsPerBase,
            ingredient.ProductSnapshot?.FatsPerBase,
            ingredient.ProductSnapshot?.CarbsPerBase,
            ingredient.ProductSnapshot?.FiberPerBase,
            ingredient.ProductSnapshot?.AlcoholPerBase,
            ingredient.NestedRecipeId?.Value,
            ingredient.NestedRecipe?.Name,
            ingredient.NestedRecipe?.Servings,
            ingredient.NestedRecipe?.TotalCalories,
            ingredient.NestedRecipe?.TotalProteins,
            ingredient.NestedRecipe?.TotalFats,
            ingredient.NestedRecipe?.TotalCarbs,
            ingredient.NestedRecipe?.TotalFiber,
            ingredient.NestedRecipe?.TotalAlcohol);
    }

    private static RecipeNutritionSummary BuildNutrition(Recipe recipe) {
        if (!recipe.IsNutritionAutoCalculated) {
            RecipeNutritionValues values = RecipeNutritionPolicy.SelectManual(
                new RecipeNutritionValues(recipe.ManualCalories, recipe.ManualProteins, recipe.ManualFats,
                    recipe.ManualCarbs, recipe.ManualFiber, recipe.ManualAlcohol),
                new RecipeNutritionValues(recipe.TotalCalories, recipe.TotalProteins, recipe.TotalFats,
                    recipe.TotalCarbs, recipe.TotalFiber, recipe.TotalAlcohol));
            return new RecipeNutritionSummary(values.TotalCalories, values.TotalProteins, values.TotalFats,
                values.TotalCarbs, values.TotalFiber, values.TotalAlcohol);
        }

        return RecipeNutritionCalculator.Calculate(recipe);
    }
}
