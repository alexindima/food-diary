namespace FoodDiary.Modules.Ai.Presentation.Responses;

public sealed record FoodNutritionHttpResponse(
    decimal Calories,
    decimal Protein,
    decimal Fat,
    decimal Carbs,
    decimal Fiber,
    decimal Alcohol,
    IReadOnlyList<FoodNutritionItemHttpResponse> Items,
    string? Notes = null);
