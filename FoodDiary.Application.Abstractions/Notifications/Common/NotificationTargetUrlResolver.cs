namespace FoodDiary.Application.Notifications.Common;

public static class NotificationTargetUrlResolver {
    public static string? Resolve(string notificationType) {
        return notificationType switch {
            NotificationTypes.FastingCheckInReminder => "/fasting?intent=check-in",
            NotificationTypes.FastingCompleted => "/fasting?intent=session-complete",
            NotificationTypes.FastingWindowStarted => "/fasting?intent=fasting-window",
            NotificationTypes.EatingWindowStarted => "/fasting?intent=eating-window",
            _ => null
        };
    }
}
