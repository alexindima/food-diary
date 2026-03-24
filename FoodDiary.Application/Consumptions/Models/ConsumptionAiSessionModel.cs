namespace FoodDiary.Application.Consumptions.Models;

public sealed record ConsumptionAiSessionModel(
    Guid Id,
    Guid ConsumptionId,
    Guid? ImageAssetId,
    string? ImageUrl,
    DateTime RecognizedAtUtc,
    string? Notes,
    IReadOnlyList<ConsumptionAiItemModel> Items);
