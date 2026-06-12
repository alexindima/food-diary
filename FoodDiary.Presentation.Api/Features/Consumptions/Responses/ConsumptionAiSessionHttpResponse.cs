namespace FoodDiary.Presentation.Api.Features.Consumptions.Responses;

public sealed record ConsumptionAiSessionHttpResponse(
    Guid Id,
    Guid ConsumptionId,
    Guid? ImageAssetId,
    string? ImageUrl,
    string Source,
    string Status,
    DateTime RecognizedAtUtc,
    string? Notes,
    IReadOnlyList<ConsumptionAiItemHttpResponse> Items);
