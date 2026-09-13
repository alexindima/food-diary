namespace FoodDiary.Application.Abstractions.Meals.Models;

public sealed record MealRecipeSourceReadModel(
    string Name,
    string? ImageUrl,
    int Servings,
    double? TotalCalories,
    double? TotalProteins,
    double? TotalFats,
    double? TotalCarbs,
    double? TotalFiber,
    double? TotalAlcohol);
