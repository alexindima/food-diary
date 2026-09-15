using FoodDiary.Results;

namespace FoodDiary.Modules.Notifications.Application.Abstractions.Common;

public interface INotificationTestScheduler {
    Task<Result<ScheduledNotificationData>> ScheduleAsync(
        Guid userId,
        int delaySeconds,
        string type,
        CancellationToken cancellationToken);
}
