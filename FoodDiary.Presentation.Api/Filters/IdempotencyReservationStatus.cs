namespace FoodDiary.Presentation.Api.Filters;

public enum IdempotencyReservationStatus {
    Acquired = 0,
    Replay = 1,
    Conflict = 2,
    InProgress = 3,
}
