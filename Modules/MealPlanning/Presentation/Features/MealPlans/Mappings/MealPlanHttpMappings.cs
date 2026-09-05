using FoodDiary.Application.MealPlanning.MealPlans.Commands.AdoptMealPlan;
using FoodDiary.Application.MealPlanning.MealPlans.Commands.GenerateShoppingList;
using FoodDiary.Application.MealPlanning.MealPlans.Models;
using FoodDiary.Application.MealPlanning.MealPlans.Queries.GetMealPlanById;
using FoodDiary.Application.MealPlanning.MealPlans.Queries.GetMealPlans;
using FoodDiary.Presentation.Api.Features.MealPlans.Responses;

namespace FoodDiary.Presentation.Api.Features.MealPlans.Mappings;

public static class MealPlanHttpMappings {
    extension(Guid userId) {
        public GetMealPlansQuery ToQuery(string? dietType) =>
            new(userId, dietType);
        public GetMealPlanByIdQuery ToGetByIdQuery(Guid planId) =>
            new(userId, planId);
        public AdoptMealPlanCommand ToAdoptCommand(Guid planId) =>
            new(userId, planId);
        public GenerateShoppingListCommand ToGenerateShoppingListCommand(Guid planId) =>
            new(userId, planId);
    }

    extension(IReadOnlyList<MealPlanSummaryModel> models) {
        public IReadOnlyList<MealPlanSummaryHttpResponse> ToHttpResponse(
        ) =>
                models.Select(m => m.ToHttpResponse()).ToList();
    }

    extension(MealPlanSummaryModel model) {
        private MealPlanSummaryHttpResponse ToHttpResponse() =>
                new(model.Id, model.Name, model.Description, model.DietType,
                    model.DurationDays, model.TargetCaloriesPerDay, model.IsCurated, model.TotalRecipes);
    }

    extension(MealPlanModel model) {
        public MealPlanHttpResponse ToHttpResponse() =>
                new(model.Id, model.Name, model.Description, model.DietType,
                    model.DurationDays, model.TargetCaloriesPerDay, model.IsCurated,
                    model.Days.Select(d => d.ToHttpResponse()).ToList());
    }

    extension(MealPlanDayModel day) {
        private MealPlanDayHttpResponse ToHttpResponse() =>
                new(day.Id, day.DayNumber,
                    day.Meals.Select(m => m.ToHttpResponse()).ToList());
    }

    extension(MealPlanMealModel meal) {
        private MealPlanMealHttpResponse ToHttpResponse() =>
                new(meal.Id, meal.MealType, meal.RecipeId, meal.RecipeName,
                    meal.Servings, meal.Calories, meal.Proteins, meal.Fats, meal.Carbs);
    }
}
