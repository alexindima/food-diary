namespace FoodDiary.Application.Abstractions.Dashboard.Models;

public sealed record DashboardMealAiItemReadModel(
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
    string Resolution);
