namespace FoodDiary.MailRelay.Domain.Emails;

public sealed record QueuedEmailMessage(
    Guid Id,
    string FromAddress,
    string FromName,
    IReadOnlyList<string> To,
    string Subject,
    string HtmlBody,
    string? TextBody,
    string? CorrelationId,
    int AttemptCount,
    int MaxAttempts,
    DateTimeOffset? CreatedAtUtc = null,
    DateTimeOffset? ModifiedAtUtc = null,
    string Purpose = "other",
    string? ReplyTo = null,
    string? InReplyTo = null,
    bool AutoSubmitted = false);
