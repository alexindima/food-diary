using FoodDiary.Application.Recipes.Queries.GetRecentRecipes;
using FoodDiary.Application.Recipes.Queries.GetRecipes;
using FoodDiary.Application.Recipes.Queries.GetRecipesWithRecent;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Presentation.Api.Features.Recipes.Requests;

namespace FoodDiary.Presentation.Api.Features.Recipes.Mappings;

public static class RecipeHttpQueryMappings {
    public static GetRecipesQuery ToQuery(this GetRecipesHttpQuery query, UserId userId) {
        return new GetRecipesQuery(userId, query.Page, query.Limit, query.Search, query.IncludePublic);
    }

    public static GetRecipesWithRecentQuery ToQuery(this GetRecipesWithRecentHttpQuery query, UserId userId) {
        return new GetRecipesWithRecentQuery(
            userId,
            Math.Max(query.Page, 1),
            Math.Clamp(query.Limit, 1, 100),
            string.IsNullOrWhiteSpace(query.Search) ? null : query.Search.Trim(),
            query.IncludePublic,
            Math.Clamp(query.RecentLimit, 1, 50));
    }

    public static GetRecentRecipesQuery ToQuery(this GetRecentRecipesHttpQuery query, UserId userId) {
        return new GetRecentRecipesQuery(userId, Math.Clamp(query.Limit, 1, 50), query.IncludePublic);
    }
}
