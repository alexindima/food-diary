namespace FoodDiary.Modules.Meals.Application.Common;

public record MealAiSessionInput(
    Guid? ImageAssetId,
    string? Source,
    DateTime? RecognizedAtUtc,
    string? Notes,
    IReadOnlyList<MealAiItemInput> Items);
