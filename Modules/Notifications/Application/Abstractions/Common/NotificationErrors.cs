using FoodDiary.Results;

namespace FoodDiary.Application.Abstractions.Notifications.Common;

public static class NotificationErrors {
    // Preserve the established notification HTTP error while removing the foreign feature dependency.
    public static Error LegacyNotFound => new("Dietologist.InvitationNotFound", "Dietologist invitation was not found.", Kind: ErrorKind.NotFound);

    public static Error TestScheduleCapacityExceeded() => new(
        "Notifications.TestScheduleCapacityExceeded",
        "Too many test notifications are already scheduled. Try again later.",
        Kind: ErrorKind.RateLimited);
}
