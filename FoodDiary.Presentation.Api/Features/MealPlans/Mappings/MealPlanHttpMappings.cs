using FoodDiary.Application.MealPlans.Commands.AdoptMealPlan;
using FoodDiary.Application.MealPlans.Commands.GenerateShoppingList;
using FoodDiary.Application.MealPlans.Models;
using FoodDiary.Application.MealPlans.Queries.GetMealPlanById;
using FoodDiary.Application.MealPlans.Queries.GetMealPlans;
using FoodDiary.Presentation.Api.Features.MealPlans.Responses;

namespace FoodDiary.Presentation.Api.Features.MealPlans.Mappings;

public static class MealPlanHttpMappings {
    public static GetMealPlansQuery ToQuery(this Guid userId, string? dietType) =>
        new(userId, dietType);

    public static GetMealPlanByIdQuery ToGetByIdQuery(this Guid userId, Guid planId) =>
        new(userId, planId);

    public static AdoptMealPlanCommand ToAdoptCommand(this Guid userId, Guid planId) =>
        new(userId, planId);

    public static GenerateShoppingListCommand ToGenerateShoppingListCommand(this Guid userId, Guid planId) =>
        new(userId, planId);

    public static IReadOnlyList<MealPlanSummaryHttpResponse> ToHttpResponse(
        this IReadOnlyList<MealPlanSummaryModel> models) =>
        models.Select(m => m.ToHttpResponse()).ToList();

    public static MealPlanSummaryHttpResponse ToHttpResponse(this MealPlanSummaryModel model) =>
        new(model.Id, model.Name, model.Description, model.DietType,
            model.DurationDays, model.TargetCaloriesPerDay, model.IsCurated, model.TotalRecipes);

    public static MealPlanHttpResponse ToHttpResponse(this MealPlanModel model) =>
        new(model.Id, model.Name, model.Description, model.DietType,
            model.DurationDays, model.TargetCaloriesPerDay, model.IsCurated,
            model.Days.Select(d => d.ToHttpResponse()).ToList());

    private static MealPlanDayHttpResponse ToHttpResponse(this MealPlanDayModel day) =>
        new(day.Id, day.DayNumber,
            day.Meals.Select(m => m.ToHttpResponse()).ToList());

    private static MealPlanMealHttpResponse ToHttpResponse(this MealPlanMealModel meal) =>
        new(meal.Id, meal.MealType, meal.RecipeId, meal.RecipeName,
            meal.Servings, meal.Calories, meal.Proteins, meal.Fats, meal.Carbs);
}
