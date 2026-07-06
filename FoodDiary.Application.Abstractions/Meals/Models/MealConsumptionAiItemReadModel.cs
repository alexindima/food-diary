using FoodDiary.Domain.Enums;

namespace FoodDiary.Application.Abstractions.Meals.Models;

public sealed record MealConsumptionAiItemReadModel(
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
