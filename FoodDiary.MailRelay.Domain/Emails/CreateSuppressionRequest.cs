namespace FoodDiary.MailRelay.Domain.Emails;

public sealed record CreateSuppressionRequest(
    string Email,
    string Reason,
    string Source,
    DateTimeOffset? ExpiresAtUtc = null);
