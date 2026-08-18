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
        string ownerToken,
        int statusCode,
        string? body,
        string? location,
        TimeSpan responseTtl,
        CancellationToken cancellationToken = default);

    Task ReleaseAsync(
        string key,
        string requestHash,
        string ownerToken,
        CancellationToken cancellationToken = default);
}
