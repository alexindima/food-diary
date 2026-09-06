using FoodDiary.Application.Abstractions.FavoriteMeals.Models;
using FoodDiary.Domain.Entities.FavoriteMeals;

namespace FoodDiary.Application.Favorites.FavoriteMeals.Mappings;

public static class FavoriteMealMappings {
    public static FavoriteMealModel ToModel(this FavoriteMeal favorite, FavoriteMealSourceModel source) =>
        new(
            favorite.Id.Value,
            favorite.MealId.Value,
            favorite.Name,
            favorite.CreatedAtUtc,
            source.Date,
            source.MealType,
            source.TotalCalories,
            source.TotalProteins,
            source.TotalFats,
            source.TotalCarbs,
            source.ItemCount);

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
