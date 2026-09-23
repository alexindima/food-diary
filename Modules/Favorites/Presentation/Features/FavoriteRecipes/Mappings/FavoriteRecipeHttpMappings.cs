using FoodDiary.Modules.Favorites.Application.FavoriteRecipes.Queries.GetFavoriteRecipePage;
using FoodDiary.Modules.Favorites.Application.FavoriteRecipes.Commands.AddFavoriteRecipe;
using FoodDiary.Modules.Favorites.Application.FavoriteRecipes.Commands.RemoveFavoriteRecipe;
using FoodDiary.Modules.Favorites.Application.FavoriteRecipes.Queries.GetFavoriteRecipes;
using FoodDiary.Modules.Favorites.Application.FavoriteRecipes.Queries.IsRecipeFavorite;
using FoodDiary.Modules.Favorites.Presentation.Features.FavoriteRecipes.Requests;

namespace FoodDiary.Modules.Favorites.Presentation.Features.FavoriteRecipes.Mappings;

public static class FavoriteRecipeHttpMappings {
    extension(GetFavoriteRecipePageHttpQuery query) {
        public GetFavoriteRecipePageQuery ToQuery(Guid userId) => new(userId, query.Page, query.Limit, query.Search);
    }

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
