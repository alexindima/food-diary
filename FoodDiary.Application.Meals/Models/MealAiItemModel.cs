using System.Diagnostics.CodeAnalysis;

namespace FoodDiary.Application.Meals.Models;

[ExcludeFromCodeCoverage]
public sealed record MealAiItemModel(
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
    double Confidence = 1,
    string Resolution = "Accepted");
