namespace FoodDiary.MailRelay.Domain.DeliveryEvents;

public sealed record IngestMailEventRequest(
    string EventType,
    string Email,
    string Source,
    string? Classification = null,
    string? ProviderMessageId = null,
    string? Reason = null,
    DateTimeOffset? OccurredAtUtc = null);
