namespace FoodDiary.Presentation.Api.Features.Consumptions.Responses;

public sealed record ConsumptionAiItemHttpResponse(
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
    double Alcohol);
