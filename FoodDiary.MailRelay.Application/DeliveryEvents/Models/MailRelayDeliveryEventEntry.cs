namespace FoodDiary.MailRelay.Application.DeliveryEvents.Models;

public sealed record MailRelayDeliveryEventEntry(
    Guid Id,
    string EventType,
    string Email,
    string Source,
    string? Classification,
    string? ProviderMessageId,
    string? Reason,
    DateTimeOffset OccurredAtUtc,
    DateTimeOffset CreatedAtUtc);
