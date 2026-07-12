using FoodDiary.Application.Abstractions.Notifications.Common;
using FoodDiary.Application.Abstractions.Notifications.Models;
using FoodDiary.Application.Notifications.Common;
using FoodDiary.Application.Notifications.Mappings;
using FoodDiary.Application.Notifications.Models;
using FoodDiary.Application.Users.Common;
using FoodDiary.Application.Users.Models;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Notifications.Services;

internal sealed class WebPushSubscriptionReadService(
    IWebPushSubscriptionReadModelRepository webPushSubscriptionRepository)
    : IWebPushSubscriptionReadService, IProfileNotificationReadService {
    public async Task<IReadOnlyList<WebPushSubscriptionModel>> GetSubscriptionsAsync(
        UserId userId,
        CancellationToken cancellationToken) {
        IReadOnlyList<WebPushSubscriptionReadModel> subscriptions = await webPushSubscriptionRepository
            .GetByUserReadModelsAsync(userId, cancellationToken)
            .ConfigureAwait(false);

        return [.. subscriptions.Select(subscription => subscription.ToModel())];
    }

    public async Task<IReadOnlyList<WebPushSubscriptionModel>> GetActiveSubscriptionsAsync(
        UserId userId,
        DateTime utcNow,
        CancellationToken cancellationToken) {
        IReadOnlyList<WebPushSubscriptionReadModel> subscriptions = await webPushSubscriptionRepository
            .GetByUserReadModelsAsync(userId, cancellationToken)
            .ConfigureAwait(false);

        return [.. subscriptions
            .Where(subscription => subscription.ExpirationTimeUtc > utcNow)
            .Select(subscription => subscription.ToModel())];
    }

    async Task<IReadOnlyList<ProfileWebPushSubscriptionModel>> IProfileNotificationReadService.GetWebPushSubscriptionsAsync(
        UserId userId,
        CancellationToken cancellationToken) {
        IReadOnlyList<WebPushSubscriptionReadModel> subscriptions = await webPushSubscriptionRepository
            .GetByUserReadModelsAsync(userId, cancellationToken)
            .ConfigureAwait(false);

        return [.. subscriptions.Select(static subscription => new ProfileWebPushSubscriptionModel(
            subscription.Endpoint,
            new Uri(subscription.Endpoint, UriKind.Absolute).Host,
            subscription.ExpirationTimeUtc,
            subscription.Locale,
            subscription.UserAgent,
            subscription.CreatedAtUtc,
            subscription.UpdatedAtUtc))];
    }
}
