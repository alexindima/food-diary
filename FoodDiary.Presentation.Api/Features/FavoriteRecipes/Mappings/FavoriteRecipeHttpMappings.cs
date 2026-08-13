using FoodDiary.Application.FavoriteRecipes.Commands.AddFavoriteRecipe;
using FoodDiary.Application.FavoriteRecipes.Commands.RemoveFavoriteRecipe;
using FoodDiary.Application.Abstractions.FavoriteRecipes.Models;
using FoodDiary.Application.FavoriteRecipes.Queries.GetFavoriteRecipes;
using FoodDiary.Application.FavoriteRecipes.Queries.IsRecipeFavorite;
using FoodDiary.Presentation.Api.Features.FavoriteRecipes.Requests;
using FoodDiary.Presentation.Api.Features.FavoriteRecipes.Responses;

namespace FoodDiary.Presentation.Api.Features.FavoriteRecipes.Mappings;

public static class FavoriteRecipeHttpMappings {
    extension(AddFavoriteRecipeHttpRequest request) {
        public AddFavoriteRecipeCommand ToCommand(Guid userId) =>
                new(userId, request.RecipeId, request.Name);
    }

    extension(Guid id) {
        public RemoveFavoriteRecipeCommand ToDeleteCommand(Guid userId) =>
            new(userId, id);
        public GetFavoriteRecipesQuery ToQuery() =>
            new(id);
        public IsRecipeFavoriteQuery ToIsFavoriteQuery(Guid userId) =>
            new(userId, id);
    }

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
