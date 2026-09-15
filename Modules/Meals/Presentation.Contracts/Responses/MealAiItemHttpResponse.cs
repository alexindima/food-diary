namespace FoodDiary.Modules.Meals.Presentation.Contracts.Responses;

public sealed record MealAiItemHttpResponse(
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
