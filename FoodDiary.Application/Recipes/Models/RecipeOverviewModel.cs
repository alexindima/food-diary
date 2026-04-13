using FoodDiary.Application.FavoriteRecipes.Models;
using FoodDiary.Application.Common.Models;

namespace FoodDiary.Application.Recipes.Models;

public sealed record RecipeOverviewModel(
    IReadOnlyList<RecipeModel> RecentItems,
    PagedResponse<RecipeModel> AllRecipes,
    IReadOnlyList<FavoriteRecipeModel> FavoriteItems,
    int FavoriteTotalCount);
