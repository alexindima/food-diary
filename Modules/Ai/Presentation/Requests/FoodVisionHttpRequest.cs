namespace FoodDiary.Modules.Ai.Presentation.Requests;

public sealed record FoodVisionHttpRequest(Guid ImageAssetId, string? Description);
