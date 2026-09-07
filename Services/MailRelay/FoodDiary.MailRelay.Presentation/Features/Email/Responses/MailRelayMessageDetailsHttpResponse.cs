namespace FoodDiary.MailRelay.Presentation.Features.Email.Responses;

public sealed record MailRelayMessageDetailsHttpResponse(
    Guid Id,
    string Status,
    string Subject,
    string? CorrelationId,
    int AttemptCount,
    int MaxAttempts,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset AvailableAtUtc,
    DateTimeOffset? LockedAtUtc,
    DateTimeOffset? SentAtUtc,
    string? LastError,
    IReadOnlyList<string>? SuppressedRecipients);
