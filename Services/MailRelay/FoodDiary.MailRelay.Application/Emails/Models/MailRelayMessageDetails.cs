namespace FoodDiary.MailRelay.Application.Emails.Models;

public sealed record MailRelayMessageDetails(
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
