namespace FoodDiary.MailRelay.Domain.Emails;

public sealed record MailRelaySuppressionEntry(
    string Email,
    string Reason,
    string Source,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc,
    DateTimeOffset? ExpiresAtUtc);
