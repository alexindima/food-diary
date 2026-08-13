using FoodDiary.Application.Abstractions.Recipes.Models;
using FoodDiary.Application.Recipes.Mappings;
using FoodDiary.Application.Recipes.Models;
using FoodDiary.Domain.Entities.Recipes;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Tests;

[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
internal static class TestRecipeOverview {
    public static RecipeOverviewReadItem From(Recipe recipe, UserId currentUserId, int usageCount = 0) {
        RecipeModel model = recipe.ToModel(usageCount, recipe.UserId == currentUserId);
        return new(
            recipe.Id,
            recipe.UserId,
            model.Name,
            model.Description,
            model.Comment,
            model.Category,
            model.ImageUrl,
            recipe.ImageAssetId,
            model.PrepTime,
            model.CookTime,
            model.Servings,
            model.TotalCalories,
            model.TotalProteins,
            model.TotalFats,
            model.TotalCarbs,
            model.TotalFiber,
            model.TotalAlcohol,
            model.IsNutritionAutoCalculated,
            model.ManualCalories,
            model.ManualProteins,
            model.ManualFats,
            model.ManualCarbs,
            model.ManualFiber,
            model.ManualAlcohol,
            recipe.Visibility,
            model.UsageCount,
            model.CreatedAt,
            model.IsOwnedByCurrentUser,
            model.QualityScore,
            model.QualityGrade,
            [.. model.Steps.Select(step => new RecipeOverviewStepReadItem(
                step.Id,
                step.StepNumber,
                step.Title,
                step.Instruction,
                step.ImageUrl,
                step.ImageAssetId,
                [.. step.Ingredients.Select(ingredient => new RecipeOverviewIngredientReadItem(
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
                    ingredient.NestedRecipeTotalAlcohol))]))]);
    }
}
