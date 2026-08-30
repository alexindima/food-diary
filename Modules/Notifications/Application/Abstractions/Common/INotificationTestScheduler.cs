using FoodDiary.Results;

namespace FoodDiary.Application.Abstractions.Notifications.Common;

public interface INotificationTestScheduler {
    Task<Result<ScheduledNotificationData>> ScheduleAsync(
        Guid userId,
        int delaySeconds,
        string type,
        CancellationToken cancellationToken);
}
