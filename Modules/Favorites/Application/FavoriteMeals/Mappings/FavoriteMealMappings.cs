using FoodDiary.Modules.Favorites.Application.Abstractions.FavoriteMeals.Models;
using FoodDiary.Modules.Favorites.Contracts.FavoriteMeals.Models;
using FoodDiary.Modules.Favorites.Domain.Entities.FavoriteMeals;

namespace FoodDiary.Modules.Favorites.Application.FavoriteMeals.Mappings;

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
            favorite.ItemCount) { ItemImageUrls = favorite.ItemImageUrls.Concat(favorite.AiImageUrls).Distinct(StringComparer.Ordinal).Take(4).ToArray(), ImageUrl = favorite.ImageUrl, TotalFiber = favorite.TotalFiber, ItemNames = favorite.ItemNames.Concat(favorite.AiItemNames).Distinct(StringComparer.Ordinal).Take(4).ToArray() };
}
