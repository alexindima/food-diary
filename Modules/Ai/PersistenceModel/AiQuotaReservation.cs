using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;

namespace FoodDiary.Modules.Ai.PersistenceModel;

internal sealed class AiQuotaReservation {
    public string RequestId { get; private set; } = string.Empty;
    public UserId UserId { get; private set; }
    public DateTime PeriodStartUtc { get; private set; }
    public string Operation { get; private set; } = string.Empty;
    public long ReservedInputTokens { get; private set; }
    public long ReservedOutputTokens { get; private set; }
    public long? ActualInputTokens { get; private set; }
    public long? ActualOutputTokens { get; private set; }
    public AiQuotaReservationState State { get; private set; }
    public DateTime ExpiresOnUtc { get; private set; }
    public DateTime CreatedOnUtc { get; private set; }
    public DateTime UpdatedOnUtc { get; private set; }

    private AiQuotaReservation() {
    }

    public static AiQuotaReservation Create(string requestId, UserId userId, DateTime periodStartUtc, string operation,
        long inputTokens, long outputTokens, DateTime expiresOnUtc, DateTime nowUtc) => new() {
            RequestId = requestId,
            UserId = userId,
            PeriodStartUtc = periodStartUtc,
            Operation = operation,
            ReservedInputTokens = inputTokens,
            ReservedOutputTokens = outputTokens,
            State = AiQuotaReservationState.Pending,
            ExpiresOnUtc = expiresOnUtc,
            CreatedOnUtc = nowUtc,
            UpdatedOnUtc = nowUtc,
        };

    public bool BelongsTo(UserId userId, DateTime periodStartUtc, string operation) =>
        UserId == userId &&
        PeriodStartUtc == periodStartUtc &&
        string.Equals(Operation, operation, StringComparison.Ordinal);

    public void Reacquire(long inputTokens, long outputTokens, DateTime expiresOnUtc, DateTime nowUtc) {
        ReservedInputTokens = inputTokens;
        ReservedOutputTokens = outputTokens;
        ActualInputTokens = null;
        ActualOutputTokens = null;
        State = AiQuotaReservationState.Pending;
        ExpiresOnUtc = expiresOnUtc;
        UpdatedOnUtc = nowUtc;
    }

    public void Complete(long inputTokens, long outputTokens, DateTime nowUtc) {
        ActualInputTokens = inputTokens;
        ActualOutputTokens = outputTokens;
        State = AiQuotaReservationState.Completed;
        UpdatedOnUtc = nowUtc;
    }

    public void Release(DateTime nowUtc) {
        State = AiQuotaReservationState.Released;
        UpdatedOnUtc = nowUtc;
    }

    public void MarkOrphaned(DateTime nowUtc) {
        State = AiQuotaReservationState.Orphaned;
        UpdatedOnUtc = nowUtc;
    }
}
