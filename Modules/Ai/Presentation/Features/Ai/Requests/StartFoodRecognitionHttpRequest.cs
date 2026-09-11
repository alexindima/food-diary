namespace FoodDiary.Presentation.Api.Features.Ai.Requests;

public sealed record StartFoodRecognitionHttpRequest(Guid Id, Guid ImageAssetId, string? Description = null);
