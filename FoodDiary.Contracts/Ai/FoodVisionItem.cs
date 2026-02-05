namespace FoodDiary.Contracts.Ai;

public sealed record FoodVisionItem(
    string NameEn,
    string? NameLocal,
    decimal Amount,
    string Unit,
    decimal Confidence);
