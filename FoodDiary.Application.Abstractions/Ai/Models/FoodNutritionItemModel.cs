namespace FoodDiary.Application.Ai.Models;

public sealed record FoodNutritionItemModel(
    string Name,
    decimal Amount,
    string Unit,
    decimal Calories,
    decimal Protein,
    decimal Fat,
    decimal Carbs,
    decimal Fiber,
    decimal Alcohol);
