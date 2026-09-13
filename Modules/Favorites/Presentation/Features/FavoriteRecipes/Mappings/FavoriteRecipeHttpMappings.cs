using FoodDiary.Application.Favorites.FavoriteRecipes.Commands.AddFavoriteRecipe;
using FoodDiary.Application.Favorites.FavoriteRecipes.Commands.RemoveFavoriteRecipe;
using FoodDiary.Application.Favorites.FavoriteRecipes.Queries.GetFavoriteRecipes;
using FoodDiary.Application.Favorites.FavoriteRecipes.Queries.IsRecipeFavorite;
using FoodDiary.Presentation.Api.Features.FavoriteRecipes.Requests;

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
}
