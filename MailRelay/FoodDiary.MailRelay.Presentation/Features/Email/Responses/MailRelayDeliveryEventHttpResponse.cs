namespace FoodDiary.MailRelay.Presentation.Features.Email.Responses;

public sealed record MailRelayDeliveryEventHttpResponse(
    Guid Id,
    string EventType,
    string Email,
    string Source,
    string? Classification,
    string? ProviderMessageId,
    string? Reason,
    DateTimeOffset OccurredAtUtc,
    DateTimeOffset CreatedAtUtc);
