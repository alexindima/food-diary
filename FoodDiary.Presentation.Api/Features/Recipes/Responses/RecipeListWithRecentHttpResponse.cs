using FoodDiary.Application.Common.Models;

namespace FoodDiary.Presentation.Api.Features.Recipes.Responses;

public sealed record RecipeListWithRecentHttpResponse(
    IReadOnlyList<RecipeHttpResponse> RecentItems,
    PagedResponse<RecipeHttpResponse> AllRecipes);
