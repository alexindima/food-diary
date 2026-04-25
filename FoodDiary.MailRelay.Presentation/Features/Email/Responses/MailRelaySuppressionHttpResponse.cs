namespace FoodDiary.MailRelay.Presentation.Features.Email.Responses;

public sealed record MailRelaySuppressionHttpResponse(
    string Email,
    string Reason,
    string Source,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc,
    DateTimeOffset? ExpiresAtUtc);
