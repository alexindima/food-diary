namespace FoodDiary.Application.Consumptions.Common;

public record ConsumptionAiSessionInput(
    Guid? ImageAssetId,
    string? Source,
    DateTime? RecognizedAtUtc,
    string? Notes,
    IReadOnlyList<ConsumptionAiItemInput> Items);
