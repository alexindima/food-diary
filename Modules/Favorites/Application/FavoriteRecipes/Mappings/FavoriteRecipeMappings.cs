using FoodDiary.Modules.Favorites.Application.Abstractions.FavoriteRecipes.Models;
using FoodDiary.Modules.Favorites.Contracts.FavoriteRecipes.Models;
using FoodDiary.Modules.Favorites.Domain.Entities.FavoriteRecipes;

namespace FoodDiary.Modules.Favorites.Application.FavoriteRecipes.Mappings;

public static class FavoriteRecipeMappings {
    public static FavoriteRecipeModel ToModel(this FavoriteRecipe favorite, FavoriteRecipeSourceModel recipe) {
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
            recipe.IngredientCount);
    }

    public static FavoriteRecipeModel ToModel(this FavoriteRecipeReadModel favorite) {
        int totalTime = (favorite.PrepTime ?? 0) + (favorite.CookTime ?? 0);

        return new FavoriteRecipeModel(
            favorite.Id,
            favorite.RecipeId,
            favorite.Name,
            favorite.CreatedAtUtc,
            favorite.RecipeName,
            favorite.ImageUrl,
            favorite.TotalCalories,
            favorite.Servings,
            totalTime > 0 ? totalTime : null,
            favorite.IngredientCount);
    }
}
