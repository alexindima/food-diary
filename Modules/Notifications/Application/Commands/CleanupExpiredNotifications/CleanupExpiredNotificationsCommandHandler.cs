using FoodDiary.Modules.Notifications.Contracts.Common;
using FoodDiary.Mediator;
using FoodDiary.Modules.Notifications.Contracts.Commands.CleanupExpiredNotifications;
using FoodDiary.Application.Abstractions.Common.Abstractions.Persistence;
using FoodDiary.Modules.Notifications.Application.Abstractions.Common;

namespace FoodDiary.Modules.Notifications.Application.Commands.CleanupExpiredNotifications;

public sealed class CleanupExpiredNotificationsCommandHandler(
    INotificationWriteRepository notificationRepository,
    TimeProvider dateTimeProvider,
    IUnitOfWork unitOfWork) : IRequestHandler<CleanupExpiredNotificationsCommand, int> {
    public async Task<int> Handle(CleanupExpiredNotificationsCommand request, CancellationToken cancellationToken) {
        NotificationCleanupPolicy policy = request.Policy;
        if (policy.BatchSize <= 0) {
            return 0;
        }

        DateTime utcNow = dateTimeProvider.GetUtcNow().UtcDateTime;

        int deleted = await notificationRepository.DeleteExpiredBatchAsync(
            policy.TransientTypes,
            utcNow.AddDays(-Math.Max(policy.TransientReadRetentionDays, 1)),
            utcNow.AddDays(-Math.Max(policy.TransientUnreadRetentionDays, 1)),
            utcNow.AddDays(-Math.Max(policy.StandardReadRetentionDays, 1)),
            utcNow.AddDays(-Math.Max(policy.StandardUnreadRetentionDays, 1)),
            policy.BatchSize,
            cancellationToken).ConfigureAwait(false);

        if (deleted > 0) {
            await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        return deleted;
    }
}
