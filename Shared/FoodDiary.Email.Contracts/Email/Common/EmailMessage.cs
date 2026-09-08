namespace FoodDiary.Application.Abstractions.Email.Common;

public sealed record EmailMessage(
    string FromAddress,
    string FromName,
    IReadOnlyList<string> ToAddresses,
    string Subject,
    string HtmlBody,
    string? TextBody,
    string? IdempotencyKey = null,
    string Purpose = "other",
    string? ReplyTo = null,
    string? InReplyTo = null,
    bool AutoSubmitted = false,
    string? CorrelationId = null);
