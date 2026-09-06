using FoodDiary.Application.Abstractions.FavoriteRecipes.Models;
using FoodDiary.Domain.Entities.Recipes;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Tests;

[ExcludeFromCodeCoverage]
internal static class TestRecipeOverview {
    public static FavoriteRecipeSourceModel From(Recipe recipe, UserId currentUserId) =>
        new(recipe.Name, recipe.ImageUrl, recipe.TotalCalories, recipe.ManualCalories,
            recipe.Servings, recipe.PrepTime, recipe.CookTime, recipe.Steps.Sum(step => step.Ingredients.Count));
}
