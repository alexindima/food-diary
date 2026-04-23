namespace FoodDiary.MailRelay.Services;

public sealed record RelayEmailMessageRequest(
    string FromAddress,
    string FromName,
    IReadOnlyList<string> To,
    string Subject,
    string HtmlBody,
    string? TextBody,
    string? CorrelationId = null,
    string? IdempotencyKey = null);
