using FoodDiary.Application.Abstractions.Recipes.Models;
using FoodDiary.Application.Recipes.Models;

namespace FoodDiary.Application.Recipes.Mappings;

public static class RecipeOverviewReadMappings {
    public static RecipeModel ToModel(
        this RecipeOverviewReadItem recipe,
        bool isFavorite = false,
        Guid? favoriteRecipeId = null) =>
        new(
            recipe.Id.Value,
            recipe.Name,
            recipe.Description,
            recipe.Comment,
            recipe.Category,
            recipe.ImageUrl,
            recipe.ImageAssetId?.Value,
            recipe.PrepTime,
            recipe.CookTime,
            recipe.Servings,
            recipe.TotalCalories,
            recipe.TotalProteins,
            recipe.TotalFats,
            recipe.TotalCarbs,
            recipe.TotalFiber,
            recipe.TotalAlcohol,
            recipe.IsNutritionAutoCalculated,
            recipe.ManualCalories,
            recipe.ManualProteins,
            recipe.ManualFats,
            recipe.ManualCarbs,
            recipe.ManualFiber,
            recipe.ManualAlcohol,
            recipe.Visibility.ToString(),
            recipe.UsageCount,
            recipe.CreatedOnUtc,
            recipe.IsOwnedByCurrentUser,
            recipe.QualityScore,
            recipe.QualityGrade,
            [.. recipe.Steps.Select(ToStepModel)],
            isFavorite,
            favoriteRecipeId);

    private static RecipeStepModel ToStepModel(RecipeOverviewStepReadItem step) =>
        new(
            step.Id,
            step.StepNumber,
            step.Title,
            step.Instruction,
            step.ImageUrl,
            step.ImageAssetId,
            [.. step.Ingredients.Select(ToIngredientModel)]);

    private static RecipeIngredientModel ToIngredientModel(RecipeOverviewIngredientReadItem ingredient) =>
        new(
            ingredient.Id,
            ingredient.Amount,
            ingredient.ProductId,
            ingredient.ProductName,
            ingredient.ProductBaseUnit,
            ingredient.ProductBaseAmount,
            ingredient.ProductCaloriesPerBase,
            ingredient.ProductProteinsPerBase,
            ingredient.ProductFatsPerBase,
            ingredient.ProductCarbsPerBase,
            ingredient.ProductFiberPerBase,
            ingredient.ProductAlcoholPerBase,
            ingredient.NestedRecipeId,
            ingredient.NestedRecipeName,
            ingredient.NestedRecipeServings,
            ingredient.NestedRecipeTotalCalories,
            ingredient.NestedRecipeTotalProteins,
            ingredient.NestedRecipeTotalFats,
            ingredient.NestedRecipeTotalCarbs,
            ingredient.NestedRecipeTotalFiber,
            ingredient.NestedRecipeTotalAlcohol);
}
