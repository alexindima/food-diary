namespace FoodDiary.Application.Abstractions.Ai.Common;

public interface IAiQuotaRepository {
    Task<AiQuotaReservationStatus> ReserveAsync(
        AiQuotaReservationRequest request,
        CancellationToken cancellationToken = default);

    Task ReconcileAsync(
        string requestId,
        AiQuotaUsage usage,
        CancellationToken cancellationToken = default);

    Task ReleaseAsync(string requestId, CancellationToken cancellationToken = default);
}
