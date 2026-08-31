namespace FoodDiary.Infrastructure.Persistence.Ai;

internal enum AiQuotaReservationState {
    Pending = 0,
    Completed = 1,
    Released = 2,
    Orphaned = 3,
}
