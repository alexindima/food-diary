namespace FoodDiary.MailRelay.Services;

public sealed record MailRelaySuppressionEntry(
    string Email,
    string Reason,
    string Source,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc,
    DateTimeOffset? ExpiresAtUtc);
