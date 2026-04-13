using FoodDiary.Application.FavoriteRecipes.Models;
using FoodDiary.Domain.Entities.FavoriteRecipes;

namespace FoodDiary.Application.FavoriteRecipes.Mappings;

public static class FavoriteRecipeMappings {
    public static FavoriteRecipeModel ToModel(this FavoriteRecipe favorite) {
        var totalTime = (favorite.Recipe.PrepTime ?? 0) + (favorite.Recipe.CookTime ?? 0);

        return new FavoriteRecipeModel(
            favorite.Id.Value,
            favorite.RecipeId.Value,
            favorite.Name,
            favorite.CreatedAtUtc,
            favorite.Recipe.Name,
            favorite.Recipe.ImageUrl,
            favorite.Recipe.TotalCalories ?? favorite.Recipe.ManualCalories,
            favorite.Recipe.Servings,
            totalTime > 0 ? totalTime : null,
            favorite.Recipe.Steps.Sum(step => step.Ingredients.Count));
    }
}
