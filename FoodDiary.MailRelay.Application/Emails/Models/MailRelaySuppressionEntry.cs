namespace FoodDiary.MailRelay.Application.Emails.Models;

public sealed record MailRelaySuppressionEntry(
    string Email,
    string Reason,
    string Source,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc,
    DateTimeOffset? ExpiresAtUtc);
