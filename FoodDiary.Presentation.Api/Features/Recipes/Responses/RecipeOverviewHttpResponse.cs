using FoodDiary.Presentation.Api.Features.FavoriteRecipes.Responses;
using FoodDiary.Presentation.Api.Responses;

namespace FoodDiary.Presentation.Api.Features.Recipes.Responses;

public sealed record RecipeOverviewHttpResponse(
    IReadOnlyList<RecipeHttpResponse> RecentItems,
    PagedHttpResponse<RecipeHttpResponse> AllRecipes,
    IReadOnlyList<FavoriteRecipeHttpResponse> FavoriteItems,
    int FavoriteTotalCount);
