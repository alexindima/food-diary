namespace FoodDiary.Application.Abstractions.Meals.Models;

public sealed record MealProductNutritionReadModel(
    double Amount,
    double ProductBaseAmount,
    int? UsdaFdcId);
