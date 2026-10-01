using FoodDiary.Modules.MealPlanning.Application.MealPlans.Commands.SaveCatalogMealPlan;
using FoodDiary.Modules.MealPlanning.Application.MealPlans.Queries.GetCatalogMealPlan;
using FoodDiary.Modules.MealPlanning.Application.MealPlans.Queries.GetCatalogMealPlans;
using FoodDiary.Modules.MealPlanning.Application.MealPlans.Queries.SearchCatalogRecipes;
using FoodDiary.Modules.MealPlanning.Presentation.MealPlans.Requests;
using FoodDiary.Presentation.Api.Requests;

namespace FoodDiary.Modules.MealPlanning.Presentation.MealPlans.Mappings;

public static class CatalogMealPlanHttpMappings {
    extension(OffsetPaginationHttpQuery query) {
        public GetCatalogMealPlansQuery ToCatalogQuery() => new(query.Page, query.Limit);
    }

    extension(Guid id) {
        public GetCatalogMealPlanQuery ToCatalogMealPlanQuery() => new(id);
    }

    extension(string? search) {
        public SearchCatalogRecipesQuery ToCatalogRecipeSearchQuery(int limit) => new(search, limit);
    }

    extension(SaveCatalogMealPlanHttpRequest request) {
        public SaveCatalogMealPlanCommand ToCatalogCommand(Guid? id) =>
            new(id, request.Name, request.Description, request.DietType, request.DurationDays,
                request.TargetCaloriesPerDay, request.IsPublished,
                [.. request.Days.Select(day => day is null
                    ? new CatalogDayInput(DayNumber: 0, [])
                    : new CatalogDayInput(day.DayNumber,
                        [.. day.Meals.Select(meal => meal is null
                            ? new CatalogMealInput(string.Empty, Guid.Empty, Servings: 0)
                            : new CatalogMealInput(meal.MealType, meal.RecipeId, meal.Servings))]))]);
    }
}
