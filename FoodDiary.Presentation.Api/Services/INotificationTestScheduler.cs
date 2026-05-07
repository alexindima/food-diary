using FoodDiary.Presentation.Api.Features.Notifications.Responses;

namespace FoodDiary.Presentation.Api.Services;

public interface INotificationTestScheduler {
    Task<ScheduledNotificationHttpResponse> ScheduleAsync(
        Guid userId,
        int delaySeconds,
        string type,
        CancellationToken cancellationToken);
}
