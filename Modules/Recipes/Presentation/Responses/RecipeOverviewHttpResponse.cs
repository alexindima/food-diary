using FoodDiary.Modules.Favorites.Presentation.Contracts.Features.FavoriteRecipes.Responses;
using FoodDiary.Presentation.Api.Responses;

namespace FoodDiary.Modules.Recipes.Presentation.Responses;

public sealed record RecipeOverviewHttpResponse(
    IReadOnlyList<RecipeHttpResponse> RecentItems,
    PagedHttpResponse<RecipeHttpResponse> AllRecipes,
    IReadOnlyList<FavoriteRecipeHttpResponse> FavoriteItems,
    int FavoriteTotalCount);
