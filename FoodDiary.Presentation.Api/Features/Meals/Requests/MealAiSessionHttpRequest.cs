namespace FoodDiary.Presentation.Api.Features.Meals.Requests;

public sealed record MealAiSessionHttpRequest(
    Guid? ImageAssetId,
    string? Source,
    DateTime? RecognizedAtUtc,
    string? Notes,
    IReadOnlyList<MealAiItemHttpRequest> Items);
