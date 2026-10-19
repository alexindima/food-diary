using FoodDiary.Modules.Notifications.Application.Abstractions.Common;
using FoodDiary.Modules.Notifications.Application.Abstractions.Models;
using FoodDiary.Modules.Users.Contracts.Common;
using FoodDiary.Modules.Users.Contracts.Models;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;

namespace FoodDiary.Modules.Notifications.Application.Services;

internal sealed class ProfileNotificationReadService(
    IWebPushSubscriptionReadModelRepository webPushSubscriptionRepository,
    TimeProvider dateTimeProvider)
    : IProfileNotificationReadService {
    public async Task<IReadOnlyList<ProfileWebPushSubscriptionModel>> GetWebPushSubscriptionsAsync(
        UserId userId,
        CancellationToken cancellationToken) {
        DateTime utcNow = dateTimeProvider.GetUtcNow().UtcDateTime;
        IReadOnlyList<WebPushSubscriptionReadModel> subscriptions = await webPushSubscriptionRepository
            .GetByUserReadModelsAsync(userId, cancellationToken)
            .ConfigureAwait(false);

        return [.. subscriptions
            .Where(subscription => subscription.ExpirationTimeUtc is null || subscription.ExpirationTimeUtc > utcNow)
            .Select(static subscription => new ProfileWebPushSubscriptionModel(
                subscription.Endpoint,
                new Uri(subscription.Endpoint, UriKind.Absolute).Host,
                subscription.ExpirationTimeUtc,
                subscription.Locale,
                subscription.UserAgent,
                subscription.CreatedAtUtc,
                subscription.UpdatedAtUtc))];
    }
}
