using FoodDiary.Results;

namespace FoodDiary.Application.Abstractions.Notifications.Common;

public static class NotificationErrors {
    public static Error TestScheduleCapacityExceeded() => new(
        "Notifications.TestScheduleCapacityExceeded",
        "Too many test notifications are already scheduled. Try again later.",
        Kind: ErrorKind.RateLimited);
}
