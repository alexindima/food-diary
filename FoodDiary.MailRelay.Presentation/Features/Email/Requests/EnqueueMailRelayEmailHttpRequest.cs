namespace FoodDiary.MailRelay.Presentation.Features.Email.Requests;

public sealed record EnqueueMailRelayEmailHttpRequest(
    string FromAddress,
    string FromName,
    IReadOnlyList<string> To,
    string Subject,
    string HtmlBody,
    string? TextBody,
    string? CorrelationId = null,
    string? IdempotencyKey = null);
