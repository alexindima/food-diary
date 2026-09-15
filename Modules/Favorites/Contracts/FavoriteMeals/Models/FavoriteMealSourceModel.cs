namespace FoodDiary.Modules.Favorites.Contracts.FavoriteMeals.Models;

public sealed record FavoriteMealSourceModel(
    DateTime Date,
    string? MealType,
    double TotalCalories,
    double TotalProteins,
    double TotalFats,
    double TotalCarbs,
    int ItemCount);
