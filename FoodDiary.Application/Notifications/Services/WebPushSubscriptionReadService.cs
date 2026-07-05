using FoodDiary.Application.Abstractions.Notifications.Common;
using FoodDiary.Application.Notifications.Common;
using FoodDiary.Application.Notifications.Mappings;
using FoodDiary.Application.Notifications.Models;
using FoodDiary.Domain.Entities.Notifications;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Notifications.Services;

internal sealed class WebPushSubscriptionReadService(
    IWebPushSubscriptionReadRepository webPushSubscriptionRepository) : IWebPushSubscriptionReadService {
    public async Task<IReadOnlyList<WebPushSubscriptionModel>> GetActiveSubscriptionsAsync(
        UserId userId,
        DateTime utcNow,
        CancellationToken cancellationToken) {
        IReadOnlyList<WebPushSubscription> subscriptions = await webPushSubscriptionRepository
            .GetByUserAsync(userId, cancellationToken)
            .ConfigureAwait(false);

        return [.. subscriptions
            .Where(subscription => subscription.ExpirationTimeUtc > utcNow)
            .Select(subscription => subscription.ToModel())];
    }
}
