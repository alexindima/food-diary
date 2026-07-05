using FoodDiary.Application.Abstractions.Notifications.Models;
using FoodDiary.Domain.Entities.Notifications;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Abstractions.Notifications.Common;

public interface IWebPushSubscriptionReadRepository {
    Task<WebPushSubscription?> GetByEndpointAsync(
        string endpoint,
        bool asTracking = false,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<WebPushSubscription>> GetByUserAsync(
        UserId userId,
        CancellationToken cancellationToken = default);

    async Task<IReadOnlyList<WebPushSubscriptionReadModel>> GetByUserReadModelsAsync(
        UserId userId,
        CancellationToken cancellationToken = default) {
        IReadOnlyList<WebPushSubscription> subscriptions = await GetByUserAsync(userId, cancellationToken).ConfigureAwait(false);
        return [.. subscriptions.Select(static subscription => new WebPushSubscriptionReadModel(
            subscription.Endpoint,
            subscription.ExpirationTimeUtc,
            subscription.Locale,
            subscription.UserAgent,
            subscription.CreatedOnUtc,
            subscription.ModifiedOnUtc))];
    }
}
