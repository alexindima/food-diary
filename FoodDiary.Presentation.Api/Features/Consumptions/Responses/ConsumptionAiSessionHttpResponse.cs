namespace FoodDiary.Presentation.Api.Features.Consumptions.Responses;

public sealed record ConsumptionAiSessionHttpResponse(
    Guid Id,
    Guid ConsumptionId,
    Guid? ImageAssetId,
    string? ImageUrl,
    DateTime RecognizedAtUtc,
    string? Notes,
    IReadOnlyList<ConsumptionAiItemHttpResponse> Items);
