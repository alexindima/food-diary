namespace FoodDiary.MailRelay.Presentation.Features.Email.Requests;

public sealed record IngestMailRelayDeliveryEventHttpRequest(
    string EventType,
    string Email,
    string Source,
    string? Classification = null,
    string? ProviderMessageId = null,
    string? Reason = null,
    DateTimeOffset? OccurredAtUtc = null);
