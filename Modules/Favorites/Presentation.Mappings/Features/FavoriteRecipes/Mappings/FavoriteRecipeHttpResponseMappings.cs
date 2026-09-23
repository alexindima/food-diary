using FoodDiary.Modules.Favorites.Contracts.FavoriteRecipes.Models;
using FoodDiary.Modules.Favorites.Presentation.Contracts.Features.FavoriteRecipes.Responses;

namespace FoodDiary.Modules.Favorites.Presentation.Mappings.Features.FavoriteRecipes.Mappings;

public static class FavoriteRecipeHttpResponseMappings {
    extension(FavoriteRecipeModel model) {
        public FavoriteRecipeHttpResponse ToHttpResponse() =>
                new(
                    model.Id,
                    model.RecipeId,
                    model.Name,
                    model.CreatedAtUtc,
                    model.RecipeName,
                    model.ImageUrl,
                    model.TotalCalories,
                    model.Servings,
                    model.TotalTimeMinutes,
                    model.IngredientCount) {
                    TotalProteins = model.TotalProteins,
                    TotalFats = model.TotalFats,
                    TotalCarbs = model.TotalCarbs,
                    TotalFiber = model.TotalFiber,
                    IngredientNames = model.IngredientNames,
                };
    }
}
