using FoodDiary.Application.Abstractions.MealPlans.Models;
using FoodDiary.Application.MealPlans.Models;
using FoodDiary.Domain.Entities.MealPlans;
using FoodDiary.Domain.Entities.Recipes;

namespace FoodDiary.Application.MealPlans.Mappings;

public static class MealPlanMappings {
    extension(MealPlan plan) {
        public MealPlanModel ToModel() {
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
        public MealPlanSummaryModel ToSummaryModel() {
            int totalRecipes = plan.Days
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
    }

    extension(MealPlanReadModel plan) {
        public MealPlanModel ToModel() {
            return new MealPlanModel(
                plan.Id,
                plan.Name,
                plan.Description,
                plan.DietType,
                plan.DurationDays,
                plan.TargetCaloriesPerDay,
                plan.IsCurated,
                plan.Days
                    .OrderBy(d => d.DayNumber)
                    .Select(d => d.ToModel())
                    .ToList());
        }
    }

    extension(MealPlanSummaryReadModel plan) {
        public MealPlanSummaryModel ToSummaryModel() {
            return new MealPlanSummaryModel(
                plan.Id,
                plan.Name,
                plan.Description,
                plan.DietType,
                plan.DurationDays,
                plan.TargetCaloriesPerDay,
                plan.IsCurated,
                plan.TotalRecipes);
        }
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
        Recipe? recipe = meal.Recipe;
        int servings = recipe?.Servings > 0 ? recipe.Servings : 1;
        double multiplier = (double)meal.Servings / servings;

        return new MealPlanMealModel(
            meal.Id.Value,
            meal.MealType.ToString(),
            meal.RecipeId.Value,
            recipe?.Name,
            meal.Servings,
            recipe?.TotalCalories.HasValue == true ? Math.Round(recipe.TotalCalories!.Value * multiplier, 1, MidpointRounding.ToEven) : null,
            recipe?.TotalProteins.HasValue == true ? Math.Round(recipe.TotalProteins!.Value * multiplier, 1, MidpointRounding.ToEven) : null,
            recipe?.TotalFats.HasValue == true ? Math.Round(recipe.TotalFats!.Value * multiplier, 1, MidpointRounding.ToEven) : null,
            recipe?.TotalCarbs.HasValue == true ? Math.Round(recipe.TotalCarbs!.Value * multiplier, 1, MidpointRounding.ToEven) : null);
    }

    private static MealPlanDayModel ToModel(this MealPlanDayReadModel day) {
        return new MealPlanDayModel(
            day.Id,
            day.DayNumber,
            day.Meals
                .OrderBy(m => m.MealType, StringComparer.Ordinal)
                .Select(m => m.ToModel())
                .ToList());
    }

    private static MealPlanMealModel ToModel(this MealPlanMealReadModel meal) {
        int servings = meal.RecipeServings > 0 ? meal.RecipeServings : 1;
        double multiplier = (double)meal.Servings / servings;

        return new MealPlanMealModel(
            meal.Id,
            meal.MealType,
            meal.RecipeId,
            meal.RecipeName,
            meal.Servings,
            meal.RecipeTotalCalories.HasValue ? Math.Round(meal.RecipeTotalCalories.Value * multiplier, 1, MidpointRounding.ToEven) : null,
            meal.RecipeTotalProteins.HasValue ? Math.Round(meal.RecipeTotalProteins.Value * multiplier, 1, MidpointRounding.ToEven) : null,
            meal.RecipeTotalFats.HasValue ? Math.Round(meal.RecipeTotalFats.Value * multiplier, 1, MidpointRounding.ToEven) : null,
            meal.RecipeTotalCarbs.HasValue ? Math.Round(meal.RecipeTotalCarbs.Value * multiplier, 1, MidpointRounding.ToEven) : null);
    }
}
