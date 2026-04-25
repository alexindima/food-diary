namespace FoodDiary.MailRelay.Application.DeliveryEvents.Models;

public sealed record IngestMailEventRequest(
    string EventType,
    string Email,
    string Source,
    string? Classification = null,
    string? ProviderMessageId = null,
    string? Reason = null,
    DateTimeOffset? OccurredAtUtc = null);
