using FoodDiary.Modules.Favorites.Contracts.FavoriteRecipes.Models;
using FoodDiary.Modules.Recipes.Domain.Entities;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Modules.Favorites.Application.Tests;

[ExcludeFromCodeCoverage]
internal static class TestRecipeOverview {
    public static FavoriteRecipeSourceModel From(Recipe recipe, UserId currentUserId) =>
        new(recipe.Name, recipe.ImageUrl, recipe.TotalCalories, recipe.ManualCalories,
            recipe.Servings, recipe.PrepTime, recipe.CookTime, recipe.Steps.Sum(step => step.Ingredients.Count));
}
