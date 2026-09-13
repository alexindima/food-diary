namespace FoodDiary.Application.Abstractions.Ai.Models;

public sealed record FoodRecognitionJobModel(
    Guid Id,
    Guid UserId,
    Guid ImageAssetId,
    string ImageUrl,
    string? Description,
    string Status,
    DateTime CreatedOnUtc,
    DateTime UpdatedOnUtc,
    FoodVisionModel? Vision = null,
    FoodNutritionModel? Nutrition = null,
    string? ErrorCode = null,
    string? NutritionErrorCode = null);
