namespace FoodDiary.Application.Consumptions.Models;

public sealed record ConsumptionAiSessionModel(
    Guid Id,
    Guid ConsumptionId,
    Guid? ImageAssetId,
    string? ImageUrl,
    string Source,
    DateTime RecognizedAtUtc,
    string? Notes,
    IReadOnlyList<ConsumptionAiItemModel> Items);
