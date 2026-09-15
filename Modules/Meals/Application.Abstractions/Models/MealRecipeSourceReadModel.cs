namespace FoodDiary.Modules.Meals.Application.Abstractions.Models;

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
