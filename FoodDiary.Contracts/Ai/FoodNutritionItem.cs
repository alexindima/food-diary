namespace FoodDiary.Contracts.Ai;

public sealed record FoodNutritionItem(
    string Name,
    decimal Amount,
    string Unit,
    decimal Calories,
    decimal Protein,
    decimal Fat,
    decimal Carbs,
    decimal Fiber,
    decimal Alcohol);
