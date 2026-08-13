namespace FoodDiary.Presentation.Api.Features.Meals.Responses;

public sealed record MealAiSessionHttpResponse(
    Guid Id,
    Guid MealId,
    Guid? ImageAssetId,
    string? ImageUrl,
    string Source,
    string Status,
    DateTime RecognizedAtUtc,
    string? Notes,
    IReadOnlyList<MealAiItemHttpResponse> Items);
