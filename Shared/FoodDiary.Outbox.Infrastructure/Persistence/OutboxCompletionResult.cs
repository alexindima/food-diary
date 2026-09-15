namespace FoodDiary.Outbox.Infrastructure.Persistence;

public enum OutboxCompletionResult {
    Processed = 0,
    Requeued = 1,
    ClaimLost = 2,
}
