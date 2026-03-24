using FoodDiary.Application.Common.Models;

namespace FoodDiary.Application.Recipes.Models;

public sealed record RecipeListWithRecentModel(
    IReadOnlyList<RecipeModel> RecentItems,
    PagedResponse<RecipeModel> AllRecipes);
