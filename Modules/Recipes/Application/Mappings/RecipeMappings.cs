using FoodDiary.Modules.Recipes.Domain.Contracts.Enums;
using FoodDiary.Modules.Recipes.Domain.Nutrition;
using FoodDiary.Modules.Products.FoodQuality.ValueObjects;
using FoodDiary.Modules.Recipes.Application.Models;
using FoodDiary.Modules.Recipes.Application.Services;
using FoodDiary.Modules.Recipes.Domain.Entities;
using FoodDiary.Domain.Primitives;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;

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
            .Select(step => ToStepModel(step, recipe.UserId, isOwnedByCurrentUser))
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
            recipe.Category.ToCode(),
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
            favoriteRecipeId) { LanguageConfirmed = recipe.LanguageConfirmed, Language = recipe.Language, MissingIngredientCount = recipe.IsNutritionAutoCalculated ? recipe.MissingIngredientCount : 0, Images = recipe.Images.OrderBy(image => image.Position).Select(image => new RecipeImageModel(image.ImageAssetId.Value, image.ImageUrl)).ToList() };
    }

    private static RecipeStepModel ToStepModel(RecipeStep step, UserId ownerId, bool isOwnedByCurrentUser) {
        return new RecipeStepModel(
            step.Id.Value,
            step.StepNumber,
            step.Title,
            step.Instruction,
            step.ImageUrl,
            step.ImageAssetId?.Value,
            step.Ingredients.Select(ingredient => ToIngredientModel(ingredient, ownerId, isOwnedByCurrentUser)).ToList()) { Images = step.Images.OrderBy(image => image.Position).Select(image => new RecipeImageModel(image.ImageAssetId.Value, image.ImageUrl)).ToList() };
    }

    private static RecipeIngredientModel ToIngredientModel(RecipeIngredient ingredient, UserId ownerId, bool isOwnedByCurrentUser) {
        RecipeIngredientProductSnapshot? product = ingredient.ProductSnapshot;
        if (product is not null && product.Visibility != Visibility.Public &&
            (!isOwnedByCurrentUser || product.OwnerUserId != ownerId.Value)) {
            product = null;
        }

        Recipe? nestedRecipe = ingredient.NestedRecipe;
        if (nestedRecipe is not null && nestedRecipe.Visibility != Visibility.Public &&
            (!isOwnedByCurrentUser || nestedRecipe.UserId != ownerId)) {
            nestedRecipe = null;
        }

        return new RecipeIngredientModel(
            ingredient.Id.Value,
            ingredient.Amount,
            product?.Id.Value,
            product?.Name,
            product?.BaseUnit.ToString(),
            product?.BaseAmount,
            product?.CaloriesPerBase,
            product?.ProteinsPerBase,
            product?.FatsPerBase,
            product?.CarbsPerBase,
            product?.FiberPerBase,
            product?.AlcoholPerBase,
            nestedRecipe?.Id.Value,
            nestedRecipe?.Name,
            nestedRecipe?.Servings,
            nestedRecipe?.TotalCalories,
            nestedRecipe?.TotalProteins,
            nestedRecipe?.TotalFats,
            nestedRecipe?.TotalCarbs,
            nestedRecipe?.TotalFiber,
            nestedRecipe?.TotalAlcohol) { TextName = ingredient.TextName, AmountText = ingredient.AmountText, NestedRecipeMissingIngredientCount = nestedRecipe?.MissingIngredientCount ?? 0 };
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
