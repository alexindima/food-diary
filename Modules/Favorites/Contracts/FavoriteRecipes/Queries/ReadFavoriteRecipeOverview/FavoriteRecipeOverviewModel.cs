using FoodDiary.Modules.Favorites.Contracts.FavoriteRecipes.Models;

namespace FoodDiary.Modules.Favorites.Contracts.FavoriteRecipes.Queries.ReadFavoriteRecipeOverview;

public sealed record FavoriteRecipeOverviewModel(IReadOnlyList<FavoriteRecipeModel> Items,
    IReadOnlyList<FavoriteRecipeModel> Preview, int Total);
