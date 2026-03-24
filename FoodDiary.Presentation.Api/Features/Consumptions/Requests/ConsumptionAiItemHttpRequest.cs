namespace FoodDiary.Presentation.Api.Features.Consumptions.Requests;

public sealed record ConsumptionAiItemHttpRequest(
    string NameEn,
    string? NameLocal,
    double Amount,
    string Unit,
    double Calories,
    double Proteins,
    double Fats,
    double Carbs,
    double Fiber,
    double Alcohol);
