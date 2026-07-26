namespace FoodDiary.Infrastructure.Persistence.Outbox;

public interface IOutboxMessage {
    DateTime CreatedOnUtc { get; }
    int AttemptCount { get; }
    DateTime? ProcessedOnUtc { get; }
    DateTime? DeadLetteredOnUtc { get; }

    void MarkClaimed(DateTime lockedUntilUtc, string lockedBy);
    void MarkProcessed(DateTime processedOnUtc);
    void MarkDeadLettered(string error, DateTime deadLetteredOnUtc);
    void MarkFailed(string error, DateTime nextAttemptOnUtc);
    void MarkReplayed(DateTime nextAttemptOnUtc);
}
