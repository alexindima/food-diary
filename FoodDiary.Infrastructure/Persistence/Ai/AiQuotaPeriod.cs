using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Infrastructure.Persistence.Ai;

internal sealed class AiQuotaPeriod {
    public UserId UserId { get; }
    public DateTime PeriodStartUtc { get; }
    public long ConsumedInputTokens { get; private set; }
    public long ConsumedOutputTokens { get; private set; }
    public long ReservedInputTokens { get; private set; }
    public long ReservedOutputTokens { get; private set; }
    public DateTime UpdatedOnUtc { get; private set; }

    private AiQuotaPeriod() {
    }

    public bool CanReserve(long inputTokens, long outputTokens, long inputLimit, long outputLimit) =>
        checked(ConsumedInputTokens + ReservedInputTokens + inputTokens) <= inputLimit &&
        checked(ConsumedOutputTokens + ReservedOutputTokens + outputTokens) <= outputLimit;

    public void Reserve(long inputTokens, long outputTokens, DateTime nowUtc) {
        ReservedInputTokens = checked(ReservedInputTokens + inputTokens);
        ReservedOutputTokens = checked(ReservedOutputTokens + outputTokens);
        UpdatedOnUtc = nowUtc;
    }

    public void Release(long inputTokens, long outputTokens, DateTime nowUtc) {
        ReservedInputTokens = checked(ReservedInputTokens - inputTokens);
        ReservedOutputTokens = checked(ReservedOutputTokens - outputTokens);
        UpdatedOnUtc = nowUtc;
    }

    public void ConsumeReserved(
        long reservedInputTokens,
        long reservedOutputTokens,
        long consumedInputTokens,
        long consumedOutputTokens,
        DateTime nowUtc) {
        ReservedInputTokens = checked(ReservedInputTokens - reservedInputTokens);
        ReservedOutputTokens = checked(ReservedOutputTokens - reservedOutputTokens);
        ConsumedInputTokens = checked(ConsumedInputTokens + consumedInputTokens);
        ConsumedOutputTokens = checked(ConsumedOutputTokens + consumedOutputTokens);
        UpdatedOnUtc = nowUtc;
    }

    public void ReconcileOrphan(
        long reservedInputTokens,
        long reservedOutputTokens,
        long actualInputTokens,
        long actualOutputTokens,
        DateTime nowUtc) {
        ConsumedInputTokens = checked(ConsumedInputTokens + actualInputTokens - reservedInputTokens);
        ConsumedOutputTokens = checked(ConsumedOutputTokens + actualOutputTokens - reservedOutputTokens);
        UpdatedOnUtc = nowUtc;
    }
}
