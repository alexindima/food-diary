namespace FoodDiary.Presentation.Api.Features.Consumptions.Requests;

public sealed record ConsumptionAiSessionHttpRequest(
    Guid? ImageAssetId,
    string? Source,
    DateTime? RecognizedAtUtc,
    string? Notes,
    IReadOnlyList<ConsumptionAiItemHttpRequest> Items);
