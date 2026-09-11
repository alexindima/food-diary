namespace FoodDiary.Presentation.Api.Features.Ai.Responses;

public sealed record FoodRecognitionJobHttpResponse(
    Guid Id, Guid ImageAssetId, string ImageUrl, string? Description, string Status,
    DateTime CreatedOnUtc, DateTime UpdatedOnUtc,
    FoodVisionHttpResponse? Vision, FoodNutritionHttpResponse? Nutrition,
    string? ErrorCode, string? NutritionErrorCode);
