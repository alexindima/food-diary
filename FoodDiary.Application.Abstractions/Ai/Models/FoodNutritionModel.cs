namespace FoodDiary.Application.Ai.Models;

public sealed record FoodNutritionModel(
    decimal Calories,
    decimal Protein,
    decimal Fat,
    decimal Carbs,
    decimal Fiber,
    decimal Alcohol,
    IReadOnlyList<FoodNutritionItemModel> Items,
    string? Notes = null);
