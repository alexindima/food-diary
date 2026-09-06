namespace FoodDiary.Application.Meals.Models;

public sealed record MealAiSessionModel(
    Guid Id,
    Guid MealId,
    Guid? ImageAssetId,
    string? ImageUrl,
    string Source,
    string Status,
    DateTime RecognizedAtUtc,
    string? Notes,
    IReadOnlyList<MealAiItemModel> Items) {
    public MealAiSessionModel(
        Guid id,
        Guid mealId,
        Guid? imageAssetId,
        string? imageUrl,
        string source,
        DateTime recognizedAtUtc,
        string? notes,
        IReadOnlyList<MealAiItemModel> items)
        : this(id, mealId, imageAssetId, imageUrl, source, "Reviewed", recognizedAtUtc, notes, items) {
    }
}
