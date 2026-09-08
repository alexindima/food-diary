namespace FoodDiary.Presentation.Api.Features.Admin.Responses;

public sealed record AdminOutgoingEmailHttpResponse(
    Guid Id, string Status, string Purpose, string FromAddress, IReadOnlyList<string> To,
    string Subject, DateTimeOffset CreatedAtUtc, DateTimeOffset? SentAtUtc,
    int AttemptCount, int MaxAttempts, string? CorrelationId, string? TextBody,
    bool ContentHidden, string? ReplyTo, string? InReplyTo);

