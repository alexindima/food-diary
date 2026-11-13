namespace FoodDiary.Presentation.Api.Filters;

public union IdempotencyReservation(
    IdempotencyReservation.Acquired,
    IdempotencyReservation.Replay,
    IdempotencyReservation.Conflict,
    IdempotencyReservation.InProgress,
    IdempotencyReservation.CapacityExceeded) {
    public sealed record Acquired(string Token);
    public sealed record Replay(int? StatusCode, string? Body, string? Location);
    public sealed class Conflict;
    public sealed class InProgress;
    public sealed class CapacityExceeded;

    public IdempotencyReservationStatus Status => this switch {
        Acquired => IdempotencyReservationStatus.Acquired,
        Replay => IdempotencyReservationStatus.Replay,
        Conflict => IdempotencyReservationStatus.Conflict,
        InProgress => IdempotencyReservationStatus.InProgress,
        CapacityExceeded => IdempotencyReservationStatus.CapacityExceeded,
        null => throw new InvalidOperationException("An idempotency reservation must have an outcome."),
    };

    public int? StatusCode => this is Replay replay ? replay.StatusCode : null;
    public string? Body => this is Replay replay ? replay.Body : null;
    public string? Location => this is Replay replay ? replay.Location : null;
    public string? OwnerToken => this is Acquired acquired ? acquired.Token : null;
}
