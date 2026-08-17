namespace FoodDiary.Infrastructure.Persistence.Outbox;

internal sealed record OutboxClaimBatch<TMessage>(
    IReadOnlyList<TMessage> Messages,
    int ReclaimedCount);
