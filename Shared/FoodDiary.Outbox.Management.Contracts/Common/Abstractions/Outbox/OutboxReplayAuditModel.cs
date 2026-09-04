namespace FoodDiary.Application.Abstractions.Common.Abstractions.Outbox;

public sealed record OutboxReplayAuditModel(
    Guid Id,
    string OutboxName,
    Guid MessageId,
    string RequestedBy,
    string Reason,
    DateTime RequestedOnUtc,
    int PreviousAttemptCount,
    string? PreviousError);
