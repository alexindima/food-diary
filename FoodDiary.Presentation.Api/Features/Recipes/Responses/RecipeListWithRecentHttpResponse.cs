using FoodDiary.Presentation.Api.Responses;

namespace FoodDiary.Presentation.Api.Features.Recipes.Responses;

public sealed record RecipeListWithRecentHttpResponse(
    IReadOnlyList<RecipeHttpResponse> RecentItems,
    PagedHttpResponse<RecipeHttpResponse> AllRecipes);
