namespace FoodDiary.Presentation.Api.Filters;

public sealed record IdempotencyReservation(
    IdempotencyReservationStatus Status,
    int? StatusCode = null,
    string? Body = null);
