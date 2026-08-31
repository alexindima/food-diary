using FoodDiary.Application.Abstractions.Ai.Common;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Infrastructure.Persistence.Ai;

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

    public static AiQuotaReservation Create(AiQuotaReservationRequest request, DateTime nowUtc) => new() {
        RequestId = request.RequestId,
        UserId = request.UserId,
        PeriodStartUtc = request.PeriodStartUtc,
        Operation = request.Operation,
        ReservedInputTokens = request.InputTokens,
        ReservedOutputTokens = request.OutputTokens,
        State = AiQuotaReservationState.Pending,
        ExpiresOnUtc = request.ExpiresOnUtc,
        CreatedOnUtc = nowUtc,
        UpdatedOnUtc = nowUtc,
    };

    public bool BelongsTo(AiQuotaReservationRequest request) =>
        UserId == request.UserId &&
        PeriodStartUtc == request.PeriodStartUtc &&
        string.Equals(Operation, request.Operation, StringComparison.Ordinal);

    public void Reacquire(AiQuotaReservationRequest request, DateTime nowUtc) {
        ReservedInputTokens = request.InputTokens;
        ReservedOutputTokens = request.OutputTokens;
        ActualInputTokens = null;
        ActualOutputTokens = null;
        State = AiQuotaReservationState.Pending;
        ExpiresOnUtc = request.ExpiresOnUtc;
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
