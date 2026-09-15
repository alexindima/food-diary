using FoodDiary.Modules.Notifications.Application.Mappings;
using FoodDiary.Modules.Notifications.Application.Abstractions.Common;
using FoodDiary.Modules.Notifications.Application.Abstractions.Models;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Modules.Notifications.Application.Common;

using FoodDiary.Modules.Notifications.Application.Models;
using FoodDiary.Application.Abstractions.Users.Models;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Modules.Notifications.Application.Services;

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
