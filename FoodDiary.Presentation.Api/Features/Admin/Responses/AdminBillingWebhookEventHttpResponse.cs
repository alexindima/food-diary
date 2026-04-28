namespace FoodDiary.Presentation.Api.Features.Admin.Responses;

public sealed record AdminBillingWebhookEventHttpResponse(
    Guid Id,
    string Provider,
    string EventId,
    string EventType,
    string? ExternalObjectId,
    string Status,
    DateTime ProcessedAtUtc,
    string? PayloadJson,
    string? ErrorMessage,
    DateTime CreatedOnUtc,
    DateTime? ModifiedOnUtc);
