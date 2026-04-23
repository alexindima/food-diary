namespace FoodDiary.MailRelay.Services;

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
    int MaxAttempts);
