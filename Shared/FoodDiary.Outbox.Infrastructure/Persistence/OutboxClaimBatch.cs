namespace FoodDiary.Outbox.Infrastructure.Persistence;

internal sealed record OutboxClaimBatch<TMessage>(
    IReadOnlyList<TMessage> Messages,
    int ReclaimedCount);
