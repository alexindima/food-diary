namespace FoodDiary.MailRelay.Services;

public sealed record CreateSuppressionRequest(
    string Email,
    string Reason,
    string Source,
    DateTimeOffset? ExpiresAtUtc = null);
