namespace FoodDiary.Modules.Ai.Presentation.Requests;

public sealed record StartFoodRecognitionHttpRequest(Guid Id, Guid ImageAssetId, string? Description = null);
