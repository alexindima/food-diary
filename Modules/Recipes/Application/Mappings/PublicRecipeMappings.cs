using FoodDiary.Modules.Recipes.Application.Models;
using FoodDiary.Modules.Recipes.Contracts.Models;

namespace FoodDiary.Modules.Recipes.Application.Mappings;

public static class PublicRecipeMappings {
    public static PublicRecipeModel ToPublicModel(this RecipeOverviewReadItem recipe) => new(
        recipe.Id.Value, recipe.Name, recipe.Description, recipe.Category, recipe.ImageUrl,
        recipe.Images.Select(image => image.ImageUrl).ToArray(), recipe.PrepTime, recipe.CookTime, recipe.Servings,
        recipe.TotalCalories, recipe.TotalProteins, recipe.TotalFats, recipe.TotalCarbs, recipe.TotalFiber,
        recipe.TotalAlcohol, recipe.MissingIngredientCount,
        recipe.Steps.Select(step => new PublicRecipeStepModel(step.StepNumber, step.Title, step.Instruction,
            GetStepImages(step),
            step.Ingredients.Select(ToPublicIngredient).ToArray())).ToArray()) { Language = recipe.Language };

    private static IReadOnlyList<string> GetStepImages(RecipeOverviewStepReadItem step) {
        if (step.Images.Count > 0) {
            return step.Images.Select(image => image.ImageUrl).ToArray();
        }
        return step.ImageUrl is { Length: > 0 } url ? [url] : [];
    }

    private static string? GetUnit(RecipeOverviewIngredientReadItem ingredient) =>
        ingredient.NestedRecipeId.HasValue ? "serving" : ingredient.ProductBaseUnit;

    private static PublicRecipeIngredientModel ToPublicIngredient(RecipeOverviewIngredientReadItem ingredient) {
        bool accessible = ingredient.ProductIsAccessible && ingredient.NestedRecipeIsAccessible;
        return new PublicRecipeIngredientModel(
            accessible ? ingredient.TextName ?? ingredient.ProductName ?? ingredient.NestedRecipeName : null,
            accessible && ingredient.TextName is null ? ingredient.Amount : null,
            accessible ? GetUnit(ingredient) : null,
            accessible ? ingredient.AmountText : null,
            accessible ? ingredient.NestedRecipeId : null, accessible);
    }
}
