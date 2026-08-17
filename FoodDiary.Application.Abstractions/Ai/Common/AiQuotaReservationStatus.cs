namespace FoodDiary.Application.Abstractions.Ai.Common;

public enum AiQuotaReservationStatus {
    Acquired = 0,
    QuotaExceeded = 1,
    InProgress = 2,
    Duplicate = 3,
}
