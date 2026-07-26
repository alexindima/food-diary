namespace FoodDiary.Infrastructure.Persistence.Outbox;

internal sealed class OutboxReplayAudit {
    public Guid Id { get; private set; }
    public string OutboxName { get; private set; } = string.Empty;
    public Guid MessageId { get; private set; }
    public string RequestedBy { get; private set; } = string.Empty;
    public string Reason { get; private set; } = string.Empty;
    public DateTime RequestedOnUtc { get; private set; }
    public int PreviousAttemptCount { get; private set; }
    public string? PreviousError { get; private set; }

    private OutboxReplayAudit() {
    }

    public static OutboxReplayAudit Create(
        string outboxName,
        Guid messageId,
        string requestedBy,
        string reason,
        DateTime requestedOnUtc,
        int previousAttemptCount,
        string? previousError) =>
        new() {
            Id = Guid.NewGuid(),
            OutboxName = outboxName,
            MessageId = messageId,
            RequestedBy = requestedBy.Trim(),
            Reason = reason.Trim(),
            RequestedOnUtc = requestedOnUtc,
            PreviousAttemptCount = previousAttemptCount,
            PreviousError = previousError,
        };
}
