using FoodDiary.Modules.Meals.Domain.Contracts.Enums;

namespace FoodDiary.Modules.Meals.Contracts.Models;

public sealed record MealAiItemProjectionReadModel(
    Guid Id,
    Guid SessionId,
    string NameEn,
    string? NameLocal,
    double Amount,
    string Unit,
    double Calories,
    double Proteins,
    double Fats,
    double Carbs,
    double Fiber,
    double Alcohol,
    double Confidence,
    MealAiItemResolution Resolution);
