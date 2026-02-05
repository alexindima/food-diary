namespace FoodDiary.Contracts.Ai;

public sealed record FoodVisionResponse(
    IReadOnlyList<FoodVisionItem> Items,
    string? Notes = null);
