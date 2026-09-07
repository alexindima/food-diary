namespace FoodDiary.MailRelay.Client.Models;

public sealed record EnqueueMailRelayEmailRequest(
    string FromAddress,
    string FromName,
    IReadOnlyList<string> To,
    string Subject,
    string HtmlBody,
    string? TextBody,
    string? CorrelationId = null,
    string? IdempotencyKey = null);
