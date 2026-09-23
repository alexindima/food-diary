using FoodDiary.Modules.Recipes.Application.Queries.ExploreRecipes;
using FoodDiary.Modules.Recipes.Application.Queries.GetRecentRecipes;
using FoodDiary.Modules.Recipes.Application.Queries.GetRecipeById;
using FoodDiary.Modules.Recipes.Application.Queries.GetRecipes;
using FoodDiary.Modules.Recipes.Application.Queries.GetRecipesOverview;
using FoodDiary.Modules.Recipes.Presentation.Requests;

namespace FoodDiary.Modules.Recipes.Presentation.Mappings;

public static class RecipeHttpQueryMappings {
    extension(GetRecipesHttpQuery query) {
        public GetRecipesQuery ToQuery(Guid userId) {
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
    }

    extension(GetRecipesOverviewHttpQuery query) {
        public GetRecipesOverviewQuery ToQuery(Guid userId) {
            return new GetRecipesOverviewQuery(
                userId,
                Math.Max(query.Page, 1),
                Math.Clamp(query.Limit, 1, 100),
                SanitizeText(query.Search),
                query.IncludePublic,
                Math.Clamp(query.RecentLimit, 1, 50),
                Math.Clamp(query.FavoriteLimit, 0, 50),
                SanitizeText(query.Category),
                NormalizePositive(query.MaxTotalTime),
                NormalizeNonNegative(query.CaloriesFrom),
                NormalizeNonNegative(query.CaloriesTo),
                query.HasImage);
        }
    }

    extension(GetRecentRecipesHttpQuery query) {
        public GetRecentRecipesQuery ToQuery(Guid userId) {
            return new GetRecentRecipesQuery(userId, Math.Clamp(query.Limit, 1, 50), query.IncludePublic);
        }
    }

    extension(Guid id) {
        public GetRecipeByIdQuery ToQuery(Guid userId, bool includePublic) {
            return new GetRecipeByIdQuery(userId, id, includePublic);
        }
    }

    extension(ExploreRecipesHttpQuery query) {
        public ExploreRecipesQuery ToExploreQuery(Guid userId) {
            return new ExploreRecipesQuery(userId, query.Page, query.Limit, query.Search,
                query.Category, query.MaxPrepTime, query.SortBy);
        }
    }

    private static string? SanitizeText(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static int? NormalizePositive(int? value) =>
        value is > 0 ? value : null;

    private static double? NormalizeNonNegative(double? value) =>
        value is >= 0 ? value : null;
}
