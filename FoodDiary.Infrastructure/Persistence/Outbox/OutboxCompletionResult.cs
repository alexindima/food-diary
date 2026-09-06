namespace FoodDiary.Infrastructure.Persistence.Outbox;

public enum OutboxCompletionResult {
    Processed = 0,
    Requeued = 1,
    ClaimLost = 2,
}
