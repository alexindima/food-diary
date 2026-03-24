namespace FoodDiary.Presentation.Api.Features.Ai.Requests;

public sealed record FoodVisionHttpRequest(Guid ImageAssetId, string? Description);
