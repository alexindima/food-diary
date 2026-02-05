namespace FoodDiary.Contracts.Ai;

public sealed record FoodNutritionResponse(
    decimal Calories,
    decimal Protein,
    decimal Fat,
    decimal Carbs,
    decimal Fiber,
    decimal Alcohol,
    IReadOnlyList<FoodNutritionItem> Items,
    string? Notes = null);
