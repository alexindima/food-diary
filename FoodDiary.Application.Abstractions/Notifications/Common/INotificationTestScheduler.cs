namespace FoodDiary.Application.Abstractions.Notifications.Common;

public interface INotificationTestScheduler {
    Task<ScheduledNotificationData> ScheduleAsync(
        Guid userId,
        int delaySeconds,
        string type,
        CancellationToken cancellationToken);
}
