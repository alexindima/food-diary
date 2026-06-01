namespace FoodDiary.Application.Consumptions.Common;

public record ConsumptionAiItemInput(
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
