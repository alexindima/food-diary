namespace FoodDiary.Presentation.Api.Features.Ai.Responses;

public sealed record FoodNutritionHttpResponse(
    decimal Calories,
    decimal Protein,
    decimal Fat,
    decimal Carbs,
    decimal Fiber,
    decimal Alcohol,
    IReadOnlyList<FoodNutritionItemHttpResponse> Items,
    string? Notes = null);
