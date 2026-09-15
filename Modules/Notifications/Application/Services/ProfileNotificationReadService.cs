using FoodDiary.Modules.Notifications.Application.Abstractions.Common;
using FoodDiary.Modules.Notifications.Application.Abstractions.Models;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Application.Abstractions.Users.Models;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Modules.Notifications.Application.Services;

internal sealed class ProfileNotificationReadService(
    IWebPushSubscriptionReadModelRepository webPushSubscriptionRepository)
    : IProfileNotificationReadService {
    public async Task<IReadOnlyList<ProfileWebPushSubscriptionModel>> GetWebPushSubscriptionsAsync(
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
