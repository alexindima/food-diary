using System.Diagnostics.CodeAnalysis;

namespace FoodDiary.Application.Consumptions.Models;

[ExcludeFromCodeCoverage]
public sealed record ConsumptionAiItemModel(
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
