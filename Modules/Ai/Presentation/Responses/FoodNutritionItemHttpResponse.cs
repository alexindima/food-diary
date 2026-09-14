namespace FoodDiary.Modules.Ai.Presentation.Responses;

public sealed record FoodNutritionItemHttpResponse(
    string Name,
    decimal Amount,
    string Unit,
    decimal Calories,
    decimal Protein,
    decimal Fat,
    decimal Carbs,
    decimal Fiber,
    decimal Alcohol);
