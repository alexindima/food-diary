namespace FoodDiary.Application.Consumptions.Models;

public sealed record ConsumptionAiSessionModel(
    Guid Id,
    Guid ConsumptionId,
    Guid? ImageAssetId,
    string? ImageUrl,
    string Source,
    string Status,
    DateTime RecognizedAtUtc,
    string? Notes,
    IReadOnlyList<ConsumptionAiItemModel> Items) {
    public ConsumptionAiSessionModel(
        Guid id,
        Guid consumptionId,
        Guid? imageAssetId,
        string? imageUrl,
        string source,
        DateTime recognizedAtUtc,
        string? notes,
        IReadOnlyList<ConsumptionAiItemModel> items)
        : this(id, consumptionId, imageAssetId, imageUrl, source, "Reviewed", recognizedAtUtc, notes, items) {
    }
}
