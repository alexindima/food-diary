using FoodDiary.Application.Abstractions.FavoriteMeals.Models;
using FoodDiary.Application.FavoriteMeals.Models;
using FoodDiary.Application.Consumptions.Models;
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

    public static FavoriteMealModel ToModel(this FavoriteMeal favorite, ConsumptionModel consumption) =>
        new(
            favorite.Id.Value,
            favorite.MealId.Value,
            favorite.Name,
            favorite.CreatedAtUtc,
            consumption.Date,
            consumption.MealType,
            consumption.TotalCalories,
            consumption.TotalProteins,
            consumption.TotalFats,
            consumption.TotalCarbs,
            consumption.Items.Count);

    public static FavoriteMealModel ToModel(this FavoriteMealReadModel favorite) =>
        new(
            favorite.Id,
            favorite.MealId,
            favorite.Name,
            favorite.CreatedAtUtc,
            favorite.MealDate,
            favorite.MealType,
            favorite.TotalCalories,
            favorite.TotalProteins,
            favorite.TotalFats,
            favorite.TotalCarbs,
            favorite.ItemCount);
}
