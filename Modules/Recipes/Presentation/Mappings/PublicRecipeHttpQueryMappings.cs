using FoodDiary.Modules.Recipes.Application.Queries.GetPublicRecipe;
using FoodDiary.Modules.Recipes.Application.Queries.GetPublicRecipeCategories;
using FoodDiary.Modules.Recipes.Application.Queries.GetPublicRecipes;
using FoodDiary.Modules.Recipes.Presentation.Requests;

namespace FoodDiary.Modules.Recipes.Presentation.Mappings;

public static class PublicRecipeHttpQueryMappings {
    public static GetPublicRecipesQuery ToQuery(this PublicRecipesHttpQuery query) =>
        new(query.Page, query.Limit, query.Search, query.Category, query.MaxTotalTime, query.SortBy, query.Language);

    public static GetPublicRecipeCategoriesQuery ToPublicCategoriesQuery(this string? search, string? language) =>
        new(search, language);

    public static GetPublicRecipeQuery ToPublicRecipeQuery(this Guid id) => new(id);
}
