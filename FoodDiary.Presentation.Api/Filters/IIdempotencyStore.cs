namespace FoodDiary.Presentation.Api.Filters;

public interface IIdempotencyStore {
    Task<IdempotencyReservation> ReserveAsync(
        string key,
        string requestHash,
        TimeSpan responseTtl,
        TimeSpan processingTtl,
        CancellationToken cancellationToken = default);

    Task CompleteAsync(
        string key,
        string requestHash,
        int statusCode,
        string? body,
        TimeSpan responseTtl,
        CancellationToken cancellationToken = default);
}
