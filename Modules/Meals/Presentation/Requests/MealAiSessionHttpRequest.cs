namespace FoodDiary.Modules.Meals.Presentation.Requests;

public sealed record MealAiSessionHttpRequest(
    Guid? ImageAssetId,
    string? Source,
    DateTime? RecognizedAtUtc,
    string? Notes,
    IReadOnlyList<MealAiItemHttpRequest> Items);
