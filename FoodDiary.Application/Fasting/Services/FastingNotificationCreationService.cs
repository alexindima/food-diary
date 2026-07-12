using FoodDiary.Application.Abstractions.Notifications.Common;
using FoodDiary.Domain.Entities.Notifications;

namespace FoodDiary.Application.Fasting.Services;

internal static class FastingNotificationCreationService {
    public static async Task<bool> TryCreateAsync(
        FastingNotificationCandidate candidate,
        INotificationDeduplicationService notificationDeduplicationService,
        INotificationWriter notificationWriter,
        CancellationToken cancellationToken) {
        if (await notificationDeduplicationService.ExistsAsync(
                candidate.UserId,
                candidate.Type,
                candidate.ReferenceId,
                cancellationToken).ConfigureAwait(false)) {
            return false;
        }

        Notification notification = FastingNotificationFactory.Create(candidate);
        await notificationWriter.AddAsync(notification, sendWebPush: true, cancellationToken).ConfigureAwait(false);
        return true;
    }
}
