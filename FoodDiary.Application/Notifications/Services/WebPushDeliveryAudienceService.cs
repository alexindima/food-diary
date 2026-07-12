using FoodDiary.Application.Abstractions.Notifications.Common;
using FoodDiary.Application.Abstractions.Notifications.Models;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Domain.Entities.Notifications;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Notifications.Services;

public sealed class WebPushDeliveryAudienceService(
    IWebPushSubscriptionReadRepository subscriptionReadRepository,
    IWebPushSubscriptionWriteRepository subscriptionWriteRepository,
    IUserDirectoryService userDirectoryService) : IWebPushDeliveryAudienceService {
    public async Task<IReadOnlyList<WebPushDeliverySubscription>> GetActiveAudienceAsync(
        UserId userId,
        string notificationType,
        DateTime utcNow,
        CancellationToken cancellationToken) {
        User? user = await userDirectoryService.GetByIdAsync(userId, cancellationToken).ConfigureAwait(false);
        if (user?.PushNotificationsEnabled != true || !IsCategoryEnabled(user, notificationType)) {
            return [];
        }

        IReadOnlyList<WebPushSubscription> subscriptions = await subscriptionReadRepository
            .GetByUserAsync(userId, cancellationToken)
            .ConfigureAwait(false);
        WebPushSubscription[] expired = [.. subscriptions.Where(subscription => subscription.ExpirationTimeUtc <= utcNow)];
        if (expired.Length > 0) {
            await subscriptionWriteRepository.DeleteRangeAsync(expired, cancellationToken).ConfigureAwait(false);
        }

        return [.. subscriptions
            .Except(expired)
            .Select(static subscription => new WebPushDeliverySubscription(
                subscription.Id.Value,
                subscription.Endpoint,
                subscription.P256Dh,
                subscription.Auth,
                subscription.Locale))];
    }

    public async Task RemoveInvalidSubscriptionsAsync(
        UserId userId,
        IReadOnlyCollection<Guid> subscriptionIds,
        CancellationToken cancellationToken) {
        if (subscriptionIds.Count == 0) {
            return;
        }

        IReadOnlyList<WebPushSubscription> subscriptions = await subscriptionReadRepository
            .GetByUserAsync(userId, cancellationToken)
            .ConfigureAwait(false);
        HashSet<Guid> ids = [.. subscriptionIds];
        WebPushSubscription[] invalid = [.. subscriptions.Where(subscription => ids.Contains(subscription.Id.Value))];
        if (invalid.Length > 0) {
            await subscriptionWriteRepository.DeleteRangeAsync(invalid, cancellationToken).ConfigureAwait(false);
        }
    }

    internal static bool IsCategoryEnabled(User user, string notificationType) {
        return notificationType switch {
            NotificationTypes.FastingCompleted => user.FastingPushNotificationsEnabled,
            NotificationTypes.EatingWindowStarted => user.FastingPushNotificationsEnabled,
            NotificationTypes.FastingWindowStarted => user.FastingPushNotificationsEnabled,
            NotificationTypes.FastingCheckInReminder => user.FastingPushNotificationsEnabled,
            NotificationTypes.DietologistInvitationReceived => user.SocialPushNotificationsEnabled,
            NotificationTypes.DietologistInvitationAccepted => user.SocialPushNotificationsEnabled,
            NotificationTypes.DietologistInvitationDeclined => user.SocialPushNotificationsEnabled,
            NotificationTypes.NewRecommendation => user.SocialPushNotificationsEnabled,
            NotificationTypes.NewComment => user.SocialPushNotificationsEnabled,
            _ => true,
        };
    }
}
