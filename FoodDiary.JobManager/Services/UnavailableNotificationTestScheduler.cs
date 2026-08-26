using FoodDiary.Application.Abstractions.Notifications.Common;
using FoodDiary.Results;

namespace FoodDiary.JobManager.Services;

internal sealed class UnavailableNotificationTestScheduler : INotificationTestScheduler {
    private static readonly Error UnavailableError = new(
        "NotificationTestScheduler.Unavailable",
        "Test notification scheduling is available only in the API host.",
        ErrorKind.Internal);

    public Task<Result<ScheduledNotificationData>> ScheduleAsync(
        Guid userId,
        int delaySeconds,
        string type,
        CancellationToken cancellationToken) =>
        Task.FromResult(Result.Failure<ScheduledNotificationData>(UnavailableError));
}
