using FoodDiary.Application.MealPlans.Models;
using FoodDiary.Domain.Entities.MealPlans;

namespace FoodDiary.Application.MealPlans.Mappings;

public static class MealPlanMappings {
    public static MealPlanModel ToModel(this MealPlan plan) {
        return new MealPlanModel(
            plan.Id.Value,
            plan.Name,
            plan.Description,
            plan.DietType.ToString(),
            plan.DurationDays,
            plan.TargetCaloriesPerDay,
            plan.IsCurated,
            plan.Days
                .OrderBy(d => d.DayNumber)
                .Select(d => d.ToModel())
                .ToList());
    }

    public static MealPlanSummaryModel ToSummaryModel(this MealPlan plan) {
        var totalRecipes = plan.Days
            .SelectMany(d => d.Meals)
            .Select(m => m.RecipeId)
            .Distinct()
            .Count();

        return new MealPlanSummaryModel(
            plan.Id.Value,
            plan.Name,
            plan.Description,
            plan.DietType.ToString(),
            plan.DurationDays,
            plan.TargetCaloriesPerDay,
            plan.IsCurated,
            totalRecipes);
    }

    private static MealPlanDayModel ToModel(this MealPlanDay day) {
        return new MealPlanDayModel(
            day.Id.Value,
            day.DayNumber,
            day.Meals
                .OrderBy(m => m.MealType)
                .Select(m => m.ToModel())
                .ToList());
    }

    private static MealPlanMealModel ToModel(this MealPlanMeal meal) {
        var recipe = meal.Recipe;
        var servings = recipe?.Servings > 0 ? recipe.Servings : 1;
        var multiplier = (double)meal.Servings / servings;

        return new MealPlanMealModel(
            meal.Id.Value,
            meal.MealType.ToString(),
            meal.RecipeId.Value,
            recipe?.Name,
            meal.Servings,
            recipe?.TotalCalories.HasValue == true ? Math.Round(recipe.TotalCalories!.Value * multiplier, 1) : null,
            recipe?.TotalProteins.HasValue == true ? Math.Round(recipe.TotalProteins!.Value * multiplier, 1) : null,
            recipe?.TotalFats.HasValue == true ? Math.Round(recipe.TotalFats!.Value * multiplier, 1) : null,
            recipe?.TotalCarbs.HasValue == true ? Math.Round(recipe.TotalCarbs!.Value * multiplier, 1) : null);
    }
}
