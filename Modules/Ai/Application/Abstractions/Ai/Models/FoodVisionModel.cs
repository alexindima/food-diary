namespace FoodDiary.Application.Abstractions.Ai.Models;

public sealed record FoodVisionModel(
    IReadOnlyList<FoodVisionItemModel> Items,
    string? Notes = null);
