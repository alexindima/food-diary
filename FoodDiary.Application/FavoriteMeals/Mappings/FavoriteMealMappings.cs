using FoodDiary.Application.FavoriteMeals.Models;
using FoodDiary.Domain.Entities.FavoriteMeals;
using FoodDiary.Domain.Entities.Meals;

namespace FoodDiary.Application.FavoriteMeals.Mappings;

public static class FavoriteMealMappings {
    public static FavoriteMealModel ToModel(this FavoriteMeal favorite) =>
        favorite.ToModel(favorite.Meal);

    public static FavoriteMealModel ToModel(this FavoriteMeal favorite, Meal meal) =>
        new(
            favorite.Id.Value,
            favorite.MealId.Value,
            favorite.Name,
            favorite.CreatedAtUtc,
            meal.Date,
            meal.MealType?.ToString(),
            meal.TotalCalories,
            meal.TotalProteins,
            meal.TotalFats,
            meal.TotalCarbs,
            meal.Items.Count);
}
