using FoodDiary.Application.Recipes.Queries.ExploreRecipes;
using FoodDiary.Application.Recipes.Queries.GetRecentRecipes;
using FoodDiary.Application.Recipes.Queries.GetRecipeById;
using FoodDiary.Application.Recipes.Queries.GetRecipes;
using FoodDiary.Application.Recipes.Queries.GetRecipesOverview;
using FoodDiary.Presentation.Api.Features.Recipes.Requests;

namespace FoodDiary.Presentation.Api.Features.Recipes.Mappings;

public static class RecipeHttpQueryMappings {
    public static GetRecipesQuery ToQuery(this GetRecipesHttpQuery query, Guid userId) {
        return new GetRecipesQuery(
            userId,
            Math.Max(query.Page, 1),
            Math.Clamp(query.Limit, 1, 100),
            SanitizeText(query.Search),
            query.IncludePublic,
            SanitizeText(query.Category),
            NormalizePositive(query.MaxTotalTime),
            NormalizeNonNegative(query.CaloriesFrom),
            NormalizeNonNegative(query.CaloriesTo),
            query.HasImage);
    }

    public static GetRecipesOverviewQuery ToQuery(this GetRecipesOverviewHttpQuery query, Guid userId) {
        return new GetRecipesOverviewQuery(
            userId,
            Math.Max(query.Page, 1),
            Math.Clamp(query.Limit, 1, 100),
            SanitizeText(query.Search),
            query.IncludePublic,
            Math.Clamp(query.RecentLimit, 1, 50),
            Math.Clamp(query.FavoriteLimit, 1, 50),
            SanitizeText(query.Category),
            NormalizePositive(query.MaxTotalTime),
            NormalizeNonNegative(query.CaloriesFrom),
            NormalizeNonNegative(query.CaloriesTo),
            query.HasImage);
    }

    public static GetRecentRecipesQuery ToQuery(this GetRecentRecipesHttpQuery query, Guid userId) {
        return new GetRecentRecipesQuery(userId, Math.Clamp(query.Limit, 1, 50), query.IncludePublic);
    }

    public static GetRecipeByIdQuery ToQuery(this Guid id, Guid userId, bool includePublic) {
        return new GetRecipeByIdQuery(userId, id, includePublic);
    }

    public static ExploreRecipesQuery ToExploreQuery(this ExploreRecipesHttpQuery query, Guid userId) {
        return new ExploreRecipesQuery(userId, query.Page, query.Limit, query.Search,
            query.Category, query.MaxPrepTime, query.SortBy);
    }

    private static string? SanitizeText(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static int? NormalizePositive(int? value) =>
        value is > 0 ? value : null;

    private static double? NormalizeNonNegative(double? value) =>
        value is >= 0 ? value : null;
}
