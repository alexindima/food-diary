namespace FoodDiary.MailRelay.Domain.Emails;

public sealed record RelayEmailMessageRequest(
    string FromAddress,
    string FromName,
    IReadOnlyList<string> To,
    string Subject,
    string HtmlBody,
    string? TextBody,
    string? CorrelationId = null,
    string? IdempotencyKey = null,
    string? MessageId = null,
    string Purpose = "other",
    string? ReplyTo = null,
    string? InReplyTo = null,
    bool AutoSubmitted = false);
