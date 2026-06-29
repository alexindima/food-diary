using FoodDiary.Application.FavoriteRecipes.Models;
using FoodDiary.Domain.Entities.FavoriteRecipes;
using FoodDiary.Domain.Entities.Recipes;

namespace FoodDiary.Application.FavoriteRecipes.Mappings;

public static class FavoriteRecipeMappings {
    public static FavoriteRecipeModel ToModel(this FavoriteRecipe favorite) {
        return favorite.ToModel(favorite.Recipe);
    }

    public static FavoriteRecipeModel ToModel(this FavoriteRecipe favorite, Recipe recipe) {
        int totalTime = (recipe.PrepTime ?? 0) + (recipe.CookTime ?? 0);

        return new FavoriteRecipeModel(
            favorite.Id.Value,
            favorite.RecipeId.Value,
            favorite.Name,
            favorite.CreatedAtUtc,
            recipe.Name,
            recipe.ImageUrl,
            recipe.TotalCalories ?? recipe.ManualCalories,
            recipe.Servings,
            totalTime > 0 ? totalTime : null,
            recipe.Steps.Sum(step => step.Ingredients.Count));
    }
}
