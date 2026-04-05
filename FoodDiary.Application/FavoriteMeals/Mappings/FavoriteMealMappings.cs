using FoodDiary.Application.FavoriteMeals.Models;
using FoodDiary.Domain.Entities.FavoriteMeals;

namespace FoodDiary.Application.FavoriteMeals.Mappings;

public static class FavoriteMealMappings {
    public static FavoriteMealModel ToModel(this FavoriteMeal favorite) =>
        new(
            favorite.Id.Value,
            favorite.MealId.Value,
            favorite.Name,
            favorite.CreatedAtUtc,
            favorite.Meal.Date,
            favorite.Meal.MealType?.ToString(),
            favorite.Meal.TotalCalories,
            favorite.Meal.TotalProteins,
            favorite.Meal.TotalFats,
            favorite.Meal.TotalCarbs,
            favorite.Meal.Items.Count);
}
