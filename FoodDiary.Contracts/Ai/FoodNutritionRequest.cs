namespace FoodDiary.Contracts.Ai;

public sealed record FoodNutritionRequest(IReadOnlyList<FoodVisionItem> Items);
