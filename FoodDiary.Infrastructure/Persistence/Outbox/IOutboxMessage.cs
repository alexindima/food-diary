namespace FoodDiary.Infrastructure.Persistence.Outbox;

public interface IOutboxMessage {
    void MarkClaimed(DateTime lockedUntilUtc, string lockedBy);
}