namespace FoodDiary.Presentation.Api.Features.Ai.Responses;

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
