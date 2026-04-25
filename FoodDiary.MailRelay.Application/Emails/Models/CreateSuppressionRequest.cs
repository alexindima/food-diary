namespace FoodDiary.MailRelay.Application.Emails.Models;

public sealed record CreateSuppressionRequest(
    string Email,
    string Reason,
    string Source,
    DateTimeOffset? ExpiresAtUtc = null);
