namespace FoodDiary.Presentation.Api.Features.Consumptions.Requests;

public sealed record ConsumptionAiSessionHttpRequest(
    Guid? ImageAssetId,
    DateTime? RecognizedAtUtc,
    string? Notes,
    IReadOnlyList<ConsumptionAiItemHttpRequest> Items);
