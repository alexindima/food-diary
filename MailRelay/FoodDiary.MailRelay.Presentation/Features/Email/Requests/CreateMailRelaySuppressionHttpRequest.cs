namespace FoodDiary.MailRelay.Presentation.Features.Email.Requests;

public sealed record CreateMailRelaySuppressionHttpRequest(
    string Email,
    string Reason,
    string Source,
    DateTimeOffset? ExpiresAtUtc = null);
