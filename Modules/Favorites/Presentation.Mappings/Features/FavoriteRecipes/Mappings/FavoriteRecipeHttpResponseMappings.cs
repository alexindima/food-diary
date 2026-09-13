using FoodDiary.Application.Abstractions.FavoriteRecipes.Models;
using FoodDiary.Presentation.Api.Features.FavoriteRecipes.Responses;

namespace FoodDiary.Presentation.Api.Features.FavoriteRecipes.Mappings;

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
                    model.IngredientCount);
    }
}
