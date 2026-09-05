namespace FoodDiary.Presentation.Api.Features.Ai.Models;

public sealed record FoodVisionItemHttpModel(
    string NameEn,
    string? NameLocal,
    decimal Amount,
    string Unit,
    decimal Confidence,
    decimal? CenterX = null,
    decimal? CenterY = null,
    decimal? LocationConfidence = null);
