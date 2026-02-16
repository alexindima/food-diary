using FoodDiary.Contracts.Common;

namespace FoodDiary.Contracts.Recipes;

public record RecipeListWithRecentResponse(
    IReadOnlyList<RecipeResponse> RecentItems,
    PagedResponse<RecipeResponse> AllRecipes);
