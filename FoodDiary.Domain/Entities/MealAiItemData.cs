namespace FoodDiary.Domain.Entities;

public sealed record MealAiItemData(
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
