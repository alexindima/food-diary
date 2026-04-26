namespace FoodDiary.Application.Abstractions.Ai.Models;

public sealed record FoodVisionItemModel(
    string NameEn,
    string? NameLocal,
    decimal Amount,
    string Unit,
    decimal Confidence);
