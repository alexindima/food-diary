namespace FoodDiary.Application.Meals.Common;

public record MealAiSessionInput(
    Guid? ImageAssetId,
    string? Source,
    DateTime? RecognizedAtUtc,
    string? Notes,
    IReadOnlyList<MealAiItemInput> Items);
